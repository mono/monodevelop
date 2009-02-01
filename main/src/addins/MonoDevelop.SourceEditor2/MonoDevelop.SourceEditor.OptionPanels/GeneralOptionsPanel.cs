// GeneralOptionsPanel.cs
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
	partial class GeneralOptionsPanel : Gtk.Bin, IOptionsPanel
	{
		public GeneralOptionsPanel()
		{
			this.Build();
			this.codeCompletioncheckbutton.Toggled += HandleCodeCompletioncheckbuttonToggled;
			this.radiobutton1.Toggled += CheckFontSelection;
			this.radiobutton2.Toggled += CheckFontSelection;
		}

		void HandleCodeCompletioncheckbuttonToggled (object sender, EventArgs e)
		{
			this.autoCodeCompletionCheckbutton.Sensitive = this.codeCompletioncheckbutton.Active;
		}
		
		void CheckFontSelection (object sender, EventArgs args)
		{
			this.fontselection.Sensitive = this.radiobutton2.Active;
		}
		
		public virtual Gtk.Widget CreatePanelWidget ()
		{
			this.codeCompletioncheckbutton.Active = DefaultSourceEditorOptions.Instance.EnableCodeCompletion;
			this.quickFinderCheckbutton.Active    = DefaultSourceEditorOptions.Instance.EnableQuickFinder;
			this.foldingCheckbutton.Active        = DefaultSourceEditorOptions.Instance.ShowFoldMargin;
			this.foldregionsCheckbutton.Active    = DefaultSourceEditorOptions.Instance.DefaultRegionsFolding;
			this.foldCommentsCheckbutton.Active   = DefaultSourceEditorOptions.Instance.DefaultCommentFolding;
			this.autoCodeCompletionCheckbutton.Active    = DefaultSourceEditorOptions.Instance.EnableAutoCodeCompletion;
			this.radiobutton1.Active              = DefaultSourceEditorOptions.Instance.EditorFontType == EditorFontType.DefaultMonospace;
			this.radiobutton2.Active              = DefaultSourceEditorOptions.Instance.EditorFontType == EditorFontType.UserSpecified;
			this.fontselection.FontName           = DefaultSourceEditorOptions.Instance.FontName;
			CheckFontSelection (this, EventArgs.Empty);
			HandleCodeCompletioncheckbuttonToggled (this, EventArgs.Empty);
			return this;
		}
		
		public virtual void ApplyChanges ()
		{
			DefaultSourceEditorOptions.Instance.EnableCodeCompletion = this.codeCompletioncheckbutton.Active;
			DefaultSourceEditorOptions.Instance.EnableQuickFinder    = this.quickFinderCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.ShowFoldMargin       = this.foldingCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.EditorFontType       = this.radiobutton1.Active ? EditorFontType.DefaultMonospace : EditorFontType.UserSpecified;
			DefaultSourceEditorOptions.Instance.FontName             = this.fontselection.FontName;
			DefaultSourceEditorOptions.Instance.DefaultRegionsFolding = this.foldregionsCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.DefaultCommentFolding = this.foldCommentsCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.EnableAutoCodeCompletion = this.autoCodeCompletionCheckbutton.Active;
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
