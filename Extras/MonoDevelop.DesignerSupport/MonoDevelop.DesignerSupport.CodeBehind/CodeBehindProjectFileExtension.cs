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
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;

using MonoDevelop.DesignerSupport;

namespace MonoDevelop.DesignerSupport.CodeBehind
{
	public class CodeBehindProjectFileExtension : NodeBuilderExtension
	{
		MonoDevelop.Projects.ProjectFileEventHandler projectFileEventHandler;
		ClassInformationEventHandler classInformationEventHandler;
		
		protected override void Initialize ()
		{	
			projectFileEventHandler = (ProjectFileEventHandler) IdeApp.Services.DispatchService.GuiDispatch (new ProjectFileEventHandler (onFileChanged));
			IdeApp.ProjectOperations.FileChangedInProject += projectFileEventHandler;
			
			classInformationEventHandler = (ClassInformationEventHandler) IdeApp.Services.DispatchService.GuiDispatch (new ClassInformationEventHandler (onClassInformationChanged));
			IdeApp.ProjectOperations.ParserDatabase.ClassInformationChanged += classInformationEventHandler;
		}
		
		public override void Dispose ()
		{
			IdeApp.ProjectOperations.FileChangedInProject -= projectFileEventHandler;
			IdeApp.ProjectOperations.ParserDatabase.ClassInformationChanged  -= classInformationEventHandler;
		}
		
		void onFileChanged (object sender, MonoDevelop.Projects.ProjectFileEventArgs e)
		{
			if (e.ProjectFile == null) return;
			
			ITreeBuilder builder = Context.GetTreeBuilder (e.ProjectFile);
			if (builder != null)
				builder.UpdateAll ();
		}
		
		void onClassInformationChanged (object sender, ClassInformationEventArgs e)
		{
			if (e.Project == null) return;
			ProjectFile file = e.Project.GetProjectFile (e.FileName);
			if (file == null) return;
		
			ITreeBuilder builder = Context.GetTreeBuilder (file);
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
				
				if (file.Project != null) {
					IParserContext ctx = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (file.Project);
					System.Collections.Generic.IList<IClass> list = DesignerSupport.Service.CodeBehindService.GetAllCodeBehindClasses (file.Project);
					
					foreach (IClass cls in ctx.GetFileContents (file.FilePath))
						if (list.Contains (cls))
							attributes = NodeAttributes.Hidden;
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
