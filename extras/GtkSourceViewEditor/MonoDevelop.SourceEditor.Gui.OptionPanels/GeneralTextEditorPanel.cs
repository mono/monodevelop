//  GeneralTextEditorPanel.cs
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
using System.Text;
using System.Collections;

using Gtk;
using Pango;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;

using MonoDevelop.Components;

using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.SourceEditor.Gui.OptionPanels
{
	/// <summary>
	/// General texteditor options panelS.
	/// </summary>
	class GeneralTextEditorPanel : OptionsPanel
	{
		GeneralTextEditorPanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			widget = new GeneralTextEditorPanelWidget ();
			return widget;
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	
		class GeneralTextEditorPanelWidget : GladeWidgetExtract 
		{	
			[Glade.Widget] CheckButton enableCodeCompletionCheckBox;
			[Glade.Widget] CheckButton enableAutoCorrectionCheckBox;
			[Glade.Widget] CheckButton enableFoldingCheckBox;
			[Glade.Widget] CheckButton showClassMethodCheckBox;
			[Glade.Widget] FontButton fontNameDisplayTextBox;
			[Glade.Widget] VBox encodingBox;
			[Glade.Widget] RadioButton use_monospace;
			[Glade.Widget] RadioButton use_sans;
			[Glade.Widget] RadioButton use_cust;
			
			public GeneralTextEditorPanelWidget () :  base ("EditorBindings.glade", "GeneralTextEditorPanel")
			{
				encodingBox.Destroy(); // this is a really dirty way of hiding encodingBox, but Hide() doesn't work
				enableCodeCompletionCheckBox.Active = TextEditorProperties.EnableCodeCompletion;
				enableAutoCorrectionCheckBox.Active = TextEditorProperties.EnableAutoCorrection;
 				enableFoldingCheckBox.Active = TextEditorProperties.EnableFolding;
 				
 				showClassMethodCheckBox.Active = TextEditorProperties.ShowClassBrowser;
 				
				switch (TextEditorProperties.FontType) {
				case EditorFontType.DefaultMonospace:
					use_monospace.Active = true;
					fontNameDisplayTextBox.Sensitive = false;
					break;
				case EditorFontType.DefaultSans:
					use_sans.Active = true;
					fontNameDisplayTextBox.Sensitive = false;
					break;
				default:
					use_cust.Active = true;
					fontNameDisplayTextBox.FontName = TextEditorProperties.FontName;
					fontNameDisplayTextBox.Sensitive = true;
					break;
				}
				
				use_monospace.Toggled += new EventHandler (ItemToggled);
				use_sans.Toggled += new EventHandler (ItemToggled);
				use_cust.Toggled += new EventHandler (ItemToggled);
				
// 				encVBox.TextWithMnemonic = StringParserService.Parse(
// 					"${res:Dialog.Options.IDEOptions.TextEditor.General.FontGroupBox.FileEncodingLabel}");

// 				Menu m = new Menu ();
// 				foreach (String name in CharacterEncodings.Names) {
// 					m.Append (new MenuItem (name));
// 				}
// 				textEncodingComboBox.Menu = m;
				
// 				int i = 0;
// 				try {
// 					Console.WriteLine("Getting encoding Property");
// 					i = CharacterEncodings.GetEncodingIndex(
// 						(Int32)((Properties) CustomizationObject).Get("Encoding", encoding));
// 				} catch {
// 					Console.WriteLine("Getting encoding Default");
// 					i = CharacterEncodings.GetEncodingIndex(encoding);
// 				}
				
// 				selectedIndex = i;
// 				encoding = CharacterEncodings.GetEncodingByIndex(i).CodePage;

// 				textEncodingComboBox.Changed += new EventHandler (OnOptionChanged);
			}

			public void Store ()
			{
				TextEditorProperties.EnableCodeCompletion = enableCodeCompletionCheckBox.Active;
				TextEditorProperties.EnableAutoCorrection = enableAutoCorrectionCheckBox.Active;
				TextEditorProperties.EnableFolding = enableFoldingCheckBox.Active;
				TextEditorProperties.ShowClassBrowser = showClassMethodCheckBox.Active;
				
				if (use_monospace.Active) {
					TextEditorProperties.FontType = EditorFontType.DefaultMonospace;
				} else if (use_sans.Active) {
					TextEditorProperties.FontType = EditorFontType.DefaultSans;
				} else {
					TextEditorProperties.FontName = fontNameDisplayTextBox.FontName;
				}
				
// 				Console.WriteLine (CharacterEncodings.GetEncodingByIndex (selectedIndex).CodePage);
// 				((Properties) CustomizationObject).Set (
// 					"Encoding",CharacterEncodings.GetEncodingByIndex (selectedIndex).CodePage);
			}
			
 			void ItemToggled (object o, EventArgs args)
			{
				fontNameDisplayTextBox.Sensitive = use_cust.Active;
			}
		}
	}
}

