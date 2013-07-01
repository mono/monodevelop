using MonoDevelop.XmlEditor;
using NUnit.Framework;
using System;

namespace MonoDevelop.XmlEditor.Tests.Parser
{
	[TestFixture]
	public class InsideAttributeValueTestFixture
	{
		[Test]
		public void InvalidString()
		{
			Assert.IsFalse(XmlParser.IsInsideAttributeValue(String.Empty, 10));
		}
		
		[Test]
		public void DoubleQuotesTest1()
		{
			string xml = "<foo a=\"";
			Assert.IsTrue(XmlParser.IsInsideAttributeValue(xml, xml.Length));
		}
		
		[Test]
		public void DoubleQuotesTest2()
		{
			string xml = "<foo a=\"\" ";
			Assert.IsFalse(XmlParser.IsInsideAttributeValue(xml, xml.Length));
		}
		
		[Test]
		public void DoubleQuotesTest3()
		{
			string xml = "<foo a=\"\"";
			Assert.IsFalse(XmlParser.IsInsideAttributeValue(xml, xml.Length));
		}
		
		[Test]
		public void DoubleQuotesTest4()
		{
			string xml = "<foo a=\" ";
			Assert.IsTrue(XmlParser.IsInsideAttributeValue(xml, xml.Length));
		}

		[Test]
		public void NoXmlElementStart()
		{
			string xml = "foo a=\"";
			Assert.IsFalse(XmlParser.IsInsideAttributeValue(xml, xml.Length));
		}
				
		[Test]
		public void DoubleQuotesTest5()
		{
			string xml = "<foo a=\"\"";
			Assert.IsTrue(XmlParser.IsInsideAttributeValue(xml, 8));
		}
		
		[Test]
		public void EqualsSignTest()
		{
			string xml = "<foo a=";
			Assert.IsFalse(XmlParser.IsInsideAttributeValue(xml, xml.Length));
		}
		
		[Test]
		public void SingleQuoteTest1()
		{
			string xml = "<foo a='";
			Assert.IsTrue(XmlParser.IsInsideAttributeValue(xml, xml.Length));
		}
		
		[Test]
		public void MixedQuotesTest1()
		{
			string xml = "<foo a='\"";
			Assert.IsTrue(XmlParser.IsInsideAttributeValue(xml, xml.Length));
		}
		
		[Test]
		public void MixedQuotesTest2()
		{
			string xml = "<foo a=\"'";
			Assert.IsTrue(XmlParser.IsInsideAttributeValue(xml, xml.Length));
		}
		
		[Test]
		public void MixedQuotesTest3()
		{
			string xml = "<foo a=\"''";
			Assert.IsTrue(XmlParser.IsInsideAttributeValue(xml, xml.Length));
		}

		[Test]
		public void MixedQuotesTest4()
		{
			string xml = "<foo a=\"''\"";
			Assert.IsFalse(XmlParser.IsInsideAttributeValue(xml, xml.Length));
		}
	}
}
