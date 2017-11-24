using MonoDevelop.Xml.Completion;
using MonoDevelop.Xml.Editor;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.CompletionData
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
		[TestCase (true)]
		[TestCase (false)]
		public void NamespaceUriCompletionString (bool autoInsertFragments)
		{
			var settingBefore = XmlEditorOptions.AutoInsertFragments;
			try {
				XmlEditorOptions.AutoInsertFragments = autoInsertFragments;
				XmlCompletionData data = new XmlCompletionData ("foo", XmlCompletionData.DataType.NamespaceUri);
				if (autoInsertFragments)
					Assert.AreEqual ("\"foo\"", data.CompletionText);
				else
					Assert.AreEqual ("foo", data.CompletionText);
			} finally {
				XmlEditorOptions.AutoInsertFragments = settingBefore;
			}
		}

		[Test]
		[TestCase (true)]
		[TestCase (false)]
		public void AttributeCompletionString (bool autoInsertFragments)
		{
			var settingBefore = XmlEditorOptions.AutoInsertFragments;
			try {
				XmlEditorOptions.AutoInsertFragments = autoInsertFragments;
				XmlCompletionData data = new XmlCompletionData ("foo", XmlCompletionData.DataType.XmlAttribute);
				if (autoInsertFragments)
					Assert.AreEqual ("foo=\"|\"", data.CompletionText);
				else
					Assert.AreEqual ("foo", data.CompletionText);
			} finally {
				XmlEditorOptions.AutoInsertFragments = settingBefore;
			}
		}
		
		[Test]
		public void AttributeValueCompletionString()
		{
			XmlCompletionData data = new XmlCompletionData("foo", XmlCompletionData.DataType.XmlAttributeValue);
			Assert.AreEqual("foo", data.CompletionText);
		}
	}
}
