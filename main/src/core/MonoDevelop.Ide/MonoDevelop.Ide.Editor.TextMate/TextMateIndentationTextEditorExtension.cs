//
// TextMateIndentationTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using System;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.Ide.Editor.TextMate
{
	public class TextMateIndentationTextEditorExtension : TextEditorExtension
	{
        protected override void Initialize ()
		{
			base.Initialize ();

            Editor.MimeTypeChanged += Editor_MimeTypeChanged;
			Editor_MimeTypeChanged (this, EventArgs.Empty);
		}

		void Editor_MimeTypeChanged (object sender, EventArgs e)
		{
			var engine = new TextMateIndentationTracker (Editor);
            if (engine.DocumentIndentEngine.IsValid) {
                Editor.IndentationTracker = engine;
            }
        }

        public override void Dispose ()
		{
			Editor.MimeTypeChanged -= Editor_MimeTypeChanged;

			base.Dispose ();
		}

		public override bool KeyPress (KeyDescriptor descriptor)
		{
            bool isActive = Editor.IndentationTracker != null && (Editor.IndentationTracker.SupportedFeatures & IndentationTrackerFeatures.CustomIndentationEngine) != IndentationTrackerFeatures.CustomIndentationEngine;
			if (!DefaultSourceEditorOptions.Instance.OnTheFlyFormatting)
				return base.KeyPress (descriptor);
            if (isActive && descriptor.SpecialKey == SpecialKey.Return) {
				if (Editor.Options.IndentStyle == IndentStyle.Virtual) {
					if (Editor.GetLine (Editor.CaretLine).Length == 0)
						Editor.CaretColumn = Editor.GetVirtualIndentationColumn (Editor.CaretLine);
				} else {
					using (var undo = Editor.OpenUndoGroup ()) {
						DoReSmartIndent ();
						var result = base.KeyPress (descriptor);
						DoReSmartIndent ();
						return result;
					}
				}
			}
			return base.KeyPress (descriptor);
		}

		void DoReSmartIndent ()
		{
			if (DefaultSourceEditorOptions.Instance.IndentStyle == IndentStyle.Auto) {
				Editor.FixVirtualIndentation ();
				return;
			}
			Editor.EnsureCaretIsNotVirtual ();
			var indent = Editor.GetVirtualIndentationString (Editor.CaretLine);
			var line = Editor.GetLine (Editor.CaretLine);
			var actualIndent = line.GetIndentation (Editor);
			if (actualIndent != indent) {
				Editor.ReplaceText (line.Offset, actualIndent.Length, indent);
			}
			Editor.FixVirtualIndentation ();
		}
	}
}
