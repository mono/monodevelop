//  BehaviorTextEditorPanel.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Collections;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;

using MonoDevelop.Components;

using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.FormattingStrategy;

namespace MonoDevelop.SourceEditor.Gui.OptionPanels
{
	/// <summary>
	/// Summary description for Form8.
	/// </summary>
	public class BehaviorTextEditorPanel : OptionsPanel
	{
		BehaviorTextEditorPanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			widget = new BehaviorTextEditorPanelWidget ();
			return widget;
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
		
		sealed class BehaviorTextEditorPanelWidget : GladeWidgetExtract 
		{
			// GTK controls
			[Glade.Widget] CheckButton autoinsertCurlyBraceCheckBox;
			[Glade.Widget] CheckButton autoInsertTemplatesCheckBox;
			[Glade.Widget] CheckButton convertTabsToSpacesCheckBox;
			[Glade.Widget] RadioButton noneIndentStyle;
			[Glade.Widget] RadioButton automaticIndentStyle;
			[Glade.Widget] RadioButton smartIndentStyle;
			[Glade.Widget] SpinButton indentAndTabSizeSpinButton;
			
			public BehaviorTextEditorPanelWidget () :  
				base ("EditorBindings.glade", "BehaviorTextEditorPanel")
			{
				// FIXME: enable when it is implemented
				autoinsertCurlyBraceCheckBox.Sensitive = false;
				
				// Set up Values
				autoinsertCurlyBraceCheckBox.Active = TextEditorProperties.AutoInsertCurlyBracket;
				
				autoInsertTemplatesCheckBox.Active  = TextEditorProperties.AutoInsertTemplates;
				
				convertTabsToSpacesCheckBox.Active  = TextEditorProperties.ConvertTabsToSpaces;
				
				//FIXME: Only one of these should be selected to hold the value
				indentAndTabSizeSpinButton.Value = TextEditorProperties.TabIndent;
				
				IndentStyle currentIndent = TextEditorProperties.IndentStyle;
				if (currentIndent == IndentStyle.None) 
					noneIndentStyle.Active = true;
				else if (currentIndent == IndentStyle.Auto) 
					automaticIndentStyle.Active = true;
				else if (currentIndent == IndentStyle.Smart) 
					smartIndentStyle.Active = true;
			}

			public void Store ()
			{
				TextEditorProperties.ConvertTabsToSpaces = convertTabsToSpacesCheckBox.Active;
				TextEditorProperties.AutoInsertCurlyBracket = autoinsertCurlyBraceCheckBox.Active;
				TextEditorProperties.AutoInsertTemplates = autoInsertTemplatesCheckBox.Active;
				
				
				if (noneIndentStyle.Active)
					TextEditorProperties.IndentStyle = IndentStyle.None;
				else if (automaticIndentStyle.Active)
					TextEditorProperties.IndentStyle = IndentStyle.Auto;
				else if (smartIndentStyle.Active)
					TextEditorProperties.IndentStyle = IndentStyle.Smart;
				
				//FIXME: Only one of these should be selected to save the value
				TextEditorProperties.TabIndent = (int) indentAndTabSizeSpinButton.Value;
			}
		}
	}
}
