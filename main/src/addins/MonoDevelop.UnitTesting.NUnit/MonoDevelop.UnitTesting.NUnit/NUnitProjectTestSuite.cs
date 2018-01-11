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
using System;
using System.Threading;
using ProjectReference = MonoDevelop.Projects.ProjectReference;
using System.Threading.Tasks;
using MonoDevelop.UnitTesting.NUnit.External;
using System.Reflection;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement;
using System.Text;

namespace MonoDevelop.UnitTesting.NUnit
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

		public NUnitProjectTestSuite (DotNetProject project, NUnitVersion version): base (project.Name, project)
		{
			NUnitVersion = version;
			storeId = Path.GetFileName (project.FileName);
			resultsPath = UnitTestService.GetTestResultsDirectory (project.BaseDirectory);
			ResultsStore = new BinaryResultsStore (resultsPath, storeId);
			this.project = project;
			project.NameChanged += OnProjectRenamed;
			IdeApp.ProjectOperations.EndBuild += OnProjectBuilt;
		}

		protected override async Task OnBuild ()
		{
			await IdeApp.ProjectOperations.Build (project).Task;
			OnProjectBuilt (null, null);
		}

		public static NUnitProjectTestSuite CreateTest (DotNetProject project)
		{
			if (!project.ParentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration).BuildEnabledForItem (project))
				return null;

			foreach (var item in project.Items) {
				var nv = GetNUnitVersion (item);
				if (nv != null)
					return new NUnitProjectTestSuite (project, nv.Value);
			}
			return null;
		}

		public static bool IsNUnitReference (ProjectReference p)
		{
			return GetNUnitVersion (p).HasValue;
		}

		public static NUnitVersion? GetNUnitVersion (ProjectItem item)
		{
			switch (item) {
			case ProjectReference pr:
				return GetNUnitVersion (pr);
			case ProjectPackageReference ppr:
				return GetNUnitVersion (ppr);
			}
			return null;
		}

		public static NUnitVersion? GetNUnitVersion (ProjectReference p)
		{
			if (p.Reference.IndexOf ("GuiUnit", StringComparison.OrdinalIgnoreCase) != -1 || p.Reference.StartsWith ("nunitlite", StringComparison.OrdinalIgnoreCase))
				return NUnitVersion.NUnit2;
			if (p.Reference.IndexOf ("nunit.framework", StringComparison.OrdinalIgnoreCase) != -1) {
				var selector = p.Project?.DefaultConfiguration.Selector;
				if (selector == null)
					return NUnitVersion.Unknown;

				var f = p.GetReferencedFileNames (selector).FirstOrDefault ();
				if (f != null && File.Exists (f)) {
					try {
						var aname = new AssemblyName (SystemAssemblyService.GetAssemblyName (f));
						if (aname.Version.Major == 2)
							return NUnitVersion.NUnit2;
						else
							return NUnitVersion.NUnit3;
					} catch (Exception ex) {
						LoggingService.LogError ("Could not get assembly version", ex);
					}
				}
			}
			return null;
		}

		internal static NUnitVersion? GetNUnitVersion (ProjectPackageReference p)
		{
			if (p.Include.IndexOf ("GuiUnit", StringComparison.OrdinalIgnoreCase) != -1)
				return NUnitVersion.NUnit2;
			if (p.Include.IndexOf ("nunit.framework", StringComparison.OrdinalIgnoreCase) != -1)
				return p.IsAtLeastVersion (new Version (3, 0)) ? NUnitVersion.NUnit3 : NUnitVersion.NUnit2;
			return null;
		}

		protected override SourceCodeLocation GetSourceCodeLocation (string fixtureTypeNamespace, string fixtureTypeName, string testName)
		{
			if (string.IsNullOrEmpty (fixtureTypeName))
				return null;
			var task = NUnitSourceCodeLocationFinder.TryGetSourceCodeLocationAsync (project, fixtureTypeNamespace, fixtureTypeName, testName);
			if (!task.Wait (2000))
				return null;
			return task.Result;

		}

		public override void Dispose ()
		{
			project.NameChanged -= OnProjectRenamed;
			IdeApp.ProjectOperations.EndBuild -= OnProjectBuilt;
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
			if (RefreshRequired)
				UpdateTests ();
		}

		public override void GetCustomTestRunner (out string assembly, out string type)
		{
			type = project.ProjectProperties.GetValue ("TestRunnerType");
			var asm = project.ProjectProperties.GetValue ("TestRunnerAssembly");
			assembly = asm != null ? project.BaseDirectory.Combine (asm.ToString ()).ToString () : null;
		}

		public override void GetCustomConsoleRunner (out string command, out string args)
		{
			var r = project.ProjectProperties.GetPathValue ("TestRunnerCommand");
			command = !string.IsNullOrEmpty (r) ? project.BaseDirectory.Combine (r).ToString () : null;
			args = project.ProjectProperties.GetValue ("TestRunnerArgs");
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

		protected override async Task<IEnumerable<string>> GetSupportAssembliesAsync ()
		{
			DotNetProject project = base.OwnerSolutionItem as DotNetProject;

			if (project != null) {
				var references = await project.GetReferences (IdeApp.Workspace.ActiveConfiguration).ConfigureAwait (false);
				// Referenced assemblies which are not in the gac and which are not localy copied have to be preloaded
				var supportAssemblies = references.Where (r => !r.IsCopyLocal && (!r.IsProjectReference || r.ReferenceOutputAssembly) && !r.IsFrameworkFile && !r.IsImplicit && !IsGacReference (r))
				                                  .Select (r => r.FilePath.FullPath.ToString ())
				                                  .Where (File.Exists)
				                                  .Distinct ();
				return supportAssemblies;
			}

			return Enumerable.Empty<string> ();
		}

		bool IsGacReference (AssemblyReference r) => string.Equals (r.Metadata.GetValue ("ResolvedFrom"), "{GAC}", StringComparison.OrdinalIgnoreCase);
	}
}

