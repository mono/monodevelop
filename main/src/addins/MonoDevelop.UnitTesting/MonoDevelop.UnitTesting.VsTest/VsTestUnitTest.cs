//
// VsTestUnitTest.cs
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
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace MonoDevelop.UnitTesting.VsTest
{
	class VsTestUnitTest : UnitTest, IVsTestTestProvider
	{
		public Project Project { get; private set; }
		TestCase test;
		IVsTestTestRunner testRunner;
		string name;
		SourceCodeLocation sourceCodeLocation;

		public VsTestUnitTest (IVsTestTestRunner testRunner, TestCase test, Project project)
			: base (test.DisplayName)
		{
			this.Project = project;
			this.testRunner = testRunner;
			this.test = test;

			Init ();
		}

		void Init ()
		{
			TestId = test.Id.ToString ();
			if (!string.IsNullOrEmpty (test.CodeFilePath))
				sourceCodeLocation = new SourceCodeLocation (test.CodeFilePath, test.LineNumber, 0);
			else {
				TypeSystemService.GetCompilationAsync (Project).ContinueWith ((t) => {
					var dotIndex = test.FullyQualifiedName.LastIndexOf (".", StringComparison.Ordinal);
					var className = test.FullyQualifiedName.Remove (dotIndex);
					var methodName = test.FullyQualifiedName.Substring (dotIndex + 1);
					var bracketIndex = methodName.IndexOf ('(');
					if (bracketIndex != -1)
						methodName = methodName.Remove (bracketIndex).Trim ();
					var compilation = t.Result;
					if (compilation == null)
						return;
					var cls = compilation.GetTypeByMetadataName (className);
					if (cls == null)
						return;
					IMethodSymbol method = null;
					while ((method = cls.GetMembers (methodName).OfType<IMethodSymbol> ().FirstOrDefault ()) == null) {
						cls = cls.BaseType;
						if (cls == null)
							return;
					}
					if (method == null)
						return;
					var source = method.Locations.FirstOrDefault (l => l.IsInSource);
					if (source == null)
						return;
					var line = source.GetLineSpan ();
					sourceCodeLocation = new SourceCodeLocation (source.SourceTree.FilePath, line.StartLinePosition.Line, line.StartLinePosition.Character);
				}).Ignore ();
			}

			int index = test.FullyQualifiedName.LastIndexOf ('.');
			if (index > 0) {
				FixtureTypeName = test.FullyQualifiedName.Substring (0, index);

				index = FixtureTypeName.LastIndexOf ('.');
				if (index > 0) {
					FixtureTypeNamespace = FixtureTypeName.Substring (0, index);
					FixtureTypeName = FixtureTypeName.Substring (index + 1);
				} else {
					FixtureTypeNamespace = string.Empty;
				}
			} else {
				FixtureTypeNamespace = string.Empty;
				FixtureTypeName = string.Empty;
			}

			index = test.DisplayName.LastIndexOf ('.');
			if (index > 0) {
				name = test.DisplayName.Substring (index + 1);
			} else {
				name = test.DisplayName;
			}
		}

		protected override UnitTestResult OnRun (TestContext testContext)
		{
			return testRunner.RunTest (testContext, this).Result;
		}

		protected override bool OnCanRun (IExecutionHandler executionContext)
		{
			return testRunner.CanRunTests (executionContext);
		}

		public override SourceCodeLocation SourceCodeLocation {
			get { return sourceCodeLocation; }
		}

		public override string Name {
			get { return name; }
		}

		public string GetChildNamespace (string name)
		{
			string childNamespace = FixtureTypeNamespace;
			if (name.Length > 0) {
				if (name.Length >= FixtureTypeNamespace.Length) {
					return String.Empty;
				}
				childNamespace = FixtureTypeNamespace.Substring (name.Length + 1);
			}

			int index = childNamespace.IndexOf ('.');
			if (index >= 0) {
				childNamespace = childNamespace.Substring (0, index);
			}

			return childNamespace;
		}

		public IEnumerable<TestCase> GetTests ()
		{
			yield return test;
		}
	}
}
