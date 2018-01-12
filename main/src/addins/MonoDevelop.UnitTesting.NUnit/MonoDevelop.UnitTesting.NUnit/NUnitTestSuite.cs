//
// NUnitTestSuite.cs
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


using MonoDevelop.UnitTesting.NUnit.External;

namespace MonoDevelop.UnitTesting.NUnit
{
	class NUnitTestSuite: UnitTestGroup
	{
		NunitTestInfo testInfo;
		NUnitAssemblyTestSuite rootSuite;
		string fullName;
		
		public NUnitTestSuite (NUnitAssemblyTestSuite rootSuite, NunitTestInfo tinfo): base (tinfo.Name)
		{
			fullName = !string.IsNullOrEmpty (tinfo.PathName) ? tinfo.PathName + "." + tinfo.Name : tinfo.Name;
			this.testInfo = tinfo;
			this.rootSuite = rootSuite;
			this.TestId = tinfo.TestId;
			this.canMergeWithParent =  !string.IsNullOrEmpty (tinfo.PathName) &&
									   string.IsNullOrEmpty (tinfo.FixtureTypeName) &&
									   string.IsNullOrEmpty (tinfo.FixtureTypeNamespace);
		}

		bool canMergeWithParent;
		public override bool CanMergeWithParent {
			get {
				return canMergeWithParent;
			}
		}
		
		public override bool HasTests {
			get {
				return true;
			}
		}
		
		public string ClassName {
			get { return fullName; }
		}
		
		protected override UnitTestResult OnRun (TestContext testContext)
		{
			return rootSuite.RunUnitTest (this, fullName, fullName, null, testContext);
		}
		
		protected override bool OnCanRun (MonoDevelop.Core.Execution.IExecutionHandler executionContext)
		{
			return rootSuite.CanRun (executionContext);
		}

		
		protected override void OnCreateTests ()
		{
			if (testInfo.Tests == null)
				return;
			
			foreach (NunitTestInfo test in testInfo.Tests) {
				UnitTest newTest;
				if (test.Tests != null)
					newTest = new NUnitTestSuite (rootSuite, test);
				else
					newTest = new NUnitTestCase (rootSuite, test, ClassName);
				newTest.FixtureTypeName = test.FixtureTypeName;
				newTest.FixtureTypeNamespace = test.FixtureTypeNamespace;
				Tests.Add (newTest);
			}
		}
		
		public override SourceCodeLocation SourceCodeLocation {
			get {
				UnitTest p = Parent;
				while (p != null) {
					NUnitAssemblyTestSuite root = p as NUnitAssemblyTestSuite;
					if (root != null)
						return root.GetSourceCodeLocation (this);
					p = p.Parent;
				}
				return null; 
			}
		}
	}
}

