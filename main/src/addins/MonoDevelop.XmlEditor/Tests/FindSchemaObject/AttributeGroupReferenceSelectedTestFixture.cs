using NUnit.Framework;
using System;
using System.Xml.Schema;
using MonoDevelop.XmlEditor.Tests.Schema;
using MonoDevelop.XmlEditor.Tests.Utils;
using MonoDevelop.XmlEditor.Completion;

namespace MonoDevelop.XmlEditor.Tests.FindSchemaObject
{
/*	/// <summary>
	/// Tests that an xs:attributeGroup/@ref is located in the schema.
	/// </summary>
	[TestFixture]
	public class AttributeGroupReferenceSelectedTestFixture : SchemaTestFixtureBase
	{
		XmlSchemaAttributeGroup schemaAttributeGroup;
		
		public override void FixtureInit()
		{
			XmlSchemaCompletionDataCollection schemas = new XmlSchemaCompletionDataCollection();
			schemas.Add(SchemaCompletionData);
			XmlSchemaCompletionData xsdSchemaCompletionData = new XmlSchemaCompletionData(ResourceManager.GetXsdSchema());
			schemas.Add(xsdSchemaCompletionData);
			XmlCompletionDataProvider provider = new XmlCompletionDataProvider(schemas, xsdSchemaCompletionData, String.Empty, null);
			
			string xml = GetSchema();			
			int index = xml.IndexOf("ref=\"coreattrs\"");
			index = xml.IndexOf("coreattrs", index);
			schemaAttributeGroup = (XmlSchemaAttributeGroup)XmlEditorView.GetSchemaObjectSelected(xml, index, provider, SchemaCompletionData);
		}
		
		[Test]
		public void AttributeGroupName()
		{
			Assert.AreEqual("coreattrs", schemaAttributeGroup.QualifiedName.Name);
		}		
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://www.w3schools.com\" xmlns=\"http://www.w3schools.com\" elementFormDefault=\"qualified\">\r\n" +
				"<xs:attributeGroup name=\"coreattrs\">" +
				"\t<xs:attribute name=\"id\" type=\"xs:string\"/>" +
				"\t<xs:attribute name=\"style\" type=\"xs:string\"/>" +
				"\t<xs:attribute name=\"title\" type=\"xs:string\"/>" +
				"</xs:attributeGroup>" +
				"\t<xs:element name=\"note\">\r\n" +
				"\t\t<xs:complexType>\r\n" +
				"\t\t\t<xs:attributeGroup ref=\"coreattrs\"/>" +
				"\t\t\t<xs:attribute name=\"name\" type=\"xs:string\"/>\r\n" +
				"\t\t</xs:complexType>\r\n" +
				"\t</xs:element>\r\n" +
				"</xs:schema>";
		}
	}*/
}
