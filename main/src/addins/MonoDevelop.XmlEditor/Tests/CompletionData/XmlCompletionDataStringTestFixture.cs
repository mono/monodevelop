
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;
using System;
using System.IO;
using System.Xml;
using MonoDevelop.XmlEditor.Tests.Utils;

namespace MonoDevelop.XmlEditor.Tests.CompletionData
{	
	[TestFixture]
	public class XmlCompletionDataStringTestFixture
	{
		[Test]
		public void XmlElementCompletionString()
		{
			XmlCompletionData data = new XmlCompletionData("foo", XmlCompletionData.DataType.XmlElement);
			Assert.AreEqual("foo", data.CompletionString);
		}
		
		[Test]
		public void NamespaceUriCompletionString()
		{
			XmlCompletionData data = new XmlCompletionData("foo", XmlCompletionData.DataType.NamespaceUri);
			Assert.AreEqual("foo", data.CompletionString);
		}
		
		[Test]
		public void AttributeCompletionString()
		{
			XmlCompletionData data = new XmlCompletionData("foo", XmlCompletionData.DataType.XmlAttribute);
			Assert.AreEqual("foo", data.CompletionString);
		}
		
		[Test]
		public void AttributeValueCompletionString()
		{
			XmlCompletionData data = new XmlCompletionData("foo", XmlCompletionData.DataType.XmlAttributeValue);
			Assert.AreEqual("foo", data.CompletionString);
		}
	}
}
