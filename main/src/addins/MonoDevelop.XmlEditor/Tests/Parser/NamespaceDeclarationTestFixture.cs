
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Parser
{
	/// <summary>
	/// When the user hits the '=' key we need to produce intellisense
	/// if the attribute is of the form 'xmlns' or 'xmlns:foo'.  This
	/// tests the parsing of the text before the cursor to see if the
	/// attribute is a namespace declaration.
	/// </summary>
	[TestFixture]
	public class NamespaceDeclarationTestFixture
	{		
		[Test]
		public void SuccessTest1()
		{
			string text = "<foo xmlns=";
			bool isNamespace = XmlParser.IsNamespaceDeclaration(text, text.Length);
			Assert.IsTrue(isNamespace, "Namespace should be recognised.");
		}
		
		[Test]
		public void SuccessTest2()
		{
			string text = "<foo xmlns =";
			bool isNamespace = XmlParser.IsNamespaceDeclaration(text, text.Length);
			Assert.IsTrue(isNamespace, "Namespace should be recognised.");
		}
		
		[Test]
		public void SuccessTest3()
		{
			string text = "<foo \r\nxmlns\r\n=";
			bool isNamespace = XmlParser.IsNamespaceDeclaration(text, text.Length);
			Assert.IsTrue(isNamespace, "Namespace should be recognised.");
		}		
		
		[Test]
		public void SuccessTest4()
		{
			string text = "<foo xmlns:nant=";
			bool isNamespace = XmlParser.IsNamespaceDeclaration(text, text.Length);
			Assert.IsTrue(isNamespace, "Namespace should be recognised.");
		}	
		
		[Test]
		public void SuccessTest5()
		{
			string text = "<foo xmlns";
			bool isNamespace = XmlParser.IsNamespaceDeclaration(text, text.Length);
			Assert.IsTrue(isNamespace, "Namespace should be recognised.");
		}		
		
		[Test]
		public void SuccessTest6()
		{
			string text = "<foo xmlns:nant";
			bool isNamespace = XmlParser.IsNamespaceDeclaration(text, text.Length);
			Assert.IsTrue(isNamespace, "Namespace should be recognised.");
		}		
		
		[Test]
		public void SuccessTest7()
		{
			string text = " xmlns=";
			bool isNamespace = XmlParser.IsNamespaceDeclaration(text, text.Length);
			Assert.IsTrue(isNamespace, "Namespace should be recognised.");
		}	
		
		[Test]
		public void SuccessTest8()
		{
			string text = " xmlns";
			bool isNamespace = XmlParser.IsNamespaceDeclaration(text, text.Length);
			Assert.IsTrue(isNamespace, "Namespace should be recognised.");
		}			
		
		[Test]
		public void SuccessTest9()
		{
			string text = " xmlns:f";
			bool isNamespace = XmlParser.IsNamespaceDeclaration(text, text.Length);
			Assert.IsTrue(isNamespace, "Namespace should be recognised.");
		}	
		
		[Test]
		public void FailureTest1()
		{
			string text = "<foo bar=";
			bool isNamespace = XmlParser.IsNamespaceDeclaration(text, text.Length);
			Assert.IsFalse(isNamespace, "Namespace should not be recognised.");
		}		
		
		[Test]
		public void FailureTest2()
		{
			string text = "";
			bool isNamespace = XmlParser.IsNamespaceDeclaration(text, text.Length);
			Assert.IsFalse(isNamespace, "Namespace should not be recognised.");
		}		
		
		[Test]
		public void FailureTest3()
		{
			string text = " ";
			bool isNamespace = XmlParser.IsNamespaceDeclaration(text, text.Length);
			Assert.IsFalse(isNamespace, "Namespace should not be recognised.");
		}			
	}
}
