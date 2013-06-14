using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Tests duplicate elements in the schema.
	/// </summary>
	[TestFixture]
	public class DuplicateElementTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList htmlChildElements;
		
		public override void FixtureInit()
		{
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("html", "http://foo/xhtml"));
		
			htmlChildElements = SchemaCompletionData.GetChildElementCompletionData(path);
		}		
		
		[Test]
		public void HtmlHasTwoChildElements()
		{
			Assert.AreEqual(2, htmlChildElements.Count, 
			                "Should be 2 child elements.");
		}
		
		[Test]
		public void HtmlChildElementHead()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(htmlChildElements, "head"), 
			              "Should have a child element called head.");
		}
		
		[Test]
		public void HtmlChildElementBody()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(htmlChildElements, "body"), 
			              "Should have a child element called body.");
		}		
		
		protected override string GetSchema()
		{
			return "<xs:schema version=\"1.0\" xml:lang=\"en\"\r\n" +
					"    xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\r\n" +
					"    targetNamespace=\"http://foo/xhtml\"\r\n" +
					"    xmlns=\"http://foo/xhtml\"\r\n" +
					"    elementFormDefault=\"qualified\">\r\n" +
					"\r\n" +
					"  <xs:element name=\"html\">\r\n" +
					"    <xs:complexType>\r\n" +
					"      <xs:choice>\r\n" +
					"        <xs:sequence>\r\n" +
					"          <xs:element name=\"head\"/>\r\n" +
					"          <xs:element name=\"body\"/>\r\n" +
					"        </xs:sequence>\r\n" +
					"        <xs:sequence>\r\n" +
					"          <xs:element name=\"body\"/>\r\n" +
					"        </xs:sequence>\r\n" +
					"      </xs:choice>\r\n" +
					"    </xs:complexType>\r\n" +
					"  </xs:element>\r\n" +
					"\r\n" +
					"</xs:schema>";
		}
	}
}
