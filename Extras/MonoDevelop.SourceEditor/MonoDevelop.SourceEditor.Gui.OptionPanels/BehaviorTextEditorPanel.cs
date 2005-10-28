// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;

using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.Document;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.SourceEditor.FormattingStrategy;

using Gtk;

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
			Add (widget = new BehaviorTextEditorPanelWidget ((IProperties) CustomizationObject));
		}
		
		public override bool StorePanelContents ()
		{
			widget.Store ((IProperties) CustomizationObject);
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
			
			public BehaviorTextEditorPanelWidget (IProperties CustomizationObject) :  
				base ("EditorBindings.glade", "BehaviorTextEditorPanel")
			{
				// FIXME: enable when it is implemented
				autoinsertCurlyBraceCheckBox.Sensitive = false;
					
				// Set up Value
				autoinsertCurlyBraceCheckBox.Active = ((IProperties)CustomizationObject).GetProperty(
					"AutoInsertCurlyBracket", true);
				autoInsertTemplatesCheckBox.Active  = ((IProperties)CustomizationObject).GetProperty(
					"AutoInsertTemplates", true);
				convertTabsToSpacesCheckBox.Active  = ((IProperties)CustomizationObject).GetProperty(
					"TabsToSpaces", false);

				//FIXME: Only one of these should be selected to hold the value
				indentAndTabSizeSpinButton.Value = ((IProperties)CustomizationObject).GetProperty(
					"TabIndent", 4);

				IndentStyle currentIndent = (IndentStyle)((IProperties) CustomizationObject).GetProperty ("IndentStyle", IndentStyle.Smart);
				if (currentIndent == IndentStyle.None) 
					noneIndentStyle.Active = true;
				else if (currentIndent == IndentStyle.Auto) 
					automaticIndentStyle.Active = true;
				else if (currentIndent == IndentStyle.Smart) 
					smartIndentStyle.Active = true;
			}

			public void Store (IProperties CustomizationObject)
			{
				((IProperties)CustomizationObject).SetProperty(
					"TabsToSpaces",           convertTabsToSpacesCheckBox.Active);
				((IProperties)CustomizationObject).SetProperty(
					"AutoInsertCurlyBracket", autoinsertCurlyBraceCheckBox.Active);
				((IProperties)CustomizationObject).SetProperty(
					"AutoInsertTemplates",    autoInsertTemplatesCheckBox.Active);

				if (noneIndentStyle.Active)
					((IProperties)CustomizationObject).SetProperty("IndentStyle", IndentStyle.None);
				else if (automaticIndentStyle.Active)
					((IProperties)CustomizationObject).SetProperty("IndentStyle", IndentStyle.Auto);
				else if (smartIndentStyle.Active)
					((IProperties)CustomizationObject).SetProperty("IndentStyle", IndentStyle.Smart);
				
				//FIXME: Only one of these should be selected to save the value
				((IProperties)CustomizationObject).SetProperty("TabIndent", indentAndTabSizeSpinButton.Value);
			}
		}
	}
}
