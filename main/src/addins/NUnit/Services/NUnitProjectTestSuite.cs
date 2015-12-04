//
// NUnitProjectTestSuite.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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


using System.IO;
using System.Linq;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using System;
using ProjectReference = MonoDevelop.Projects.ProjectReference;

namespace MonoDevelop.NUnit
{
	public class NUnitProjectTestSuite: NUnitAssemblyTestSuite
	{
		DotNetProject project;
		string resultsPath;
		string storeId;

		public override IList<string> UserAssemblyPaths {
			get {
				return project.GetUserAssemblyPaths (IdeApp.Workspace.ActiveConfiguration);
			}
		}

		public NUnitProjectTestSuite (DotNetProject project): base (project.Name, project)
		{
			storeId = Path.GetFileName (project.FileName);
			resultsPath = MonoDevelop.NUnit.RootTest.GetTestResultsDirectory (project.BaseDirectory);
			ResultsStore = new BinaryResultsStore (resultsPath, storeId);
			this.project = project;
			project.NameChanged += new SolutionItemRenamedEventHandler (OnProjectRenamed);
			IdeApp.ProjectOperations.EndBuild += new BuildEventHandler (OnProjectBuilt);
		}
		
		public static NUnitProjectTestSuite CreateTest (DotNetProject project)
		{
			if (!project.ParentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration).BuildEnabledForItem (project))
				return null;

			foreach (var p in project.References)
				if (IsNUnitReference (p))
					return new NUnitProjectTestSuite (project);
			return null;
		}

		public static bool IsNUnitReference (ProjectReference p)
		{
			return p.Reference.IndexOf ("GuiUnit", StringComparison.OrdinalIgnoreCase) != -1 || p.Reference.IndexOf ("nunit.framework") != -1 || p.Reference.IndexOf ("nunit.core") != -1 || p.Reference.IndexOf ("nunitlite") != -1;
		}

		protected override SourceCodeLocation GetSourceCodeLocation (string fixtureTypeNamespace, string fixtureTypeName, string methodName)
		{
			if (string.IsNullOrEmpty (fixtureTypeName) || string.IsNullOrEmpty (fixtureTypeName))
				return null;
			var ctx = TypeSystemService.GetCompilation (project);
			var cls = ctx.MainAssembly.GetTypeDefinition (fixtureTypeNamespace ?? "", fixtureTypeName, 0);
			if (cls == null)
				return null;
			
			if (cls.Name != methodName) {
				foreach (var met in cls.GetMethods ()) {
					if (met.Name == methodName)
						return new SourceCodeLocation (met.Region.FileName, met.Region.BeginLine, met.Region.BeginColumn);
				}
				
				int idx = methodName != null ? methodName.IndexOf ('(') : -1;
				if (idx > 0) {
					methodName = methodName.Substring (0, idx);
					foreach (var met in cls.GetMethods ()) {
						if (met.Name == methodName)
							return new SourceCodeLocation (met.Region.FileName, met.Region.BeginLine, met.Region.BeginColumn);
					}
				}
			}
			return new SourceCodeLocation (cls.Region.FileName, cls.Region.BeginLine, cls.Region.BeginColumn);
		}
		
		public override void Dispose ()
		{
			project.NameChanged -= new SolutionItemRenamedEventHandler (OnProjectRenamed);
			IdeApp.ProjectOperations.EndBuild -= new BuildEventHandler (OnProjectBuilt);
			base.Dispose ();
		}
		
		void OnProjectRenamed (object sender, SolutionItemRenamedEventArgs e)
		{
			UnitTestGroup parent = Parent as UnitTestGroup;
			if (parent != null)
				parent.UpdateTests ();
		}
		
		void OnProjectBuilt (object s, BuildEventArgs args)
		{
			if (RefreshRequired) {
				UpdateTests ();
			} else {
				Gtk.Application.Invoke (delegate {
					OnProjectBuiltWithoutTestChange (EventArgs.Empty);
				});
			}
		}

		public override void GetCustomTestRunner (out string assembly, out string type)
		{
			type = (string) project.ExtendedProperties ["TestRunnerType"];
			var asm = project.ExtendedProperties ["TestRunnerAssembly"];
			assembly = asm != null ? project.BaseDirectory.Combine (asm.ToString ()).ToString () : null;
		}

		public override void GetCustomConsoleRunner (out string command, out string args)
		{
			var r = project.ExtendedProperties ["TestRunnerCommand"];
			command = r != null ? project.BaseDirectory.Combine (r.ToString ()).ToString () : null;
			args = (string)project.ExtendedProperties ["TestRunnerArgs"];
			if (command == null && args == null) {
				var guiUnit = project.References.FirstOrDefault (pref => pref.ReferenceType == ReferenceType.Assembly && StringComparer.OrdinalIgnoreCase.Equals (Path.GetFileName (pref.Reference), "GuiUnit.exe"));
				if (guiUnit != null) {
					command = guiUnit.Reference;
				}

				var projectReference = project.References.FirstOrDefault (pref => pref.ReferenceType == ReferenceType.Project && pref.Reference.StartsWith ("GuiUnit", StringComparison.OrdinalIgnoreCase));
				if (IdeApp.IsInitialized && command == null && projectReference != null) {
					var guiUnitProject = IdeApp.Workspace.GetAllProjects ().First (f => f.Name == projectReference.Reference);
					if (guiUnitProject != null)
						command = guiUnitProject.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
				}
			}
		}

		protected override string AssemblyPath {
			get { return project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration); }
		}
		
		protected override string TestInfoCachePath {
			get { return Path.Combine (resultsPath, storeId + ".test-cache"); }
		}
		
		protected override IEnumerable<string> SupportAssemblies {
			get {
				// Referenced assemblies which are not in the gac and which are not localy copied have to be preloaded
				DotNetProject project = base.OwnerSolutionItem as DotNetProject;
				if (project != null) {
					foreach (var pr in project.References) {
						if (pr.ReferenceType != ReferenceType.Package && !pr.LocalCopy && pr.ReferenceOutputAssembly) {
							foreach (string file in pr.GetReferencedFileNames (IdeApp.Workspace.ActiveConfiguration))
								yield return file;
						}
					}
				}
			}
		}

		public event EventHandler ProjectBuiltWithoutTestChange;

		protected virtual void OnProjectBuiltWithoutTestChange (EventArgs e)
		{
			var handler = ProjectBuiltWithoutTestChange;
			if (handler != null)
				handler (this, e);
		}
	}
}

