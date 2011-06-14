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
using System.Threading;

namespace MonoDevelop.MacDev
{
	public abstract class XibCodeBehind
	{
		public XibCodeBehind (DotNetProject project)
		{
			this.Project = project;
			project.FileChangedInProject += HandleProjectFileChangedInProject;
			project.FileAddedToProject += HandleProjectFileAddedToProject;
		}

		void HandleProjectFileAddedToProject (object sender, ProjectFileEventArgs args)
		{
			foreach (ProjectFileEventInfo e in args) {
				if (e.Project.Loading)
					continue;
				
				var pf = e.ProjectFile;
				if (IsDesignerFile (pf)) {
					var xibFile = GetXibFile (pf);
					if (xibFile != null)
						ThreadPool.QueueUserWorkItem (delegate { UpdateXibCodebehind (xibFile, pf, true); });
				} else if (IsXibFile (pf)) {
					ThreadPool.QueueUserWorkItem (delegate { UpdateXibCodebehind (pf, true); });
				}
			}
		}

		void HandleProjectFileChangedInProject (object sender, ProjectFileEventArgs args)
		{
			foreach (ProjectFileEventInfo e in args) {
				if (IsXibFile (e.ProjectFile))
					ThreadPool.QueueUserWorkItem (delegate { UpdateXibCodebehind (e.ProjectFile); });
			}
		}
		
		static bool IsXibFile (ProjectFile pf)
		{
			return pf.BuildAction == BuildAction.Page && pf.FilePath.Extension ==".xib";
		}
		
		static bool IsDesignerFile (ProjectFile pf)
		{
			return pf.BuildAction == BuildAction.Compile && pf.FilePath.FileNameWithoutExtension.EndsWith (".xib.designer");
		}
		
		public DotNetProject Project { get; private set ; }
		
		internal IEnumerable<KeyValuePair<ProjectFile,ProjectFile>> GetDesignerFilesNeedBuilding
			(IEnumerable<ProjectFile> allFiles, bool forceRegen)
		{
			var pairs = from x in allFiles
				where IsXibFile (x)
				let d = GetDesignerFile (x)
				where d != null
				select new KeyValuePair<ProjectFile, ProjectFile> (x, d);
			
			if (forceRegen)
				return pairs;
			
			var projWrite = File.GetLastWriteTime (Project.FileName);
			return from p in pairs
				let t = File.GetLastWriteTime (p.Value.FilePath)
				where t < projWrite || t < File.GetLastWriteTime (p.Key.FilePath)
				select p;
		}

		static ProjectFile GetDesignerFile (ProjectFile xibFile)
		{
			return xibFile.DependentChildren.Where (x => x.Name.StartsWith (xibFile.Name + ".designer")).FirstOrDefault ();
		}
		
		static ProjectFile GetXibFile (ProjectFile designerFile)
		{
			var parent = designerFile.DependsOnFile;
			if (parent != null && IsXibFile (parent) && designerFile.FilePath.FileName.StartsWith (parent.FilePath.FileName + ".designer."))
				return parent;
			return null;
		}

		internal void GenerateDesignerCode (CodeBehindWriter writer, ProjectFile xibFile, ProjectFile designerFile)
		{
			var ccu = Generate (xibFile, writer.Provider, writer.GeneratorOptions);
			writer.Write (ccu, designerFile.FilePath);
		}
		
		public abstract CodeCompileUnit Generate (ProjectFile xibFile, CodeDomProvider provider, 
		                                          CodeGeneratorOptions generator);
		
		void UpdateXibCodebehind (ProjectFile xibFile)
		{
			UpdateXibCodebehind (xibFile, false);
		}
		
		void UpdateXibCodebehind (ProjectFile xibFile, bool force)
		{
			var designerFile = GetDesignerFile (xibFile);
			if (designerFile != null)
				UpdateXibCodebehind (xibFile, designerFile, false);
		}
		
		void UpdateXibCodebehind (ProjectFile xibFile, ProjectFile designerFile, bool force)
		{
			try {
				if (!force && File.GetLastWriteTime (xibFile.FilePath) <= File.GetLastWriteTime (designerFile.FilePath))
					return;
				var writer = CodeBehindWriter.CreateForProject (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (),
				                                                (DotNetProject)xibFile.Project);
				GenerateDesignerCode (writer, xibFile, designerFile);
				writer.WriteOpenFiles ();
			} catch (Exception ex) {
				LoggingService.LogError (String.Format ("Error generating code for xib file '{0}'", xibFile.FilePath), ex);
			}
		}
	}
}
