// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.DirectoryServices; // for SortDirection
using System.ComponentModel;

using MonoDevelop.Gui;
using MonoDevelop.DefaultEditor.Gui.Editor;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.Core.Properties;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;
//using MonoDevelop.XmlForms;
//using MonoDevelop.Gui.XmlForms;
using MonoDevelop.TextEditor;


namespace MonoDevelop.Gui.Dialogs
{
	public class SortOptionsDialog //: BaseSharpDevelopForm
	{/*
		public static readonly string removeDupesOption       = "MonoDevelop.Gui.Dialogs.SortOptionsDialog.RemoveDuplicateLines";
		public static readonly string caseSensitiveOption     = "MonoDevelop.Gui.Dialogs.SortOptionsDialog.CaseSensitive";
		public static readonly string ignoreWhiteSpacesOption = "MonoDevelop.Gui.Dialogs.SortOptionsDialog.IgnoreWhitespaces";
		public static readonly string sortDirectionOption     = "MonoDevelop.Gui.Dialogs.SortOptionsDialog.SortDirection";
		
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
