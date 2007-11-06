//  SortOptionsDialog.cs
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
using System.Drawing;
using System.DirectoryServices; // for SortDirection
using System.ComponentModel;

using MonoDevelop.Core.Gui;
using MonoDevelop.DefaultEditor.Gui.Editor;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.Core.Properties;

using MonoDevelop.Core;
using MonoDevelop.Core;
//using MonoDevelop.XmlForms;
//using MonoDevelop.Core.Gui.XmlForms;
using MonoDevelop.TextEditor;


namespace MonoDevelop.Core.Gui.Dialogs
{
	public class SortOptionsDialog //: BaseSharpDevelopForm
	{/*
		public static readonly string removeDupesOption       = "MonoDevelop.Core.Gui.Dialogs.SortOptionsDialog.RemoveDuplicateLines";
		public static readonly string caseSensitiveOption     = "MonoDevelop.Core.Gui.Dialogs.SortOptionsDialog.CaseSensitive";
		public static readonly string ignoreWhiteSpacesOption = "MonoDevelop.Core.Gui.Dialogs.SortOptionsDialog.IgnoreWhitespaces";
		public static readonly string sortDirectionOption     = "MonoDevelop.Core.Gui.Dialogs.SortOptionsDialog.SortDirection";
		
		static PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
		
		public SortOptionsDialog()
		{
			this.SetupFromXml(propertyService.DataDirectory + @"\resources\dialogs\SortOptionsDialog.xfrm");
			
			AcceptButton = (Button)ControlDictionary["okButton"];
			CancelButton = (Button)ControlDictionary["cancelButton"];
			((CheckBox)ControlDictionary["removeDupesCheckBox"]).Checked = propertyService.GetProperty(removeDupesOption, false);
			((CheckBox)ControlDictionary["caseSensitiveCheckBox"]).Checked = propertyService.GetProperty(caseSensitiveOption, true);
			((CheckBox)ControlDictionary["ignoreWhiteSpacesCheckBox"]).Checked = propertyService.GetProperty(ignoreWhiteSpacesOption, false);
			
			((RadioButton)ControlDictionary["ascendingRadioButton"]).Checked = ((SortDirection)propertyService.GetProperty(sortDirectionOption, SortDirection.Ascending)) == SortDirection.Ascending;
			((RadioButton)ControlDictionary["descendingRadioButton"]).Checked = ((SortDirection)propertyService.GetProperty(sortDirectionOption, SortDirection.Ascending)) == SortDirection.Descending;
			
			// insert event handlers
			ControlDictionary["okButton"].Click  += new EventHandler(OkEvent);
		}
		
		void OkEvent(object sender, EventArgs e)
		{
			propertyService.SetProperty(removeDupesOption, ((CheckBox)ControlDictionary["removeDupesCheckBox"]).Checked);
			propertyService.SetProperty(caseSensitiveOption, ((CheckBox)ControlDictionary["caseSensitiveCheckBox"]).Checked);
			propertyService.SetProperty(ignoreWhiteSpacesOption, ((CheckBox)ControlDictionary["ignoreWhiteSpacesCheckBox"]).Checked);
			if (((RadioButton)ControlDictionary["ascendingRadioButton"]).Checked) {
				propertyService.SetProperty(sortDirectionOption, SortDirection.Ascending);
			} else {
				propertyService.SetProperty(sortDirectionOption, SortDirection.Descending);
			}
		}*/
	}
}
