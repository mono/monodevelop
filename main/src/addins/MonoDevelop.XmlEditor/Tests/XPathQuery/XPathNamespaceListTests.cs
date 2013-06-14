
using MonoDevelop.Core;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace MonoDevelop.XmlEditor.Tests.XPathQuery
{
/*	[TestFixture]
	public class XPathNamespaceListTests
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
			XPathNamespaceList list = new XPathNamespaceList();
			list.WriteTo(writer);
			string expectedXml = "<XPathNamespaceList />";
			
			Assert.AreEqual(expectedXml, xml.ToString());
		}
		
		[Test]
		public void ToXmlOneItem()
		{
			XPathNamespaceList list = new XPathNamespaceList();
			list.Add("n", "http://mono-project.com");			
			list.WriteTo(writer);
			string expectedXml = "<XPathNamespaceList>\n" +
				"\t<Namespace>Prefix [n] Uri [http://mono-project.com]</Namespace>\n" +
				"</XPathNamespaceList>";
			
			Assert.AreEqual(expectedXml, xml.ToString());			
		}
		
		[Test]
		public void FromXmlNoItems()
		{
			XPathNamespaceList list = new XPathNamespaceList();
			list.WriteTo(writer);
			
			string propertiesXml = "<SerializedNode>" + xml.ToString() + "</SerializedNode>";
			XmlTextReader reader = new XmlTextReader(new StringReader(propertiesXml));
			list = new XPathNamespaceList();
			list = (XPathNamespaceList)list.ReadFrom(reader);
			
			Assert.AreEqual(0, list.GetNamespaces().Length);
		}
				
		[Test]
		public void FromXmlOneItem()
		{
			XPathNamespaceList list = new XPathNamespaceList();
			list.Add("n", "http://mono-project.com");
			list.WriteTo(writer);
			
			string propertiesXml = "<SerializedNode>" + xml.ToString() + "</SerializedNode>";
			XmlTextReader reader = new XmlTextReader(new StringReader(propertiesXml));
			list = new XPathNamespaceList();
			list = (XPathNamespaceList)list.ReadFrom(reader);
			
			XmlNamespace[] namespaces = list.GetNamespaces();
			Assert.AreEqual(1, namespaces.Length);
			Assert.AreEqual("n", namespaces[0].Prefix);
			Assert.AreEqual("http://mono-project.com", namespaces[0].Uri);
		}
		
		[Test]
		public void FromXmlTwoItems()
		{
			XPathNamespaceList list = new XPathNamespaceList();
			list.Add("a", "Namespace-a");
			list.Add("b", "Namespace-b");
			list.WriteTo(writer);
			
			string propertiesXml = "<SerializedNode>" + xml.ToString() + "</SerializedNode>";
			XmlTextReader reader = new XmlTextReader(new StringReader(propertiesXml));
			list = new XPathNamespaceList();
			list = (XPathNamespaceList)list.ReadFrom(reader);
			
			XmlNamespace[] namespaces = list.GetNamespaces();
			Assert.AreEqual(2, namespaces.Length);
			Assert.AreEqual("Namespace-a", namespaces[0].Uri);
			Assert.AreEqual("Namespace-b", namespaces[1].Uri);
		}		
		
		[Test]
		public void InvalidNamespace()
		{
			string xml = "<XPathNamespaceList>\n" +
				"\t<Namespace>Prefix [n] Uri [http://mono-project.com]</Namespace>\n" +
				"\t<Namespace></Namespace>\n" +
				"</XPathNamespaceList>";

			string propertiesXml = "<SerializedNode>" + xml.ToString() + "</SerializedNode>";
			XmlTextReader reader = new XmlTextReader(new StringReader(propertiesXml));
			XPathNamespaceList list = new XPathNamespaceList();
			list = (XPathNamespaceList)list.ReadFrom(reader);
			
			XmlNamespace[] namespaces = list.GetNamespaces();
			Assert.AreEqual(2, namespaces.Length);
			Assert.AreEqual("http://mono-project.com", namespaces[0].Uri);
			Assert.AreEqual(String.Empty, namespaces[1].Uri);
		}
		
		[Test]
		public void FromXmlContainingNoXPathNamespaceList()
		{
			XPathNamespaceList list = new XPathNamespaceList();
			XmlTextReader reader = new XmlTextReader(new StringReader("<SerializedNode/>"));
			list = (XPathNamespaceList)list.ReadFrom(reader);
			Assert.AreEqual(0, list.GetNamespaces().Length);
		}		
	}*/
}
