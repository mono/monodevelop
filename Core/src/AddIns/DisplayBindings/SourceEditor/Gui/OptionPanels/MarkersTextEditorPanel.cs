// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;

using MonoDevelop.TextEditor.Document;
using MonoDevelop.Internal.ExternalTool;
using MonoDevelop.Core.Properties;

using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Gui.Dialogs;

using Gtk;
using MonoDevelop.Gui.Widgets;

namespace MonoDevelop.EditorBindings.Gui.OptionPanels
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
			Add (widget = new MarkersTextEditorPanelWidget ((IProperties) CustomizationObject));	
		}
		
		public override bool StorePanelContents()
		{
			widget.Store ((IProperties) CustomizationObject);
			return true;
		}

		class MarkersTextEditorPanelWidget : GladeWidgetExtract 
		{
			// Services
			FileUtilityService FileUtilityService = (
				FileUtilityService)ServiceManager.GetService(typeof(FileUtilityService));
			StringParserService StringParserService = (
				StringParserService)ServiceManager.GetService (typeof (StringParserService));
			
			// Gtk Controls
			[Glade.Widget] Label markersLabel;
			[Glade.Widget] Label characterMarkersLabel;
			[Glade.Widget] Label rulersLabel;
			[Glade.Widget] Label atColumnLabel;
			[Glade.Widget] CheckButton showLineNumberCheckBox;
			[Glade.Widget] CheckButton showBracketHighlighterCheckBox;
			[Glade.Widget] CheckButton showErrorsCheckBox;
			[Glade.Widget] CheckButton showVRulerCheckBox;
			[Glade.Widget] SpinButton  vRulerRowTextBox;

			public MarkersTextEditorPanelWidget (IProperties CustomizationObject) :  
				base ("EditorBindings.glade", "MarkersTextEditorPanel")
			{
				showLineNumberCheckBox.Active         = ((IProperties)CustomizationObject).GetProperty(
					"ShowLineNumbers", true);
				showBracketHighlighterCheckBox.Active = ((IProperties)CustomizationObject).GetProperty(
					"ShowBracketHighlight", true);
				showErrorsCheckBox.Active             = ((IProperties)CustomizationObject).GetProperty(
					"ShowErrors", true);
				showVRulerCheckBox.Active             = ((IProperties)CustomizationObject).GetProperty(
					"ShowVRuler", false);
			
				vRulerRowTextBox.Value = ((IProperties)CustomizationObject).GetProperty("VRulerRow", 80);

				// disabled
				showErrorsCheckBox.Sensitive = false;
			}

			public void Store (IProperties CustomizationObject)
			{
				((IProperties)CustomizationObject).SetProperty("ShowLineNumbers",      showLineNumberCheckBox.Active);
				((IProperties)CustomizationObject).SetProperty("ShowBracketHighlight", showBracketHighlighterCheckBox.Active);
				((IProperties)CustomizationObject).SetProperty("ShowErrors",           showErrorsCheckBox.Active);
				((IProperties)CustomizationObject).SetProperty("ShowVRuler",           showVRulerCheckBox.Active);
				try {
					((IProperties)CustomizationObject).SetProperty("VRulerRow", vRulerRowTextBox.Value);
				} 
				catch { }
			}
		}
	}
}
