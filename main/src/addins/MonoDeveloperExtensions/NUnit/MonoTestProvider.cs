//
// MonoTestProvider.cs
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
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.NUnit;
using System.Collections.Generic;

namespace MonoDeveloper
{	
	class MonoTestProvider: ITestProvider
	{
		public IEnumerable<UnitTest> CreateUnitTests (IWorkspaceObject entry)
		{
			if (entry is DotNetProject) {
				DotNetProject project = (DotNetProject) entry;
				MonoSolutionItemHandler handler = ProjectExtensionUtil.GetItemHandler (project) as MonoSolutionItemHandler;
				if (handler != null) {
					if (handler.UnitTest != null) {
						yield return (UnitTest)handler.UnitTest;
					} else {
						string testFileBase = handler.GetTestFileBase ();
						UnitTest testSuite = new MonoTestSuite (project, project.Name, testFileBase);
						handler.UnitTest = testSuite;
						yield return testSuite;
					}
				}
			}
		}
		
		public Type[] GetOptionTypes ()
		{
			return null;
		}
	}
	
	class MonoTestSuite: NUnitAssemblyTestSuite
	{
		string basePath;
		
		public MonoTestSuite (Project p, string name, string basePath): base (name, p)
		{
			this.basePath = basePath;
		}
		
		protected override string AssemblyPath {
			get {
				return basePath + "default.dll";
			}
		}
		
		protected override string TestInfoCachePath {
			get { return Path.Combine (Path.GetDirectoryName (AssemblyPath), "test-cache"); }
		}
	}
}
