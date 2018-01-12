//
// ExternalTestRunner.cs
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
using System.Linq;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using NUnit.Core;
using NUnit.Framework;
using NUnit.Core.Filters;

namespace MonoDevelop.UnitTesting.NUnit.External
{
	public class NUnitTestRunner: MarshalByRefObject
	{
		public NUnitTestRunner ()
		{
			PreloadAssemblies ();
		}

		public static void PreloadAssemblies ()
		{
			// Note: We need to load all nunit.*.dll assemblies before we do *anything* else in this class
			// This is to ensure that we always load the assemblies from the monodevelop directory and not
			// from the directory of the assembly under test. For example we wnat to load
			// /Applications/MonoDevelop/lib/Addins/nunit.framework.dll and not /user/app/foo/bin/debug/nunit.framework.dll

			// Force the loading of the NUnit.Framework assembly.
			// It's needed since that dll is not located in the test dll directory.
			var path = Path.GetDirectoryName (typeof(NUnitTestRunner).Assembly.Location);
			string nunitPath = Path.Combine (path, "nunit.framework.dll");
			string nunitCorePath = Path.Combine (path, "nunit.core.dll");
			string nunitCoreInterfacesPath = Path.Combine (path, "nunit.core.interfaces.dll");

			Assembly.LoadFrom (nunitCoreInterfacesPath);
			Assembly.LoadFrom (nunitCorePath);
			Assembly.LoadFrom (nunitPath);
		}

		public void Initialize ()
		{
			// Initialize ExtensionHost if not already done
			if ( !CoreExtensions.Host.Initialized )
				CoreExtensions.Host.InitializeService();
		}
		
		public TestResult Run (EventListener listener, string[] nameFilter, string path, string suiteName, string[] supportAssemblies, string testRunnerType, string testRunnerAssembly)
		{
			InitSupportAssemblies (supportAssemblies);

			ITestFilter filter = TestFilter.Empty;
			if (nameFilter != null && nameFilter.Length > 0)
				filter = new TestNameFilter (nameFilter);

			TestRunner tr;
			if (!string.IsNullOrEmpty (testRunnerType)) {
				Type runnerType;
				if (string.IsNullOrEmpty (testRunnerAssembly))
					runnerType = Type.GetType (testRunnerType, true);
				else {
					var asm = Assembly.LoadFrom (testRunnerAssembly);
					runnerType = asm.GetType (testRunnerType);
				}
				tr = (TestRunner)Activator.CreateInstance (runnerType);
			} else
				tr = new RemoteTestRunner ();

			TestPackage package = new TestPackage (path);
			if (!string.IsNullOrEmpty (suiteName))
				package.TestName = suiteName;
			tr.Load (package);
			return tr.Run (listener, filter, false, LoggingThreshold.All);
		}
		
		public NunitTestInfo GetTestInfo (string path, string[] supportAssemblies)
		{
			InitSupportAssemblies (supportAssemblies);
			TestSuite rootTS = new TestSuiteBuilder ().Build (new TestPackage (path));
			return BuildTestInfo (rootTS);
		}
		
		internal NunitTestInfo BuildTestInfo (Test test)
		{
			NunitTestInfo ti = new NunitTestInfo ();
			// The name of inherited tests include the base class name as prefix.
			// That prefix has to be removed
			string tname = test.TestName.Name;
			// Find the last index of the dot character that is not a part of the test parameters
			// Parameterized methods can contain '.' as class name & they don't seem to prefix base class name, so it's safe to skip them
			if (!(test.Parent is ParameterizedMethodSuite)) {
				int j = tname.IndexOf ('(');
				int i = tname.LastIndexOf ('.', (j == -1) ? (tname.Length - 1) : j);
				if (i != -1)
					tname = tname.Substring (i + 1);
			}

			if (test.FixtureType != null) {
				ti.FixtureTypeName = test.FixtureType.Name;
				ti.FixtureTypeNamespace = test.FixtureType.Namespace;
			} else if (test.TestType == "ParameterizedTest") {
				ti.FixtureTypeName = test.Parent.FixtureType.Name;
				ti.FixtureTypeNamespace = test.Parent.FixtureType.Namespace;
			}
			ti.Name = tname;
			ti.TestId = test.TestName.FullName;

			// Trim short name from end of full name to get the path
			string testNameWithDelimiter = "." + tname;
			if (test.TestName.FullName.EndsWith (testNameWithDelimiter)) {
				int pathLength = test.TestName.FullName.Length - testNameWithDelimiter.Length;
				ti.PathName = test.TestName.FullName.Substring(0, pathLength );
			}
			else
				ti.PathName = null;
			
			if (test.Tests != null && test.Tests.Count > 0) {
				ti.Tests = new NunitTestInfo [test.Tests.Count];
				for (int n=0; n<test.Tests.Count; n++)
					ti.Tests [n] = BuildTestInfo ((Test)test.Tests [n]);
			}
			ti.IsExplicit = test.RunState == RunState.Explicit;
			return ti;
		}
		
		void InitSupportAssemblies (string[] supportAssemblies)
		{
			// Preload support assemblies (they may not be in the test assembly directory nor in the gac)
			foreach (string asm in supportAssemblies) {
				try {
					Assembly.LoadFrom (asm);
				} catch (Exception e) {
					Console.WriteLine ("Couldn't load assembly {0}", asm);
					Console.WriteLine (e);
				}
			}
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
		
	[Serializable]
	public class TestNameFilter: ITestFilter
	{
		string[] names;
		
		public TestNameFilter (params string[] names)
		{
			this.names = names;
		}
		
		#region ITestFilter implementation 
		
		public bool Pass (ITest test)
		{
			if (!test.IsSuite && names.Any (n => test.TestName.FullName == n))
				return true;
			if (test.Tests != null) {
				foreach (ITest ct in test.Tests) {
					if (Pass (ct))
						return true;
				}
			}
			return false;
		}
		
		public bool Match (ITest test)
		{
			return Pass (test);
		}
		
		public bool IsEmpty {
			get {
				return false;
			}
		}
		
		#endregion 
	}
}

