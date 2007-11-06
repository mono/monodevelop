//  TextEditorOptionsPanel.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Markus Palme <MarkusPalme@gmx.de>
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
