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
using MonoDevelop.UnitTesting.NUnit;
using MonoDevelop.UnitTesting.VsTest;
using NUnit.Framework;
using System;
using UnitTests;

namespace MonoDevelop.UnitTesting.Tests
{
	[TestFixture]
	class VsTestUnitTestTests : TestBase
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
		[TestCase ("MyClass.MyTest", "Name with dot.", "", "MyClass", "Name with dot.")]
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

		/// <summary>
		/// Bug 701330: DTS: When adding Unit Test into the application, the application will crash with a stack trace that seems to try to recursively add unit test over 85,000 times
		/// </summary>
		[Test]
		public void TestVSTS701330 ()
		{
			var grp = new VsTestNamespaceTestGroup (null, null, null, "Test");
			var uri = new Uri ("/test/Test.cs");
			grp.AddTest (new MyVsTestUnitTest ("Test", "Test", "TestCase1"));
			grp.AddTest (new MyVsTestUnitTest ("Test", "Test.Test", "TestCase1"));
		}

		/// <summary>
		/// VSTS Bug 729387: [Feedback] Broken text editor unit test #6735
		/// </summary>
		[Test]
		public void TestVSTS729387 ()
		{
			var test = CreateVsUnitTest ("Namespace.MyTest.Test1", "Test1");
			Assert.AreEqual ("Namespace.MyTest.Test1", test.TestSourceCodeDocumentId);
		}

		/// <summary>
		/// VSTS Bug 901156: [Feedback] Weird nesting in "Unit Tests" explorer/window.
		/// </summary>
		[Test]
		public void TestVSTS901156 ()
		{
			var parentNamespace = new VsTestNamespaceTestGroup (null, null, null, string.Empty);
			var test1 = new MyVsTestUnitTest ("Namespace.childNamespace.TestClass.TestMethod1", "Namespace.childNamespace", "TestClass");
			var test2 = new MyVsTestUnitTest ("Namespace.childNamespace.TestClass.TestMethod2", "Namespace.childNamespace", "TestClass");
			parentNamespace.AddTest (test1);
			parentNamespace.AddTest (test2);
			var currentNamespace = (VsTestNamespaceTestGroup)parentNamespace.Tests [0];
			var currentClass = (VsTestTestClass)currentNamespace.Tests [0];
			Assert.AreEqual (currentNamespace.FixtureTypeNamespace, "Namespace.childNamespace");
			Assert.AreEqual (currentNamespace.Tests.Count, 1);
			Assert.AreEqual (currentClass.Tests.Count, 2);
		}

		class MyVsTestUnitTest : VsTestUnitTest
		{
			public MyVsTestUnitTest (string displayName, string fixtureTypeNamespace, string fixtureTypeName) : base(displayName)
			{
				FixtureTypeNamespace = fixtureTypeNamespace;
				FixtureTypeName = fixtureTypeName;
			}
		}
	}

	[TestFixture]
	class NUnitTestUnitTestTests : TestBase
	{

		/// <summary>
		/// VSTS Bug 1042673: [Bug] Make the nesting for Mono NUnit test consistent with the vstests (mstest, nunit, xunit)
		/// </summary>
		[Test]
		public void TestVSTS1042673 ()
		{
			var case1 = new NUnitTestSuite (null, DirectClassCase ());
			var case2 = new NUnitTestSuite (null, NamespaceWithDot ());
			var case3 = new NUnitTestSuite (null, NestedNamespace ());
			var case4 = new NUnitTestSuite (null, NamespaceNoDot ());  

			Assert.AreEqual(case1.Tests[0].Title, "TestClass");
			Assert.AreEqual(case2.Tests[0].Title, "A.B");
			Assert.AreEqual(case3.Tests[0].Title, "A.B.C");
			Assert.AreEqual(case4.Tests[0].Title, "A");

		}

		NunitTestInfo DirectClassCase ()
		{
			NunitTestInfo classInfo = new NunitTestInfo {
				FixtureTypeName = "TestClass",
				FixtureTypeNamespace = "",
				Name = "TestClass"
			};

			NunitTestInfo methodInfo = new NunitTestInfo {
				FixtureTypeName = "TestClass",
				FixtureTypeNamespace = "",
				Name = "TestMethod"
			};
			classInfo.Tests [0] = methodInfo;

			return classInfo;
		}

		NunitTestInfo NamespaceWithDot ()
		{
			NunitTestInfo namespaceInfo = new NunitTestInfo {
				FixtureTypeName = "",
				FixtureTypeNamespace = "",
				Name = "A"
			};
			NunitTestInfo namespaceInfo2 = new NunitTestInfo {
				FixtureTypeName = "",
				FixtureTypeNamespace = "",
				Name = "B"
			};
			namespaceInfo.Tests [0] = namespaceInfo2;

			NunitTestInfo classInfo = new NunitTestInfo {
				FixtureTypeName = "TestClassAB",
				FixtureTypeNamespace = "A.B",
				Name = "TestClassAB"
			};
			namespaceInfo2.Tests [0] = classInfo;

			NunitTestInfo methodInfo = new NunitTestInfo {
				FixtureTypeName = "TestClassAB",
				FixtureTypeNamespace = "A.B",
				Name = "TestMethodAB"
			};
			classInfo.Tests [0] = methodInfo;

			return namespaceInfo;
		}

		NunitTestInfo NestedNamespace ()
		{
			NunitTestInfo namespaceInfo = new NunitTestInfo {
				FixtureTypeName = "",
				FixtureTypeNamespace = "",
				Name = "A"
			};
			NunitTestInfo namespaceInfo2 = new NunitTestInfo {
				FixtureTypeName = "",
				FixtureTypeNamespace = "",
				Name = "B"
			};
			namespaceInfo.Tests [0] = namespaceInfo2;
			NunitTestInfo namespaceInfo3 = new NunitTestInfo {
				FixtureTypeName = "",
				FixtureTypeNamespace = "",
				Name = "C"
			};
			namespaceInfo2.Tests [0] = namespaceInfo3;

			NunitTestInfo classInfo = new NunitTestInfo {
				FixtureTypeName = "TestClassABC",
				FixtureTypeNamespace = "A.B.C",
				Name = "TestClassABC"
			};
			namespaceInfo3.Tests [0] = classInfo;

			NunitTestInfo methodInfo = new NunitTestInfo {
				FixtureTypeName = "TestClassABC",
				FixtureTypeNamespace = "A.B.C",
				Name = "TestMethodABC"
			};
			classInfo.Tests [0] = methodInfo;

			return namespaceInfo;
		}

		NunitTestInfo NamespaceNoDot ()
		{
			NunitTestInfo namespaceInfo = new NunitTestInfo {
				FixtureTypeName = "",
				FixtureTypeNamespace = "",
				Name = "A"
			};

			NunitTestInfo classInfo = new NunitTestInfo {
				FixtureTypeName = "TestClassA",
				FixtureTypeNamespace = "A",
				Name = "TestClassA"
			};
			namespaceInfo.Tests [0] = classInfo;

			NunitTestInfo methodInfo = new NunitTestInfo {
				FixtureTypeName = "TestClassA",
				FixtureTypeNamespace = "A",
				Name = "TestMethodA"
			};
			classInfo.Tests [0] = methodInfo;

			return namespaceInfo;
		}
	}
}
