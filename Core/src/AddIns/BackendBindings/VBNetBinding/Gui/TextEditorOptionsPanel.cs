// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Markus Palme" email="MarkusPalme@gmx.de"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;

using MonoDevelop.Projects;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;

namespace VBBinding
{
	public class TextEditorOptionsPanel : AbstractOptionPanel
	{
		public override void LoadPanelContents()
		{
			SetupFromXml(Path.Combine(PropertyService.DataDirectory, 
			                          @"resources\panels\VBSpecificTextEditorOptions.xfrm"));
			((CheckBox)ControlDictionary["enableEndConstructsCheckBox"]).Checked   = PropertyService.GetProperty("VBBinding.TextEditor.EnableEndConstructs", true);
			((CheckBox)ControlDictionary["enableCasingCheckBox"]).Checked = PropertyService.GetProperty("VBBinding.TextEditor.EnableCasing", true);
		}
		
		public override bool StorePanelContents()
		{
			PropertyService.SetProperty("VBBinding.TextEditor.EnableEndConstructs", ((CheckBox)ControlDictionary["enableEndConstructsCheckBox"]).Checked);
			PropertyService.SetProperty("VBBinding.TextEditor.EnableCasing", ((CheckBox)ControlDictionary["enableCasingCheckBox"]).Checked);
			
			return true;
		}
	}
}
