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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using Mono.TextEditor;

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
//			indentationCombobox.InsertText (3, GettextCatalog.GetString ("Virtual"));

			controlLeftRightCombobox.InsertText (0, GettextCatalog.GetString ("MonoDevelop"));
			controlLeftRightCombobox.InsertText (1, GettextCatalog.GetString ("Emacs"));
			controlLeftRightCombobox.InsertText (2, GettextCatalog.GetString ("SharpDevelop"));
			
			autoInsertBraceCheckbutton.Toggled += HandleAutoInsertBraceCheckbuttonToggled;
		}
		
		public virtual Gtk.Widget CreatePanelWidget ()
		{
			//			this.autoInsertTemplateCheckbutton.Active  = DefaultSourceEditorOptions.Options.AutoInsertTemplates;
			this.autoInsertBraceCheckbutton.Active = DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket;
			this.smartSemicolonPlaceCheckbutton.Active = DefaultSourceEditorOptions.Instance.SmartSemicolonPlacement;
			
			this.tabAsReindentCheckbutton.Active = DefaultSourceEditorOptions.Instance.TabIsReindent;
			this.indentationCombobox.Active = (int)DefaultSourceEditorOptions.Instance.IndentStyle;
			this.controlLeftRightCombobox.Active = (int)DefaultSourceEditorOptions.Instance.ControlLeftRightMode;
			this.useViModesCheck.Active = DefaultSourceEditorOptions.Instance.UseViModes;
			this.checkbuttonOnTheFlyFormatting.Active = DefaultSourceEditorOptions.Instance.OnTheFlyFormatting;
			
			checkbuttonFormatOnSave.Active = PropertyService.Get ("AutoFormatDocumentOnSave", false);
			checkbuttonAutoSetSearchPatternCasing.Active = PropertyService.Get ("AutoSetPatternCasing", false);
			HandleAutoInsertBraceCheckbuttonToggled (null, null);
			return this;
		}

		void HandleAutoInsertBraceCheckbuttonToggled (object sender, EventArgs e)
		{
			this.smartSemicolonPlaceCheckbutton.Sensitive = this.autoInsertBraceCheckbutton.Active;
		}
		
		public virtual void ApplyChanges ()
		{
			//DefaultSourceEditorOptions.Options.AutoInsertTemplates = this.autoInsertTemplateCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket = this.autoInsertBraceCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.SmartSemicolonPlacement = this.smartSemicolonPlaceCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.IndentStyle = (IndentStyle)this.indentationCombobox.Active;
			DefaultSourceEditorOptions.Instance.TabIsReindent = this.tabAsReindentCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.ControlLeftRightMode = (ControlLeftRightMode)this.controlLeftRightCombobox.Active;
			DefaultSourceEditorOptions.Instance.UseViModes = this.useViModesCheck.Active;
			DefaultSourceEditorOptions.Instance.OnTheFlyFormatting = this.checkbuttonOnTheFlyFormatting.Active;
			PropertyService.Set ("AutoSetPatternCasing", checkbuttonAutoSetSearchPatternCasing.Active);
			PropertyService.Set ("AutoFormatDocumentOnSave", checkbuttonFormatOnSave.Active);
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
