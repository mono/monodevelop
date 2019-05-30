//
// FileNestingTests.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2019, Microsoft Inc. (http://microsoft.com)
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
using System.IO;
using NUnit.Framework;
using MonoDevelop.Projects.FileNesting;
using UnitTests;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class FileNestingTests : TestBase
	{
		[TestCase ("Index.cshtml.cs", "Index.cshtml")]
		[TestCase ("file.html.css", "file.html")]
		[TestCase ("bootstrap.css.map", "bootstrap.css")]
		[TestCase ("jquery.js", "jquery.ts")]
		[TestCase ("site-vsdoc.js", "site.js")]
		[TestCase ("jquery.min.js", "jquery.js")]
		[TestCase ("template.cs", "template.tt")]
		[TestCase ("template.doc", "template.tt")]
		[TestCase (".bowerrc", "bower.json")]
		public void GetParentFileTest (string inputFile, string expectedParentFile)
		{
			var folder = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
			Directory.CreateDirectory (folder);

			string inputFileDestination = Path.Combine (folder, inputFile);
			string parentFileDestination = Path.Combine (folder, expectedParentFile);

			File.Create (inputFileDestination);
			File.Create (parentFileDestination);

			string parentFile = FileNestingService.GetParentFile (inputFileDestination);
			Assert.That (parentFile, Is.EqualTo (parentFileDestination), $"Was expecting parent file {parentFileDestination} for {inputFileDestination} but got {parentFile}");

			Directory.Delete (folder, true);
		}
	}
}
