// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Markus Palme" email="MarkusPalme@gmx.de"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;

using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Properties;

using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Core.Services;

namespace VBBinding
{
	public class VBCompilerPanel : AbstractOptionPanel
	{
		VBCompilerParameters config = null;
		
		public override void LoadPanelContents()
		{
			SetupFromXml(Path.Combine(PropertyService.DataDirectory, 
			                          @"resources\panels\VBCompilerPanel.xfrm"));
			
			DotNetProjectConfiguration cfg = (DotNetProjectConfiguration)((IProperties)CustomizationObject).GetProperty("Config");
			config = (VBCompilerParameters) cfg.CompilationParameters;
			
			FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
			((ComboBox)ControlDictionary["compilerVersionComboBox"]).Items.Add("Standard");
			foreach (string runtime in fileUtilityService.GetAvaiableRuntimeVersions()) {
				((ComboBox)ControlDictionary["compilerVersionComboBox"]).Items.Add(runtime);
			}
			
			((ComboBox)ControlDictionary["compilerVersionComboBox"]).Text = config.VBCompilerVersion.Length == 0 ? "Standard" : config.VBCompilerVersion;
		}

		public override bool StorePanelContents()
		{
			config.VBCompilerVersion = ControlDictionary["compilerVersionComboBox"].Text;
			return true;
		}
	}
}
