using MonoDevelop.Xml.Completion;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
{
	[TestFixture]
	public class ParentElementPathTestFixture 
	{
		const string namespaceURI = "http://foo/foo.xsd";

		[Test]
		public void SuccessTest1()
		{
			AssertParentPath (
				"<foo xmlns='" + namespaceURI + "' >$<",
				new QualifiedName ("foo", namespaceURI)
			);
		}

		[Test]
		public void SuccessTest2()
		{
			AssertParentPath (
				"<foo xmlns='" + namespaceURI + "' ><bar></bar><$",
				new QualifiedName ("foo", namespaceURI)
			);
		}		

		[Test]
		public void SuccessTest3()
		{
			AssertParentPath (
				"<foo xmlns='" + namespaceURI + "' ><bar/><$",
				new QualifiedName ("foo", namespaceURI)
			);
		}

		[Test]
		public void SuccessTest4()
		{
			AssertParentPath (
				"<bar xmlns='http://test.com'/><foo xmlns='" + namespaceURI + "' ><$",
				new QualifiedName ("foo", namespaceURI)
			);
		}

		public void AssertParentPath (string doc, params QualifiedName[] qualifiedNames)
		{
			TestXmlParser.AssertState (doc, p =>
				Assert.AreEqual (
					new XmlElementPath (qualifiedNames),
					XmlElementPath.Resolve (p.Nodes.ToArray ())
				)
			);;
		}
	}
}
