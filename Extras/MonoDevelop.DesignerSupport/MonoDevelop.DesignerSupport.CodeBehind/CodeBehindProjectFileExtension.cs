//
// CodeBehindProjectFileExtension.cs: Adds CodeBehind classes to the Solution 
// 		Pad, and hides CodeBehind files. 
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
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
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Projects.Item;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.SolutionViewPad;

using MonoDevelop.DesignerSupport;

namespace MonoDevelop.DesignerSupport.CodeBehind
{
	public class CodeBehindProjectFileExtension : NodeBuilderExtension
	{
		CodeBehindService.CodeBehindClassEventHandler classChangeHandler;
		CodeBehindService.CodeBehindFileEventHandler fileChangeHandler;
		
		protected override void Initialize ()
		{	
			classChangeHandler = (CodeBehindService.CodeBehindClassEventHandler)
				IdeApp.Services.DispatchService.GuiDispatch (new CodeBehindService.CodeBehindClassEventHandler (onClassChanged));
			DesignerSupport.Service.CodeBehindService.CodeBehindClassUpdated += classChangeHandler;
// TODO: Project Conversion
//			fileChangeHandler = (CodeBehindService.CodeBehindFileEventHandler)
//				IdeApp.Services.DispatchService.GuiDispatch (new CodeBehindService.CodeBehindFileEventHandler (onFileChanged));
//			DesignerSupport.Service.CodeBehindService.CodeBehindFileUpdated += fileChangeHandler;
		}
		
		public override void Dispose ()
		{
			DesignerSupport.Service.CodeBehindService.CodeBehindClassUpdated -= classChangeHandler;
			DesignerSupport.Service.CodeBehindService.CodeBehindFileUpdated -= fileChangeHandler;
		}
		
		void onFileChanged (ProjectFile file)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (file);
			if (builder != null)
				builder.UpdateAll ();
		}
		
		void onClassChanged (IClass cls)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (cls);
			if (builder != null)
				builder.UpdateAll ();
			
			//refresh the file containing the class to make it hidden/visible
			//FIXME: can we locate the node in the tree more efficiently than looking up its parent project
			//       and updating it and all its children? Direct lookup won't work with hidden nodes.
			if (cls.Region == null) return;
			string filename = cls.Region.FileName;
			if (filename == null) return;
			
			IProject proj = ProjectService.GetProjectContainingFile (filename);
			if (proj == null) return;
			
			builder = Context.GetTreeBuilder (proj);
			if (builder != null)
				builder.UpdateAll ();
		}
		
		public override bool CanBuildNode (Type dataType)
		{
			return typeof (ProjectFile).IsAssignableFrom (dataType);
		}
	
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			if (builder.Options ["ShowCodeBehindClasses"]) {
				ProjectFile file = (ProjectFile) dataObject;
				if (file.Project != null) {
					IClass cls = DesignerSupport.Service.CodeBehindService.GetCodeBehind (file);
					if (cls != null)
						builder.AddChild (cls);
				}
			}
		}
		
		public override void GetNodeAttributes (ITreeNavigator parentNode, object dataObject, ref NodeAttributes attributes)
		{
			if (! (parentNode.Options ["ShowCodeBehindFiles"] || parentNode.Options ["ShowAllFiles"])) {
				ProjectFile file = (ProjectFile) dataObject;
				
				if (file.Project != null){					
					//only hide the file if all the classes it contains are codebehind classes
					if (DesignerSupport.Service.CodeBehindService.ContainsOnlyCodeBehind(file))
						attributes |= NodeAttributes.Hidden;
				}
			}
		}
	
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			if (builder.Options ["ShowCodeBehindClasses"]) {
				ProjectFile file = (ProjectFile) dataObject;
				if (file.Project != null) {
					IClass cls = DesignerSupport.Service.CodeBehindService.GetCodeBehind (file);
					return (cls != null);
				}
			}
			return false;
		}

	}
}
