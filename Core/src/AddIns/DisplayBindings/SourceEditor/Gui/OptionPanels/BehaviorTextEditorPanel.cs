// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;

using MonoDevelop.Internal.ExternalTool;
using MonoDevelop.Internal.Templates;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.Services;
using MonoDevelop.TextEditor;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.EditorBindings.FormattingStrategy;

using Gtk;

namespace MonoDevelop.EditorBindings.Gui.OptionPanels
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
			// Services
			StringParserService StringParserService = (
				StringParserService)ServiceManager.GetService (typeof (StringParserService));
			
			// GTK controls
			[Glade.Widget] Label tabsGroupBoxLabel;
			[Glade.Widget] Label behaviourGroupBoxLabel;
			[Glade.Widget] Label tabSizeLabel;
			[Glade.Widget] Label indentSizeLabel;
			[Glade.Widget] Label indentLabel;
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

				if (IndentStyle.None.Equals(
					    (IndentStyle) ((IProperties)CustomizationObject).GetProperty(
						    "IndentStyle", IndentStyle.Smart))){
					noneIndentStyle.Active = true;
				}
				else if (IndentStyle.Auto.Equals(
						 (IndentStyle) ((IProperties)CustomizationObject).GetProperty(
							 "IndentStyle", IndentStyle.Smart))){
					automaticIndentStyle.Active = true;
				}
				else if (IndentStyle.Smart.Equals(
						 (IndentStyle) ((IProperties)CustomizationObject).GetProperty(
							 "IndentStyle", IndentStyle.Smart))){
					// FIXME: renable this when smart indent is back
					//smartIndentStyle.Active = true;
				}
				
				// FIXME: re-enable these when their options are implemented
				autoinsertCurlyBraceCheckBox.Sensitive = false;
   				// FIXME: renable this when smart indent is back
				//smartIndentStyle.Sensitive = false;
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
				// FIXME: renable this when smart indent is back
				//else if (smartIndentStyle.Active)
				//	((IProperties)CustomizationObject).SetProperty("IndentStyle", IndentStyle.Smart);
				
				//FIXME: Only one of these should be selected to save the value
				((IProperties)CustomizationObject).SetProperty("TabIndent", indentAndTabSizeSpinButton.Value);
			}
		}
	}
}
