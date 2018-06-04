//
// Copyright (c) Microsoft Corp
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

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using MonoDevelop.Xml.Editor;

namespace MonoDevelop.Xml.Tests.Schema
{
	[TestFixture]
	public class SchemaValidationTests
	{
		[Test]
		public async Task ValidateXsltValid ()
		{
			var text =
@"<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>
<xsl:variable name='foo' select='bar' />
</xsl:stylesheet>";

			var results = await XmlEditorService.Validate (text, "test.xslt", CancellationToken.None);
			Assert.AreEqual (0, results.Count);
		}

		[Test]
		public async Task ValidateXsltInvalid ()
		{
			var text =
@"<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>
<xsl:varibble name='foo' select='bar' />
</xsl:stylesheet>";

			var results = await XmlEditorService.Validate (text, "test.xslt", CancellationToken.None);
			Assert.AreEqual (1, results.Count);
			Assert.AreEqual ("test.xslt", results [0].FileName);
			Assert.AreEqual (2, results [0].Line);
			Assert.AreEqual (2, results [0].Column);
			Assert.IsTrue (results [0].ErrorText.StartsWith ("The element 'stylesheet' in namespace 'http://www.w3.org/1999/XSL/Transform' has invalid child element 'varibble'", System.StringComparison.Ordinal));
		}
	}
}
