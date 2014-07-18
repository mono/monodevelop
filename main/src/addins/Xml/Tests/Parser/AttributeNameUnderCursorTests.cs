using System.Linq;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
{
	[TestFixture]
	public class AttributeNameUnderCursorTests
	{
		[Test]
		public void SuccessTest1()
		{
			AssertAttributeName ("<a foo$", "foo");
		}
		
		[Test]
		public void SuccessTest2()
		{
			AssertAttributeName ("<a foo=$", "foo");
		}
		
		[Test]
		public void SuccessTest3()
		{
			AssertAttributeName ("<a foo='$", "foo");
		}
		
		[Test]
		public void SuccessTest4()
		{
			AssertAttributeName ("<a type='a$", "type");
		}

		public void AssertAttributeName (string doc, string name)
		{
			TestXmlParser.AssertState (doc, p => {
				var att = p.Nodes.First () as XAttribute;
				Assert.NotNull (att);
				Assert.AreEqual (name, GetName (p));
			});
		}

		static string GetName (XmlParser parser)
		{
			var namedObject = parser.Nodes.First () as INamedXObject;
			Assert.NotNull (namedObject);
			if (namedObject.IsNamed)
				return namedObject.Name.ToString ();

			var state = parser.CurrentState as XmlNameState;
			Assert.NotNull (state);
			return ((IXmlParserContext)parser).KeywordBuilder.ToString ();
		}
	}
}
