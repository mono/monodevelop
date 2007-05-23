// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Text;
using System.Collections;

using Gtk;
using Pango;

using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Gui.Dialogs;

using MonoDevelop.Components;

using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.SourceEditor.Gui.OptionPanels
{
	/// <summary>
	/// General texteditor options panelS.
	/// </summary>
	public class GeneralTextEditorPanel : AbstractOptionPanel
	{
		GeneralTextEditorPanelWidget widget;
		
		public override void LoadPanelContents()
		{
			Add (widget = new GeneralTextEditorPanelWidget ());
		}
		
		public override bool StorePanelContents()
		{
			widget.Store ();
			return true;
		}
	
		class GeneralTextEditorPanelWidget : GladeWidgetExtract 
		{	
			[Glade.Widget] CheckButton enableCodeCompletionCheckBox;
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
// 						(Int32)((IProperties) CustomizationObject).GetProperty("Encoding", encoding));
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
// 				((IProperties) CustomizationObject).SetProperty (
// 					"Encoding",CharacterEncodings.GetEncodingByIndex (selectedIndex).CodePage);
			}
			
 			void ItemToggled (object o, EventArgs args)
			{
				fontNameDisplayTextBox.Sensitive = use_cust.Active;
			}
		}
	}
}

