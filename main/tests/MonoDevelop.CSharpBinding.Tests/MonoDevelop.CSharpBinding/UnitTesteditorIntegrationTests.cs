//
// UnitTesteditorIntegrationTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using NUnit.Framework;
using MonoDevelop.CSharp;
using System.Threading;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.UnitTesting;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.CSharpBinding.Tests
{
	[TestFixture]
	class UnitTesteditorIntegrationTests : Ide.TextEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharpWithReferences;
		protected override IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			yield return new UnitTestTextEditorExtension ();
		}

		class UnitTestMarkers: IUnitTestMarkers2
		{
			public string TestMethodAttributeMarker { get; set; }
			public string TestCaseMethodAttributeMarker { get; set; }
			public string TestCaseSourceAttributeMarker { get; set; }
			public string IgnoreTestMethodAttributeMarker { get; set; }
			public string IgnoreTestClassAttributeMarker { get; set; }
		}

		IUnitTestMarkers [] unitTestMarkers = {
			new UnitTestMarkers {
				TestMethodAttributeMarker = "NUnit.Framework.TestAttribute",
				TestCaseMethodAttributeMarker = "NUnit.Framework.TestCaseAttribute",
				TestCaseSourceAttributeMarker = "NUnit.Framework.TestCaseSourceAttribute",
				IgnoreTestMethodAttributeMarker = "NUnit.Framework.IgnoreAttribute",
				IgnoreTestClassAttributeMarker = "NUnit.Framework.IgnoreAttribute"
			}
		};

		async Task Setup (string input, Func<UnitTestTextEditorExtension, Task> test)
		{
			var text = @"namespace NUnit.Framework {
	public class TestFixtureAttribute : System.Attribute {} 
	public class TestAttribute : System.Attribute {} 
	public class TestCaseSourceAttribute : System.Attribute {} 
} namespace TestNs { " + input + "}";
			int endPos = text.IndexOf ('$');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			AnalysisCore.AnalysisOptions.EnableUnitTestEditorIntegration.Set (true);

			using (var testCase = await SetupTestCase (text, Math.Max (0, endPos))) {
				var doc = testCase.Document;
				await doc.UpdateParseDocument ();

				var compExt = doc.GetContent<UnitTestTextEditorExtension> ();
				await test (compExt);
			}
		}

		[Test]
		public async Task TestSimple ()
		{
			await Setup (@"using NUnit.Framework;
[TestFixture]
class TestClass
{
	[Test]
	public void MyTest () {}
}
", async ext => {
				var tests = await ext.GatherUnitTests (unitTestMarkers, default (CancellationToken));
				Assert.IsNotNull (tests);
				Assert.AreEqual (2, tests.Count);
			});
		}

		[Test]
		public async Task TestNoTests ()
		{
			await Setup (@"using NUnit.Framework;
class TestClass
{
	public void MyTest () {}
}
", async ext => {
				var tests = await ext.GatherUnitTests (unitTestMarkers, default (CancellationToken));
				Assert.IsNotNull (tests);
				Assert.AreEqual (0, tests.Count);
			});
		}



		/// <summary>
		/// Bug 14522 - Unit test editor integration does not work for derived classes 
		/// </summary>
		[Test]
		public async Task TestBug14522 ()
		{
			await Setup (@"using NUnit.Framework;
[TestFixture]
abstract class MyBase
{
}

public class Derived : MyBase
{
	[Test]
	public void MyTest () {}
}
", async ext => {
				var tests = await ext.GatherUnitTests (unitTestMarkers, default (CancellationToken));
				Assert.IsNotNull (tests);
				Assert.AreEqual (2, tests.Count);

				Assert.AreEqual ("TestNs.Derived", tests [0].UnitTestIdentifier);
				Assert.AreEqual ("TestNs.Derived.MyTest", tests [1].UnitTestIdentifier);
			});
		}


		/// <summary>
		/// Bug 19651 - Should not require [TestFixture] for Unit Test Integration
		/// </summary>
		[Test]
		public async Task TestBug19651 ()
		{
			await Setup (@"using NUnit.Framework;
class TestClass
{
	[Test]
	public void MyTest () {}
}
", async ext => {
				var tests = await ext.GatherUnitTests (unitTestMarkers, default (CancellationToken));
				Assert.IsNotNull (tests);
				Assert.AreEqual (2, tests.Count);
			});
		}

		/// <summary>
		/// Issue #3869 "[TestCaseSource ("xzy")]" isn't seen as a test in the text editor. No dot next to it. 
		/// </summary>
		[Test]
		public async Task TestIssue3869 ()
		{
			await Setup (@"using NUnit.Framework;
class TestClass
{
	[TestCaseSource(""DivideCases"")]
    public void DivideTest(int n, int d, int q)
    {
        Assert.AreEqual(q, n / d);
    }

    static object[] DivideCases =
    {
        new object[] { 12, 3, 4 },
        new object[] { 12, 2, 6 },
        new object[] { 12, 4, 3 }
    };
}
", async ext => {
				var tests = await ext.GatherUnitTests (unitTestMarkers, default (CancellationToken));
				Assert.IsNotNull (tests);
				Assert.AreEqual (2, tests.Count);
			});
		}
	}
}

