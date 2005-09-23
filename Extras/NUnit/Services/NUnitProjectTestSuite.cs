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

using MonoDevelop.Services;
using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Parser;

using NUnit.Core;

namespace MonoDevelop.NUnit
{
	public class NUnitProjectTestSuite: NUnitAssemblyTestSuite
	{
		Project project;
		DateTime lastAssemblyTime;
		string resultsPath;
		string storeId;
		
		public NUnitProjectTestSuite (Project project): base (project.Name, project)
		{
			storeId = Path.GetFileName (project.FileName);
			resultsPath = Path.Combine (project.BaseDirectory, "test-results");
			ResultsStore = new XmlResultsStore (resultsPath, storeId);
			this.project = project;
			lastAssemblyTime = GetAssemblyTime ();
			project.NameChanged += new CombineEntryRenamedEventHandler (OnProjectRenamed);
			Runtime.ProjectService.EndBuild += new ProjectCompileEventHandler (OnProjectBuilt);
		}
		
		public static NUnitProjectTestSuite CreateTest (Project project)
		{
			foreach (ProjectReference p in project.ProjectReferences)
				if (p.Reference.IndexOf ("nunit.framework") != -1)
					return new NUnitProjectTestSuite (project);
			return null;
		}

		protected override SourceCodeLocation GetSourceCodeLocation (string fullClassName, string methodName)
		{
			IParserContext ctx = Runtime.ProjectService.ParserDatabase.GetProjectParserContext (project);
			IClass cls = ctx.GetClass (fullClassName);
			if (cls == null)
				return null;
			
			foreach (IMethod met in cls.Methods) {
				if (met.Name == methodName)
					return new SourceCodeLocation (cls.Region.FileName, met.Region.BeginLine, met.Region.BeginColumn);
			}
			return new SourceCodeLocation (cls.Region.FileName, cls.Region.BeginLine, cls.Region.BeginColumn);
		}
		
		public override void Dispose ()
		{
			project.NameChanged -= new CombineEntryRenamedEventHandler (OnProjectRenamed);
			Runtime.ProjectService.EndBuild -= new ProjectCompileEventHandler (OnProjectBuilt);
			base.Dispose ();
		}
		
		void OnProjectRenamed (object sender, CombineEntryRenamedEventArgs e)
		{
			UnitTestGroup parent = Parent as UnitTestGroup;
			if (parent != null)
				parent.UpdateTests ();
		}
		
		void OnProjectBuilt (bool success)
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
			get { return project.GetOutputFileName (); }
		}
		
		protected override string TestInfoCachePath {
			get { return Path.Combine (resultsPath, storeId + ".test-cache"); }
		}
	}
}

