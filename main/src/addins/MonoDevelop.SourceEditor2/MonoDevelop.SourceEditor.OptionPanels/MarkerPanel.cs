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
			this.showLineNumbersCheckbutton.Active = DefaultSourceEditorOptions.Instance.ShowLineNumberMargin;
			this.underlineErrorsCheckbutton.Active = DefaultSourceEditorOptions.Instance.UnderlineErrors;
			this.highlightMatchingBracketCheckbutton.Active = DefaultSourceEditorOptions.Instance.HighlightMatchingBracket;
			this.highlightCurrentLineCheckbutton.Active = DefaultSourceEditorOptions.Instance.HighlightCaretLine;
			this.showRulerCheckbutton.Active = DefaultSourceEditorOptions.Instance.ShowRuler;
			this.showInvLinesCheckbutton.Active = DefaultSourceEditorOptions.Instance.ShowInvalidLines;
			this.showSpacesCheckbutton.Active = DefaultSourceEditorOptions.Instance.ShowSpaces;
			this.showTabsCheckbutton.Active = DefaultSourceEditorOptions.Instance.ShowTabs;
			this.showEolCheckbutton.Active = DefaultSourceEditorOptions.Instance.ShowEolMarkers;
			return this;
		}
		
		public virtual void ApplyChanges ()
		{
			DefaultSourceEditorOptions.Instance.ShowLineNumberMargin = this.showLineNumbersCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.UnderlineErrors = this.underlineErrorsCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.HighlightMatchingBracket = this.highlightMatchingBracketCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.HighlightCaretLine = this.highlightCurrentLineCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.ShowRuler = this.showRulerCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.ShowInvalidLines = this.showInvLinesCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.ShowSpaces = this.showSpacesCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.ShowTabs = this.showTabsCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.ShowEolMarkers = this.showEolCheckbutton.Active;
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
