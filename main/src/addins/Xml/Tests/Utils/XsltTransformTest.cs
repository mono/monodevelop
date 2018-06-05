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

using System.IO;
using System.Xml;
using MonoDevelop.Xml.Editor;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Utils
{
	[TestFixture]
	public class XsltTransformTest
	{
		[Test]
		public void TestTransform ()
		{
			var xml =
@"<?xml-stylesheet type='text/xsl' href='hello.xsl'?>
<foo><bar>thing</bar><baz>other</baz></foo>";
			var xslt = @"<xsl:stylesheet xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"" version=""1.0"">
<xsl:template match='foo'><a>bar=<xsl:value-of select='bar'/>;<xsl:apply-templates select='baz'/></a></xsl:template>
<xsl:template match='baz'>baz=<xsl:value-of select='.'/>;</xsl:template>
</xsl:stylesheet>";
			var expected = "<?xml version=\"1.0\" encoding=\"utf-8\"?><a>bar=thing;baz=other;</a>";

			(var transform, var error) = XmlEditorService.CompileStylesheet (xslt, "test.xml");
			Assert.NotNull (transform);
			Assert.IsNull (error);

			var outWriter = new EncodedStringWriter (System.Text.Encoding.UTF8);
			using (var reader = XmlReader.Create (new StringReader (xml))) {
				transform.Transform (reader, null, outWriter);
			}
			Assert.AreEqual (expected, outWriter.ToString ());
		}

		[Test]
		public void TestTransformCompileFile ()
		{
			var xslt = @"<xsl:stylesheet xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"" version=""1.0"">
<xsl:emplate match='foo'><a>bar=<xsl:value-of select='bar'/>;<xsl:apply-templates select='baz'/></a></xsl:emplate>
<xsl:template match='baz'>baz=<xsl:value-of select='.'/>;</xsl:template>
</xsl:stylesheet>";
			var expected = "'xsl:emplate' cannot be a child of the 'xsl:stylesheet' element.";

			(var transform, var error) = XmlEditorService.CompileStylesheet (xslt, "test.xml");
			Assert.NotNull (error);
			Assert.IsNull (transform);

			Assert.AreEqual (expected, error.ErrorText);
		}
	}
}
