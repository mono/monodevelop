//
// VsTestNamespaceTestGroup.cs
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
using MonoDevelop.Projects;
using MonoDevelop.UnitTesting;

namespace MonoDevelop.UnitTesting.VsTest
{
	class VsTestNamespaceTestGroup : UnitTestGroup, IVsTestTestProvider
	{
		public Project Project { get; private set; }
		VsTestNamespaceTestGroup currentNamespace;
		VsTestTestClass currentClass;
		IVsTestTestRunner testRunner;

		public VsTestNamespaceTestGroup (IVsTestTestRunner testRunner, UnitTestGroup parent, Project project, string name)
			: base (name)
		{
			this.Project = project;
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
				var VsTestTest = new VsTestUnitTest (testRunner, test, Project);
				AddTest (VsTestTest);
			}
		}

		void AddTest (VsTestUnitTest VsTestTest)
		{
			string childNamespace = VsTestTest.GetChildNamespace (FixtureTypeNamespace);
			if (string.IsNullOrEmpty (childNamespace)) {
				if (currentClass == null || currentClass.FixtureTypeName != VsTestTest.FixtureTypeName) {
					currentClass = new VsTestTestClass (testRunner, Project, VsTestTest.FixtureTypeName);
					Tests.Add (currentClass);
				}
				currentClass.Tests.Add (VsTestTest);
			} else if (currentNamespace.Name == childNamespace) {
				currentNamespace.AddTest (VsTestTest);
			} else {
				currentNamespace = new VsTestNamespaceTestGroup (testRunner, currentNamespace, Project, childNamespace);
				currentNamespace.AddTest (VsTestTest);
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
			return testRunner.RunTest (testContext, this).Result;
		}

		public IEnumerable<TestCase> GetTests ()
		{
			foreach (IVsTestTestProvider testProvider in Tests) {
				foreach (TestCase childTest in testProvider.GetTests ()) {
					yield return childTest;
				}
			}
		}
	}
}
