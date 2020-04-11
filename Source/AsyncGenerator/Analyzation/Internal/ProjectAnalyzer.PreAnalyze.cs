using System;
using System.Collections.Generic;
using System.Linq;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Extensions;
using AsyncGenerator.Core.Extensions.Internal;
using AsyncGenerator.Extensions;
using AsyncGenerator.Extensions.Internal;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Analyzation.Internal
{
	internal partial class ProjectAnalyzer
	{
		private AsyncCounterpartsSearchOptions _preAnalyzeSearchOptions;

		/// <summary>
		/// Set the method conversion to Ignore for all method data that are inside the given document and can not be
		/// converted due to the language limitations or an already existing async counterpart.
		/// </summary>
		/// <param name="documentData">The document data to be pre-analyzed</param>
		private void PreAnalyzeDocumentData(DocumentData documentData)
		{
			_preAnalyzeSearchOptions = AsyncCounterpartsSearchOptions.EqualParameters | AsyncCounterpartsSearchOptions.IgnoreReturnType;
			// When searhing for missing async member we have to search also for overloads with a cancellation token
			var searchWithTokens = _configuration.UseCancellationTokens || _configuration.CanScanForMissingAsyncMembers != null;
			if (searchWithTokens)
			{
				_preAnalyzeSearchOptions |= AsyncCounterpartsSearchOptions.HasCancellationToken;
			}
			if (_configuration.SearchAsyncCounterpartsInInheritedTypes)
			{
				_preAnalyzeSearchOptions |= AsyncCounterpartsSearchOptions.SearchInheritedTypes;
			}

			foreach (var typeNode in documentData.Node
				.DescendantNodes()
				.OfType<TypeDeclarationSyntax>())
			{
				var typeData = documentData.GetOrCreateTypeData(typeNode);
				typeData.Conversion = _configuration.GetTypeConversion(typeData.Symbol);
				PreAnalyzeType(typeData);

				var typeIgnored = typeData.GetSelfAndAncestorsTypeData().Any(o => o.Conversion == TypeConversion.Ignore);

				// TODO: fields can have anonymous functions that have async calls
				foreach (var memberNode in typeNode.Members)
				{
					if (memberNode is BaseMethodDeclarationSyntax methodNode)
					{
						var methodData = documentData.GetOrCreateBaseMethodData(methodNode, typeData);
						if (typeIgnored)
						{
							methodData.Ignore(IgnoreReason.TypeConversion, true);
						}
						else
						{
							PreAnalyzeFunctionData(methodData, documentData.SemanticModel);
						}

						foreach (var node in methodNode.DescendantNodes())
						{
							switch (node.Kind())
							{
								case SyntaxKind.ParenthesizedLambdaExpression:
								case SyntaxKind.AnonymousMethodExpression:
								case SyntaxKind.SimpleLambdaExpression:
									var anonFunData = documentData.GetOrCreateAnonymousFunctionData((AnonymousFunctionExpressionSyntax)node, methodData);
									if (methodData.Conversion == MethodConversion.Ignore)
									{
										anonFunData.Ignore(IgnoreReason.Cascade, true);
									}
									else
									{
										PreAnalyzeAnonymousFunction(anonFunData, documentData.SemanticModel);
									}
									break;
								case SyntaxKind.LocalFunctionStatement:
									var localFunData = documentData.GetOrCreateLocalFunctionData((LocalFunctionStatementSyntax)node, methodData);
									if (methodData.Conversion == MethodConversion.Ignore)
									{
										localFunData.Ignore(IgnoreReason.Cascade, true);
									}
									else
									{
										PreAnalyzeLocalFunction(localFunData, documentData.SemanticModel);
									}
									break;
							}
						}
					}
					else if (memberNode is PropertyDeclarationSyntax propertyNode)
					{
						var propertyData = documentData.GetOrCreatePropertyData(propertyNode, typeData);
						if (typeIgnored)
						{
							propertyData.Ignore(IgnoreReason.TypeConversion, true);
						}
						else
						{
							PreAnalyzePropertyData(propertyData, documentData.SemanticModel);
						}
					}
					else if (memberNode is BaseFieldDeclarationSyntax fieldNode)
					{
						var fieldData = documentData.GetOrCreateBaseFieldData(fieldNode, typeData);
						if (typeIgnored)
						{
							fieldData.Ignore(IgnoreReason.TypeConversion, true);
						}
						else
						{
							PreAnalyzeFieldData(fieldData, documentData.SemanticModel);
						}
					}
				}
			}
		}

		private void PreAnalyzeType(TypeData typeData)
		{
			// TODO: validate conversion
			if (typeData.Conversion == TypeConversion.Ignore)
			{
				return;
			}
			if (typeData.Conversion == TypeConversion.Unknown && typeData.ParentTypeData?.GetSelfAndAncestorsTypeData()
				    .Any(o => o.Conversion == TypeConversion.NewType) == true)
			{
				typeData.Conversion = TypeConversion.Copy;
			}


			typeData.IsPartial = typeData.Node.IsPartial();
		}

		private void PreAnalyzeFieldData(BaseFieldData fieldData, SemanticModel semanticModel)
		{
			if (fieldData.TypeData.Conversion == TypeConversion.Partial)
			{
				fieldData.Ignore(IgnoreReason.PartialContainingType);
				return;
			}
			if (fieldData.TypeData.Conversion == TypeConversion.Ignore)
			{
				fieldData.Ignore(IgnoreReason.Cascade);
				return;
			}
			if (fieldData.TypeData.IsNewType)
			{
				foreach (var variable in fieldData.Variables)
				{
					variable.Conversion = FieldVariableConversion.Smart;
				}
			}
		}

		private void PreAnalyzePropertyData(PropertyData propertyData, SemanticModel semanticModel)
		{
			var newType = propertyData.TypeData.IsNewType;
			if (!_configuration.PropertyConversion)
			{
				// Ignore getter and setter accessors and copy the property if needed
				propertyData.IgnoreAccessors(IgnoreReason.PropertyConversion);
			}
			if (newType)
			{
				propertyData.Copy();  // TODO: copy only if needed
			}
			else if(propertyData.TypeData.GetSelfAndAncestorsTypeData().Any(o => o.Conversion == TypeConversion.Partial || o.Conversion == TypeConversion.Ignore))
			{
				propertyData.Conversion = PropertyConversion.Ignore; // For partial types we do not want to copy the property
			}
			if (!_configuration.PropertyConversion)
			{
				return;
			}
			var getter = propertyData.GetAccessorData;
			if (getter != null)
			{
				PreAnalyzeFunctionData(getter, semanticModel);
			}
			var setter = propertyData.SetAccessorData;
			if (setter != null)
			{
				PreAnalyzeFunctionData(setter, semanticModel);
			}
		}


		private void PreAnalyzeFunctionData(FunctionData functionData, SemanticModel semanticModel)
		{
			var methodSymbol = functionData.Symbol;

			if (functionData.Conversion == MethodConversion.Ignore)
			{
				return;
			}

			functionData.Conversion = _configuration.GetMethodConversion(methodSymbol);
			// TODO: validate conversion
			if (functionData.Conversion == MethodConversion.Ignore)
			{
				functionData.Ignore(IgnoreReason.MethodConversion, true);
				return;
			}

			functionData.ForceAsync = functionData.Conversion.HasFlag(MethodConversion.ToAsync);
			var newType = functionData.TypeData.IsNewType;
			// Here we want to log only ignored methods that were explicitly set to async
			void IgnoreOrCopy(IgnoreReason reason, bool forceCopy = false)
			{
				if (newType)
				{
					// The method should be copied only when it is referenced by a method that will be generated, is implementing/overriding an
					// interface/base type or it is explicitly flagged by the user to bo copied
					functionData.CanBeAsync = false;
					if (forceCopy || functionData.Conversion.HasFlag(MethodConversion.Copy))
					{
						functionData.Copy();
					}
					else
					{
						// Scan references in order to later decide whether the method will be copied or ignored.
						functionData.Smart();
					}
				}
				else
				{
					if (functionData.ForceAsync)
					{
						reason = reason.WithSeverity(DiagnosticSeverity.Warning);
					}

					functionData.Ignore(reason);
				}
			}

			if (methodSymbol.IsAsync || methodSymbol.Name.EndsWith("Async"))
			{
				IgnoreOrCopy(IgnoreReason.AlreadyAsync);
				return;
			}

			// Constructors should always be copied for new types
			if (newType && 
			    (
				    methodSymbol.MethodKind == MethodKind.Constructor ||
					methodSymbol.MethodKind == MethodKind.Destructor ||
					methodSymbol.MethodKind == MethodKind.StaticConstructor
				))
			{
				IgnoreOrCopy(IgnoreReason.NotSupported($"Unsupported method kind {methodSymbol.MethodKind}"), true);
				return;
			}

			if (
				methodSymbol.MethodKind != MethodKind.Ordinary && 
				methodSymbol.MethodKind != MethodKind.ExplicitInterfaceImplementation &&
				methodSymbol.MethodKind != MethodKind.PropertyGet &&
				methodSymbol.MethodKind != MethodKind.PropertySet)
			{
				IgnoreOrCopy(IgnoreReason.NotSupported($"Unsupported method kind {methodSymbol.MethodKind}"));
				return;
			}

			if (methodSymbol.Parameters.Any(o => o.RefKind == RefKind.Out))
			{
				IgnoreOrCopy(IgnoreReason.OutParameters);
				return;
			}
			FillFunctionLocks(functionData, semanticModel);

			if (!(functionData is MethodOrAccessorData methodData))
			{
				return;
			}

			// Override user configuration if the method is set to be copied on a partial type conversion
			if (methodData.Conversion == MethodConversion.Copy && !newType)
			{
				methodData.AddDiagnostic(
					"Method cannot be copied, when the containing type conversion is not set to be a new type. " +
					$"Override the method conversion to '{MethodConversion.Unknown}'",
					DiagnosticSeverity.Warning);
				methodData.Conversion = MethodConversion.Unknown;
			}

			// Check if explicitly implements external interfaces
			if (methodSymbol.MethodKind == MethodKind.ExplicitInterfaceImplementation)
			{
				foreach (var interfaceMember in methodSymbol.ExplicitInterfaceImplementations)
				{
					// Check if the interface member has an async counterpart
					var asyncConterparts = FillRelatedAsyncMethods(methodData, interfaceMember);
					if (methodSymbol.ContainingAssembly.Name != interfaceMember.ContainingAssembly.Name)
					{
						methodData.ExternalRelatedMethods.TryAdd(interfaceMember);
						if (!asyncConterparts.Any())
						{
							IgnoreOrCopy(IgnoreReason.ExplicitImplementsExternalMethodWithoutAsync(interfaceMember), true);
							return;
						}
					}
					else
					{
						methodData.ImplementedInterfaces.TryAdd(interfaceMember);
					}
					// For new types we need to copy all interface members
					if (newType)
					{
						methodData.SoftCopy();
					}
				}
			}

			// Check if the method is overriding an external method
			var overridenMethod = methodSymbol.OverriddenMethod;
			while (overridenMethod != null)
			{
				// Check if the member has an async counterpart that is not implemented in the current type (missing member)
				var asyncConterparts = FillRelatedAsyncMethods(methodData, overridenMethod);
				if (methodSymbol.ContainingAssembly.Name != overridenMethod.ContainingAssembly.Name)
				{
					methodData.ExternalRelatedMethods.TryAdd(overridenMethod);
					if (!asyncConterparts.Any())
					{
						IgnoreOrCopy(IgnoreReason.OverridesExternalMethodWithoutAsync(overridenMethod), true);
						return;
					}
				}
				else
				{
					methodData.OverridenMethods.TryAdd(overridenMethod);
				}
				if (overridenMethod.OverriddenMethod != null)
				{
					overridenMethod = overridenMethod.OverriddenMethod;
				}
				else
				{
					break;
				}
			}
			methodData.BaseOverriddenMethod = overridenMethod;

			// Check if the method is implementing an external interface, if true skip as we cannot modify externals
			// FindImplementationForInterfaceMember will find the first implementation method starting from the deepest base class
			var type = methodSymbol.ContainingType;
			var interfaceMembers = type.AllInterfaces
				.SelectMany(
					o => o.GetMembers(methodSymbol.Name)
						.Where(
							m =>
							{
								// Find out if the method implements the interface member or an override 
								// method that implements it
								var impl = type.FindImplementationForInterfaceMember(m);
								return methodSymbol.EqualTo(impl) || methodData.OverridenMethods.Any(ov => ov.EqualTo(impl));
							}
						))
				.OfType<IMethodSymbol>()
				.ToList();
			foreach (var interfaceMember in interfaceMembers)
			{
				// Check if the member has an async counterpart that is not implemented in the current type (missing member)
				var asyncConterparts = FillRelatedAsyncMethods(methodData, interfaceMember);
				if (methodSymbol.ContainingAssembly.Name != interfaceMember.ContainingAssembly.Name)
				{
					// Check if there is an internal interface member that hides this method
					if (interfaceMembers.Where(o => o.ContainingAssembly.Name == methodSymbol.ContainingAssembly.Name)
						.Any(o => o.GetHiddenMethods().Any(hm => hm.EqualTo(interfaceMember))))
					{
						continue;
					}
					methodData.ExternalRelatedMethods.TryAdd(interfaceMember);
					if (!asyncConterparts.Any())
					{
						IgnoreOrCopy(IgnoreReason.ImplementsExternalMethodWithoutAsync(interfaceMember), true);
						return;
					}
				}
				else
				{
					methodData.ImplementedInterfaces.TryAdd(interfaceMember);
				}
				// For new types we need to copy all interface member
				if (newType)
				{
					methodData.SoftCopy();
				}
			}

			// Verify if there is already an async counterpart for this method
			//TODO: this is not correct when generating methods with a cancellation token as here we do not know
			// if the generated method will have the cancellation token parameter or not
			var asyncCounterparts = GetAsyncCounterparts(methodSymbol.OriginalDefinition, _preAnalyzeSearchOptions).ToList();
			if (asyncCounterparts.Any())
			{
				if (!_preAnalyzeSearchOptions.HasFlag(AsyncCounterpartsSearchOptions.HasCancellationToken) && asyncCounterparts.Count > 1)
				{
					throw new InvalidOperationException($"Method {methodSymbol} has more than one async counterpart");
				}
				// We shall get a maximum of two async counterparts when the HasCancellationToken flag is used
				if (_preAnalyzeSearchOptions.HasFlag(AsyncCounterpartsSearchOptions.HasCancellationToken) && asyncCounterparts.Count > 2)
				{
					throw new InvalidOperationException($"Method {methodSymbol} has more than two async counterparts");
				}

				foreach (var asyncCounterpart in asyncCounterparts)
				{
					// Check if the async counterpart has a cancellation token
					if (asyncCounterpart.Parameters.Length > methodSymbol.Parameters.Length)
					{
						methodData.AsyncCounterpartWithTokenSymbol = asyncCounterpart;
					}
					else
					{
						methodData.AsyncCounterpartSymbol = asyncCounterpart;
					}

					methodData.IgnoreAsyncCounterpart = _configuration.IgnoreAsyncCounterpartsPredicates.Any(p => p(asyncCounterpart));
				}
				// TODO: define a better logic
				// We should not ignore if none of the async counterparts is in the sync method type.
				if (asyncCounterparts.Any(o => o.ContainingType.EqualTo(methodSymbol.ContainingType))
				/*(_configuration.UseCancellationTokens && asyncCounterparts.Count == 2) ||
			(!_configuration.UseCancellationTokens && asyncCounterparts.Count == 1)*/
				)
				{
					IgnoreOrCopy(IgnoreReason.AsyncCounterpartExists);
					methodData.CancellationTokenRequired = methodData.AsyncCounterpartWithTokenSymbol != null;
					return;
				}
			}

			// Create an override async method if any of the related async methods is virtual or abstract
			// We need to do this here so that the method body will get scanned for async counterparts
			if (methodData.RelatedAsyncMethods.Any(o => o.IsVirtual || o.IsAbstract) &&
			    _configuration.CanScanForMissingAsyncMembers?.Invoke(methodData.TypeData.Symbol) == true)
			{
				if (!methodData.Conversion.HasAnyFlag(MethodConversion.ToAsync, MethodConversion.Smart))
				{
					methodData.Smart();
				}
				if (newType)
				{
					methodData.SoftCopy();
				}

				// We have to generate the cancellation token parameter if the async member has more parameters that the sync counterpart
				var asyncMember = methodData.RelatedAsyncMethods
					.Where(o => o.IsVirtual || o.IsAbstract)
					.FirstOrDefault(o => o.Parameters.Length > methodData.Symbol.Parameters.Length);
				if (asyncMember != null)
				{
					methodData.CancellationTokenRequired = true;
					// We suppose that the cancellation token is the last parameter
					methodData.MethodCancellationToken = asyncMember.Parameters.Last().HasExplicitDefaultValue
						? MethodCancellationToken.Optional
						: MethodCancellationToken.Required;
				}
			}
		}

		private List<IMethodSymbol> FillRelatedAsyncMethods(MethodOrAccessorData methodOrAccessorData, IMethodSymbol symbol)
		{
			var asyncConterparts = GetAsyncCounterparts(symbol, _preAnalyzeSearchOptions).ToList();
			if (asyncConterparts.Any())
			{
				foreach (var asyncConterpart in asyncConterparts)
				{
					methodOrAccessorData.RelatedAsyncMethods.TryAdd(asyncConterpart);
				}
			}
			return asyncConterparts;
		}

		private void PreAnalyzeAnonymousFunction(AnonymousFunctionData functionData, SemanticModel semanticModel)
		{
			if (functionData.MethodData.Conversion.HasFlag(MethodConversion.Copy))
			{
				functionData.Copy();
				return;
			}
			var funcionSymbol = functionData.Symbol;
			void Copy(string reason)
			{
				functionData.Copy();
				if (!string.IsNullOrEmpty(reason))
				{
					functionData.AddDiagnostic(reason, DiagnosticSeverity.Hidden);
				}
			}
			
			if (funcionSymbol.IsAsync)
			{
				Copy(IgnoreReason.AlreadyAsync.Reason);
				return;
			}
			if (funcionSymbol.Parameters.Any(o => o.RefKind == RefKind.Out))
			{
				Copy(IgnoreReason.OutParameters.Reason);
				return;
			}

			if (!functionData.Node.Parent.IsKind(SyntaxKind.Argument))
			{
				Copy($"Is not passed as an argument but instead as a {Enum.GetName(typeof(SyntaxKind), functionData.Node.Parent.Kind())} which is currently not supported");
				return;
			}
			var argumentNode = (ArgumentSyntax)functionData.Node.Parent;
			if (!argumentNode.Parent.Parent.IsKind(SyntaxKind.InvocationExpression))
			{
				Copy($"Is passed as an argument to a {Enum.GetName(typeof(SyntaxKind), argumentNode.Parent.Parent.Kind())} which is currently not supported");
				return;
			}
			var invocationNode = (InvocationExpressionSyntax)argumentNode.Parent.Parent;
			//var argumentListNode = (ArgumentListSyntax)argumentNode.Parent;
			//var index = argumentListNode.Arguments.IndexOf(argumentNode);
			var symbol = semanticModel.GetSymbolInfo(invocationNode.Expression).Symbol;
			if (!(symbol is IMethodSymbol))
			{
				Copy($"Is passed as an argument to a symbol {symbol} which is currently not supported");
				return;
			}
			FillFunctionLocks(functionData, semanticModel);
		}

		private void PreAnalyzeLocalFunction(LocalFunctionData functionData, SemanticModel semanticModel)
		{
			if (functionData.MethodData.Conversion.HasFlag(MethodConversion.Copy))
			{
				functionData.Copy();
				return;
			}
			//TODO
			FillFunctionLocks(functionData, semanticModel);
		}

		private void FillFunctionLocks(FunctionData functionData, SemanticModel semanticModel)
		{
			var bodyNode = functionData.GetBodyNode();
			if (bodyNode == null)
			{
				return;
			}
			var locks = bodyNode.DescendantNodes().OfType<LockStatementSyntax>().ToList();
			if (!locks.Any())
			{
				return;
			}
			foreach (var lockNode in locks)
			{
				ISymbol symbol;
				if (lockNode.Expression is TypeOfExpressionSyntax typeOfExpression)
				{
					symbol = semanticModel.GetSymbolInfo(typeOfExpression.Type).Symbol;
				}
				else
				{
					symbol = semanticModel.GetSymbolInfo(lockNode.Expression).Symbol;
				}
				functionData.Locks.Add(new LockData(symbol, lockNode));
			}
		}
	}
}
