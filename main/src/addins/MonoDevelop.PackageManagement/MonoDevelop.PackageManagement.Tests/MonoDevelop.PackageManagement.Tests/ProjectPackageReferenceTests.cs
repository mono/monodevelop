//
// ProjectPackageReferenceTests.cs
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
using System.Xml;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects.MSBuild;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ProjectPackageReferenceTests
	{
		[Test]
		public void Write_Version_WrittenAsAttribute ()
		{
			var p = new MSBuildProject ();
			p.LoadXml ("<Project ToolsVersion=\"15.0\" />");
			ProjectPackageReference.AddKnownItemAttributes (p);

			var item = p.AddNewItem ("PackageReference", "Test");
			var packageReference = new TestableProjectPackageReference ("Test", "1.2.3");
			packageReference.CallWrite (item);

			string xml = p.SaveToString ();
			var doc = new XmlDocument ();
			doc.LoadXml (xml);

			var itemGroupElement = (XmlElement)doc.DocumentElement.ChildNodes[0];
			var packageReferenceElement = (XmlElement)itemGroupElement.ChildNodes[0];

			Assert.AreEqual ("PackageReference", packageReferenceElement.Name);
			Assert.AreEqual ("1.2.3", packageReferenceElement.GetAttribute ("Version"));
			Assert.AreEqual (0, packageReferenceElement.ChildNodes.Count);
			Assert.IsTrue (packageReferenceElement.IsEmpty);
		}

		[Test]
		public void IsAtLeastVersion_Simple ()
		{
			var reference = ProjectPackageReference.Create ("NUnit", "3.2");
			Assert.IsTrue (reference.IsAtLeastVersion (new Version (3, 0)));
			Assert.IsTrue (reference.IsAtLeastVersion (new Version (3, 2)));
			Assert.IsTrue (reference.IsAtLeastVersion (new Version (3, 0, 0)));
			Assert.IsTrue (reference.IsAtLeastVersion (new Version (3, 2, 0)));
			Assert.IsTrue (reference.IsAtLeastVersion (new Version (3, 2, 0, 0)));
			Assert.IsTrue (reference.IsAtLeastVersion (new Version (3, 0, 0, 0)));
			Assert.IsFalse (reference.IsAtLeastVersion (new Version (4, 0)));
			Assert.IsFalse (reference.IsAtLeastVersion (new Version (3, 3, 0)));
			Assert.IsFalse (reference.IsAtLeastVersion (new Version (3, 2, 1)));
		}

		[Test]
		[TestCase ("[3.2,)")]
		[TestCase ("(3.2,]")]
		[TestCase ("[3.2]")]
		public void IsAtLeastVersion_Range (string versionRange)
		{
			var reference = ProjectPackageReference.Create ("NUnit", versionRange);
			Assert.IsTrue (reference.IsAtLeastVersion (new Version (3, 0)));
			Assert.IsTrue (reference.IsAtLeastVersion (new Version (3, 2)));
			Assert.IsFalse (reference.IsAtLeastVersion (new Version (3, 2, 1)));
			Assert.IsFalse (reference.IsAtLeastVersion (new Version (3, 3)));
		}
	}
}
