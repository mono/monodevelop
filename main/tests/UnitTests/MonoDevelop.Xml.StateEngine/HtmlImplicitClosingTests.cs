//
// HtmlImplicitClosingTests.cs
//
// Author:
//       Dipam Changede <dipamchang@gmail.com>
//
// 
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
using System.Linq;
using MonoDevelop.AspNet.StateEngine;
using NUnit.Framework;

namespace MonoDevelop.Xml.StateEngine
{
	[TestFixture]
	public class HtmlImplicitClosingTests : ParsingTests
	{

		public override XmlFreeState CreateRootState ()
		{
			return new XmlFreeState (new HtmlTagState (), new HtmlClosingTagState (true));
		}


		[Test]
		public void TestHtmlImplicitClosing ()
		{
			TestParser parser = new TestParser (CreateRootState ());
			parser.Parse(@"
<html>
	<body>
		<li><li>$
		<dt><img><dd>$</dd>
		<tr><tr>$</tr></li>
		<p>
		<table>$</table>
		<td><th><td>$
	</body>
</html>
",
				delegate {
					parser.AssertPath ("//html/body/li");
				},
				delegate {
					parser.AssertPath ("//html/body/li/dd");
				},
				delegate {
					parser.AssertPath ("//html/body/li/tr");
				},
				delegate {
					parser.AssertPath ("//html/body/table");
				},
				delegate {
					parser.AssertPath ("//html/body/td");
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (1);

		}

	}
}

