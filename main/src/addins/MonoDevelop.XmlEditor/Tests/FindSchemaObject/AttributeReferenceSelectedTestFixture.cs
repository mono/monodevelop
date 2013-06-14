using NUnit.Framework;
using System;
using System.Xml.Schema;
using MonoDevelop.XmlEditor.Completion;
using MonoDevelop.XmlEditor.Tests.Schema;
using MonoDevelop.XmlEditor.Tests.Utils;

namespace MonoDevelop.XmlEditor.Tests.FindSchemaObject
{
/*	/// <summary>
	/// Tests that an xs:attribute/@ref is located in the schema.
	/// </summary>
	[TestFixture]
	public class AttributeReferenceSelectedTestFixture : SchemaTestFixtureBase
	{
		XmlSchemaAttribute schemaAttribute;
		
		public override void FixtureInit()
		{
			XmlSchemaCompletionDataCollection schemas = new XmlSchemaCompletionDataCollection();
			schemas.Add(SchemaCompletionData);
			XmlSchemaCompletionData xsdSchemaCompletionData = new XmlSchemaCompletionData(ResourceManager.GetXsdSchema());
			schemas.Add(xsdSchemaCompletionData);
			XmlCompletionDataProvider provider = new XmlCompletionDataProvider(schemas, xsdSchemaCompletionData, String.Empty, null);
			
			string xml = GetSchema();			
			int index = xml.IndexOf("ref=\"dir\"");
			index = xml.IndexOf("dir", index);
			schemaAttribute = (XmlSchemaAttribute)XmlEditorView.GetSchemaObjectSelected(xml, index, provider, SchemaCompletionData);
		}
		
		[Test]
		public void AttributeName()
		{
			Assert.AreEqual("dir", schemaAttribute.QualifiedName.Name);
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
					"      <xs:attribute name=\"id\" ref=\"dir\"/>\r\n" +
					"    </xs:complexType>\r\n" +
					"  </xs:element>\r\n" +
					"\r\n" +
					"  <xs:element name=\"head\" type=\"xs:string\"/>\r\n" +
					"  <xs:element name=\"body\" type=\"xs:string\"/>\r\n" +
					"\r\n" +
					"    <xs:attribute name=\"dir\">\r\n" +
					"      <xs:simpleType>\r\n" +
					"        <xs:restriction base=\"xs:token\">\r\n" +
					"          <xs:enumeration value=\"ltr\"/>\r\n" +
					"          <xs:enumeration value=\"rtl\"/>\r\n" +
					"        </xs:restriction>\r\n" +
					"      </xs:simpleType>\r\n" +
					"    </xs:attribute>\r\n" +
					"</xs:schema>";
		}
	}*/
}
