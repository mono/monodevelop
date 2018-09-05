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
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using Mono.TextEditor;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	partial class BehaviorPanel : Gtk.Bin, IOptionsPanel
	{
		public BehaviorPanel ()
		{
			this.Build ();
			indentationCombobox.InsertText (0, GettextCatalog.GetString ("None"));
			indentationCombobox.InsertText (1, GettextCatalog.GetString ("Automatic"));
			indentationCombobox.InsertText (2, GettextCatalog.GetString ("Smart"));
//			indentationCombobox.InsertText (3, GettextCatalog.GetString ("Virtual"));

			controlLeftRightCombobox.InsertText (0, GettextCatalog.GetString ("Unix"));
			controlLeftRightCombobox.InsertText (1, GettextCatalog.GetString ("Windows"));
			
			autoInsertBraceCheckbutton.Toggled += HandleAutoInsertBraceCheckbuttonToggled;

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			tabAsReindentCheckbutton.SetCommonAccessibilityAttributes ("BehaviorPanel.tabsAsReindent", "",
			                                                           GettextCatalog.GetString ("Check to use the Tab key as a reindent command"));
			smartBackspaceCheckbutton.SetCommonAccessibilityAttributes ("BehaviorPanel.smartBackspace", "",
			                                                            GettextCatalog.GetString ("Check to make Backspace remove indentation"));
			autoInsertBraceCheckbutton.SetCommonAccessibilityAttributes ("BehaviorPanel.autoInsertBrace", "",
			                                                             GettextCatalog.GetString ("Check to automatically insert braces"));
			smartSemicolonPlaceCheckbutton.SetCommonAccessibilityAttributes ("BehaviorPanel.smartSemicolon", "",
			                                                                 GettextCatalog.GetString ("Check to use smart semicolon placement"));
			checkbuttonFormatOnSave.SetCommonAccessibilityAttributes ("BehaviorPanel.formatOnSave", "",
			                                                          GettextCatalog.GetString ("Check to reformat the document when saving"));
			checkbuttonAutoSetSearchPatternCasing.SetCommonAccessibilityAttributes ("BehaviorPanel.autoSearchPattern", "",
			                                                                        GettextCatalog.GetString ("Check to automatically set the search pattern case sensitivity"));
			checkbuttonEnableSelectionSurrounding.SetCommonAccessibilityAttributes ("BehaviorPanel.enableSelectionSurrounding", "",
			                                                                        GettextCatalog.GetString ("Check to enable selection surrounding keys"));
			checkbuttonGenerateFormattingUndoStep.SetCommonAccessibilityAttributes ("BehaviorPanel.generateFormattingUndo", "",
			                                                                        GettextCatalog.GetString ("Check to add undo step for formatting changes"));
			indentationCombobox.SetCommonAccessibilityAttributes ("BehaviorPanel.indentation", label1,
			                                                      GettextCatalog.GetString ("Select the indentation mode"));
			controlLeftRightCombobox.SetCommonAccessibilityAttributes ("BehaviorPanel.wordBreakMode", label2,
			                                                           GettextCatalog.GetString ("Select the word break mode"));
		}

		public virtual Control CreatePanelWidget ()
		{
			//			this.autoInsertTemplateCheckbutton.Active  = DefaultSourceEditorOptions.Options.AutoInsertTemplates;
			autoInsertBraceCheckbutton.Active = DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket;
			smartSemicolonPlaceCheckbutton.Active = DefaultSourceEditorOptions.Instance.SmartSemicolonPlacement;
			
			tabAsReindentCheckbutton.Active = DefaultSourceEditorOptions.Instance.TabIsReindent;
			indentationCombobox.Active = Math.Min (2, (int)DefaultSourceEditorOptions.Instance.IndentStyle);
			controlLeftRightCombobox.Active = (int)DefaultSourceEditorOptions.Instance.WordNavigationStyle;
			checkbuttonGenerateFormattingUndoStep.Active = DefaultSourceEditorOptions.Instance.GenerateFormattingUndoStep;


			checkbuttonFormatOnSave.Active = PropertyService.Get ("AutoFormatDocumentOnSave", false);
			checkbuttonAutoSetSearchPatternCasing.Active = PropertyService.Get ("AutoSetPatternCasing", false);
			checkbuttonEnableSelectionSurrounding.Active = DefaultSourceEditorOptions.Instance.EnableSelectionWrappingKeys;
			smartBackspaceCheckbutton.Active = DefaultSourceEditorOptions.Instance.SmartBackspace;
			
			HandleAutoInsertBraceCheckbuttonToggled (null, null);
			return this;
		}

		void HandleAutoInsertBraceCheckbuttonToggled (object sender, EventArgs e)
		{
			smartSemicolonPlaceCheckbutton.Sensitive = autoInsertBraceCheckbutton.Active;
		}
		
		public virtual void ApplyChanges ()
		{
			//DefaultSourceEditorOptions.Options.AutoInsertTemplates = this.autoInsertTemplateCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket = autoInsertBraceCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.SmartSemicolonPlacement = smartSemicolonPlaceCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.IndentStyle = (MonoDevelop.Ide.Editor.IndentStyle)indentationCombobox.Active;
			DefaultSourceEditorOptions.Instance.TabIsReindent = tabAsReindentCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.WordNavigationStyle = (WordNavigationStyle)controlLeftRightCombobox.Active;
			DefaultSourceEditorOptions.Instance.GenerateFormattingUndoStep = checkbuttonGenerateFormattingUndoStep.Active;
			PropertyService.Set ("AutoSetPatternCasing", checkbuttonAutoSetSearchPatternCasing.Active);
			PropertyService.Set ("AutoFormatDocumentOnSave", checkbuttonFormatOnSave.Active);
			DefaultSourceEditorOptions.Instance.EnableSelectionWrappingKeys = checkbuttonEnableSelectionSurrounding.Active;
			DefaultSourceEditorOptions.Instance.SmartBackspace = smartBackspaceCheckbutton.Active;
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
