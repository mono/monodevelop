using MonoDevelop.Xml.Completion;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
{
	/// <summary>
	/// Tests the XmlParser.GetActiveElementStartPathAtIndex which finds the element
	/// path where the index is at. The index may be in the middle or start of the element
	/// tag.
	/// </summary>
	[TestFixture]
	public class ActiveElementUnderCursorTests
	{
		const string namespaceURI = "http://foo.com/foo.xsd";
		
		[Test]
		public void PathTest1()
		{
			TestXmlParser.AssertTree (
				"<foo xmlns='" + namespaceURI + "'$><bar>",
				n => TestXmlParser.AssertPath (
					n,
					new QualifiedName("foo", namespaceURI)
				)
			);
		}		
		
		[Test]
		public void PathTest2()
		{
			TestXmlParser.AssertTree (
				"<foo xmlns='" + namespaceURI + "'><$bar>",
				n => TestXmlParser.AssertPath (
					n,
					new QualifiedName("foo", namespaceURI),
					new QualifiedName("bar", namespaceURI)
				)
			);
		}
		
		[Test]
		public void PathTest3()
		{
			TestXmlParser.AssertTree (
				"<foo xmlns='" + namespaceURI + "'><b$ar>",
				n => TestXmlParser.AssertPath (
					n,
					new QualifiedName("foo", namespaceURI),
					new QualifiedName("bar", namespaceURI)
				)
			);
		}
		
		[Test]
		public void PathTest4()
		{
			TestXmlParser.AssertTree (
				"<foo xmlns='" + namespaceURI + "'><bar$>",
				n => TestXmlParser.AssertPath (
					n,
					new QualifiedName ("foo", namespaceURI),
					new QualifiedName ("bar", namespaceURI)
				)
			);
		}
		
		[Test]
		public void PathTest5()
		{
			TestXmlParser.AssertTree (
				"<foo xmlns='" + namespaceURI + "'><bar a$='a'>",
				n => TestXmlParser.AssertPath (
					n,
					new QualifiedName ("foo", namespaceURI),
					new QualifiedName ("bar", namespaceURI)
				)
			);
		}
		
		[Test]
		public void PathTest6()
		{
			TestXmlParser.AssertTree (
				"<foo xmlns='" + namespaceURI + "'><bar a='a$'>",
				n => TestXmlParser.AssertPath (
					n,
					new QualifiedName ("foo", namespaceURI),
					new QualifiedName ("bar", namespaceURI)
				)
			);
		}
		
		[Test]
		public void PathTest7()
		{
			TestXmlParser.AssertTree (
				"<foo xmlns='" + namespaceURI + "'><bar a='a'  $>",
				n => TestXmlParser.AssertPath (
					n,
					new QualifiedName("foo", namespaceURI),
					new QualifiedName("bar", namespaceURI)
				)
			);
		}
		
		[Test]
		public void PathTest8()
		{
			TestXmlParser.AssertTree (
				"<foo xmlns='" + namespaceURI + "'><bar>$",
				n => TestXmlParser.AssertPath (
					n,
					new QualifiedName("foo", namespaceURI),
					new QualifiedName("bar", namespaceURI)
				)
			);
		}
		
		[Test]
		public void PathTest9()
		{
			TestXmlParser.AssertTree (
				"<foo xmlns='" + namespaceURI + "'><bar \n\n hi='$'>",
				n => TestXmlParser.AssertPath (
					n,
					new QualifiedName ("foo", namespaceURI),
					new QualifiedName ("bar", namespaceURI)
				)
			);
		}
		
		[Test]
		public void PathTest10()
		{
			TestXmlParser.AssertTree (
				"<foo xmlns='" + namespaceURI + "'><bar $Id=\r\n</foo>",
				n => TestXmlParser.AssertPath (
					n,
					new QualifiedName ("foo", namespaceURI),
					new QualifiedName ("bar", namespaceURI)
				)
			);
		}
		
		[Test]
		public void PathTest11()
		{
			TestXmlParser.AssertTree (
				"<fo$o xmlns='" + namespaceURI + "'>",
				n => TestXmlParser.AssertPath (
					n,
					new QualifiedName ("foo", namespaceURI)
				)
			);
		}
	}
}
