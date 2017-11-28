//
// XmlFormatterTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Xml.Formatting;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Formatting
{
	[TestFixture]
	public class XmlFormatterTests
	{
		TextStylePolicy textPolicy;
		XmlFormattingPolicy xmlPolicy;

		[SetUp]
		public void Init ()
		{
			textPolicy = new TextStylePolicy ();
			xmlPolicy = new XmlFormattingPolicy ();
		}

		[Test]
		public void OmitXmlDeclarationIsFalse_XmlDeclarationDoesNotExist_XmlDeclarationIsAdded ()
		{
			xmlPolicy.DefaultFormat.OmitXmlDeclaration = false;
			string input = "<a></a>";
			string expectedResult = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<a>\n</a>";

			string result = XmlFormatter.FormatXml (textPolicy, xmlPolicy, input);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void OmitXmlDeclarationIsTrue_XmlDeclarationExists_XmlDeclarationRemoved ()
		{
			xmlPolicy.DefaultFormat.OmitXmlDeclaration = true;
			string input = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<a></a>";
			string expectedResult = "<a>\n</a>";

			string result = XmlFormatter.FormatXml (textPolicy, xmlPolicy, input);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void OmitXmlDeclarationIsFalse_XmlDeclarationExists_XmlDeclarationUnmodified ()
		{
			xmlPolicy.DefaultFormat.OmitXmlDeclaration = false;
			string input = "<?xml version=\"1.0\"?><a></a>";
			string expectedResult = "<?xml version=\"1.0\"?>\n<a>\n</a>";

			string result = XmlFormatter.FormatXml (textPolicy, xmlPolicy, input);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void AlignAttributesMaxOnePerLine ()
		{
			var myTextPolicy = textPolicy.WithTabsToSpaces (true);
			var myXmlPolicy = xmlPolicy.Clone ();
			myXmlPolicy.DefaultFormat.AlignAttributes = true;
			myXmlPolicy.DefaultFormat.MaxAttributesPerLine = 1;
			myXmlPolicy.DefaultFormat.OmitXmlDeclaration = true;
			string input = @"<test123 abc=""x"" def=""y"" ghi=""z"" />";
			//9 spaces = "<test123 ".Length
			string expectedResult ="<test123 abc=\"x\"\n         def=\"y\"\n         ghi=\"z\" />";

			string result = XmlFormatter.FormatXml (myTextPolicy, myXmlPolicy, input);

			Assert.AreEqual (expectedResult, result);
		}

		[Test]
		public void AlignAttributesMaxOnePerLineTabs ()
		{
			var myTextPolicy = textPolicy.WithTabsToSpaces (false).WithTabWidth(4);
			var myXmlPolicy = xmlPolicy.Clone ();
			myXmlPolicy.DefaultFormat.AlignAttributes = true;
			myXmlPolicy.DefaultFormat.MaxAttributesPerLine = 1;
			myXmlPolicy.DefaultFormat.OmitXmlDeclaration = true;
			string input = @"<test123 abc=""x"" def=""y"" ghi=""z"" />";
			//9 spaces = "<test123 ".Length
			string expectedResult = "<test123 abc=\"x\"\n\t\t def=\"y\"\n\t\t ghi=\"z\" />";

			string result = XmlFormatter.FormatXml (myTextPolicy, myXmlPolicy, input);

			Assert.AreEqual (expectedResult, result);
		}
	}
}

