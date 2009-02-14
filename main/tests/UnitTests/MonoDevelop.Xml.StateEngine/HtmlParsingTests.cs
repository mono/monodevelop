// 
// HtmlParsingTests.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
	
	
	public class HtmlParsingTests : ParsingTests
	{
		
		public override XmlFreeState CreateRootState ()
		{
			return new XmlFreeState (new HtmlTagState (true), new HtmlClosingTagState (true));
		}
		
		
		[Test]
		public void TestAutoClosing ()
		{
			TestParser parser = new TestParser (CreateRootState ());
			parser.Parse (@"
<html>
	<body>
		<p><img>$
		<p><div> $ </div>
		<p>
		<p><a href =""http://mono-project.com/"" ><b>foo $ </a>
		<p>
		<p>$
	</body>
</html>
",
				delegate {
					parser.AssertPath ("//html/body/p");
				},
				delegate {
					parser.AssertPath ("//html/body/div");
				},
				delegate {
					parser.AssertPath ("//html/body/p/a/b");
				},
				delegate {
					parser.AssertPath ("//html/body/p");
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (8);
		}
		
	}
}
