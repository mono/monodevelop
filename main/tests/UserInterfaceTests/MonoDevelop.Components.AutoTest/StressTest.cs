// 
// StressTestAttribute.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Components.AutoTest
{
	public class StressTest
	{
		List<TestCase> cachedTests;
		
		public StressTest ()
		{
		}
		
		public AutoTestClientSession Session {
			get { return TestService.Session; }
		}
		
		protected virtual void Setup ()
		{
		}
		
		protected virtual void TearDown ()
		{
		}
	
		public void Run (TestPlan testPlan)
		{
			List<TestCase> tests = GetTests ();
			
			Setup ();
			for (int n=0; n<testPlan.Repeat; n++) {
				Console.WriteLine ("Test suite iteration: " + n);
				foreach (TestCase t in tests) {
					int ti = testPlan.GetIterationsForTest (t.Method.Name);
					for (int i=0; i<ti; i++) {
						Console.WriteLine ("Running test {0}: {1}", t.Method.Name, i);
						t.Method.Invoke (this, null);
						if (i < ti-1 && t.UndoMethod != null) {
							t.UndoMethod.Invoke (this, null);
						}
					}
				}
			}
			TearDown ();
		}
		
		public bool HasTest (string name)
		{
			return GetTests ().Any (t => t.Method.Name == name);
		}
		
		List<TestCase> GetTests ()
		{
			if (cachedTests != null)
				return cachedTests;
			
			List<TestCase> tests = new List<TestCase> ();
			
			foreach (MethodInfo met in GetType().GetMethods ())
			{
				foreach (TestStepAttribute sat in met.GetCustomAttributes (typeof(TestStepAttribute), true)) {
					TestCase tc = new TestCase () {
						Method = met,
						RunAfter = sat.RunAfter,
						Stage = sat.Stage
					};
					AddSorted (tests, tc);
				}
				foreach (UndoTestStepAttribute sat in met.GetCustomAttributes (typeof(UndoTestStepAttribute), true)) {
					AddUndo (tests, sat.Undoes, met);
				}
			}
			cachedTests = tests;
			return tests;
		}
		
		void AddSorted (List<TestCase> list, TestCase t)
		{
			for (int n=0; n<list.Count; n++) {
				if (list[n].Method.Name == t.RunAfter) {
					list.Insert (n+1, t);
					return;
				}
				if (list[n].RunAfter == t.Method.Name) {
					list.Insert (n, t);
					return;
				}
			}
			list.Add (t);
		}
		
		void AddUndo (List<TestCase> list, string methodName, MethodInfo undoer)
		{
			foreach (TestCase t in list) {
				if (t.Method.Name == methodName)
					t.UndoMethod = undoer;
			}
		}
	}
	
	class TestCase
	{
		public MethodInfo Method { get; set; }
		public MethodInfo UndoMethod { get; set; }
		public string RunAfter { get; set; }
		public string Stage { get; set; }
	}
	
	public class TestPlan
	{
		Dictionary<string,int> itersByTest = new Dictionary<string, int> ();
		
		public int Repeat { get; set; }
		
		public int Iterations { get; set; }
		
		public int GetIterationsForTest (string name)
		{
			int res;
			if (itersByTest.TryGetValue (name, out res))
				return res;
			else
				return Iterations > 0 ? Iterations : 1;
		}
		
		public void SetIterationsForTest (string name, int iterations)
		{
			itersByTest [name] = iterations;
		}
	}
}

