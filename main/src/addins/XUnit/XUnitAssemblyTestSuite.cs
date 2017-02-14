//
// XUnitAssemblyTestSuite.cs
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
using MonoDevelop.NUnit;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using System.Linq;
using System.IO;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.XUnit
{
	public abstract class XUnitAssemblyTestSuite: UnitTestGroup, IExecutableTest
	{
		object locker = new object ();

		static XUnitTestLoader loader = new XUnitTestLoader ();
		static XUnitTestExecutor executor = new XUnitTestExecutor ();
		XUnitTestInfoCache cache;

		public abstract string AssemblyPath { get; }
		public abstract string CachePath { get; }
		public abstract IList<string> SupportAssemblies { get; }

		DateTime lastAssemblyTime;
		XUnitExecutionSession session;

		public XUnitAssemblyTestSuite (string name): base (name)
		{
			cache = new XUnitTestInfoCache (this);
		}

		public XUnitAssemblyTestSuite (string name, DotNetProject project): base (name, project)
		{
			cache = new XUnitTestInfoCache (this);
		}

		public XUnitExecutionSession CreateExecutionSession ()
		{
			session = new XUnitExecutionSession (this);

			foreach (var test in Tests) {
				var xunitTest = test as IExecutableTest;
				if (xunitTest != null) {
					var childSession = xunitTest.CreateExecutionSession ();
					session.AddChildSession (childSession);
				}
			}

			return session;
		}

		public override bool HasTests {
			get {
				return true;
			}
		}

		protected bool RefreshRequired {
			get {
				return lastAssemblyTime != GetAssemblyTime ();
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

		protected override void OnCreateTests ()
		{
			lock (locker) {
				if (Status == TestStatus.Loading)
					return;

				var testInfo = cache.GetTestInfo ();
				if (testInfo != null) {
					FillTests (testInfo);
					return;
				}

				Status = TestStatus.Loading;
			}

			lastAssemblyTime = GetAssemblyTime ();

			loader.AsyncLoadTestInfo (this, cache);
		}

		public override IAsyncOperation Refresh ()
		{
			AsyncOperation oper = new AsyncOperation ();
			System.Threading.ThreadPool.QueueUserWorkItem (delegate {
				lock (locker) {
					try {
						while (Status == TestStatus.Loading) {
							Monitor.Wait (locker);
						}
						if (RefreshRequired) {
							lastAssemblyTime = GetAssemblyTime ();
							UpdateTests ();
							OnCreateTests (); // Force loading
							while (Status == TestStatus.Loading) {
								Monitor.Wait (locker);
							}
						}
						oper.SetCompleted (true);
					} catch {
						oper.SetCompleted (false);
					}
				}
			});
			return oper;
		}

		public void OnTestSuiteLoaded (XUnitTestInfo testInfo)
		{
			lock (locker) {
				Status = TestStatus.Ready;
				Monitor.PulseAll (locker);
			}

			DispatchService.GuiDispatch (delegate {
				AsyncCreateTests (testInfo);
			});
		}

		void AsyncCreateTests (XUnitTestInfo testInfo)
		{
			Tests.Clear ();
			FillTests (testInfo);
			cache.SetTestInfo (testInfo);
			OnTestChanged ();
		}

		void FillTests (XUnitTestInfo testInfo)
		{
			if (testInfo == null || testInfo.Tests == null)
				return;

			foreach (var child in testInfo.Tests) {
				UnitTest test;
				if (child.Tests == null)
					test = new XUnitTestCase (this, executor, child);
				else
					test = new XUnitTestSuite (this, executor, child);
				Tests.Add (test);
			}
		}

		protected override UnitTestResult OnRun (TestContext testContext)
		{
			return executor.RunAssemblyTestSuite (this, testContext);
		}

		protected override void OnActiveConfigurationChanged ()
		{
			UpdateTests ();
			base.OnActiveConfigurationChanged ();
		}

		public override void Dispose ()
		{
			try {
				if (CachePath != null) {
					cache.WriteToDisk ();
				}
			} catch {
			}
			base.Dispose ();
		}
	}
}

