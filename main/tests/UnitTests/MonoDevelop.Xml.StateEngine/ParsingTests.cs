// 
// ParsingTests.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Content;

using NUnit.Framework;

namespace MonoDevelop.Xml.StateEngine
{
	
	[TestFixture]
	public class ParsingTests : UnitTests.TestBase
	{
		
		[Test]
		public void AttributeName ()
		{
			TestParser parser = new TestParser (new XmlFreeState());
			parser.Parse (@"
<doc>
	<tag.a>
		<tag.b id=""$foo"" />
	</tag.a>
</doc>
",
				delegate {
					parser.AssertStateIs<XmlDoubleQuotedAttributeValueState> ();
					parser.AssertPath ("//doc/tag.a/tag.b/@id");
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (0);
		}
		
		[Test, Ignore ("Not working")]
		public void IncompleteAttribute ()
		{
			TestParser parser = new TestParser (new XmlFreeState());
			parser.Parse (@"
<doc>
	<tag.a att = >
		<tag.b id='$foo' />
	</tag.a>
</doc>
",
				delegate {
					parser.AssertStateIs<XmlSingleQuotedAttributeValueState> ();
					parser.AssertPath ("//doc/tag.a/tag.b/@id");
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (1);
		}
		
		[Test]
		public void Unclosed ()
		{
			TestParser parser = new TestParser (new XmlFreeState());
			parser.Parse (@"
<doc>
	<tag.a>
		<tag.b><tag.b>$
	</tag.a>$
</doc>
",
				delegate {
					parser.AssertStateIs<XmlFreeState> ();
					parser.AssertNodeDepth (5);
					parser.AssertPath ("//doc/tag.a/tag.b/tag.b");
				},
				delegate {
					parser.AssertStateIs<XmlFreeState> ();
					parser.AssertNodeDepth (2);
					parser.AssertPath ("//doc");
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (2);
		}
		
		
	}
	
}
