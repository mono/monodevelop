//
// VsTestUnitTestTests.cs
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

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MonoDevelop.UnitTesting.VsTest;
using NUnit.Framework;

namespace MonoDevelop.UnitTesting.Tests
{
	[TestFixture]
	class VsTestUnitTestTests
	{
		VsTestUnitTest CreateVsUnitTest (string fullyQualifiedName)
		{
			return CreateVsUnitTest (fullyQualifiedName, fullyQualifiedName);
		}

		VsTestUnitTest CreateVsUnitTest (string fullyQualifiedName, string displayName)
		{
			var testCase = CreateTestCase (fullyQualifiedName, displayName);
			return new VsTestUnitTest (null, testCase, null);
		}

		TestCase CreateTestCase (string fullyQualifiedName, string displayName)
		{
			return new TestCase {
				DisplayName = displayName,
				FullyQualifiedName = fullyQualifiedName,
				CodeFilePath = "test.cs"
			};
		}

		[TestCase ("MyTest", "MyTestDisplayName", "", "", "MyTestDisplayName")]
		[TestCase ("MyClass.MyTest", "MyClass.MyTestDisplayName", "", "MyClass", "MyTestDisplayName")]
		[TestCase ("A.B.MyTest", "A.B.MyTestDisplayName", "A", "B", "MyTestDisplayName")]
		[TestCase ("A.B.MyTest", "A.B.MyTest(text: \"ab\")", "A", "B", "MyTest(text: \"ab\")")]
		[TestCase ("A.B.MyTest", "A.B.MyTest(text: \"a.b\")", "A", "B", "MyTest(text: \"a.b\")")]
		public void TestName (
			string fullyQualifiedName,
			string displayName,
			string expectedFixtureTypeNamespace,
			string expectedFixtureTypeName,
			string expectedName)
		{
			var test = CreateVsUnitTest (fullyQualifiedName, displayName);

			Assert.AreEqual (expectedFixtureTypeNamespace, test.FixtureTypeNamespace);
			Assert.AreEqual (expectedFixtureTypeName, test.FixtureTypeName);
			Assert.AreEqual (expectedName, test.Name);
		}
	}
}
