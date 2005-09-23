//
// NUnitAssemblyTestSuite.cs
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
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using NUnit.Core;

namespace MonoDevelop.NUnit
{
	public abstract class NUnitAssemblyTestSuite: UnitTestGroup
	{
		object locker = new object ();
		UnitTest[] oldList;
		TestInfoCache testInfoCache = new TestInfoCache ();
		bool cacheLoaded;
		
		static Queue loadQueue = new Queue ();
		static bool loaderRunning;
		
		public NUnitAssemblyTestSuite (string name): base (name)
		{
		}
		
		public NUnitAssemblyTestSuite (string name, CombineEntry ownerCombineEntry): base (name, ownerCombineEntry)
		{
		}
		
		public override void Dispose ()
		{
			try {
				if (TestInfoCachePath != null) {
					Console.WriteLine ("saving TestInfoCachePath = " + TestInfoCachePath);
					testInfoCache.Write (TestInfoCachePath);
				}
			} catch {
			}
		}
		
		protected override void OnActiveConfigurationChanged ()
		{
			UpdateTests ();
			base.OnActiveConfigurationChanged ();
		}
		
		internal SourceCodeLocation GetSourceCodeLocation (UnitTest test)
		{
			if (test is NUnitTestCase) {
				NUnitTestCase t = (NUnitTestCase) test;
				return GetSourceCodeLocation (t.ClassName, t.Name);
			} else if (test is NUnitTestSuite) {
				NUnitTestSuite t = (NUnitTestSuite) test;
				return GetSourceCodeLocation (t.ClassName, null);
			} else
				return null;
		}
		
		protected virtual SourceCodeLocation GetSourceCodeLocation (string fullClassName, string methodName)
		{
			return null;
		}
		
		protected override void OnCreateTests ()
		{
			lock (locker) {
				if (Status == TestStatus.Loading)
					return;
					
				TestInfo ti = testInfoCache.GetInfo (AssemblyPath);
				if (ti != null) {
					FillTests (ti);
					return;
				}
				
				Status = TestStatus.Loading;
			}
			
			if (oldList != null) {
				foreach (UnitTest t in oldList)
					Tests.Add (t);
			}

			OnTestStatusChanged ();
			
			LoadData ld = new LoadData ();
			ld.Path = AssemblyPath;
			ld.TestInfoCachePath = cacheLoaded ? null : TestInfoCachePath;
			ld.Callback = new WaitCallback (AsyncCreateTests);
			
			AsyncLoadTest (ld);

			// Read the cache from disk only once
			cacheLoaded = true;
		}
		
		void AsyncCreateTests (object ob)
		{
			TestStatus newStatus = TestStatus.Ready;
			
			try {
				LoadData loadData = (LoadData) ob;
				
				if (loadData.Error != null) {
					newStatus = TestStatus.LoadError;
					return;
				}
				
				Tests.Clear ();

				if (loadData.Info == null) {
					oldList = new UnitTest [0];
					return;
				}

				FillTests (loadData.Info);
				
				// If the async loader has loaded a cache, reuse it.
				if (loadData.InfoCache != null)
					testInfoCache = loadData.InfoCache;
				
				testInfoCache.SetInfo (AssemblyPath, loadData.Info);
			}
			catch (Exception ex) {
				Console.WriteLine (ex);
				newStatus = TestStatus.LoadError;
			}
			finally {
				lock (locker) {
					Status = newStatus;
				}
				OnTestChanged ();
			}
		}
		
		void FillTests (TestInfo ti)
		{
			if (ti.Tests == null) return;
			foreach (TestInfo test in ti.Tests) {
				if (test.Tests != null)
					Tests.Add (new NUnitTestSuite (this, test));
				else
					Tests.Add (new NUnitTestCase (this, test));
			}
			oldList = new UnitTest [Tests.Count];
			Tests.CopyTo (oldList, 0);
		}
		
		static void AsyncLoadTest (LoadData ld)
		{
			lock (loadQueue) {
				if (!loaderRunning) {
					Thread t = new Thread (new ThreadStart (RunAsyncLoadTest));
					t.IsBackground = true;
					t.Start ();
					loaderRunning = true;
				}
				loadQueue.Enqueue (ld);
				Monitor.Pulse (loadQueue);
			}
		}
		
		static void RunAsyncLoadTest ()
		{
			while (true)
			{
				LoadData ld;
				lock (loadQueue) {
					if (loadQueue.Count == 0) {
						if (!Monitor.Wait (loadQueue, 5000, true)) {
							loaderRunning = false;
							return;
						}
					}
					ld = (LoadData)loadQueue.Dequeue ();
				}
				
				try {
					// If the information is cached in a file and it is up to date information,
					// there is no need to parse again the assembly.

					if (ld.TestInfoCachePath != null && File.Exists (ld.TestInfoCachePath)) {
						ld.InfoCache = TestInfoCache.Read (ld.TestInfoCachePath);
						TestInfo info = ld.InfoCache.GetInfo (ld.Path);
						if (info != null) {
							ld.Info = info;
							ld.Callback (ld);
							continue;
						}
					}
				} catch (Exception ex) {
					Console.WriteLine (ex);	// Remove this
				}
				
				ExternalTestRunner runner = null;
				
				try {
					if (File.Exists (ld.Path)) {
						runner = (ExternalTestRunner) Runtime.ProcessService.CreateExternalProcessObject (typeof(ExternalTestRunner), false);
						ld.Info = runner.GetTestInfo (ld.Path);
					}
				} catch (Exception ex) {
					ld.Error = ex;
				}
				finally {
					try {
						if (runner != null)
							runner.Dispose ();
					} catch {}
				}
				
				try {
					ld.Callback (ld);
				} catch {
				}
			}
		}
		
		protected override UnitTestResult OnRun (TestContext testContext)
		{
			return RunUnitTest (this, "", testContext);
		}
		
		internal UnitTestResult RunUnitTest (UnitTest test, string suiteName, TestContext testContext)
		{
			ExternalTestRunner runner = (ExternalTestRunner) Runtime.ProcessService.CreateExternalProcessObject (typeof(ExternalTestRunner), false);
			LocalTestMonitor localMonitor = new LocalTestMonitor (testContext, runner, test, suiteName);
			
			IFilter filter = null;
			
			NUnitCategoryOptions categoryOptions = (NUnitCategoryOptions) test.GetOptions (typeof(NUnitCategoryOptions));
			if (categoryOptions.EnableFilter && categoryOptions.Categories.Count > 0) {
				string[] cats = new string [categoryOptions.Categories.Count];
				categoryOptions.Categories.CopyTo (cats, 0);
				filter = new CategoryFilter (cats, categoryOptions.Exclude);
			}
			
			RunData rd = new RunData ();
			rd.Runner = runner;
			rd.Test = this;
			testContext.Monitor.CancelRequested += new TestHandler (rd.Cancel);
			
			UnitTestResult result;
			
			try {
				TestResult res = runner.Run (localMonitor, filter, AssemblyPath, suiteName, null);
				result = localMonitor.GetLocalTestResult (res);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				if (localMonitor.RunningTest != null) {
					RuntimeErrorCleanup (testContext, localMonitor.RunningTest, ex);
				} else {
					testContext.Monitor.ReportRuntimeError (null, ex);
					throw ex;
				}
				result = UnitTestResult.CreateFailure (ex);
			} finally {
				testContext.Monitor.CancelRequested -= new TestHandler (rd.Cancel);
				runner.Dispose ();
			}
			
			return result;
		}
		
		void RuntimeErrorCleanup (TestContext testContext, UnitTest t, Exception ex)
		{
			UnitTestResult result = UnitTestResult.CreateFailure (ex);
			t.RegisterResult (testContext, result);
			while (t != null && t != this) {
				testContext.Monitor.EndTest (t, result);
				t.Status = TestStatus.Ready;
				t = t.Parent;
			}
		}
		
		protected abstract string AssemblyPath {
			get;
		}
		
		// File where cached test info for this test suite will be saved
		// Returns null by default which means that test info will not be saved.
		protected virtual string TestInfoCachePath {
			get { return null; }
		}
		
		class LoadData
		{
			public string Path;
			public string TestInfoCachePath;
			public Exception Error;
			public TestInfo Info;
			public TestInfoCache InfoCache;
			public WaitCallback Callback;
		}
		
		class RunData
		{
			public ExternalTestRunner Runner;
			public UnitTest Test;
			
			public void Cancel ()
			{
				Runner.Dispose ();
				ClearRunningStatus (Test);
			}
			
			void ClearRunningStatus (UnitTest t)
			{
				t.Status = TestStatus.Ready;
				UnitTestGroup group = t as UnitTestGroup;
				if (group == null) return;
				foreach (UnitTest ct in group.Tests)
					ClearRunningStatus (ct);
			}
		}
		
		[Serializable]
		class TestInfoCache
		{
			Hashtable table = new Hashtable ();
			
			[NonSerialized]
			bool modified;
			
			public void SetInfo (string path, TestInfo info)
			{
				if (File.Exists (path)) {
					CachedTestInfo cti = new CachedTestInfo ();
					cti.LastWriteTime = File.GetLastWriteTime (path);
					cti.Info = info;
					table [path] = cti;
					modified = true;
				}
			}
			
			public TestInfo GetInfo (string path)
			{
				CachedTestInfo cti = (CachedTestInfo) table [path];
				if (cti != null && File.Exists (path) && File.GetLastWriteTime (path) == cti.LastWriteTime)
					return cti.Info;
				else
					return null;
			}
			
			public static TestInfoCache Read (string file)
			{
				BinaryFormatter bf = new BinaryFormatter ();
				Stream s = new FileStream (file, FileMode.Open, FileAccess.Read);
				try {
					return (TestInfoCache) bf.Deserialize (s);
				} finally {
					s.Close ();
				}
			}
			
			public void Write (string file)
			{
				if (modified) {
					BinaryFormatter bf = new BinaryFormatter ();
					Stream s = new FileStream (file, FileMode.Create, FileAccess.Write);
					try {
						bf.Serialize (s, this);
					} finally {
						s.Close ();
					}
				}
			}
		}
		
		[Serializable]
		class CachedTestInfo
		{
			public DateTime LastWriteTime;
			public TestInfo Info;
		}
	}
}

