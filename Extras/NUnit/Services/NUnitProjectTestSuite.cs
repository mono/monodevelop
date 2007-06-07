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


using System;
using System.Reflection;
using System.IO;
using System.Collections;

using MonoDevelop.Core;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Projects.Item;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;

using NUnit.Core;

namespace MonoDevelop.NUnit
{
	public class NUnitProjectTestSuite: NUnitAssemblyTestSuite
	{
		IProject project;
		DateTime lastAssemblyTime;
		string resultsPath;
		string storeId;
		
		public NUnitProjectTestSuite (IProject project): base (project.Name, project)
		{
			storeId = Path.GetFileName (project.FileName);
			resultsPath = Path.Combine (project.BasePath, "test-results");
			ResultsStore = new XmlResultsStore (resultsPath, storeId);
			this.project = project;
			lastAssemblyTime = GetAssemblyTime ();
			project.NameChanged += new EventHandler<RenameEventArgs> (OnProjectRenamed);
			ProjectService.EndBuild  += new EventHandler<BuildEventArgs> (OnProjectBuilt);
		}
		
		public static NUnitProjectTestSuite CreateTest (IProject project)
		{
			foreach (ProjectItem item in project.Items) {
				ReferenceProjectItem p = item as ReferenceProjectItem;
				if (p == null)
					continue;
				if (p.Include.IndexOf ("nunit.framework") != -1)
					return new NUnitProjectTestSuite (project);
			}
			return null;
		}

		protected override SourceCodeLocation GetSourceCodeLocation (string fullClassName, string methodName)
		{
// TODO: Project Conversion
//			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (project);
//			IClass cls = ctx.GetClass (fullClassName);
//			if (cls == null)
//				return null;
//			
//			foreach (IMethod met in cls.Methods) {
//				if (met.Name == methodName)
//					return new SourceCodeLocation (cls.Region.FileName, met.Region.BeginLine, met.Region.BeginColumn);
//			}
//			return new SourceCodeLocation (cls.Region.FileName, cls.Region.BeginLine, cls.Region.BeginColumn);
			return null;
		}
		
		public override void Dispose ()
		{
			project.NameChanged -= new EventHandler<RenameEventArgs> (OnProjectRenamed);
			ProjectService.EndBuild -= new EventHandler<BuildEventArgs> (OnProjectBuilt);
			base.Dispose ();
		}
		
		void OnProjectRenamed (object sender, RenameEventArgs e)
		{
			UnitTestGroup parent = Parent as UnitTestGroup;
			if (parent != null)
				parent.UpdateTests ();
		}
		
		void OnProjectBuilt (object s, BuildEventArgs args)
		{
			if (lastAssemblyTime != GetAssemblyTime ()) {
				lastAssemblyTime = GetAssemblyTime ();
				UpdateTests ();
			}
		}
		
		DateTime GetAssemblyTime ()
		{
			string path = AssemblyPath;
			if (File.Exists (path))
				return File.GetLastWriteTime (path);
			else
				return DateTime.MinValue;
		}
	
		protected override string AssemblyPath {
			get { return ProjectService.GetOutputFileName (project); }
		}
		
		protected override string TestInfoCachePath {
			get { return Path.Combine (resultsPath, storeId + ".test-cache"); }
		}
	}
}

