using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonoDevelop.Ide.TypeSystem;

namespace ICSharpCode.NRefactory6.CSharp
{
	class RefactoringHelpers
	{
		public static TypeSyntax ConvertType(SemanticModel model, int position, ITypeSymbol type)
		{
			return SyntaxFactory.ParseTypeName(type.ToMinimalDisplayString(model, position));
		}

		static string GetNameProposal(string eventName)
		{
			return "On" + char.ToUpper(eventName[0]) + eventName.Substring(1);
		}

		static string CreateBaseNameFromString(string str)
		{
			if (string.IsNullOrEmpty(str)) {
				return "empty";
			}
			var sb = new StringBuilder();
			bool firstLetter = true, wordStart = false;
			foreach (char ch in str) {
				if (char.IsWhiteSpace(ch)) {
					wordStart = true;
					continue;
				}
				if (!char.IsLetter(ch))
					continue;
				if (firstLetter) {
					sb.Append(char.ToLower(ch));
					firstLetter = false;
					continue;
				}
				if (wordStart) {
					sb.Append(char.ToUpper(ch));
					wordStart = false;
					continue;
				}
				sb.Append(ch);
			}
			return sb.Length == 0 ? "str" : sb.ToString();
		}

		public static string CreateBaseName(SyntaxNode node, ITypeSymbol type)
		{
			string name = null;

			if (node.IsKind(SyntaxKind.Argument))
				node = ((ArgumentSyntax)node).Expression;

			if (node.IsKind(SyntaxKind.NullLiteralExpression))
				return "o";
			if (node.IsKind(SyntaxKind.InvocationExpression))
				return CreateBaseName(((InvocationExpressionSyntax)node).Expression, type);
			if (node.IsKind(SyntaxKind.IdentifierName)) {
				name = node.ToString();
			} else if (node is MemberAccessExpressionSyntax) {
				name = ((MemberAccessExpressionSyntax)node).Name.ToString();
			} else if (node is LiteralExpressionSyntax) {
				var pe = (LiteralExpressionSyntax)node;
				if (pe.IsKind(SyntaxKind.StringLiteralExpression)) {
					name = CreateBaseNameFromString(pe.Token.ToString());
				} else {
					return char.ToLower(type.Name[0]).ToString();
				}
			} else if (node is ArrayCreationExpressionSyntax) {
				name = "arr";
			} else {
				if (type.TypeKind == TypeKind.Error)
					return "par";
				name = GuessNameFromType(type);
			}
			var sb = new StringBuilder();
			sb.Append(char.ToLower(name[0]));
			for (int i = 1; i < name.Length; i++) {
				var ch = name[i];
				if (char.IsLetterOrDigit(ch) || ch == '_')
					sb.Append(ch);
			}
			return sb.ToString();
		}

		internal static string GuessNameFromType(ITypeSymbol returnType)
		{
			switch (returnType.SpecialType) {
			case SpecialType.System_Object:
				return "obj";
			case SpecialType.System_Boolean:
				return "b";
			case SpecialType.System_Char:
				return "ch";
			case SpecialType.System_SByte:
			case SpecialType.System_Byte:
				return "b";
			case SpecialType.System_Int16:
			case SpecialType.System_UInt16:
			case SpecialType.System_Int32:
			case SpecialType.System_UInt32:
			case SpecialType.System_Int64:
			case SpecialType.System_UInt64:
				return "i";
			case SpecialType.System_Decimal:
				return "d";
			case SpecialType.System_Single:
				return "f";
			case SpecialType.System_Double:
				return "d";
			case SpecialType.System_String:
				return "str";
			case SpecialType.System_IntPtr:
			case SpecialType.System_UIntPtr:
				return "ptr";
			case SpecialType.System_DateTime:
				return "date";
			}
			if (returnType.TypeKind == TypeKind.Array)
				return "arr";
			switch (returnType.GetFullName()) {
			case "System.Exception":
				return "e";
			case "System.Object":
			case "System.Func":
			case "System.Action":
				return "action";
			}
			return string.IsNullOrEmpty(returnType.Name) ? "obj" : returnType.Name;
		}
	}
}
