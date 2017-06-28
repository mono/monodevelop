using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SignatureHelp;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.CSharp.Completion
{
	class SignatureHelpParameterHintingData : Ide.CodeCompletion.ParameterHintingData
	{
		public SignatureHelpParameterHintingData(SignatureHelpItem item)
		{
			Item = item;
		}

		public SignatureHelpItem Item { get; }

		public override int ParameterCount => Item.Parameters.Length;

		public override bool IsParameterListAllowed => Item.IsVariadic;

		public override string GetParameterName (int parameter) => Item.Parameters[parameter].Name;


		public override bool Equals (object obj)
		{
			var other = obj as SignatureHelpParameterHintingData;
			if (other == null)
				return false;
			return Item.ToString () == other.Item.ToString ();
		}

		public override Task<TooltipInformation> CreateTooltipInformation (TextEditor editor, DocumentContext ctx, int currentParameter, bool smartWrap, CancellationToken cancelToken)
		{
			var tt = new TooltipInformation ();
			var markup = new StringBuilder ();
			var theme = DefaultSourceEditorOptions.Instance.GetEditorTheme ();
			AppendTaggedText (markup, theme, Item.PrefixDisplayParts);
			for (int i = 0; i < Item.Parameters.Length; i++) {
				if (i > 0) {
					AppendTaggedText (markup, theme, Item.SeparatorDisplayParts);
				}
				var p = Item.Parameters [i];
				if (i == currentParameter)
					markup.Append ("<b>");
				AppendTaggedText (markup, theme, p.DisplayParts);
				if (i == currentParameter)
					markup.Append ("</b>");
			}
			AppendTaggedText (markup, theme, Item.SuffixDisplayParts);

			var documentation = Item.DocumentationFactory (cancelToken).ToList ();
			if (documentation.Count > 0) {
				var summaryMarkup = new StringBuilder ();
				AppendTaggedText (summaryMarkup, theme, documentation);
				tt.SummaryMarkup = summaryMarkup.ToString ();
			}

			tt.SignatureMarkup = markup.ToString ();
			return Task.FromResult (tt);
		}

		void AppendTaggedText (StringBuilder markup, EditorTheme theme, IEnumerable<TaggedText> text)
		{
			foreach (var part in text) {
				markup.Append ("<span foreground=\"");
				markup.Append (GetThemeColor (theme, GetThemeColor (part.Tag)));
				markup.Append ("\">");
				markup.Append (part.Text);
				markup.Append ("</span>");
			}
		}

		static string GetThemeColor (EditorTheme theme, string scope)
		{
			return SyntaxHighlightingService.GetColorFromScope (theme, scope, EditorThemeColors.Foreground).ToPangoString ();
		}

		static string GetThemeColor (string tag)
		{
			switch (tag) {
			case TextTags.Keyword:
				return "keyword";

			case TextTags.Class:
				return EditorThemeColors.UserTypes;
			case TextTags.Delegate:
				return EditorThemeColors.UserTypesDelegates;
			case TextTags.Enum:
				return EditorThemeColors.UserTypesEnums;
			case TextTags.Interface:
				return EditorThemeColors.UserTypesInterfaces;
			case TextTags.Module:
				return EditorThemeColors.UserTypes;
			case TextTags.Struct:
				return EditorThemeColors.UserTypesValueTypes;
			case TextTags.TypeParameter:
				return EditorThemeColors.UserTypesTypeParameters;

			case TextTags.Alias:
			case TextTags.Assembly:
			case TextTags.Field:
			case TextTags.ErrorType:
			case TextTags.Event:
			case TextTags.Label:
			case TextTags.Local:
			case TextTags.Method:
			case TextTags.Namespace:
			case TextTags.Parameter:
			case TextTags.Property:
			case TextTags.RangeVariable:
				return "source.cs";

			case TextTags.NumericLiteral:
				return "constant.numeric";

			case TextTags.StringLiteral:
				return "string.quoted";

			case TextTags.Space:
			case TextTags.LineBreak:
				return "source.cs";

			case TextTags.Operator:
				return "keyword.source";

			case TextTags.Punctuation:
				return "punctuation";

			case TextTags.AnonymousTypeIndicator:
			case TextTags.Text:
				return "source.cs";

			default:
				LoggingService.LogWarning ("Warning unexpected text tag: " + tag);
				return "source.cs";
			}
		}


	}
}
