using NUnit.Framework;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Tests.Parser
{
	[TestFixture]
	public class InsideAttributeValueTestFixture
	{
		[Test]
		public void InvalidString()
		{
			AssertNotInsideAttributeValue ("$");
		}
		
		[Test]
		public void DoubleQuotesTest1()
		{
			string xml = "<foo a=\"$";
			AssertInsideAttributeValue (xml);
		}
		
		[Test]
		public void DoubleQuotesTest2()
		{
			string xml = "<foo a=\"\" $";
			AssertNotInsideAttributeValue (xml);
		}
		
		[Test]
		public void DoubleQuotesTest3()
		{
			string xml = "<foo a=\"\"$";
			AssertNotInsideAttributeValue (xml);
		}
		
		[Test]
		public void DoubleQuotesTest4()
		{
			string xml = "<foo a=\" $";
			AssertInsideAttributeValue (xml);
		}

		[Test]
		public void NoXmlElementStart()
		{
			string xml = "foo a=\"$";
			AssertNotInsideAttributeValue (xml);
		}
				
		[Test]
		public void DoubleQuotesTest5()
		{
			string xml = "<foo a=\"$\"";
			AssertInsideAttributeValue (xml);
		}
		
		[Test]
		public void EqualsSignTest()
		{
			string xml = "<foo a=$";
			AssertNotInsideAttributeValue (xml);
		}
		
		[Test]
		public void SingleQuoteTest1()
		{
			string xml = "<foo a='$";
			AssertInsideAttributeValue (xml);
		}
		
		[Test]
		public void MixedQuotesTest1()
		{
			string xml = "<foo a='\"$";
			AssertInsideAttributeValue (xml);
		}
		
		[Test]
		public void MixedQuotesTest2()
		{
			string xml = "<foo a=\"'$";
			AssertInsideAttributeValue (xml);
		}
		
		[Test]
		public void MixedQuotesTest3()
		{
			string xml = "<foo a=\"''$";
			AssertInsideAttributeValue (xml);
		}

		[Test]
		public void MixedQuotesTest4()
		{
			string xml = "<foo a=\"''\"$";
			AssertNotInsideAttributeValue (xml);
		}

		public void AssertInsideAttributeValue (string doc)
		{
			TestXmlParser.AssertState (doc, p => p.AssertStateIs<XmlAttributeValueState> ());
		}

		public void AssertNotInsideAttributeValue (string doc)
		{
			TestXmlParser.AssertState (doc, p => p.AssertStateIsNot<XmlAttributeValueState> ());
		}
	}
}
