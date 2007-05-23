// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
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
	public class BehaviorTextEditorPanel : AbstractOptionPanel
	{
		BehaviorTextEditorPanelWidget widget;
		
		public override void LoadPanelContents ()
		{
			Add (widget = new BehaviorTextEditorPanelWidget ());
		}
		
		public override bool StorePanelContents ()
		{
			widget.Store ();
			return true;
		}
		
		class BehaviorTextEditorPanelWidget : GladeWidgetExtract 
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
