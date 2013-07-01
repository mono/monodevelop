using NUnit.Framework;
using System.IO;
using System.Text;
using System.Xml;

namespace MonoDevelop.XmlEditor.Tests.XPathQuery
{
/*	[TestFixture]
	public class XPathHistoryListTests
	{
		StringBuilder xml;
		XmlWriter writer;
		
		[SetUp]
		public void Init()
		{
			xml = new StringBuilder();
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.OmitXmlDeclaration = true;
			settings.IndentChars = "\t";
			writer = XmlWriter.Create(xml, settings);
		}
				
		[Test]
		public void ToXmlNoItems()
		{
			XPathHistoryList list = new XPathHistoryList();
			list.WriteTo(writer);
			
			string expectedXml = "<XPathHistoryList />";
			Assert.AreEqual(expectedXml, xml.ToString());
		}
		
		[Test]
		public void ToXmlOneItem()
		{
			XPathHistoryList list = new XPathHistoryList();
			list.Add("//test");
			list.WriteTo(writer);
			
			string expectedXml = "<XPathHistoryList>\n" +
				"\t<XPath>//test</XPath>\n" +
				"</XPathHistoryList>";
			Assert.AreEqual(expectedXml, xml.ToString());
		}
		
		[Test]
		public void FromXmlNoItems()
		{
			XPathHistoryList list = new XPathHistoryList();
			list.WriteTo(writer);
			
			string propertiesXml = "<SerializedNode>" + xml.ToString() + "</SerializedNode>";
			XmlTextReader reader = new XmlTextReader(new StringReader(propertiesXml));
			list = new XPathHistoryList();
			list = (XPathHistoryList)list.ReadFrom(reader);
			
			Assert.AreEqual(0, list.GetXPaths().Length);
		}
				
		[Test]
		public void FromXmlOneItem()
		{
			XPathHistoryList list = new XPathHistoryList();
			list.Add("//test");
			list.WriteTo(writer);
			
			string propertiesXml = "<SerializedNode>" + xml.ToString() + "</SerializedNode>";
			XmlTextReader reader = new XmlTextReader(new StringReader(propertiesXml));
			list = new XPathHistoryList();
			list = (XPathHistoryList)list.ReadFrom(reader);
			string[] xpaths = list.GetXPaths();

			Assert.AreEqual(1, xpaths.Length);
			Assert.AreEqual("//test", xpaths[0]);
		}
		
		[Test]
		public void FromXmlTwoItems()
		{
			XPathHistoryList list = new XPathHistoryList();
			list.Add("//test");
			list.Add("//a");
			list.WriteTo(writer);
			
			string propertiesXml = "<SerializedNode>" + xml.ToString() + "</SerializedNode>";
			XmlTextReader reader = new XmlTextReader(new StringReader(propertiesXml));
			list = new XPathHistoryList();
			list = (XPathHistoryList)list.ReadFrom(reader);
			
			string[] xpaths = list.GetXPaths();
			Assert.AreEqual(2, xpaths.Length);
			Assert.AreEqual("//test", xpaths[0]);
			Assert.AreEqual("//a", xpaths[1]);
		}				
		
		[Test]
		public void FromXmlContainingNoXPathHistoryList()
		{
			XPathHistoryList list = new XPathHistoryList();
			XmlTextReader reader = new XmlTextReader(new StringReader("<SerializedNode/>"));
			list = (XPathHistoryList)list.ReadFrom(reader);
			Assert.AreEqual(0, list.GetXPaths().Length);
		}
	}*/
}
