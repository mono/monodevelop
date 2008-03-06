
using MonoDevelop.XmlEditor;
using NUnit.Framework;
using System;

namespace MonoDevelop.XmlEditor.Tests.Parser
{
	[TestFixture]
	public class AttributeValueUnderCursorTests
	{
		[Test]
		public void SuccessTest1()
		{
			string text = "<a foo='abc'";
			Assert.AreEqual("abc", XmlParser.GetAttributeValueAtIndex(text, text.Length - 1));
		}
		
		[Test]
		public void SuccessTest2()
		{
			string text = "<a foo=\"abc\"";
			Assert.AreEqual("abc", XmlParser.GetAttributeValueAtIndex(text, text.Length - 1));
		}
		
		[Test]
		public void SuccessTest3()
		{
			string text = "<a foo='abc'";
			Assert.AreEqual("abc", XmlParser.GetAttributeValueAtIndex(text, text.Length - 2));
		}
		
		[Test]
		public void SuccessTest4()
		{
			string text = "<a foo='abc'";
			Assert.AreEqual("abc", XmlParser.GetAttributeValueAtIndex(text, text.IndexOf("abc")));
		}
		
		[Test]
		public void SuccessTest5()
		{
			string text = "<a foo=''";
			Assert.AreEqual(String.Empty, XmlParser.GetAttributeValueAtIndex(text, text.Length - 1));
		}
		
		[Test]
		public void SuccessTest6()
		{
			string text = "<a foo='a'";
			Assert.AreEqual("a", XmlParser.GetAttributeValueAtIndex(text, text.Length - 1));
		}
		
		[Test]
		public void SuccessTest7()
		{
			string text = "<a foo='a\"b\"c'";
			Assert.AreEqual("a\"b\"c", XmlParser.GetAttributeValueAtIndex(text, text.Length - 1));
		}
		
		[Test]
		public void FailureTest1()
		{
			string text = "<a foo='a";
			Assert.AreEqual(String.Empty, XmlParser.GetAttributeValueAtIndex(text, text.Length - 1));
		}
	}
}
