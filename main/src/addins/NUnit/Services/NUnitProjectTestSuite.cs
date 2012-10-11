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
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using System;

namespace MonoDevelop.NUnit
{
	public class NUnitProjectTestSuite: NUnitAssemblyTestSuite
	{
		DotNetProject project;
		string resultsPath;
		string storeId;

		public override IList<string> UserAssemblyPaths {
			get {
				return project.GetUserAssemblyPaths (project.ParentSolution.DefaultConfigurationSelector);
			}
		}

		public NUnitProjectTestSuite (DotNetProject project): base (project.Name, project)
		{
			storeId = Path.GetFileName (project.FileName);
			resultsPath = Path.Combine (project.BaseDirectory, "test-results");
			ResultsStore = new XmlResultsStore (resultsPath, storeId);
			this.project = project;
			project.NameChanged += new SolutionItemRenamedEventHandler (OnProjectRenamed);
			IdeApp.ProjectOperations.EndBuild += new BuildEventHandler (OnProjectBuilt);
		}
		
		public static NUnitProjectTestSuite CreateTest (DotNetProject project)
		{
			foreach (var p in project.References)
				if (p.Reference.IndexOf ("nunit.framework") != -1 || p.Reference.IndexOf ("nunit.core") != -1)
					return new NUnitProjectTestSuite (project);
			return null;
		}

		protected override SourceCodeLocation GetSourceCodeLocation (string fixtureTypeNamespace, string fixtureTypeName, string methodName)
		{
			if (fixtureTypeName == null)
				return null;
			var ctx = TypeSystemService.GetCompilation (project);
			var cls = ctx.MainAssembly.GetTypeDefinition (fixtureTypeNamespace, fixtureTypeName, 0);
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
			if (RefreshRequired)
				UpdateTests ();
		}

		protected override UnitTestResult OnRun (TestContext testContext)
		{
			TestRunnerType = (string) project.ExtendedProperties ["TestRunnerType"];
			var asm = project.ExtendedProperties ["TestRunnerAssembly"];
			TestRunnerAssembly = asm != null ? project.BaseDirectory.Combine (asm.ToString ()).ToString () : null;
			return base.OnRun (testContext);
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
						if (pr.ReferenceType != ReferenceType.Package && !pr.LocalCopy) {
							foreach (string file in pr.GetReferencedFileNames (IdeApp.Workspace.ActiveConfiguration))
								yield return file;
						}
					}
				}
			}
		}
	}
}

