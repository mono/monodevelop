
using MonoDevelop.Core.Properties;
using NUnit.Framework;
using System;
using System.Xml;

namespace MonoDevelop.XmlEditor.Tests.XPathQuery
{
	[TestFixture]
	public class XPathHistoryListTests
	{
		[Test]
		public void ToXmlElementNoItems()
		{
			XmlDocument doc = new XmlDocument();
			XPathHistoryList list = new XPathHistoryList();
			XmlElement element = list.ToXmlElement(doc);
			Assert.AreEqual("XPathHistoryList", element.LocalName);
			Assert.AreEqual(0, element.ChildNodes.Count);
		}
		
		[Test]
		public void ToXmlElementOneItem()
		{
			XmlDocument doc = new XmlDocument();
			XPathHistoryList list = new XPathHistoryList();
			list.Add("//test");
			XmlElement element = list.ToXmlElement(doc);
			Assert.AreEqual(1, element.ChildNodes.Count);
			XmlElement xpathElement = (XmlElement)element.ChildNodes[0];
			Assert.AreEqual("XPath", xpathElement.LocalName);
			Assert.AreEqual("//test", xpathElement.InnerText);
		}
		
		[Test]
		public void FromXmlElementNoItems()
		{
			XmlDocument doc = new XmlDocument();
			XPathHistoryList list = new XPathHistoryList();
			XmlElement xpathListElement = list.ToXmlElement(doc);
			
			list = new XPathHistoryList();
			list = (XPathHistoryList)list.FromXmlElement(xpathListElement);
			
			Assert.AreEqual(0, list.GetXPaths().Length);
		}
		
		[Test]
		public void FromNullXmlElement()
		{
			XPathHistoryList list = new XPathHistoryList();
			Assert.IsNull(list.FromXmlElement(null));
		}
		
		[Test]
		public void FromXmlElementOneItem()
		{
			XmlDocument doc = new XmlDocument();
			XPathHistoryList list = new XPathHistoryList();
			list.Add("//test");
			XmlElement xpathListElement = list.ToXmlElement(doc);
			
			list = new XPathHistoryList();
			list = (XPathHistoryList)list.FromXmlElement(xpathListElement);
			
			string[] xpaths = list.GetXPaths();
			Assert.AreEqual(1, xpaths.Length);
			Assert.AreEqual("//test", xpaths[0]);
		}
		
		[Test]
		public void FromXmlElementTwoItems()
		{
			XmlDocument doc = new XmlDocument();
			XPathHistoryList list = new XPathHistoryList();
			list.Add("//test");
			list.Add("//a");
			XmlElement xpathListElement = list.ToXmlElement(doc);
			
			list = new XPathHistoryList();
			list = (XPathHistoryList)list.FromXmlElement(xpathListElement);
			
			string[] xpaths = list.GetXPaths();
			Assert.AreEqual(2, xpaths.Length);
			Assert.AreEqual("//test", xpaths[0]);
			Assert.AreEqual("//a", xpaths[1]);
		}		
		
		[Test]
		[ExpectedException(typeof(UnknownPropertyNodeException))]
		public void FromInvalidXPathHistoryXmlElement()
		{
			XmlDocument doc = new XmlDocument();
			XmlElement xpathListElement = doc.CreateElement("Test");
			XmlElement xpathElement = doc.CreateElement("XPath");
			xpathElement.InnerText = "//test";
			xpathListElement.AppendChild(xpathElement);
						
			XPathHistoryList list = new XPathHistoryList();
			list = (XPathHistoryList)list.FromXmlElement(xpathListElement);
		}
	}
}
