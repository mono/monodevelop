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

using System.Linq;
using NUnit.Framework;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using MonoDevelop.Ide.TypeSystem;


namespace MonoDevelop.Xml.Tests.Parser
{
	
	[TestFixture]
	public class ParsingTests
	{
		public virtual XmlRootState CreateRootState ()
		{
			return new XmlRootState ();
		}
		
		[Test]
		public void AttributeName ()
		{
			var parser = new TestXmlParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<tag.a>
		<tag.b id=""$foo"" />
	</tag.a>
</doc>
",
				delegate {
					parser.AssertStateIs<XmlAttributeValueState> ();
					parser.AssertPath ("//doc/tag.a/tag.b/@id");
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (0);
		}
		
		[Test]
		public void Attributes ()
		{
			var parser = new TestXmlParser (CreateRootState ());
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
		
		[Test]
		public void AttributeRecovery ()
		{
			var parser = new TestXmlParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<tag.a>
		<tag.b arg='fff' sdd = sdsds= 'foo' ff = 5 $ />
	</tag.a>
<a><b valid/></a>
</doc>
",
				delegate {
					parser.AssertStateIs<XmlTagState> ();
					parser.AssertAttributes ("arg", "fff", "sdd", "sdsds", "ff", "5");
					parser.AssertErrorCount (3);
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (4);
		}
		
		[Test]
		public void IncompleteTags ()
		{
			var parser = new TestXmlParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<tag.a att1 >
		<tag.b att2="" >
			<tag.c att3 = ' 
				<tag.d att4 = >
					<tag.e att5='' att6=' att7 = >
						<tag.f id='$foo' />
					</tag.e>
				</tag.d>
			</tag.c>
		</tag.b>
	</tag.a>
</doc>
",
				delegate {
					parser.AssertStateIs<XmlAttributeValueState> ();
					parser.AssertNodeDepth (9);
					parser.AssertPath ("//doc/tag.a/tag.b/tag.c/tag.d/tag.e/tag.f/@id");
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (5, x => x.ErrorType == ErrorType.Error);
		}

		[Test]
		public void Unclosed ()
		{
			var parser = new TestXmlParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<tag.a>
		<tag.b><tag.b>$
	</tag.a>$
</doc>
",
				delegate {
					parser.AssertStateIs<XmlRootState> ();
					parser.AssertNodeDepth (5);
					parser.AssertPath ("//doc/tag.a/tag.b/tag.b");
				},
				delegate {
					parser.AssertStateIs<XmlRootState> ();
					parser.AssertNodeDepth (2);
					parser.AssertPath ("//doc");
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (2);
		}



		[Test]
		public void ClosingTagWithWhitespace ()
		{
			var parser = new TestXmlParser (CreateRootState ());
			parser.Parse (@"<doc><a></ a></doc >");
			parser.AssertEmpty ();
			parser.AssertErrorCount (0);
		}


		[Test]
		public void BadClosingTag ()
		{
			var parser = new TestXmlParser (CreateRootState ());
			parser.Parse (@"<doc><x><abc></ab c><cd></cd></x></doc>");
			parser.AssertEmpty ();
			parser.AssertErrorCount (2);
		}

		[Test]
		public void Misc ()
		{
			var parser = new TestXmlParser (CreateRootState ());
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
			var parser = new TestXmlParser (CreateRootState (), true);
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
			Assert.AreEqual (dt.InternalDeclarationRegion.Begin.Line, 4);
			Assert.AreEqual (dt.InternalDeclarationRegion.End.Line, 7);
			parser.AssertNoErrors ();
		}

		[Test]
		public void NamespacedAttributes ()
		{
			var parser = new TestXmlParser (CreateRootState (), true);
			parser.Parse (@"<tag foo:bar='1' foo:bar:baz='2' foo='3' />");
			parser.AssertEmpty ();
			var doc = (XDocument) parser.Nodes.Peek ();
			var el = (XElement) doc.FirstChild;
			Assert.AreEqual (3, el.Attributes.Count ());
			Assert.AreEqual ("foo", el.Attributes.ElementAt (0).Name.Prefix);
			Assert.AreEqual ("bar", el.Attributes.ElementAt (0).Name.Name);
			Assert.AreEqual ("foo", el.Attributes.ElementAt (1).Name.Prefix);
			Assert.AreEqual ("bar:baz", el.Attributes.ElementAt (1).Name.Name);
			Assert.IsNull (el.Attributes.ElementAt (2).Name.Prefix);
			Assert.AreEqual ("foo", el.Attributes.ElementAt (2).Name.Name);
			Assert.AreEqual (3, el.Attributes.Count ());
			parser.AssertErrorCount (1);
			Assert.AreEqual (1, parser.Errors [0].Region.Begin.Line);
			Assert.AreEqual (26, parser.Errors [0].Region.Begin.Column);
		}

		[Test]
		public void SimpleTree ()
		{
			var parser = new TestXmlParser (CreateRootState (), true);
			parser.Parse (@"
<doc>
	<a>
		<b>
			<c/>
			<d>
				<e/>
			</d>
			<f>
				<g/>
			</f>
		</b>
	</a>
</doc>");
			parser.AssertErrorCount (0);

			var doc = ((XDocument)parser.Nodes.Peek ()).RootElement;
			Assert.NotNull (doc);
			Assert.AreEqual ("doc", doc.Name.Name);
			Assert.True (doc.IsEnded);

			var a = (XElement)doc.FirstChild;
			Assert.NotNull (a);
			Assert.AreEqual ("a", a.Name.Name);
			Assert.True (a.IsEnded);
			Assert.False (a.IsSelfClosing);
			Assert.IsNull (a.NextSibling);

			var b = (XElement)a.FirstChild;
			Assert.NotNull (b);
			Assert.AreEqual ("b", b.Name.Name);
			Assert.True (b.IsEnded);
			Assert.False (b.IsSelfClosing);
			Assert.IsNull (b.NextSibling);

			var c = (XElement) b.FirstChild;
			Assert.NotNull (c);
			Assert.AreEqual ("c", c.Name.Name);
			Assert.True (c.IsEnded);
			Assert.True (c.IsSelfClosing);
			Assert.IsNull (c.FirstChild);

			var d = (XElement) c.NextSibling;
			Assert.True (d.IsEnded);
			Assert.False (d.IsSelfClosing);
			Assert.AreEqual ("d", d.Name.Name);

			var e = (XElement) d.FirstChild;
			Assert.NotNull (e);
			Assert.True (e.IsEnded);
			Assert.True (e.IsSelfClosing);
			Assert.AreEqual ("e", e.Name.Name);

			var f = (XElement) d.NextSibling;
			Assert.AreEqual (f, b.LastChild);
			Assert.True (f.IsEnded);
			Assert.False (f.IsSelfClosing);
			Assert.AreEqual ("f", f.Name.Name);

			var g = (XElement) f.FirstChild;
			Assert.NotNull (g);
			Assert.True (g.IsEnded);
			Assert.True (g.IsSelfClosing);
			Assert.AreEqual ("g", g.Name.Name);
		}
	}
}
