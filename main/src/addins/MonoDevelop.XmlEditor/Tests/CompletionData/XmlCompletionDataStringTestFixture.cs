using MonoDevelop.XmlEditor.Completion;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.CompletionData
{	
	[TestFixture]
	public class XmlCompletionDataStringTestFixture
	{
		[Test]
		public void XmlElementCompletionString()
		{
			XmlCompletionData data = new XmlCompletionData("foo", XmlCompletionData.DataType.XmlElement);
			Assert.AreEqual("foo", data.CompletionText);
		}
		
		[Test]
		public void NamespaceUriCompletionString()
		{
			XmlCompletionData data = new XmlCompletionData("foo", XmlCompletionData.DataType.NamespaceUri);
			Assert.AreEqual("foo", data.CompletionText);
		}
		
		[Test]
		public void AttributeCompletionString()
		{
			XmlCompletionData data = new XmlCompletionData("foo", XmlCompletionData.DataType.XmlAttribute);
			Assert.AreEqual("foo", data.CompletionText);
		}
		
		[Test]
		public void AttributeValueCompletionString()
		{
			XmlCompletionData data = new XmlCompletionData("foo", XmlCompletionData.DataType.XmlAttributeValue);
			Assert.AreEqual("foo", data.CompletionText);
		}
	}
}
