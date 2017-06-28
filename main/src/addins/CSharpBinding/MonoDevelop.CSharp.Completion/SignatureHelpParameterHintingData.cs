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
			markup.AppendTaggedText (theme, Item.PrefixDisplayParts);
			for (int i = 0; i < Item.Parameters.Length; i++) {
				if (i > 0) {
					markup.AppendTaggedText (theme, Item.SeparatorDisplayParts);
				}
				var p = Item.Parameters [i];
				if (i == currentParameter)
					markup.Append ("<b>");
				markup.AppendTaggedText (theme, p.DisplayParts);
				if (i == currentParameter)
					markup.Append ("</b>");
			}
			markup.AppendTaggedText (theme, Item.SuffixDisplayParts);

			var documentation = Item.DocumentationFactory (cancelToken).ToList ();
			if (documentation.Count > 0) {
				var summaryMarkup = new StringBuilder ();
				summaryMarkup.AppendTaggedText (theme, documentation);
				tt.SummaryMarkup = summaryMarkup.ToString ();
			}

			tt.SignatureMarkup = markup.ToString ();
			return Task.FromResult (tt);
		}





	}
}
