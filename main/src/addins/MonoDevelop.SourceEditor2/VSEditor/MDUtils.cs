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
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.TypeSystem;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
	static class MDUtils
	{
		const int MaxParamColumnCount = 100;

		public static string ClassificationsToMarkup (ITextSnapshot snapshot, IList<ClassificationSpan> classifications, IParameter currentParameter)
		{
			var markup = new StringBuilder ();
			var theme = DefaultSourceEditorOptions.Instance.GetEditorTheme ();
			Span? locus = currentParameter?.Locus;
			bool inDocumentation = false;
			for (int i = 0; i < classifications.Count; i++) {
				var part = classifications [i];
				if (!inDocumentation) {
					if (part.ClassificationType.Classification == ClassificationTypeNames.Text) {
						inDocumentation = true;
						markup.Append ("<span font='" + FontService.SansFontName + "' size='small'>");
						markup.AppendLine ();
					}
					else {
						markup.Append ("<span foreground=\"");
						markup.Append (GetThemeColor (theme, GetThemeColor (part.ClassificationType.Classification)));
						markup.Append ("\">");
					}
				}

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
					if (inDocumentation) {
						AppendAndBreakText (markup, snapshot.GetText (part.Span), 0, MaxParamColumnCount);
					} else {
						markup.Append(Ambience.EscapeText (snapshot.GetText (part.Span)));
					}
				}
				if (!inDocumentation)
					markup.Append ("</span>");
			}
			if (inDocumentation)
				markup.Append ("</span>");

			if (currentParameter != null) {
				if (!string.IsNullOrEmpty(currentParameter.Documentation)) {
					markup.Append ("<span font='" + FontService.SansFontName + "'");
					//markup.Append ("foreground ='" + GetThemeColor (theme, "source.cs") + "'");
					markup.Append (" size='small'>");
					markup.AppendLine ();
					markup.AppendLine ();
					markup.Append ("<b>");
					markup.Append (currentParameter.Name);
					markup.Append (": </b>");
					markup.Append (currentParameter.Documentation);
					markup.Append ("</span>");
				}
			}

			return markup.ToString ();
		}

		static void AppendAndBreakText (StringBuilder markup, string text, int col, int maxColumn)
		{
			var idx = maxColumn - col > 0 && maxColumn - col < text.Length ? text.IndexOf (' ', maxColumn - col) : -1;
			if (idx < 0) {
				markup.Append (Ambience.EscapeText (text));
			} else {
				markup.Append (Ambience.EscapeText (text.Substring (0, idx)));
				if (idx + 1 >= text.Length)
					return;
				markup.AppendLine ();
				AppendAndBreakText (markup, text.Substring (idx + 1), 0, maxColumn);
			}
		}

		private static void AppendSpan (ITextSnapshot snapshot, StringBuilder markup, EditorTheme theme, ClassificationSpan part)
		{
			markup.Append ("<span foreground=\"");
			markup.Append (GetThemeColor (theme, "source.cs"));
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

			public const string FieldName = "field name";
			public const string EnumMemberName = "enum member name";
			public const string ConstantName = "constant name";
			public const string LocalName = "local name";
			public const string ParameterName = "parameter name";
			public const string MethodName = "method name";
			public const string ExtensionMethodName = "extension method name";
			public const string PropertyName = "property name";
			public const string EventName = "event name";
 
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

		/// <summary>
		/// Gets a snapshot point out of a monodevelop line/column pair or null.
		/// </summary>
		/// <returns>The snapshot point or null if the coordinate is invalid.</returns>
		/// <param name="snapshot">The snapshot.</param>
		/// <param name="line">Line number (1 based).</param>
		/// <param name="column">Column number (1 based).</param>
		public static SnapshotPoint? GetSnapshotPoint (this ITextSnapshot snapshot, int line, int column)
		{
			if (TryGetSnapshotPoint (snapshot, line, column, out var snapshotPoint)) 
				return snapshotPoint;
			return null;
		}

		/// <summary>
		/// Tries to get a snapshot point out of a monodevelop line/column pair.
		/// </summary>
		/// <returns><c>true</c>, if get snapshot point was set, <c>false</c> otherwise.</returns>
		/// <param name="snapshot">The snapshot.</param>
		/// <param name="line">Line number (1 based).</param>
		/// <param name="column">Column number (1 based).</param>
		/// <param name="snapshotPoint">The snapshot point if return == true.</param>
		public static bool TryGetSnapshotPoint (this ITextSnapshot snapshot, int line, int column, out SnapshotPoint snapshotPoint)
		{
			if (line < 1 || line > snapshot.LineCount) {
				snapshotPoint = default (SnapshotPoint);
				return false;
			}

			var lineSegment = snapshot.GetLineFromLineNumber (line - 1);
			if (column < 1 || column > lineSegment.LengthIncludingLineBreak) {
				snapshotPoint = default (SnapshotPoint);
				return false;
			}

			snapshotPoint = new SnapshotPoint (snapshot, lineSegment.Start.Position + column - 1);
			return true;
		}
	}

}
