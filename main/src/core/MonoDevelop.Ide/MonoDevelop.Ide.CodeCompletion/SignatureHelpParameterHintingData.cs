//
// SignatureHelpParameterHintingData.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
using MonoDevelop.Ide.Fonts;
using System;

namespace MonoDevelop.Ide.CodeCompletion
{
	/// <summary>
	/// SignatureHelpItem is internal, therefore this class has only internal visibility.
	/// </summary>
	class SignatureHelpParameterHintingData : Ide.CodeCompletion.ParameterHintingData
	{
		public SignatureHelpParameterHintingData (SignatureHelpItem item)
		{
			if (item == null)
				throw new System.ArgumentNullException (nameof (item));
			Item = item;
		}

		public SignatureHelpItem Item { get; }

		public override int ParameterCount => Item.Parameters.Length;

		public override bool IsParameterListAllowed => Item.IsVariadic;

		public override string GetParameterName (int parameter) => Item.Parameters [parameter].Name;


		public override bool Equals (object obj)
		{
			var other = obj as SignatureHelpParameterHintingData;
			if (other == null)
				return false;
			return Item.ToString () == other.Item.ToString ();
		}

		const int MaxParamColumnCount = 100;
		public override Task<TooltipInformation> CreateTooltipInformation (TextEditor editor, DocumentContext ctx, int currentParameter, bool smartWrap, CancellationToken cancelToken)
		{
			var tt = new TooltipInformation ();
			var markup = new StringBuilder ();
			var theme = SyntaxHighlightingService.GetIdeFittingTheme (DefaultSourceEditorOptions.Instance.GetEditorTheme ());

			markup.AppendTaggedText (theme, Item.PrefixDisplayParts);
			var prefixLength = Item.PrefixDisplayParts.GetFullText ().Length;
			var prefixSpacer = new string (' ', prefixLength);
			var col = 0;
			var separatorLength = Item.SeparatorDisplayParts.GetFullText ().Length;

			for (int i = 0; i < Item.Parameters.Length; i++) {

				if (i > 0) {
					markup.AppendTaggedText (theme, Item.SeparatorDisplayParts);
					col += separatorLength;
				}

				if (col > MaxParamColumnCount) {
					markup.AppendLine ();
					markup.Append (prefixSpacer);
					col = 0;
				}

				var p = Item.Parameters [i];
				if (i == currentParameter)
					markup.Append ("<b>");
				if (p.IsOptional) {
					markup.Append ("[");
					col++;
				}
				markup.AppendTaggedText (theme, p.DisplayParts);
				col += p.DisplayParts.GetFullText ().Length;
				if (p.IsOptional) {
					markup.Append ("]");
					col++;
				}
				if (i == currentParameter)
					markup.Append ("</b>");
			}

			markup.AppendTaggedText (theme, Item.SuffixDisplayParts);

			List<TaggedText> documentation;
			try {
				documentation = Item.DocumentationFactory (cancelToken).ToList ();
			} catch (Exception e) {
				documentation = EmptyTaggedTextList;
				LoggingService.LogError ("Error while getting parameter documentation", e);
			}

			if (documentation.Count > 0) {
				markup.Append ("<span font='" + FontService.SansFontName + "' size='small'>");
				markup.AppendLine ();
				markup.AppendLine ();
				markup.AppendTaggedText (theme, documentation, 0, MaxParamColumnCount);
				markup.Append ("</span>");
			}

			if (currentParameter >= 0 && currentParameter < Item.Parameters.Length) {
				var p = Item.Parameters [currentParameter];
				if (p.DocumentationFactory != null) {
					try {
						documentation = p.DocumentationFactory (cancelToken).ToList ();
					} catch (Exception e) {
						documentation = EmptyTaggedTextList;
						LoggingService.LogError ("Error while getting parameter documentation", e);
					}
					if (documentation.Count > 0) {
						markup.Append ("<span font='" + FontService.SansFontName + "' size='small'>");
						markup.AppendLine ();
						markup.AppendLine ();
						markup.Append ("<b>");
						markup.Append (p.Name);
						markup.Append (": </b>");
						markup.AppendTaggedText (theme, documentation, p.Name.Length + 2, MaxParamColumnCount);
						markup.Append ("</span>");
					}
				}
			}

			tt.SignatureMarkup = markup.ToString ();
			return Task.FromResult (tt);
		}

		static readonly List<TaggedText> EmptyTaggedTextList = new List<TaggedText> ();
	}
}