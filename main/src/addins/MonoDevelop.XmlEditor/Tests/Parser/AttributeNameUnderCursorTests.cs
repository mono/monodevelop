using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Parser
{
	[TestFixture]
	public class AttributeNameUnderCursorTests
	{
		[Test]
		public void SuccessTest1()
		{
			string text = "<a foo";
			Assert.AreEqual("foo", XmlParser.GetAttributeNameAtIndex(text, text.Length));
		}
		
		[Test]
		public void SuccessTest2()
		{
			string text = "<a foo";
			Assert.AreEqual("foo", XmlParser.GetAttributeNameAtIndex(text, text.IndexOf("foo")));
		}
		
		[Test]
		public void SuccessTest3()
		{
			string text = "<a foo";
			Assert.AreEqual("foo", XmlParser.GetAttributeNameAtIndex(text, text.IndexOf("oo")));
		}
		
		[Test]
		public void SuccessTest4()
		{
			string text = "<a foo";
			Assert.AreEqual("foo", XmlParser.GetAttributeNameAtIndex(text, text.Length - 2));
		}
		
		[Test]
		public void SuccessTest5()
		{
			string text = "<a foo=";
			Assert.AreEqual("foo", XmlParser.GetAttributeNameAtIndex(text, 3));
		}
		
		[Test]
		public void SuccessTest6()
		{
			string text = "<a foo=";
			Assert.AreEqual("foo", XmlParser.GetAttributeNameAtIndex(text, text.Length));
		}
		
		[Test]
		public void SuccessTest7()
		{
			string text = "<a foo='";
			Assert.AreEqual("foo", XmlParser.GetAttributeNameAtIndex(text, text.Length));
		}
		
		[Test]
		public void SuccessTest8()
		{
			string text = "<a type='a";
			Assert.AreEqual("type", XmlParser.GetAttributeNameAtIndex(text, text.Length));
		}
		
		[Test]
		public void SuccessTest9()
		{
			string text = "<a type='a'";
			Assert.AreEqual("type", XmlParser.GetAttributeNameAtIndex(text, text.Length - 1));
		}
	}
}
