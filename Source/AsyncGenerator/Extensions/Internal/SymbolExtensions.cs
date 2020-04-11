using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Extensions;
using AsyncGenerator.Core.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Extensions.Internal
{
	internal static class SymbolExtensions
	{
		internal static bool IsTaskType(this ITypeSymbol typeSymbol)
		{
			return typeSymbol.Name == nameof(Task) &&
			       typeSymbol.ContainingNamespace.ToString() == "System.Threading.Tasks";
		}

		internal static bool SupportsTaskType(this ITypeSymbol typeSymbol)
		{
			if (typeSymbol is ITypeParameterSymbol typeParameterSymbol)
			{
				return typeParameterSymbol.ConstraintTypes.All(o => o.IsTaskType()) &&
				       !typeParameterSymbol.HasValueTypeConstraint;
			}

			return IsTaskType(typeSymbol);
		}

		/// <summary>
		/// Check if the retrived type can be returned without having an await. We need to consider that the given types will be wrapped in a <see cref="Task{T}"/>.
		/// Also when dealing with type parameters we need to check if the they can be applied to the given type
		/// </summary>
		/// <param name="retrivedType"></param>
		/// <param name="toReturnType"></param>
		/// <returns></returns>
		internal static bool IsAwaitRequired(this ITypeSymbol retrivedType, ITypeSymbol toReturnType)
		{
			if (retrivedType.EqualTo(toReturnType))
			{
				return true;
			}
			var retrivedNamedType = retrivedType as INamedTypeSymbol;
			var toReturnNamedType = toReturnType as INamedTypeSymbol;
			if (retrivedNamedType == null)
			{
				if (retrivedType is ITypeParameterSymbol retrivedParamType)
				{
					return retrivedParamType.CanApply(toReturnType);
				}
				return false;
			}
			if (toReturnNamedType == null)
			{
				if (toReturnType is ITypeParameterSymbol toReturnParamType)
				{
					return toReturnParamType.CanApply(retrivedType);
				}
				return false;
			}

			if (!retrivedType.OriginalDefinition.EqualTo(toReturnType.OriginalDefinition))
			{
				return false;
			}
			// If the original definitions are equal then we need to check the type arguments if they match
			for (var i = 0; i < retrivedNamedType.TypeArguments.Length; i++)
			{
				if (!retrivedNamedType.TypeArguments[i].IsAwaitRequired(toReturnNamedType.TypeArguments[i]))
				{
					return false;
				}
			}
			return true;
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
			return SyntaxNodeExtensions.ConstructNameSyntax(symbol.ToString(), insideCref: insideCref, onlyName: onlyName);
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
