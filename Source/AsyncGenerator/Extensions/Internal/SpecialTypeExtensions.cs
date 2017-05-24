using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Extensions
{
	internal static class SpecialTypeExtensions
	{
		internal static PredefinedTypeSyntax ToPredefinedType(this SpecialType specialType)
		{
			switch (specialType)
			{
				case SpecialType.System_Object:
					return PredefinedType(Token(SyntaxKind.ObjectKeyword));
				case SpecialType.System_Void:
					return PredefinedType(Token(SyntaxKind.VoidKeyword));
				case SpecialType.System_Boolean:
					return PredefinedType(Token(SyntaxKind.BoolKeyword));
				case SpecialType.System_Char:
					return PredefinedType(Token(SyntaxKind.CharKeyword));
				case SpecialType.System_SByte:
					return PredefinedType(Token(SyntaxKind.SByteKeyword));
				case SpecialType.System_Byte:
					return PredefinedType(Token(SyntaxKind.ByteKeyword));
				case SpecialType.System_Int16:
					return PredefinedType(Token(SyntaxKind.ShortKeyword));
				case SpecialType.System_UInt16:
					return PredefinedType(Token(SyntaxKind.UShortKeyword));
				case SpecialType.System_Int32:
					return PredefinedType(Token(SyntaxKind.IntKeyword));
				case SpecialType.System_UInt32:
					return PredefinedType(Token(SyntaxKind.UIntKeyword));
				case SpecialType.System_Int64:
					return PredefinedType(Token(SyntaxKind.LongKeyword));
				case SpecialType.System_UInt64:
					return PredefinedType(Token(SyntaxKind.ULongKeyword));
				case SpecialType.System_Decimal:
					return PredefinedType(Token(SyntaxKind.DecimalKeyword));
				case SpecialType.System_Single:
					return PredefinedType(Token(SyntaxKind.FloatKeyword));
				case SpecialType.System_Double:
					return PredefinedType(Token(SyntaxKind.DoubleKeyword));
				case SpecialType.System_String:
					return PredefinedType(Token(SyntaxKind.StringKeyword));
				default:
					return null;
			}
		}
	}
}
