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
using System.Collections.Generic;
using NUnit.Core;
namespace MonoDevelop.NUnit.External
{
	public class NUnitTestRunner: MarshalByRefObject
	{
		public void PreloadAssemblies (string nunitPath, string nunitCorePath, string nunitCoreInterfacesPath)
		{
			// Note: We need to load all nunit.*.dll assemblies before we do *anything* else in this class
			// This is to ensure that we always load the assemblies from the monodevelop directory and not
			// from the directory of the assembly under test. For example we wnat to load
			// /Applications/MonoDevelop/lib/Addins/nunit.framework.dll and not /user/app/foo/bin/debug/nunit.framework.dll

			// In some cases MS.NET can't properly resolve assemblies even if they
			// are already loaded. For example, when deserializing objects from remoting.
			AppDomain.CurrentDomain.AssemblyResolve += delegate (object s, ResolveEventArgs args) {
				foreach (Assembly am in AppDomain.CurrentDomain.GetAssemblies ()) {
					if (am.GetName ().FullName == args.Name)
						return am;
				}
				return null;
			};
			
			// Force the loading of the NUnit.Framework assembly.
			// It's needed since that dll is not located in the test dll directory.
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
		
		public TestResult Run (EventListener listener, ITestFilter filter, string path, string suiteName, List<string> supportAssemblies, string testRunnerType, string testRunnerAssembly)
		{
			InitSupportAssemblies (supportAssemblies);
			
			if (filter == null)
				filter = TestFilter.Empty;

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

			var package = new TestPackage (path);
			if (!string.IsNullOrEmpty (suiteName))
				package.TestName = suiteName;
			tr.Load (package);
			return tr.Run (listener, filter, false, LoggingThreshold.All);
		}
		
		public NunitTestInfo GetTestInfo (string path, List<string> supportAssemblies)
		{
			InitSupportAssemblies (supportAssemblies);
			
			TestSuite rootTS = new TestSuiteBuilder ().Build (new TestPackage (path));
			return BuildTestInfo (rootTS);
		}
		
		NunitTestInfo BuildTestInfo (Test test)
		{
			var ti = new NunitTestInfo ();
			// The name of inherited tests include the base class name as prefix.
			// That prefix has to be removed
			string tname = test.TestName.Name;
			int i = tname.LastIndexOf ('.');
			if (i != -1)
				tname = tname.Substring (i + 1);
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
			if (test.TestName.FullName.EndsWith (testNameWithDelimiter, StringComparison.Ordinal)) {
				int pathLength = test.TestName.FullName.Length - testNameWithDelimiter.Length;
				ti.PathName = test.TestName.FullName.Substring (0, pathLength);
			} else
				ti.PathName = null;
			
			if (test.Tests != null && test.Tests.Count > 0) {
				ti.Tests = new NunitTestInfo [test.Tests.Count];
				for (int n=0; n<test.Tests.Count; n++)
					ti.Tests [n] = BuildTestInfo ((Test)test.Tests [n]);
			}
			return ti;
		}
		
		void InitSupportAssemblies (List<string> supportAssemblies)
		{
			// Preload support assemblies (they may not be in the test assembly directory nor in the gac)
			foreach (string asm in supportAssemblies)
				Assembly.LoadFrom (asm);
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}

	[Serializable]
	public class NunitTestInfo
	{
		public string Name;
		public string PathName;
		public string TestId;
		public string FixtureTypeName;
		public string FixtureTypeNamespace;
		public NunitTestInfo[] Tests;
	}
		
	[Serializable]
	public class TestNameFilter: ITestFilter
	{
		readonly string[] names;
		
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

