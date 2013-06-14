
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.XPathQuery
{
	[TestFixture]
	public class XmlNamespaceTests
	{
		[Test]
		public void SimpleNamespace()
		{
			XmlNamespace ns = new XmlNamespace("s", "http://sharpdevelop.com");
			Assert.AreEqual("s", ns.Prefix);
			Assert.AreEqual("http://sharpdevelop.com", ns.Uri);
		}
	}
}
