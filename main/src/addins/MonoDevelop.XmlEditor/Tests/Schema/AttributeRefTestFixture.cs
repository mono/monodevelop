using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Tests attribute refs
	/// </summary>
	[TestFixture]
	public class AttributeRefTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList attributes;
		
		public override void FixtureInit()
		{
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("html", "http://foo/xhtml"));
			attributes = SchemaCompletionData.GetAttributeCompletionData(path);
		}
		
		[Test]
		public void HtmlAttributeCount()
		{
			Assert.AreEqual(4, attributes.Count, 
			                "Should be 4 attributes.");
		}
		
		[Test]
		public void HtmlLangAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributes, "lang"), "Attribute lang not found.");
		}
		
		[Test]
		public void HtmlIdAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributes, "id"), "Attribute id not found.");
		}		
		
		[Test]
		public void HtmlDirAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributes, "dir"), "Attribute dir not found.");
		}			
		
		[Test]
		public void HtmlXmlLangAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributes, "xml:lang"), "Attribute xml:lang not found.");
		}				
		
		protected override string GetSchema()
		{
			return "<xs:schema version=\"1.0\" xml:lang=\"en\"\r\n" +
					"    xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\r\n" +
					"    targetNamespace=\"http://foo/xhtml\"\r\n" +
					"    xmlns=\"http://foo/xhtml\"\r\n" +
					"    elementFormDefault=\"qualified\">\r\n" +
					"  <xs:element name=\"html\">\r\n" +
					"    <xs:complexType>\r\n" +
					"      <xs:sequence>\r\n" +
					"        <xs:element ref=\"head\"/>\r\n" +
					"        <xs:element ref=\"body\"/>\r\n" +
					"      </xs:sequence>\r\n" +
					"      <xs:attributeGroup ref=\"i18n\"/>\r\n" +
					"      <xs:attribute name=\"id\" type=\"xs:ID\"/>\r\n" +
					"    </xs:complexType>\r\n" +
					"  </xs:element>\r\n" +
					"\r\n" +
					"  <xs:element name=\"head\" type=\"xs:string\"/>\r\n" +
					"  <xs:element name=\"body\" type=\"xs:string\"/>\r\n" +
					"\r\n" +
					"  <xs:attributeGroup name=\"i18n\">\r\n" +
					"    <xs:annotation>\r\n" +
					"      <xs:documentation>\r\n" +
					"      internationalization attributes\r\n" +
					"      lang        language code (backwards compatible)\r\n" +
					"      xml:lang    language code (as per XML 1.0 spec)\r\n" +
					"      dir         direction for weak/neutral text\r\n" +
					"      </xs:documentation>\r\n" +
					"    </xs:annotation>\r\n" +
					"    <xs:attribute name=\"lang\" type=\"LanguageCode\"/>\r\n" +
					"    <xs:attribute ref=\"xml:lang\"/>\r\n" +
					"\r\n" +
					"    <xs:attribute name=\"dir\">\r\n" +
					"      <xs:simpleType>\r\n" +
					"        <xs:restriction base=\"xs:token\">\r\n" +
					"          <xs:enumeration value=\"ltr\"/>\r\n" +
					"          <xs:enumeration value=\"rtl\"/>\r\n" +
					"        </xs:restriction>\r\n" +
					"      </xs:simpleType>\r\n" +
					"    </xs:attribute>\r\n" +
					"  </xs:attributeGroup>\r\n" +
					"</xs:schema>";
		}
	}
}
