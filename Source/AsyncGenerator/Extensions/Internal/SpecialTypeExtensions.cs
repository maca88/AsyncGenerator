using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncGenerator.Extensions.Internal
{
	internal static class SpecialTypeExtensions
	{
		internal static PredefinedTypeSyntax ToPredefinedType(this SpecialType specialType)
		{
			switch (specialType)
			{
				case SpecialType.System_Object:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));
				case SpecialType.System_Void:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
				case SpecialType.System_Boolean:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword));
				case SpecialType.System_Char:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.CharKeyword));
				case SpecialType.System_SByte:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.SByteKeyword));
				case SpecialType.System_Byte:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword));
				case SpecialType.System_Int16:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ShortKeyword));
				case SpecialType.System_UInt16:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UShortKeyword));
				case SpecialType.System_Int32:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword));
				case SpecialType.System_UInt32:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword));
				case SpecialType.System_Int64:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword));
				case SpecialType.System_UInt64:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ULongKeyword));
				case SpecialType.System_Decimal:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DecimalKeyword));
				case SpecialType.System_Single:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.FloatKeyword));
				case SpecialType.System_Double:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword));
				case SpecialType.System_String:
					return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword));
				default:
					return null;
			}
		}
	}
}
