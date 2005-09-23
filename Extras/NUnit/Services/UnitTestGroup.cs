//
// UnitTestGroup.cs
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
using MonoDevelop.Core.Services;
using System.Collections;
using MonoDevelop.Internal.Project;

namespace MonoDevelop.NUnit
{
	public class UnitTestGroup: UnitTest
	{
		UnitTestCollection tests;
		
		public UnitTestGroup (string name): base (name)
		{
		}
		
		protected UnitTestGroup (string name, CombineEntry ownerCombineEntry): base (name, ownerCombineEntry)
		{
		}
		
		public UnitTestCollection Tests {
			get {
				if (tests == null) {
					tests = new UnitTestCollection (this);
					OnCreateTests ();
				}
				return tests;
			}
		}
		
		public UnitTestCollection GetFailedTests (DateTime date)
		{
			UnitTestCollection col = new UnitTestCollection ();
			CollectFailedTests (col, date);
			return col;
		}
		
		void CollectFailedTests (UnitTestCollection col, DateTime date)
		{
			foreach (UnitTest t in Tests) {
				if (t is UnitTestGroup)
					((UnitTestGroup)t).CollectFailedTests (col, date);
				else {
					UnitTestResult res = t.Results.GetLastResult (date);
					if (res != null && res.IsFailure)
						col.Add (t);
				}
			}
		}
		
		public void UpdateTests ()
		{
			if (tests != null) {
				foreach (UnitTest t in tests)
					t.Dispose ();
				tests = null;
				OnTestChanged ();
			}
		}
		
		public override void SaveResults ()
		{
			base.SaveResults ();
			if (tests != null) {
				foreach (UnitTest t in tests)
					t.SaveResults ();
			}
		}
		
		
		public override int CountTestCases ()
		{
			int total = 0;
			foreach (UnitTest t in Tests)
				total += t.CountTestCases ();
			return total;
		}
		
		protected virtual void OnCreateTests ()
		{
		}
		
		protected override UnitTestResult OnRun (TestContext testContext)
		{
			UnitTestResult tres = new UnitTestResult ();
			OnBeginTest (testContext);
			
			try {
				foreach (UnitTest t in Tests) {
					UnitTestResult res;
					try {
						res = OnRunChildTest (t, testContext);
						if (testContext.Monitor.IsCancelRequested)
							break;
					} catch (Exception ex) {
						res = UnitTestResult.CreateFailure (ex);
					}
					tres.Time += res.Time;
					tres.Status |= res.Status;
					tres.TotalFailures += res.TotalFailures;
					tres.TotalSuccess += res.TotalSuccess;
					tres.TotalIgnored += res.TotalIgnored;
				}
			} finally {
				OnEndTest (testContext);
			}
			return tres;
		}
		
		protected virtual void OnBeginTest (TestContext testContext)
		{
		}
		
		protected virtual UnitTestResult OnRunChildTest (UnitTest test, TestContext testContext)
		{
			return test.Run (testContext);
		}
		
		protected virtual void OnEndTest (TestContext testContext)
		{
		}
		
		internal override void FindRegressions (UnitTestCollection list, DateTime fromDate, DateTime toDate)
		{
			foreach (UnitTest test in Tests)
				test.FindRegressions (list, fromDate, toDate);
		}
		
		public override void Dispose ()
		{
			base.Dispose ();

			if (tests != null) {
				foreach (UnitTest t in tests)
					t.Dispose ();
			}
		}
		
	}
}

