
using System.Linq;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
{
	[TestFixture]
	public class AttributeValueUnderCursorTests
	{
		[Test]
		public void SuccessTest1()
		{
			AssertAttributeValue ("<a foo='abc$'", "abc");
		}
		
		[Test]
		public void SuccessTest2()
		{
			AssertAttributeValue ("<a foo=\"abc$\"", "abc");
		}
		
		[Test]
		public void SuccessTest3()
		{
			AssertAttributeValue ("<a foo='abc$'", "abc");
		}
		
		[Test]
		public void SuccessTest4()
		{
			AssertAttributeValue ("<a foo='$abc'", "");
		}
		
		[Test]
		public void SuccessTest5()
		{
			AssertAttributeValue ("<a foo='$a", "");
		}
		
		[Test]
		public void SuccessTest6()
		{
			AssertAttributeValue ("<a foo='a$'", "a");
		}
		
		[Test]
		public void SuccessTest7()
		{
			AssertAttributeValue ("<a foo='a\"b\"c$'", "a\"b\"c");
		}
		
		[Test]
		public void FailureTest1()
		{
			TestXmlParser.AssertState ("<a foo=''$", p => Assert.IsNull (p.Nodes.LastOrDefault () as XAttribute));
		}

		public void AssertAttributeValue (string doc, string val)
		{
			TestXmlParser.AssertState (doc, p => {
				p.AssertStateIs<XmlAttributeValueState> ();
				Assert.AreEqual (val, ((IXmlParserContext)p).KeywordBuilder.ToString ());
			});
		}
	}
}
