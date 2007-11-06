//  VBDocConfigurationPanel.cs
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
using System.Drawing;

using MonoDevelop.Projects;
using MonoDevelop.Core.Gui.Dialogs;

using MonoDevelop.Core;
using MonoDevelop.Core.Properties;

namespace VBBinding
{
	public class VBDOCConfigurationPanel  : AbstractOptionPanel
	{
		VBCompilerParameters compilerParameters = null;
		VBProject project = null;
		ResourceService resourceService = (ResourceService)ServiceManager.Services.GetService(typeof(IResourceService));
		static PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
		
		///<summary>
		/// Returns if the filename will be parsed when running VB.DOC.
		/// </summary>
		public static bool IsFileIncluded(string filename, VBProject project)
		{
			DotNetProjectConfiguration config = (DotNetProjectConfiguration) project.ActiveConfiguration;
			VBCompilerParameters compilerparameters = (VBCompilerParameters) config.CompilationParameters;
			return Array.IndexOf(compilerparameters.VBDOCFiles, filename) == -1;
		}
		
		
		public VBDOCConfigurationPanel() : base(propertyService.DataDirectory + @"\resources\panels\ProjectOptions\VBDOCConfigurationPanel.xfrm")
		{
			CustomizationObjectChanged += new EventHandler(SetValues);
			ControlDictionary["BrowseOutputFileButton"].Click += new EventHandler(BrowseOutputFileButton_Click);
		}
		
		private void BrowseOutputFileButton_Click(object sender, EventArgs e) {
			SaveFileDialog dialog = new SaveFileDialog();
			dialog.Filter = "XML files (*.xml)|*.xml";
			if(dialog.ShowDialog() == DialogResult.OK) {
				((TextBox)ControlDictionary["OutputFileTextBox"]).Text = dialog.FileName;
			}
		}
		
		public override bool ReceiveDialogMessage(DialogMessage message)
		{
			if (message == DialogMessage.OK) {
				if (compilerParameters == null) {
					return true;
				}
				
				compilerParameters.VBDOCOutputFile = ((TextBox)ControlDictionary["OutputFileTextBox"]).Text;
				compilerParameters.VBDOCCommentPrefix = ((TextBox)ControlDictionary["CommentPrefixTextBox"]).Text;
				
				string[] files = new string[((CheckedListBox)ControlDictionary["FileListBox"]).Items.Count - ((CheckedListBox)ControlDictionary["FileListBox"]).CheckedIndices.Count];
				int count = 0;
				for(int index = 0; index < ((CheckedListBox)ControlDictionary["FileListBox"]).Items.Count; index++) {
					if(((CheckedListBox)ControlDictionary["FileListBox"]).GetItemChecked(index) == false) {
						files[count] = (string)((CheckedListBox)ControlDictionary["FileListBox"]).Items[index];
						count++;
					}
				}
				compilerParameters.VBDOCFiles = files;
			}
			return true;
		}
		
		void SetValues(object sender, EventArgs e)
		{
			DotNetProjectConfiguration config = (DotNetProjectConfiguration) ((IProperties)CustomizationObject).GetProperty("Config");
			compilerParameters = (VBCompilerParameters) config.CompilationParameters;
			project = (VBProject)((IProperties)CustomizationObject).GetProperty("Project");
			
			((TextBox)ControlDictionary["OutputFileTextBox"]).Text = compilerParameters.VBDOCOutputFile;
			((TextBox)ControlDictionary["CommentPrefixTextBox"]). Text = compilerParameters.VBDOCCommentPrefix;
			
			foreach(ProjectFile pfile in project.ProjectFiles) {
				bool included = IsFileIncluded(pfile.Name, project);
				((CheckedListBox)ControlDictionary["FileListBox"]).Items.Add(pfile.Name, included);
			}
		}
	}
}
