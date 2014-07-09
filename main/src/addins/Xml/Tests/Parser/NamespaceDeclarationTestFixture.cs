
using System.Linq;
using MonoDevelop.Xml.Dom;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
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
			AssertIsNamespaceDeclaration("<foo xmlns=$");
		}
		
		[Test]
		public void SuccessTest2()
		{
			AssertIsNamespaceDeclaration("<foo xmlns =$");
		}
		
		[Test]
		public void SuccessTest3()
		{
			AssertIsNamespaceDeclaration("<foo \r\nxmlns\r\n=$");
		}		
		
		[Test]
		public void SuccessTest4()
		{
			AssertIsNamespaceDeclaration("<foo xmlns:nant=$");
		}	
		
		[Test]
		public void FailureTest1()
		{
			AssertNotNamespaceDeclaration("<foo xmlns$");
		}		
		
		[Test]
		public void FailureTest2()
		{
			AssertNotNamespaceDeclaration("<foo xmlns:nant$");
		}		
		
		[Test]
		public void FailureTest3()
		{
			AssertNotNamespaceDeclaration(" xmlns=$");
		}	
		
		[Test]
		public void FailureTest4()
		{
			AssertNotNamespaceDeclaration(" xmlns$");
		}			
		
		[Test]
		public void FailureTest5()
		{
			AssertNotNamespaceDeclaration(" xmlns:f$");
		}	
		
		[Test]
		public void FailureTest6()
		{
			AssertNotNamespaceDeclaration("<foo bar=$");
		}		
		
		[Test]
		public void FailureTest7()
		{
			AssertNotNamespaceDeclaration("$");
		}

		public void AssertIsNamespaceDeclaration (string doc)
		{
			TestXmlParser.AssertState (doc, p => {
				var node = p.Nodes.FirstOrDefault () as XAttribute;
				Assert.NotNull (node);
				Assert.IsTrue (node.IsNamed);
				if (node.Name.HasPrefix)
					Assert.AreEqual ("xmlns", node.Name.Prefix);
				else
					Assert.AreEqual ("xmlns", node.Name.Name);
			});
		}

		public void AssertNotNamespaceDeclaration (string doc)
		{
			TestXmlParser.AssertState (doc, p => {
				var node = p.Nodes.FirstOrDefault () as XAttribute;
				if (node != null && node.IsNamed) {
					if (node.Name.HasPrefix)
						Assert.AreNotEqual ("xmlns", node.Name.Prefix);
					else
						Assert.AreNotEqual ("xmlns", node.Name.Name);
				}
			});
		}
	}
}
