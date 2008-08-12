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
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Gui;

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
			this.showLineNumbersCheckbutton.Active = SourceEditorOptions.Options.ShowLineNumberMargin;
			this.underlineErrorsCheckbutton.Active = SourceEditorOptions.Options.UnderlineErrors;
			this.highlightMatchingBracketCheckbutton.Active = SourceEditorOptions.Options.HighlightMatchingBracket;
			this.highlightCurrentLineCheckbutton.Active = SourceEditorOptions.Options.HighlightCaretLine;
			this.showRulerCheckbutton.Active = SourceEditorOptions.Options.ShowRuler;
			this.rulerColSpinbutton.Value = SourceEditorOptions.Options.RulerColumn;
			this.showInvLinesCheckbutton.Active = SourceEditorOptions.Options.ShowInvalidLines;
			this.showSpacesCheckbutton.Active = SourceEditorOptions.Options.ShowSpaces;
			this.showTabsCheckbutton.Active = SourceEditorOptions.Options.ShowTabs;
			this.showEolCheckbutton.Active = SourceEditorOptions.Options.ShowEolMarkers;
			return this;
		}
		
		public virtual void ApplyChanges ()
		{
			SourceEditorOptions.Options.ShowLineNumberMargin = this.showLineNumbersCheckbutton.Active;
			SourceEditorOptions.Options.UnderlineErrors = this.underlineErrorsCheckbutton.Active;
			SourceEditorOptions.Options.HighlightMatchingBracket = this.highlightMatchingBracketCheckbutton.Active;
			SourceEditorOptions.Options.HighlightCaretLine = this.highlightCurrentLineCheckbutton.Active;
			SourceEditorOptions.Options.ShowRuler = this.showRulerCheckbutton.Active;
			SourceEditorOptions.Options.RulerColumn = (int)this.rulerColSpinbutton.Value;
			SourceEditorOptions.Options.ShowInvalidLines = this.showInvLinesCheckbutton.Active;
			SourceEditorOptions.Options.ShowSpaces = this.showSpacesCheckbutton.Active;
			SourceEditorOptions.Options.ShowTabs = this.showTabsCheckbutton.Active;
			SourceEditorOptions.Options.ShowEolMarkers = this.showEolCheckbutton.Active;
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
