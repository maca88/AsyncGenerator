using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Core.Extensions;
using AsyncGenerator.Core.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static AsyncGenerator.Core.Extensions.Internal.SyntaxNodeHelper;

namespace AsyncGenerator.Extensions.Internal
{
	internal static class SymbolExtensions
	{
		internal static ITypeSymbol UnwrapGenericTaskOrValueTask(this ITypeSymbol typeSymbol)
		{
			if (!typeSymbol.IsTaskOrValueTaskType() || !(typeSymbol is INamedTypeSymbol taskType))
			{
				return null;
			}

			if (!taskType.IsGenericType)
			{
				return taskType.ContainingAssembly.GetTypeByMetadataName("System.Void");
			}

			return taskType.TypeArguments.First();
		}

		internal static bool IsVoid(this ITypeSymbol typeSymbol)
		{
			return typeSymbol.Name == typeof(void).Name && typeSymbol.ContainingNamespace.ToString() == "System";
		}

		internal static bool SupportsTaskOrValueTaskType(this ITypeSymbol typeSymbol)
		{
			if (typeSymbol is ITypeParameterSymbol typeParameterSymbol)
			{
				return typeParameterSymbol.ConstraintTypes.All(o => o.IsTaskOrValueTaskType()) &&
				       !typeParameterSymbol.HasValueTypeConstraint;
			}

			return typeSymbol.IsTaskOrValueTaskType();
		}

		/// <summary>
		/// Check if the retrieved type can be returned without having an await. We need to consider that the given types will be wrapped in a <see cref="Task{T}"/>.
		/// Also when dealing with type parameters we need to check if the they can be applied to the given type
		/// </summary>
		/// <param name="retrievedType"></param>
		/// <param name="toReturnType"></param>
		/// <returns></returns>
		internal static bool IsAwaitRequired(this ITypeSymbol retrievedType, ITypeSymbol toReturnType)
		{
			return IsAwaitRequired(retrievedType, toReturnType, 0);
		}

		private static bool IsAwaitRequired(this ITypeSymbol retrievedType, ITypeSymbol toReturnType, int level)
		{
			if (retrievedType.EqualTo(toReturnType) || (level == 0 && toReturnType.IsVoid()))
			{
				return false;
			}

			var retrievedNamedType = retrievedType as INamedTypeSymbol;
			var toReturnNamedType = toReturnType as INamedTypeSymbol;
			if (retrievedNamedType == null)
			{
				if (retrievedType is ITypeParameterSymbol retrievedParamType)
				{
					return !retrievedParamType.CanApply(toReturnType);
				}
				return true;
			}
			if (toReturnNamedType == null)
			{
				if (toReturnType is ITypeParameterSymbol toReturnParamType)
				{
					return !toReturnParamType.CanApply(retrievedType);
				}
				return true;
			}

			if (!retrievedType.OriginalDefinition.EqualTo(toReturnType.OriginalDefinition))
			{
				return true;
			}
			// If the original definitions are equal then we need to check the type arguments if they match
			for (var i = 0; i < retrievedNamedType.TypeArguments.Length; i++)
			{
				if (retrievedNamedType.TypeArguments[i].IsAwaitRequired(toReturnNamedType.TypeArguments[i], level + 1))
				{
					return true;
				}
			}
			return false;
		}

		internal static bool IsAccessor(this ISymbol symbol)
		{
			return symbol.IsPropertyAccessor() || symbol.IsEventAccessor();
		}

		internal static bool IsPropertyAccessor(this ISymbol symbol)
		{
			return (symbol as IMethodSymbol)?.MethodKind.IsPropertyAccessor() == true;
		}

		internal static bool IsPropertyAccessor(this IMethodSymbol symbol)
		{
			return symbol.MethodKind.IsPropertyAccessor();
		}

		internal static bool? IsAutoPropertyAccessor(this IMethodSymbol symbol)
		{
			var syntax = symbol.DeclaringSyntaxReferences.FirstOrDefault();
			if (syntax == null)
			{
				return null;
			}
			return syntax.Span.Length == 4; // get; or set;
		}

		internal static bool IsObsolete(this IMethodSymbol symbol)
		{
			return symbol.GetAttributes().Any(o => o.AttributeClass.Name == nameof(ObsoleteAttribute));
		}

		internal static bool? IsVirtualAbstractOrInterface(this IMethodSymbol symbol)
		{
			return symbol.IsVirtual || symbol.IsAbstract || symbol.ContainingType.IsAbstract;
		}

		internal static TypeSyntax CreateTypeSyntax(this ITypeSymbol symbol, bool insideCref = false, bool onlyName = false)
		{
			var predefinedType = symbol.SpecialType.ToPredefinedType();
			if (predefinedType != null)
			{
				return symbol.IsNullable()
					? (TypeSyntax)SyntaxFactory.NullableType(predefinedType)
					: predefinedType;
			}
			return ConstructNameSyntax(symbol.ToString(), insideCref: insideCref, onlyName: onlyName);
		}

		internal static bool IsEventAccessor(this ISymbol symbol)
		{
			var method = symbol as IMethodSymbol;
			return method != null &&
			       (method.MethodKind == MethodKind.EventAdd ||
			        method.MethodKind == MethodKind.EventRaise ||
			        method.MethodKind == MethodKind.EventRemove);
		}

		internal static bool IsNullable(this ITypeSymbol symbol)
		{
			return symbol?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
		}
	}
}
