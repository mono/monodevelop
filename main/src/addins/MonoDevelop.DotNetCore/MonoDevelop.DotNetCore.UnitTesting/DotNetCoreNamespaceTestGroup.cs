//
// DotNetCoreNamespaceTestGroup.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MonoDevelop.Core.Execution;
using MonoDevelop.UnitTesting;

namespace MonoDevelop.DotNetCore.UnitTesting
{
	class DotNetCoreNamespaceTestGroup : UnitTestGroup, IDotNetCoreTestProvider
	{
		DotNetCoreNamespaceTestGroup currentNamespace;
		DotNetCoreTestClass currentClass;
		IDotNetCoreTestRunner testRunner;

		public DotNetCoreNamespaceTestGroup (IDotNetCoreTestRunner testRunner, UnitTestGroup parent, string name)
			: base (name)
		{
			currentNamespace = this;
			this.testRunner = testRunner;

			if (parent == null || String.IsNullOrEmpty (parent.FixtureTypeNamespace)) {
				FixtureTypeNamespace = name;
			} else {
				FixtureTypeNamespace = parent.FixtureTypeNamespace + "." + name;
			}
		}

		public void AddTests (IEnumerable<TestCase> tests)
		{
			foreach (TestCase test in tests) {
				var dotNetCoreTest = new DotNetCoreUnitTest (testRunner, test);
				AddTest (dotNetCoreTest);
			}
		}

		void AddTest (DotNetCoreUnitTest dotNetCoreTest)
		{
			string childNamespace = dotNetCoreTest.GetChildNamespace (FixtureTypeNamespace);
			if (string.IsNullOrEmpty (childNamespace)) {
				if (currentClass == null || currentClass.FixtureTypeName != dotNetCoreTest.FixtureTypeName) {
					currentClass = new DotNetCoreTestClass (testRunner, dotNetCoreTest.FixtureTypeName);
					Tests.Add (currentClass);
				}
				currentClass.Tests.Add (dotNetCoreTest);
			} else if (currentNamespace.Name == childNamespace) {
				currentNamespace.AddTest (dotNetCoreTest);
			} else {
				currentNamespace = new DotNetCoreNamespaceTestGroup (testRunner, currentNamespace, childNamespace);
				currentNamespace.AddTest (dotNetCoreTest);
				Tests.Add (currentNamespace);
			}
		}

		protected override bool OnCanRun (IExecutionHandler executionContext)
		{
			return testRunner.CanRunTests (executionContext);
		}

		public override bool HasTests {
			get { return true; }
		}

		protected override UnitTestResult OnRun (TestContext testContext)
		{
			return testRunner.RunTest (testContext, this);
		}

		public IEnumerable<TestCase> GetTests ()
		{
			foreach (IDotNetCoreTestProvider testProvider in Tests) {
				foreach (TestCase childTest in testProvider.GetTests ()) {
					yield return childTest;
				}
			}
		}
	}
}
