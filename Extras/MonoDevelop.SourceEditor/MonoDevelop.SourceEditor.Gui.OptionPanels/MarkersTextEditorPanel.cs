// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;

using MonoDevelop.SourceEditor.Properties;
using MonoDevelop.Core.Properties;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;

using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.SourceEditor.Gui.OptionPanels
{
	/// <summary>
	/// Summary description for Form9.
	/// </summary>

	public class MarkersTextEditorPanel : AbstractOptionPanel
	{
		MarkersTextEditorPanelWidget widget;
		
		public override void LoadPanelContents()
		{
			// set up the form controls instance
			Add (widget = new MarkersTextEditorPanelWidget ());	
		}
		
		public override bool StorePanelContents()
		{
			widget.Store ();
			return true;
		}

		class MarkersTextEditorPanelWidget : GladeWidgetExtract 
		{
			// Gtk Controls
			[Glade.Widget] CheckButton showLineNumberCheckBox;
			[Glade.Widget] CheckButton showBracketHighlighterCheckBox;
			[Glade.Widget] CheckButton showErrorsCheckBox;
			[Glade.Widget] CheckButton showVRulerCheckBox;
			[Glade.Widget] CheckButton highlightCurrentLineCheckBox;
			[Glade.Widget] CheckButton showControlCharactersCheckBox;
			[Glade.Widget] SpinButton  vRulerRowTextBox;
			[Glade.Widget] ComboBox    wrapModeComboBox;
			
			public MarkersTextEditorPanelWidget () :  
				base ("EditorBindings.glade", "MarkersTextEditorPanel")
			{
				showLineNumberCheckBox.Active = TextEditorProperties.ShowLineNumbers;
				showBracketHighlighterCheckBox.Active = TextEditorProperties.ShowMatchingBracket;
				showErrorsCheckBox.Active = TextEditorProperties.UnderlineErrors;
				showControlCharactersCheckBox.Active = TextEditorProperties.ShowControlCharacters;
				
				highlightCurrentLineCheckBox.Active = TextEditorProperties.HighlightCurrentLine;
				highlightCurrentLineCheckBox.Sensitive = MonoDevelop.SourceEditor.Gui.SourceEditorView.HighlightCurrentLineSupported;
				
				showVRulerCheckBox.Active = TextEditorProperties.ShowVerticalRuler;
				vRulerRowTextBox.Value = TextEditorProperties.VerticalRulerRow;
				
				wrapModeComboBox.Active = (int) TextEditorProperties.WrapMode;
				
				// FIXME: re-enable when implemented
				showErrorsCheckBox.Sensitive = false;
			}

			public void Store ()
			{
				TextEditorProperties.ShowLineNumbers = showLineNumberCheckBox.Active;
				TextEditorProperties.ShowMatchingBracket = showBracketHighlighterCheckBox.Active;
				TextEditorProperties.UnderlineErrors = showErrorsCheckBox.Active;
				TextEditorProperties.ShowVerticalRuler = showVRulerCheckBox.Active;
				TextEditorProperties.HighlightCurrentLine = highlightCurrentLineCheckBox.Active;
				TextEditorProperties.ShowControlCharacters = showControlCharactersCheckBox.Active;
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
