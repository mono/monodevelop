//
// CompletionOptionsPanel.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	[System.ComponentModel.ToolboxItem(false)]
	partial class CompletionOptionsPanel : Gtk.Bin, IOptionsPanel
	{
		public CompletionOptionsPanel ()
		{
			this.Build ();
			insertParenthesesCheckbutton.Toggled += InsertParensToggled;
			autoCodeCompletionCheckbutton.Toggled += AutomaticCompletionToggled;
			includeKeywordsCheckbutton.Visible = includeCodeSnippetsCheckbutton.Visible = false;
			automaticCompletionModeCheckbutton.Sensitive = false;
			hbox4.Visible = hbox5.Visible = false;
		}

		void InsertParensToggled (object sender, EventArgs e)
		{
			openingRadiobutton.Sensitive = bothRadiobutton.Sensitive = insertParenthesesCheckbutton.Active;
		}

		#region IOptionsPanel implementation

		void IOptionsPanel.Initialize (OptionsDialog dialog, object dataObject)
		{
		}

		Control IOptionsPanel.CreatePanelWidget ()
		{
			autoCodeCompletionCheckbutton.Active = DefaultSourceEditorOptions.Instance.EnableAutoCodeCompletion;
			showImportsCheckbutton.Active = IdeApp.Preferences.AddImportedItemsToCompletionList;
			includeKeywordsCheckbutton.Active = IdeApp.Preferences.IncludeKeywordsInCompletionList;
			includeCodeSnippetsCheckbutton.Active = IdeApp.Preferences.IncludeCodeSnippetsInCompletionList;
			automaticCompletionModeCheckbutton.Active = !IdeApp.Preferences.ForceSuggestionMode;

			insertParenthesesCheckbutton.Visible = false;
			openingRadiobutton.Visible = false;
			bothRadiobutton.Visible = false;

			InsertParensToggled (this, EventArgs.Empty);
			AutomaticCompletionToggled (this, EventArgs.Empty);
			return this;
		}

		void AutomaticCompletionToggled (object sender, EventArgs e)
		{
			includeKeywordsCheckbutton.Sensitive = includeCodeSnippetsCheckbutton.Sensitive = !autoCodeCompletionCheckbutton.Active;
			automaticCompletionModeCheckbutton.Sensitive = autoCodeCompletionCheckbutton.Active;
		}

		bool IOptionsPanel.IsVisible ()
		{
			return true;
		}

		bool IOptionsPanel.ValidateChanges ()
		{
			return true;
		}

		void IOptionsPanel.ApplyChanges ()
		{
			DefaultSourceEditorOptions.Instance.EnableAutoCodeCompletion = autoCodeCompletionCheckbutton.Active;
			IdeApp.Preferences.AddImportedItemsToCompletionList.Value = showImportsCheckbutton.Active;
			IdeApp.Preferences.IncludeKeywordsInCompletionList.Value = includeKeywordsCheckbutton.Active;
			IdeApp.Preferences.IncludeCodeSnippetsInCompletionList.Value = includeCodeSnippetsCheckbutton.Active;
			IdeApp.Preferences.ForceSuggestionMode.Value = !automaticCompletionModeCheckbutton.Active;
		}

		#endregion
	}
}

