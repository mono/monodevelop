//
// CodeBehindProjectFileExtension.cs: Adds CodeBehind classes to the Solution 
// 		Pad, and hides CodeBehind files. 
//
// Authors:
//   Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (C) 2006 Michael Hutchinson
// Copyright (C) 2007 Novell, Inc.
//
//
// This source code is licenced under The MIT License:
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
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Core.Gui;
using MonoDevelop.DesignerSupport;

namespace MonoDevelop.DesignerSupport.CodeBehind
{
	public class CodeBehindProjectFileExtension : NodeBuilderExtension
	{
		CodeBehindService.CodeBehindClassEventHandler classChangeHandler;
		
		protected override void Initialize ()
		{	
			classChangeHandler = (CodeBehindService.CodeBehindClassEventHandler)
				DispatchService.GuiDispatch (new CodeBehindService.CodeBehindClassEventHandler (onClassChanged));
			DesignerSupport.Service.CodeBehindService.CodeBehindClassUpdated += classChangeHandler;
		}
		
		public override void Dispose ()
		{
			DesignerSupport.Service.CodeBehindService.CodeBehindClassUpdated -= classChangeHandler;
		}
		
		void onClassChanged (object sender, CodeBehindClassEventArgs e)
		{
			if (e.ChildFiles.Count > 0) {
				ITreeBuilder builder = Context.GetTreeBuilder (e.Project);
				if (builder != null)
					builder.UpdateAll ();
			} else {
				foreach (ProjectFile affected in e.ParentFiles) {
					ITreeBuilder builder = Context.GetTreeBuilder (affected);
					if (builder != null)
						builder.UpdateAll ();
				}
			}
		}
		
		public override bool CanBuildNode (Type dataType)
		{
			return typeof (ProjectFile).IsAssignableFrom (dataType);
		}
	
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			if (builder.Options ["GroupCodeBehind"]) {
				ProjectFile file = (ProjectFile) dataObject;
				CodeBehindClass cls = DesignerSupport.Service.CodeBehindService.GetChildClass (file);
				if (cls.IClass == null) {
					builder.AddChild (cls);
				} else {
					IList<ProjectFile> children = DesignerSupport.Service.CodeBehindService.GetProjectFileChildren (file, cls.IClass);
					foreach (ProjectFile child in children)
						builder.AddChild (child);
				}
			}
		}
		
		public override void GetNodeAttributes (ITreeNavigator parentNode, object dataObject, ref NodeAttributes attributes)
		{
			if (parentNode.Options ["GroupCodeBehind"] && !parentNode.Options ["ShowAllFiles"] && !(parentNode.DataItem is ProjectFile))
				if (DesignerSupport.Service.CodeBehindService.ContainsCodeBehind ((ProjectFile) dataObject))
					attributes |= NodeAttributes.Hidden;
		}
	
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			if (builder.Options ["GroupCodeBehind"])
				return DesignerSupport.Service.CodeBehindService.HasChildren ((ProjectFile) dataObject);
			
			return false;
		}

	}
}
