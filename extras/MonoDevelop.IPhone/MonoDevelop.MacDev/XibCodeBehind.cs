// 
// CodeBehind.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.CodeDom;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using MonoDevelop.MacDev.InterfaceBuilder;
using MonoDevelop.Core;
using System.CodeDom.Compiler;
using MonoDevelop.DesignerSupport;
using System.IO;
using MonoDevelop.Projects;

namespace MonoDevelop.MacDev
{
	public abstract class XibCodeBehind
	{
		public XibCodeBehind (DotNetProject project)
		{
			this.Project = project;
			project.FileChangedInProject += HandleProjectFileChangedInProject;
		}

		void HandleProjectFileChangedInProject (object sender, ProjectFileEventArgs e)
		{
			//update codebehind
			if (e.ProjectFile.BuildAction == BuildAction.Page && e.ProjectFile.FilePath.Extension ==".xib")
				System.Threading.ThreadPool.QueueUserWorkItem (delegate { UpdateXibCodebehind (e.ProjectFile); });
		}
		
		public DotNetProject Project { get; private set ; }
		
		public BuildResult UpdateXibCodebehind (CodeBehindWriter writer, IEnumerable<ProjectFile> allFiles, bool forceRegen)
		{
			BuildResult result = null;
			var projWrite = File.GetLastWriteTime (Project.FileName);
			
			foreach (var xibFile in allFiles.Where (x => x.FilePath.Extension == ".xib" && x.BuildAction == BuildAction.Page)) {
				var designerFile = GetDesignerFile (xibFile);
				if (designerFile == null)
					continue;
				
				try {
					var designerWrite = File.GetLastWriteTime (designerFile.FilePath);
					if (forceRegen || designerWrite < projWrite || designerWrite < File.GetLastWriteTime (xibFile.FilePath)) {
						GenerateDesignerCode (writer, xibFile, designerFile);
					}
				} catch (Exception ex) {
					result = result ?? new BuildResult ();
					result.AddError (xibFile.FilePath, 0, 0, null, ex.Message);
					LoggingService.LogError (String.Format ("Error generating code for xib file '{0}'", xibFile.FilePath), ex);
				}
			}
			return result;
		}

		static ProjectFile GetDesignerFile (ProjectFile xibFile)
		{
			return xibFile.DependentChildren.Where (x => x.Name.StartsWith (xibFile.Name + ".designer")).FirstOrDefault ();
		}

		void GenerateDesignerCode (CodeBehindWriter writer, ProjectFile xibFile, ProjectFile designerFile)
		{
			var ns = new CodeNamespace (((DotNetProject)designerFile.Project).GetDefaultNamespace (designerFile.FilePath));
			var ccu = new CodeCompileUnit ();
			ccu.Namespaces.Add (ns);
			foreach (var ctd in GetTypes (XDocument.Load (xibFile.FilePath), writer.Provider, writer.GeneratorOptions))
				ns.Types.Add (ctd);
			writer.Write (ccu, designerFile.FilePath);
		}
		
		public void UpdateXibCodebehind (ProjectFile xibFile)
		{
			var designerFile = GetDesignerFile (xibFile);
			
			try {
				if (designerFile == null || File.GetLastWriteTime (xibFile.FilePath) <= File.GetLastWriteTime (designerFile.FilePath))
					return;
				var writer = CodeBehindWriter.CreateForProject (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (),
				                                                (DotNetProject)xibFile.Project);
				GenerateDesignerCode (writer, xibFile, designerFile);
				writer.WriteOpenFiles ();
			} catch (Exception ex) {
				LoggingService.LogError (String.Format ("Error generating code for xib file '{0}'", xibFile.FilePath), ex);
			}
		}
		
		public abstract IEnumerable<CodeTypeDeclaration> GetTypes (XDocument xibDoc, CodeDomProvider provider,
		                                                           CodeGeneratorOptions generatorOptions);
	}
}
