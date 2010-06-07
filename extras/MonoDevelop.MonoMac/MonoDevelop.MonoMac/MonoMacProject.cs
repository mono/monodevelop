// 
// MonoMacProject.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009-2010 Novell, Inc. (http://www.novell.com)
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
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using System.Reflection;
using MonoDevelop.MacDev.Plist;

namespace MonoDevelop.MonoMac
{
	
	public class MonoMacProject : DotNetProject
	{
		public override string ProjectType {
			get { return "MonoMac"; }
		}
		
		#region Constructors
		
		public MonoMacProject ()
		{
		}
		
		public MonoMacProject (string languageName)
			: base (languageName)
		{
		}
		
		public MonoMacProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
			/* TODO
			var mainNibAtt = projectOptions.Attributes ["MainNibFile"];
			if (mainNibAtt != null) {
				this.mainNibFile = mainNibAtt.InnerText;	
			}
			*/
		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			var conf = new MonoMacProjectConfiguration (name);
			conf.CopyFrom (base.CreateConfiguration (name));
			return conf;
		}
		
		#endregion
		
		#region Execution
		
		protected override ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel,
		                                                            DotNetProjectConfiguration configuration)
		{
			var conf = (MonoMacProjectConfiguration) configuration;
			
			return new MonoMacExecutionCommand (TargetRuntime, TargetFramework, conf.AppDirectory, conf.DebugMode) {
				UserAssemblyPaths = GetUserAssemblyPaths (configSel)
			};
		}
		
		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configSel)
		{
			var conf = (MonoMacProjectConfiguration) GetConfiguration (configSel);
			
			if (!Directory.Exists (conf.AppDirectory)) {
				Gtk.Application.Invoke (delegate {
					MessageService.ShowError (GettextCatalog.GetString ("The application has not been built."));
				});
				return;
			}
			
			base.OnExecute (monitor, context, configSel);
		}
		
		#endregion
		
		//taken from monodevelop.aspnet.mvc
		protected override void PopulateSupportFileList (MonoDevelop.Projects.FileCopySet list, ConfigurationSelector solutionConfiguration)
		{
			base.PopulateSupportFileList (list, solutionConfiguration);
			
			//HACK: workaround for MD not local-copying package references
			foreach (var projectReference in References) {
				if (projectReference.Package != null && projectReference.Package.Name == "monomac") {
					if (projectReference.LocalCopy && projectReference.ReferenceType == ReferenceType.Gac)
						foreach (var assem in projectReference.Package.Assemblies)
							list.Add (assem.Location);
					break;
				}
			}
		}
		
		#region Platform properties
		
		public override bool SupportsFramework (MonoDevelop.Core.Assemblies.TargetFramework framework)
		{
			if (!framework.IsCompatibleWithFramework ("1.1"))
				return false;
			else
				return base.SupportsFramework (framework);
		}
		
		#endregion
		
		#region CodeBehind files
		
		public override string GetDefaultBuildAction (string fileName)
		{
			if (fileName.EndsWith (groupedExtensions[0]))
				return BuildAction.Page;
			return base.GetDefaultBuildAction (fileName);
		}
		
		static string[] groupedExtensions = { ".xib" };
		
		//based on MoonlightProject
		protected override void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			//short-circuit if the project is being deserialised
			if (Loading) {
				base.OnFileAddedToProject (e);
				return;
			}
			
			/* TODO
			if (String.IsNullOrEmpty (MainNibFile) && Path.GetFileName (e.ProjectFile.FilePath) == "MainWindow.xib") {
				MainNibFile = e.ProjectFile.FilePath;
			}
			*/
			
			//find any related files, e.g codebehind
			//FIXME: base this on the controller class names defined in the xib
			var filesToAdd = MonoDevelop.DesignerSupport.CodeBehind.GuessDependencies (this, e.ProjectFile, groupedExtensions);
			
			//let the base fire the event before we add files
			//don't want to fire events out of order of files being added
			base.OnFileAddedToProject (e);
			
			//make sure that the parent and child files are in the project
			if (filesToAdd != null) {
				foreach (string file in filesToAdd) {
					//NOTE: this only adds files if they are not already in the project
					AddFile (file);
				}
			}
		}
		
		protected override void OnFileChangedInProject (MonoDevelop.Projects.ProjectFileEventArgs e)
		{
			/* TODO
			//update codebehind
			if (e.ProjectFile.BuildAction == BuildAction.Page && e.ProjectFile.FilePath.Extension ==".xib")
				System.Threading.ThreadPool.QueueUserWorkItem (delegate { CodeBehind.UpdateXibCodebehind (e.ProjectFile); });
			*/
			base.OnFileChangedInProject (e);
		}
		
		#endregion
		
		public ProjectFile GetInfoPlist ()
		{
			var name = BaseDirectory.Combine ("Info.plist");
			var pf = Files.GetFile (name);
			if (pf != null)
				return pf;
			
			var doc = new PlistDocument ();
			doc.Root = new PlistDictionary ();
			doc.WriteToFile (name);
			return AddFile (name);
		}
	}
}
