
using MonoDevelop.Core.Properties;
using NUnit.Framework;
using System;
using System.Xml;

namespace MonoDevelop.XmlEditor.Tests.XPathQuery
{
	[TestFixture]
	public class XPathNamespaceListTests
	{
		[Test]
		public void ToXmlElementNoItems()
		{
			XmlDocument doc = new XmlDocument();
			XPathNamespaceList list = new XPathNamespaceList();
			XmlElement element = list.ToXmlElement(doc);
			Assert.AreEqual("XPathNamespaceList", element.LocalName);
			Assert.AreEqual(0, element.ChildNodes.Count);
		}
		
		[Test]
		public void ToXmlElementOneItem()
		{
			XmlDocument doc = new XmlDocument();
			XPathNamespaceList list = new XPathNamespaceList();
			list.Add("n", "http://mono-project.com");
			XmlElement element = list.ToXmlElement(doc);
			Assert.AreEqual(1, element.ChildNodes.Count);
			XmlElement namespaceElement = (XmlElement)element.ChildNodes[0];
			Assert.AreEqual("Namespace", namespaceElement.LocalName);
			XmlNamespace xmlNs = new XmlNamespace("n", "http://mono-project.com");
			Assert.AreEqual(xmlNs.ToString(), namespaceElement.InnerText);
		}
		
		[Test]
		public void FromXmlElementNoItems()
		{
			XmlDocument doc = new XmlDocument();
			XPathNamespaceList list = new XPathNamespaceList();
			XmlElement namespaceListElement = list.ToXmlElement(doc);
			
			list = new XPathNamespaceList();
			list = (XPathNamespaceList)list.FromXmlElement(namespaceListElement);
			
			Assert.AreEqual(0, list.GetNamespaces().Length);
		}
		
		[Test]
		public void FromNullXmlElement()
		{
			XPathNamespaceList list = new XPathNamespaceList();
			Assert.IsNull(list.FromXmlElement(null));
		}
		
		[Test]
		public void FromXmlElementOneItem()
		{
			XmlDocument doc = new XmlDocument();
			XPathNamespaceList list = new XPathNamespaceList();
			list.Add("n", "http://mono-project.com");
			XmlElement namespaceListElement = list.ToXmlElement(doc);
			
			list = new XPathNamespaceList();
			list = (XPathNamespaceList)list.FromXmlElement(namespaceListElement);
			
			XmlNamespace[] namespaces = list.GetNamespaces();
			Assert.AreEqual(1, namespaces.Length);
			Assert.AreEqual("n", namespaces[0].Prefix);
			Assert.AreEqual("http://mono-project.com", namespaces[0].Uri);
		}
		
		[Test]
		public void FromXmlElementTwoItems()
		{
			XmlDocument doc = new XmlDocument();
			XPathNamespaceList list = new XPathNamespaceList();
			list.Add("a", "Namespace-a");
			list.Add("b", "Namespace-b");
			XmlElement namespaceListElement = list.ToXmlElement(doc);
			
			list = new XPathNamespaceList();
			list = (XPathNamespaceList)list.FromXmlElement(namespaceListElement);
			
			XmlNamespace[] namespaces = list.GetNamespaces();
			Assert.AreEqual(2, namespaces.Length);
			Assert.AreEqual("Namespace-a", namespaces[0].Uri);
			Assert.AreEqual("Namespace-b", namespaces[1].Uri);
		}		
		
		[Test]
		[ExpectedException(typeof(UnknownPropertyNodeException))]
		public void FromInvalidXPathHistoryXmlElement()
		{
			XmlDocument doc = new XmlDocument();
			XmlElement namespaceListElement = doc.CreateElement("Test");
			XmlElement namespaceElement = doc.CreateElement("Namespace");
			namespaceListElement.AppendChild(namespaceElement);
						
			XPathNamespaceList list = new XPathNamespaceList();
			list = (XPathNamespaceList)list.FromXmlElement(namespaceListElement);
		}
	}
}
