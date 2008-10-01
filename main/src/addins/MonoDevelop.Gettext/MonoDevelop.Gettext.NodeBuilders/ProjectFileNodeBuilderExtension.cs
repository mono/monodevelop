// 
// ProjectFileNodeBuilderExtension.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using MonoDevelop.Core.Collections;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Gettext.NodeBuilders
{
	class ProjectFileNodeBuilderExtension: NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ProjectFile).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof (ProjectFileNodeCommandHandler); }
		}
	}
	
	class ProjectFileNodeCommandHandler: NodeCommandHandler
	{
		const string scanForTranslationsProperty = "Gettext.ScanForTranslations";
		
		[CommandHandler (Commands.ScanForTranslations)]
		[AllowMultiSelection]
		public void Handler ()
		{
			//if all of the selection is already checked, then toggle checks them off
			//else it turns them on. hence we need to find if they're all checked,
			bool allChecked = true;
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile file = (ProjectFile) node.DataItem;
				object prop = file.ExtendedProperties [scanForTranslationsProperty];
				bool val = prop == null? true : (bool) prop;
				if (!val) {
					allChecked = false;
					break;
				}
			}
			
			Set<SolutionEntityItem> projects = new Set<SolutionEntityItem> ();
			
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile file = (ProjectFile) node.DataItem;
				projects.Add (file.Project);
				if (allChecked) {
					file.ExtendedProperties [scanForTranslationsProperty] = false;
				} else {
					file.ExtendedProperties.Remove (scanForTranslationsProperty);
				}
			}
				
			IdeApp.ProjectOperations.Save (projects);
		}
		
		[CommandUpdateHandler (Commands.ScanForTranslations)]
		public void UpdateHandler (CommandInfo cinfo)
		{
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile file = (ProjectFile) node.DataItem;
				object prop = file.ExtendedProperties [scanForTranslationsProperty];
				bool val = prop == null? true : (bool) prop;
				if (val) {
					cinfo.Checked = true;
				} else if (cinfo.Checked) {
					cinfo.CheckedInconsistent = true;
				}
			}
		}
	}
}
