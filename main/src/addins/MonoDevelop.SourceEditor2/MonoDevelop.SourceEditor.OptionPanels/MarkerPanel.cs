// MarkerPanel.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;
using Mono.TextEditor;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	
	public partial class MarkerPanel : Gtk.Bin, IOptionsPanel
	{
		public MarkerPanel()
		{
			this.Build();
		}
		
		public virtual Gtk.Widget CreatePanelWidget ()
		{
			this.showLineNumbersCheckbutton.Active = DefaultSourceEditorOptions.Instance.ShowLineNumberMargin;
			this.underlineErrorsCheckbutton.Active = DefaultSourceEditorOptions.Instance.UnderlineErrors;
			this.highlightMatchingBracketCheckbutton.Active = DefaultSourceEditorOptions.Instance.HighlightMatchingBracket;
			this.highlightCurrentLineCheckbutton.Active = DefaultSourceEditorOptions.Instance.HighlightCaretLine;
			this.showRulerCheckbutton.Active = DefaultSourceEditorOptions.Instance.ShowRuler;
			this.enableAnimationCheckbutton1.Active = DefaultSourceEditorOptions.Instance.EnableAnimations;
			this.enableHighlightUsagesCheckbutton.Active = DefaultSourceEditorOptions.Instance.EnableHighlightUsages;
			this.drawIndentMarkersCheckbutton.Active = DefaultSourceEditorOptions.Instance.DrawIndentationMarkers;
			this.showWhitespacesCombobox.AppendText (GettextCatalog.GetString ("Never"));
			this.showWhitespacesCombobox.AppendText (GettextCatalog.GetString ("Selection"));
			this.showWhitespacesCombobox.AppendText (GettextCatalog.GetString ("Always"));
			this.showWhitespacesCombobox.Active = (int)DefaultSourceEditorOptions.Instance.ShowWhitespaces;
			this.checkbuttonSpaces.Active = DefaultSourceEditorOptions.Instance.IncludeWhitespaces.HasFlag (IncludeWhitespaces.Space);
			this.checkbuttonTabs.Active = DefaultSourceEditorOptions.Instance.IncludeWhitespaces.HasFlag (IncludeWhitespaces.Tab);
			this.checkbuttonLineEndings.Active = DefaultSourceEditorOptions.Instance.IncludeWhitespaces.HasFlag (IncludeWhitespaces.LineEndings);
			this.enableQuickDiffCheckbutton.Active = DefaultSourceEditorOptions.Instance.EnableQuickDiff;
			return this;
		}
		
		public virtual void ApplyChanges ()
		{
			DefaultSourceEditorOptions.Instance.ShowLineNumberMargin = this.showLineNumbersCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.UnderlineErrors = this.underlineErrorsCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.HighlightMatchingBracket = this.highlightMatchingBracketCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.HighlightCaretLine = this.highlightCurrentLineCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.ShowRuler = this.showRulerCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.EnableAnimations = this.enableAnimationCheckbutton1.Active;
			DefaultSourceEditorOptions.Instance.EnableHighlightUsages = this.enableHighlightUsagesCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.DrawIndentationMarkers = this.drawIndentMarkersCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.ShowWhitespaces = (ShowWhitespaces) this.showWhitespacesCombobox.Active;
			DefaultSourceEditorOptions.Instance.EnableQuickDiff = this.enableQuickDiffCheckbutton.Active;

			var include = IncludeWhitespaces.None;
			if (checkbuttonSpaces.Active)
				include |= IncludeWhitespaces.Space;
			if (checkbuttonTabs.Active)
				include |= IncludeWhitespaces.Tab;
			if (checkbuttonLineEndings.Active)
				include |= IncludeWhitespaces.LineEndings;
			DefaultSourceEditorOptions.Instance.IncludeWhitespaces = include;
		}

		public void Initialize (OptionsDialog dialog, object dataObject)
		{
		}

		public bool IsVisible ()
		{
			return true;
		}

		public bool ValidateChanges ()
		{
			return true;
		}
	}
	
}
