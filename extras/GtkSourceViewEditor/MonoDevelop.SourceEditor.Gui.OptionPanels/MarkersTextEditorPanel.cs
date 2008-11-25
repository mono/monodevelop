//  MarkersTextEditorPanel.cs
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

using Gtk;

using MonoDevelop.Components;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;

using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.SourceEditor.Gui.OptionPanels
{
	/// <summary>
	/// Summary description for Form9.
	/// </summary>

	class MarkersTextEditorPanel : OptionsPanel
	{
		MarkersTextEditorPanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			// set up the form controls instance
			widget = new MarkersTextEditorPanelWidget ();
			return widget;	
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}

		class MarkersTextEditorPanelWidget : GladeWidgetExtract 
		{
			// Gtk Controls
			[Glade.Widget] CheckButton showLineNumberCheckBox;
			[Glade.Widget] CheckButton showBracketHighlighterCheckBox;
			[Glade.Widget] CheckButton showVRulerCheckBox;
			[Glade.Widget] CheckButton highlightCurrentLineCheckBox;
			[Glade.Widget] CheckButton highlightSpacesCheckBox;
			[Glade.Widget] CheckButton highlightTabsCheckBox;
			[Glade.Widget] CheckButton highlightNewlinesCheckBox;
			[Glade.Widget] SpinButton  vRulerRowTextBox;
			[Glade.Widget] ComboBox    wrapModeComboBox;
			
			public MarkersTextEditorPanelWidget () :  
				base ("EditorBindings.glade", "MarkersTextEditorPanel")
			{
				showLineNumberCheckBox.Active = TextEditorProperties.ShowLineNumbers;
				showBracketHighlighterCheckBox.Active = TextEditorProperties.ShowMatchingBracket;
				
				highlightCurrentLineCheckBox.Active = TextEditorProperties.HighlightCurrentLine;

				highlightSpacesCheckBox.Active = TextEditorProperties.HighlightSpaces;
				highlightTabsCheckBox.Active = TextEditorProperties.HighlightTabs;
				highlightNewlinesCheckBox.Active = TextEditorProperties.HighlightNewlines;				
				
				showVRulerCheckBox.Active = TextEditorProperties.ShowVerticalRuler;
				vRulerRowTextBox.Value = TextEditorProperties.VerticalRulerRow;
				
				wrapModeComboBox.Active = (int) TextEditorProperties.WrapMode;
			}

			public void Store ()
			{
				TextEditorProperties.ShowLineNumbers = showLineNumberCheckBox.Active;
				TextEditorProperties.ShowMatchingBracket = showBracketHighlighterCheckBox.Active;
				TextEditorProperties.ShowVerticalRuler = showVRulerCheckBox.Active;
				TextEditorProperties.HighlightCurrentLine = highlightCurrentLineCheckBox.Active;				
				TextEditorProperties.HighlightSpaces = highlightSpacesCheckBox.Active;
				TextEditorProperties.HighlightTabs = highlightTabsCheckBox.Active;
				TextEditorProperties.HighlightNewlines = highlightNewlinesCheckBox.Active;
				try {
					TextEditorProperties.VerticalRulerRow = (int) vRulerRowTextBox.Value;
				} 
				catch { }
				try {
					TextEditorProperties.WrapMode = (WrapMode) wrapModeComboBox.Active;
				} 
				catch { }
			}
		}
	}
}
