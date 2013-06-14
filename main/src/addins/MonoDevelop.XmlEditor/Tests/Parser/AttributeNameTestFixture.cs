using MonoDevelop.XmlEditor;
using NUnit.Framework;
using System;

namespace MonoDevelop.XmlEditor.Tests.Parser
{
	/// <summary>
	/// Tests that we can detect the attribute's name.
	/// </summary>
	[TestFixture]
	public class AttributeNameTestFixture
	{		
		[Test]
		public void SuccessTest1()
		{
			string text = " foo='a";
			Assert.AreEqual("foo", XmlParser.GetAttributeName(text, text.Length), "Should have retrieved the attribute name 'foo'");
		}

		[Test]
		public void SuccessTest2()
		{
			string text = " foo='";
			Assert.AreEqual("foo", XmlParser.GetAttributeName(text, text.Length), "Should have retrieved the attribute name 'foo'");
		}		
		
		[Test]
		public void SuccessTest3()
		{
			string text = " foo=";
			Assert.AreEqual("foo", XmlParser.GetAttributeName(text, text.Length), "Should have retrieved the attribute name 'foo'");
		}			
		
		[Test]
		public void SuccessTest4()
		{
			string text = " foo=\"";
			Assert.AreEqual("foo", XmlParser.GetAttributeName(text, text.Length), "Should have retrieved the attribute name 'foo'");
		}	
		
		[Test]
		public void SuccessTest5()
		{
			string text = " foo = \"";
			Assert.AreEqual("foo", XmlParser.GetAttributeName(text, text.Length), "Should have retrieved the attribute name 'foo'");
		}			
		
		[Test]
		public void SuccessTest6()
		{
			string text = " foo = '#";
			Assert.AreEqual("foo", XmlParser.GetAttributeName(text, text.Length), "Should have retrieved the attribute name 'foo'");
		}	
		
		[Test]
		public void FailureTest1()
		{
			string text = "foo=";
			Assert.AreEqual(String.Empty, XmlParser.GetAttributeName(text, text.Length), "Should have retrieved the attribute name 'foo'");
		}		
		
		[Test]
		public void FailureTest2()
		{
			string text = "foo=<";
			Assert.AreEqual(String.Empty, XmlParser.GetAttributeName(text, text.Length), "Should have retrieved the attribute name 'foo'");
		}		
		
		[Test]
		public void FailureTest3()
		{
			string text = "a";
			Assert.AreEqual(String.Empty, XmlParser.GetAttributeName(text, text.Length));
		}	
		
		[Test]
		public void FailureTest4()
		{
			string text = " a";
			Assert.AreEqual(String.Empty, XmlParser.GetAttributeName(text, text.Length));
		}	
		
		[Test]
		public void EmptyString()
		{
			Assert.AreEqual(String.Empty, XmlParser.GetAttributeName(String.Empty, 10));
		}
	}
}
