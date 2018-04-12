using System.Linq;
using MonoDevelop.Xml.Dom;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
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
			AssertAttributeName ("<a foo='a$", "foo");
		}

		[Test]
		public void SuccessTest2()
		{
			AssertAttributeName ("<a foo='$", "foo");
		}		
		
		[Test]
		public void SuccessTest3()
		{
			AssertAttributeName ("<a foo=$", "foo");
		}			
		
		[Test]
		public void SuccessTest4()
		{
			AssertAttributeName ("<a foo=\"$", "foo");
		}	
		
		[Test]
		public void SuccessTest5()
		{
			AssertAttributeName ("<a foo = \"$", "foo");
		}			
		
		[Test]
		public void SuccessTest6()
		{
			AssertAttributeName ("<a foo = '#$", "foo");
		}	
		
		[Test]
		public void SuccessTest7()
		{
			AssertAttributeName ("< foo=$", "foo");
		}
		
		[Test]
		public void FailureTest1()
		{
			NotAttribute ("foo=$");
		}		
		
		[Test]
		public void FailureTest2()
		{
			NotAttribute ("foo=<$");
		}		
		
		[Test]
		public void FailureTest3()
		{
			NotAttribute ("a$");
		}	
		
		[Test]
		public void FailureTest4()
		{
			// It's ok if we are already in attribute naming state at this point
			// even if element is not named yet, but until = is written, attribute
			// is not named yet, hence null
			AssertAttributeName ("< a$", null);
		}	
		
		[Test]
		public void EmptyString()
		{
			NotAttribute ("$");
		}

		static void AssertAttributeName (string doc, string name)
		{
			TestXmlParser.AssertState (doc, p => {
				var att = p.Nodes.FirstOrDefault () as XAttribute;
				Assert.NotNull (att);
				Assert.NotNull (att.Name);
				Assert.IsNull (att.Name.Prefix);
				Assert.AreEqual (att.Name.Name, name);
			});
		}

		static void NotAttribute (string doc)
		{
			TestXmlParser.AssertState (doc, p => {
				var att = p.Nodes.FirstOrDefault () as XAttribute;
				Assert.IsNull (att);
			});
		}
	}
}
