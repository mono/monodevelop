using System;
using MonoDevelop.Xml.Completion;
using MonoDevelop.Xml.Parser;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
{
	[TestFixture]
	public class ActiveElementStartPathTestFixture 
	{
		const string namespaceURI = "http://foo.com/foo.xsd";
		
		[Test]
		public void PathTest1()
		{
			AssertElementPath (
				"<foo xmlns='" + namespaceURI + "' $",
				new QualifiedName("foo", namespaceURI)
			);
		}		
		
		[Test]
		public void PathTest2()
		{
			AssertElementPath (
				"<foo xmlns='" + namespaceURI + "' ><bar $",
				new QualifiedName("foo", namespaceURI),
				new QualifiedName("bar", namespaceURI)
			);
		}			
		
		[Test]
		public void PathTest3()
		{
			AssertElementPath (
				"<f:foo xmlns:f='" + namespaceURI + "' ><f:bar $",
				new QualifiedName ("foo", namespaceURI, "f"),
				new QualifiedName ("bar", namespaceURI, "f")
			);
		}		
		
		[Test]
		public void PathTest4()
		{
			AssertElementPath (
				"<x:foo xmlns:x='" + namespaceURI + "' $",
				new QualifiedName("foo", namespaceURI, "x")
			);
		}	
		
		[Test]
		public void PathTest5()
		{
			AssertElementPath (
				"<foo xmlns='" + namespaceURI + "'>\r\n<y\r\n" + "Id = 'foo' $",
				new QualifiedName ("foo", namespaceURI),
				new QualifiedName ("y", namespaceURI)
			);
		}
		
		[Test]
		public void PathTest6()
		{
			AssertElementPath (
				"<bar xmlns='http://bar'/>\r\n<foo xmlns='" + namespaceURI + "' $",
				new QualifiedName ("foo", namespaceURI)
			);
		}
		
		/// <summary>
		/// Tests that we get no path back if we are outside the start
		/// tag.
		/// </summary>
		[Test]
		public void OutOfStartTagPathTest1()
		{
			TestXmlParser.AssertState (
				"<foo xmlns='" + namespaceURI + "'> $",
				p => p.AssertStateIs<XmlRootState> ()
			);
		}

		static void AssertElementPath (string text, params QualifiedName[] qualifiedNames)
		{
			TestXmlParser.AssertState (text, p => {
				var arr = p.Nodes.ToArray ();
				Array.Reverse (arr);
				Assert.AreEqual (
					new XmlElementPath (qualifiedNames),
					XmlElementPath.Resolve (arr)
				);
			});
		}
	}
}
