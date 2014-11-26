//
// XUnitProjectTestSuite.cs
//
// Author:
//       Sergey Khabibullin <sergey@khabibullin.com>
//
// Copyright (c) 2014 Sergey Khabibullin
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
using System.Linq;
using MonoDevelop.NUnit;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.XUnit
{
	/// <summary>
	/// Root test node for every project that has references to xunit dlls
	/// </summary>
	public class XUnitProjectTestSuite: XUnitAssemblyTestSuite
	{
		DotNetProject project;
		string resultsPath;
		string storeId;

		public XUnitProjectTestSuite (DotNetProject project): base (project.Name, project)
		{
			this.project = project;
			storeId = Path.GetFileName (project.FileName);
			resultsPath = GetTestResultsDirectory (project.BaseDirectory);
			ResultsStore = new BinaryResultsStore (resultsPath, storeId);
			project.NameChanged += new SolutionItemRenamedEventHandler (OnProjectRenamed);
			IdeApp.ProjectOperations.EndBuild += new BuildEventHandler (OnProjectBuilt);
		}

		public override string AssemblyPath {
			get {
				return project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
			}
		}

		public override string CachePath {
			get {
				return Path.Combine (resultsPath, storeId + ".xunit-test-cache");
			}
		}

		public override IList<string> SupportAssemblies {
			get {
				return project.References // references that are not copied localy
					.Where (r => !r.LocalCopy && r.ReferenceType != ReferenceType.Package)
					.SelectMany (r => r.GetReferencedFileNames (IdeApp.Workspace.ActiveConfiguration)).ToList ();
			}
		}

		public static UnitTest CreateTest (IWorkspaceObject entry)
		{
			var project = entry as DotNetProject;
			if (project != null) {
				foreach (var r in project.References) {
					if (r.Reference == "xunit")
						return new XUnitProjectTestSuite (project);
				}
			}

			return null;
		}

		public override SourceCodeLocation GetSourceCodeLocation (UnitTest unitTest)
		{
			XUnitTestInfo info = null;

			if (unitTest is XUnitTestCase)
				info = ((XUnitTestCase)unitTest).TestInfo;
			if (unitTest is XUnitTestSuite)
				info = ((XUnitTestSuite)unitTest).TestInfo;

			if (info == null || info.Type == null)
				return null;

			string namespaceName = "", className = "", methodName = info.Method;

			// extract namespace and class
			string[] nameParts = info.Type.Split ('.');
			if (nameParts.Length == 1) {
				className = nameParts [0];
			} else {
				namespaceName = String.Join (".", nameParts, 0, nameParts.Length - 1);
				className = nameParts [nameParts.Length - 1];
			}

			var compilation = TypeSystemService.GetCompilation (project);
			var type = compilation.MainAssembly.GetTypeDefinition (namespaceName, className);

			if (type == null)
				return null;

			// try to find the method's location
			var method = type.GetMethods ().FirstOrDefault (m => m.Name == methodName);
			if (method != null)
				return new SourceCodeLocation (method.Region.FileName, method.Region.BeginLine, method.Region.BeginColumn);

			// or at least return the location of the type
			return new SourceCodeLocation (type.Region.FileName, type.Region.BeginLine, type.Region.BeginColumn);
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

		public override void Dispose ()
		{
			project.NameChanged -= new SolutionItemRenamedEventHandler (OnProjectRenamed);
			IdeApp.ProjectOperations.EndBuild -= new BuildEventHandler (OnProjectBuilt);
			base.Dispose ();
		}

		static string GetTestResultsDirectory (string baseDirectory)
		{
			var cacheDir = TypeSystemService.GetCacheDirectory (baseDirectory, true);
			var resultsDir = Path.Combine (cacheDir, "test-results");

			if (!Directory.Exists (resultsDir))
				Directory.CreateDirectory (resultsDir);

			return resultsDir;
		}
	}
}
