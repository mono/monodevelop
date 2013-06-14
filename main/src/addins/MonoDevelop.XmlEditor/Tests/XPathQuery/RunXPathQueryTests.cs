using MonoDevelop.XmlEditor;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.XPath;

namespace MonoDevelop.XmlEditor.Tests.XPathQuery
{
/*	[TestFixture]
	public class RunXPathQueryTests
	{		
		[Test]
		public void OneElementNode()
		{
			string xml = "<root>\r\n" +
				"\t<foo/>\r\n" +
				"</root>";

			XPathNodeMatch[] nodes = XmlEditorView.SelectNodes(xml, "//foo");
			XPathNodeMatch node = nodes[0];
			IXmlLineInfo lineInfo = node as IXmlLineInfo;
			Assert.AreEqual(1, nodes.Length);
			Assert.AreEqual(1, node.LineNumber);
			Assert.AreEqual(2, node.LinePosition);
			Assert.AreEqual("foo", node.Value);
			Assert.AreEqual("<foo/>", node.DisplayValue);
			Assert.AreEqual(XPathNodeType.Element, node.NodeType);
			Assert.IsNotNull(lineInfo);
		}
		
		[Test]
		public void OneElementNodeWithNamespace()
		{
			string xml = "<root xmlns='http://foo.com'>\r\n" +
				"\t<foo></foo>\r\n" +
				"</root>";
			List<XmlNamespace> namespaces = new List<XmlNamespace>();
			namespaces.Add(new XmlNamespace("f", "http://foo.com"));
			ReadOnlyCollection<XmlNamespace> readOnlyNamespaces = new ReadOnlyCollection<XmlNamespace>(namespaces);
			XPathNodeMatch[] nodes = XmlEditorView.SelectNodes(xml, "//f:foo", readOnlyNamespaces);
			XPathNodeMatch node = nodes[0];
			IXmlLineInfo lineInfo = node as IXmlLineInfo;
			Assert.AreEqual(1, nodes.Length);
			Assert.AreEqual(1, node.LineNumber);
			Assert.AreEqual(2, node.LinePosition);
			Assert.AreEqual("foo", node.Value);
			Assert.AreEqual("<foo>", node.DisplayValue);
			Assert.AreEqual(XPathNodeType.Element, node.NodeType);
			Assert.IsNotNull(lineInfo);
		}
		
		[Test]
		public void ElementWithNamespacePrefix()
		{
			string xml = "<f:root xmlns:f='http://foo.com'>\r\n" +
				"\t<f:foo></f:foo>\r\n" +
				"</f:root>";
			List<XmlNamespace> namespaces = new List<XmlNamespace>();
			namespaces.Add(new XmlNamespace("fo", "http://foo.com"));
			ReadOnlyCollection<XmlNamespace> readOnlyNamespaces = new ReadOnlyCollection<XmlNamespace>(namespaces);
			XPathNodeMatch[] nodes = XmlEditorView.SelectNodes(xml, "//fo:foo", readOnlyNamespaces);
			XPathNodeMatch node = nodes[0];
			IXmlLineInfo lineInfo = node as IXmlLineInfo;
			Assert.AreEqual(1, nodes.Length);
			Assert.AreEqual(1, node.LineNumber);
			Assert.AreEqual(2, node.LinePosition);
			Assert.AreEqual("f:foo", node.Value);
			Assert.AreEqual("<f:foo>", node.DisplayValue);
			Assert.AreEqual(XPathNodeType.Element, node.NodeType);
			Assert.IsNotNull(lineInfo);
		}
		
		[Test]
		public void NoNodeFound()
		{
			string xml = "<root>\r\n" +
				"\t<foo/>\r\n" +
				"</root>";
			XPathNodeMatch[] nodes = XmlEditorView.SelectNodes(xml, "//bar");
			Assert.AreEqual(0, nodes.Length);
		}
		
		[Test]
		public void TextNode()
		{
			string xml = "<root>\r\n" +
				"\t<foo>test</foo>\r\n" +
				"</root>";
			XPathNodeMatch[] nodes = XmlEditorView.SelectNodes(xml, "//foo/text()");
			XPathNodeMatch node = nodes[0];
			Assert.AreEqual(1, nodes.Length);
			Assert.AreEqual(1, node.LineNumber);
			Assert.AreEqual(6, node.LinePosition);
			Assert.AreEqual("test", node.Value);
			Assert.AreEqual("test", node.DisplayValue);
		}
		
		[Test]
		public void CommentNode()
		{
			string xml = "<!-- Test --><root/>";
			XPathNodeMatch[] nodes = XmlEditorView.SelectNodes(xml, "//comment()");
			XPathNodeMatch node = nodes[0];
			Assert.AreEqual(1, nodes.Length);
			Assert.AreEqual(0, node.LineNumber);
			Assert.AreEqual(4, node.LinePosition);
			Assert.AreEqual(" Test ", node.Value);
			Assert.AreEqual("<!-- Test -->", node.DisplayValue);
		}
		
		[Test]
		public void EmptyCommentNode()
		{
			string xml = "<!----><root/>";
			XPathNodeMatch[] nodes = XmlEditorView.SelectNodes(xml, "//comment()");
			XPathNodeMatch node = nodes[0];
			Assert.AreEqual(1, nodes.Length);
			Assert.AreEqual(0, node.LineNumber);
			Assert.AreEqual(4, node.LinePosition);
			Assert.AreEqual(String.Empty, node.Value);
			Assert.AreEqual("<!---->", node.DisplayValue);
		}
		
		[Test]
		public void NamespaceNode()
		{
			string xml = "<root xmlns='http://foo.com'/>";
			XPathNodeMatch[] nodes = XmlEditorView.SelectNodes(xml, "//namespace::*");
			XPathNodeMatch node = nodes[0];
			XPathNodeMatch xmlNamespaceNode = nodes[1];
			Assert.AreEqual(2, nodes.Length);
			Assert.AreEqual(0, node.LineNumber);
			Assert.AreEqual(6, node.LinePosition);
			Assert.AreEqual("xmlns=\"http://foo.com\"", node.Value);
			Assert.AreEqual("xmlns=\"http://foo.com\"", node.DisplayValue);
			Assert.IsFalse(xmlNamespaceNode.HasLineInfo());
			Assert.AreEqual("xmlns:xml=\"http://www.w3.org/XML/1998/namespace\"", xmlNamespaceNode.Value);
		}
		
		[Test]
		public void ProcessingInstructionNode()
		{
			string xml = "<root><?test processinstruction='1.0'?></root>";
			XPathNodeMatch[] nodes = XmlEditorView.SelectNodes(xml, "//processing-instruction()");
			XPathNodeMatch node = nodes[0];
			Assert.AreEqual("test", node.Value);
			Assert.AreEqual("<?test processinstruction='1.0'?>", node.DisplayValue);
			Assert.AreEqual(0, node.LineNumber);
			Assert.AreEqual(8, node.LinePosition);
		}
		
		[Test]
		public void AttributeNode()
		{
			string xml = "<root>\r\n" +
				"\t<foo Id='ab'></foo>\r\n" +
				"</root>";
			XPathNodeMatch[] nodes = XmlEditorView.SelectNodes(xml, "//foo/@Id");
			XPathNodeMatch node = nodes[0];
			Assert.AreEqual(1, nodes.Length);
			Assert.AreEqual(1, node.LineNumber);
			Assert.AreEqual(6, node.LinePosition);
			Assert.AreEqual("Id", node.Value);
			Assert.AreEqual("@Id", node.DisplayValue);
		}
	}*/
}
