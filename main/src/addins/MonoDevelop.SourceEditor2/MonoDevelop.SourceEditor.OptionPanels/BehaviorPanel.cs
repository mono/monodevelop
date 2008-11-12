// BehaviorPanel.cs
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
using MonoDevelop.Core;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public partial class BehaviorPanel : Gtk.Bin, IOptionsPanel
	{
		public BehaviorPanel ()
		{
			this.Build ();
			indentationCombobox.InsertText (0, GettextCatalog.GetString ("None"));
			indentationCombobox.InsertText (1, GettextCatalog.GetString ("Automatic"));
			indentationCombobox.InsertText (2, GettextCatalog.GetString ("Smart"));

			controlLeftRightCombobox.InsertText (0, GettextCatalog.GetString ("MonoDevelop"));
			controlLeftRightCombobox.InsertText (1, GettextCatalog.GetString ("Emacs"));
			controlLeftRightCombobox.InsertText (2, GettextCatalog.GetString ("SharpDevelop"));
		}
		
		public virtual Gtk.Widget CreatePanelWidget ()
		{
//			this.autoInsertTemplateCheckbutton.Active  = SourceEditorOptions.Options.AutoInsertTemplates;
			this.convertTabsToSpacesCheckbutton.Active = SourceEditorOptions.Options.TabsToSpaces;
			this.autoInsertBraceCheckbutton.Active = SourceEditorOptions.Options.AutoInsertMatchingBracket;
			this.tabAsReindentCheckbutton.Active = SourceEditorOptions.Options.TabIsReindent;
			this.indentationCombobox.Active = (int)SourceEditorOptions.Options.IndentStyle;
			this.indentAndTabSizeSpinbutton.Value = SourceEditorOptions.Options.TabSize;
			this.removeTrailingWhitespacesCheckbutton.Active  = SourceEditorOptions.Options.RemoveTrailingWhitespaces;
			this.tabsAfterNonTabsCheckbutton.Active  = SourceEditorOptions.Options.AllowTabsAfterNonTabs;
			this.controlLeftRightCombobox.Active  = (int)SourceEditorOptions.Options.ControlLeftRightMode;
			this.useViModesCheck.Active = SourceEditorOptions.Options.UseViModes;
			return this;
		}
		
		public virtual void ApplyChanges ()
		{
			//SourceEditorOptions.Options.AutoInsertTemplates = this.autoInsertTemplateCheckbutton.Active;
			SourceEditorOptions.Options.TabsToSpaces = this.convertTabsToSpacesCheckbutton.Active;
			SourceEditorOptions.Options.AutoInsertMatchingBracket = this.autoInsertBraceCheckbutton.Active;
			SourceEditorOptions.Options.IndentStyle = (MonoDevelop.Ide.Gui.Content.IndentStyle)this.indentationCombobox.Active;
			SourceEditorOptions.Options.TabSize = (int)this.indentAndTabSizeSpinbutton.Value;
			SourceEditorOptions.Options.TabIsReindent = this.tabAsReindentCheckbutton.Active;
			SourceEditorOptions.Options.RemoveTrailingWhitespaces = this.removeTrailingWhitespacesCheckbutton.Active;
			SourceEditorOptions.Options.AllowTabsAfterNonTabs = this.tabsAfterNonTabsCheckbutton.Active;
			SourceEditorOptions.Options.ControlLeftRightMode = (ControlLeftRightMode)this.controlLeftRightCombobox.Active;
			SourceEditorOptions.Options.UseViModes = this.useViModesCheck.Active;
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
