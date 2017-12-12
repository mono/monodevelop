using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.TypeSystem;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
	static class MDUtils
	{
		public static string ClassificationsToMarkup (ITextSnapshot snapshot, IList<ClassificationSpan> classifications, Span? locus)
		{
			var markup = new StringBuilder ();
			var theme = DefaultSourceEditorOptions.Instance.GetEditorTheme ();
			foreach (var part in classifications) {
				//if () { TODO: Check if we hanle new lines
				//	markup.AppendLine ();
				//	continue;
				//}
				markup.Append ("<span foreground=\"");
				markup.Append (GetThemeColor (theme, GetThemeColor (part.ClassificationType.Classification)));
				markup.Append ("\">");
				if (locus is Span locusSpan && part.Span.Intersection (locusSpan) is SnapshotSpan intersection) {
					if (intersection.Start == part.Span.Start) {
						if (intersection.End == part.Span.End) {
							markup.Append ("<b>");
							markup.Append (Ambience.EscapeText (snapshot.GetText (part.Span)));
							markup.Append ("</b>");
						}
						else {
							markup.Append ("<b>");
							markup.Append (Ambience.EscapeText (snapshot.GetText (intersection)));
							markup.Append ("</b>");
							markup.Append (Ambience.EscapeText (snapshot.GetText (intersection.End, part.Span.End - intersection.End)));
						}
					}
					else {
						if (intersection.End == part.Span.End) {
							markup.Append (Ambience.EscapeText (snapshot.GetText (part.Span.Start, intersection.Start - part.Span.Start)));
							markup.Append ("<b>");
							markup.Append (Ambience.EscapeText (snapshot.GetText (intersection)));
							markup.Append ("</b>");
						}
						else {
							markup.Append (Ambience.EscapeText (snapshot.GetText (part.Span.Start, intersection.Start - part.Span.Start)));
							markup.Append ("<b>");
							markup.Append (Ambience.EscapeText (snapshot.GetText (intersection)));
							markup.Append ("</b>");
							markup.Append (Ambience.EscapeText (snapshot.GetText (intersection.End, part.Span.End - intersection.End)));
						}
					}
				}
				else {
					markup.Append (Ambience.EscapeText (snapshot.GetText (part.Span)));
				}
				markup.Append ("</span>");
			}
			return markup.ToString ();
		}

		private static void AppendSpan (ITextSnapshot snapshot, StringBuilder markup, EditorTheme theme, ClassificationSpan part)
		{
			markup.Append ("<span foreground=\"");
			markup.Append (GetThemeColor (theme, GetThemeColor (part.ClassificationType.Classification)));
			markup.Append ("\">");
			markup.Append (Ambience.EscapeText (snapshot.GetText (part.Span)));
			markup.Append ("</span>");
		}

		static string GetThemeColor (EditorTheme theme, string scope)
		{
			return SyntaxHighlightingService.GetColorFromScope (theme, scope, EditorThemeColors.Foreground).ToPangoString ();
		}

		static string GetThemeColor (string type)
		{
			switch (type) {
			case ClassificationTypeNames.Keyword:
				return "keyword";
			case ClassificationTypeNames.ClassName:
				return EditorThemeColors.UserTypes;
			case ClassificationTypeNames.DelegateName:
				return EditorThemeColors.UserTypesDelegates;
			case ClassificationTypeNames.EnumName:
				return EditorThemeColors.UserTypesEnums;
			case ClassificationTypeNames.InterfaceName:
				return EditorThemeColors.UserTypesInterfaces;
			case ClassificationTypeNames.ModuleName:
				return EditorThemeColors.UserTypes;
			case ClassificationTypeNames.StructName:
				return EditorThemeColors.UserTypesValueTypes;
			case ClassificationTypeNames.TypeParameterName:
				return EditorThemeColors.UserTypesTypeParameters;
			case ClassificationTypeNames.Identifier:
				return "source.cs";//TODO: This things say .cs, C#
			case ClassificationTypeNames.NumericLiteral:
				return "constant.numeric";
			case ClassificationTypeNames.StringLiteral:
				return "string.quoted";
			case ClassificationTypeNames.WhiteSpace:
				return "source.cs";
			case ClassificationTypeNames.Operator:
				return "keyword.source";
			case ClassificationTypeNames.Punctuation:
				return "punctuation";
			case ClassificationTypeNames.Text:
				return "source.cs";
			default:
				LoggingService.LogWarning ("Warning unexpected classification type: " + type);
				return "source.cs";
			}
		}

		public static class ClassificationTypeNames
		{
			public const string Comment = "comment";
			public const string ExcludedCode = "excluded code";
			public const string Identifier = "identifier";
			public const string Keyword = "keyword";
			public const string NumericLiteral = "number";
			public const string Operator = "operator";
			public const string PreprocessorKeyword = "preprocessor keyword";
			public const string StringLiteral = "string";
			public const string WhiteSpace = "whitespace";
			public const string Text = "text";

			public const string PreprocessorText = "preprocessor text";
			public const string Punctuation = "punctuation";
			public const string VerbatimStringLiteral = "string - verbatim";

			public const string ClassName = "class name";
			public const string DelegateName = "delegate name";
			public const string EnumName = "enum name";
			public const string InterfaceName = "interface name";
			public const string ModuleName = "module name";
			public const string StructName = "struct name";
			public const string TypeParameterName = "type parameter name";

			public const string XmlDocCommentAttributeName = "xml doc comment - attribute name";
			public const string XmlDocCommentAttributeQuotes = "xml doc comment - attribute quotes";
			public const string XmlDocCommentAttributeValue = "xml doc comment - attribute value";
			public const string XmlDocCommentCDataSection = "xml doc comment - cdata section";
			public const string XmlDocCommentComment = "xml doc comment - comment";
			public const string XmlDocCommentDelimiter = "xml doc comment - delimiter";
			public const string XmlDocCommentEntityReference = "xml doc comment - entity reference";
			public const string XmlDocCommentName = "xml doc comment - name";
			public const string XmlDocCommentProcessingInstruction = "xml doc comment - processing instruction";
			public const string XmlDocCommentText = "xml doc comment - text";

			public const string XmlLiteralAttributeName = "xml literal - attribute name";
			public const string XmlLiteralAttributeQuotes = "xml literal - attribute quotes";
			public const string XmlLiteralAttributeValue = "xml literal - attribute value";
			public const string XmlLiteralCDataSection = "xml literal - cdata section";
			public const string XmlLiteralComment = "xml literal - comment";
			public const string XmlLiteralDelimiter = "xml literal - delimiter";
			public const string XmlLiteralEmbeddedExpression = "xml literal - embedded expression";
			public const string XmlLiteralEntityReference = "xml literal - entity reference";
			public const string XmlLiteralName = "xml literal - name";
			public const string XmlLiteralProcessingInstruction = "xml literal - processing instruction";
			public const string XmlLiteralText = "xml literal - text";
		}
	}
}
