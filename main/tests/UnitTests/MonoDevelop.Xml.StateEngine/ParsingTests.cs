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
using System.Linq;

using NUnit.Framework;

namespace MonoDevelop.Xml.StateEngine
{
	
	[TestFixture]
	public class ParsingTests
	{
		
		public virtual XmlFreeState CreateRootState ()
		{
			return new XmlFreeState ();
		}
		
		[Test]
		public void AttributeName ()
		{
			TestParser parser = new TestParser (CreateRootState ());
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
		
		[Test]
		public void Attributes ()
		{
			TestParser parser = new TestParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<tag.a name=""foo"" arg=5 wibble = 6 bar.baz = 'y.ff7]' $ />
</doc>
",
				delegate {
					parser.AssertStateIs<XmlTagState> ();
					parser.AssertAttributes ("name", "foo", "arg", "5", "wibble", "6", "bar.baz", "y.ff7]");
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (0);
		}
		
		[Test, Ignore ("Not working")]
		public void AttributeRecovery ()
		{
			TestParser parser = new TestParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<tag.a>
		<tag.b arg='fff' sdd = sdsds= 'foo' ff $ />
	</tag.a>
<a><b valid/></a>
</doc>
",
				delegate {
					parser.AssertStateIs<XmlTagState> ();
					parser.AssertAttributes ("arg", "fff", "sdd", "", "sdsds", "foo", "ff", "");
					parser.AssertErrorCount (1);
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (1);
		}
		
		[Test]
		public void IncompleteTags ()
		{
			TestParser parser = new TestParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<tag.a att >
		<tag.b att="" >
			<tag.c att = ' 
				<tag.d att = >
					<tag.e att='' att=' att = >
						<tag.f id='$foo' />
					</tag.e>
				</tag.d>
			</tag.c>
		</tag.b>
	</tag.a>
</doc>
",
				delegate {
					parser.AssertStateIs<XmlSingleQuotedAttributeValueState> ();
					parser.AssertNodeDepth (9);
					parser.AssertPath ("//doc/tag.a/tag.b/tag.c/tag.d/tag.e/tag.f/@id");
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (5);
		}
		
		[Test]
		public void Unclosed ()
		{
			TestParser parser = new TestParser (CreateRootState ());
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
		
		[Test]
		public void Misc ()
		{
			TestParser parser = new TestParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<!DOCTYPE $  >
	<![CDATA[ ]  $ ]  ]]>
	<!--   <foo> <bar arg=""> $  -->
</doc>
",
				delegate {
					parser.AssertStateIs<XmlDocTypeState> ();
					parser.AssertNodeDepth (3);
					parser.AssertPath ("//doc/<!DOCTYPE>");
				},
				delegate {
					parser.AssertStateIs<XmlCDataState> ();
					parser.AssertNodeDepth (3);
					parser.AssertPath ("//doc/<![CDATA[ ]]>");
				},
				delegate {
					parser.AssertStateIs<XmlCommentState> ();
					parser.AssertNodeDepth (3);
					parser.AssertPath ("//doc/<!-- -->");
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (0);
		}
		
		[Test]
		public void DocTypeCapture ()
		{
			TestParser parser = new TestParser (CreateRootState (), true);
			parser.Parse (@"
		<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN""
""DTD/xhtml1-strict.dtd""
[
<!-- foo -->
<!bar #baz>
]>
<doc><foo/></doc>");
			parser.AssertEmpty ();
			XDocument doc = (XDocument)parser.Nodes.Peek ();
			Assert.IsTrue (doc.FirstChild is XDocType);
			XDocType dt = (XDocType) doc.FirstChild;
			Assert.AreEqual ("html", dt.RootElement.FullName);
			Assert.AreEqual ("-//W3C//DTD XHTML 1.0 Strict//EN", dt.PublicFpi);
			Assert.AreEqual ("DTD/xhtml1-strict.dtd", dt.Uri);
			Assert.AreEqual (dt.InternalDeclarationRegion.Start.Line, 4);
			Assert.AreEqual (dt.InternalDeclarationRegion.End.Line, 7);
			parser.AssertNoErrors ();
		}
	}
	
}
