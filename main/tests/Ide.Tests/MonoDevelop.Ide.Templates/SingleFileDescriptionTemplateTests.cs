//
// SingleFileDescriptionTemplateTests.cs
//
// Author:
//       josemiguel <jostor@microsoft.com>
//
// Copyright (c) 2018 
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
using System.IO;
using System.Xml;
using NUnit.Framework;

namespace MonoDevelop.Ide.Templates
{
	[TestFixture]
	public class SingleFileDescriptionTemplateTests
	{
		SingleFileDescriptionTemplate fileDescriptionTemplate;

		string ResourcesFileXml => @"<?xml version=""1.0"" encoding=""UTF-8"" ?><File name=""${Name}.resx"" CustomTool=""ResXFileCodeGenerator""/>";
		string T4FileXml => @"<?xml version=""1.0"" encoding=""UTF-8"" ?><File name=""${Name}.tt"" CustomTool=""TextTemplatingFilePreprocessor""/>";
		string RazorFileXml => @"<?xml version=""1.0"" encoding=""UTF-8"" ?><File name=""${Name}.cshtml"" BuildAction=""None"" CustomTool=""RazorTemplatePreprocessor"" />";
		string FileXml  => @"<?xml version=""1.0"" encoding=""UTF-8"" ?><File name=""${Name}.cs""/>";

		public void InitializeTest (string extension)
		{
			var xmlDocument = new XmlDocument ();

			switch (extension) {
			case ".resx":
				xmlDocument.LoadXml (ResourcesFileXml);
				break;
			case ".tt":
				xmlDocument.LoadXml (T4FileXml);
				break;
			case ".cshtml":
				xmlDocument.LoadXml (RazorFileXml);
				break;
			default:
				xmlDocument.LoadXml (FileXml);
				break;
			}

			fileDescriptionTemplate = new SingleFileDescriptionTemplate ();
			fileDescriptionTemplate.Load (xmlDocument.DocumentElement, null);
		}

		[TestCase ("Resources.resx", "Resources.resx")]
		[TestCase ("Resources.es.resx", "Resources.es.resx")]
		[TestCase ("Resources", "Resources.resx")]
		[TestCase ("Resources.es", "Resources.es.resx")]
		[TestCase ("Foo.suffix1.suffix2.suffix3.resx", "Foo.suffix1.suffix2.suffix3.resx")]
		[TestCase ("Foo.suffix1.suffix2.suffix3", "Foo.suffix1.suffix2.suffix3.resx")]
		[TestCase ("foo", "foo.tt")]
		[TestCase ("foo.1", "foo.1.tt")]
		[TestCase ("foo.tt", "foo.tt")]
		[TestCase ("foo.1.2.3.tt", "foo.1.2.3.tt")]
		[TestCase ("Index.cshtml", "Index.cshtml")]
		[TestCase ("Index.foo.cshtml", "Index.foo.cshtml")]
		[TestCase ("Index", "Index.cshtml")]
		[TestCase ("Index.foo", "Index.foo.cshtml")]
		[TestCase ("Class1.cs", "Class1.cs")]
		[TestCase ("Class1", "Class1.cs")]
		public void NewTemplateNameMatchesExpectedFileName (string newTemplateName, string expectedFileName)
		{
			InitializeTest (Path.GetExtension (expectedFileName));

			var fileName = fileDescriptionTemplate.GetFileName (null, null, string.Empty, string.Empty, newTemplateName);

			Assert.That (expectedFileName, Is.EqualTo (fileName));
		}
	}
}
