using MonoDevelop.Xml.Completion;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.CompletionData
{
	[TestFixture]
	public class XmlCompletionDataIconTestFixture
	{
		[Test]
		public void IconNotNull ()
		{
			XmlCompletionData data = new XmlCompletionData ("foo");
			Assert.IsFalse (data.Icon.IsNull);
		}
		
		[Test]
		public void IconNotEmptyString ()
		{
			XmlCompletionData data = new XmlCompletionData ("foo");
			Assert.IsTrue (data.Icon.Name.Length > 0);
		}
	}
}
