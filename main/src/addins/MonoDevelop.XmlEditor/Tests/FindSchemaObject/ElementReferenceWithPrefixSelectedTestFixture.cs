using MonoDevelop.XmlEditor.Completion;
using NUnit.Framework;
using System;
using System.Xml.Schema;
using MonoDevelop.XmlEditor.Tests.Schema;
using MonoDevelop.XmlEditor.Tests.Utils;

namespace MonoDevelop.XmlEditor.Tests.FindSchemaObject
{
/*	/// <summary>
	/// Tests that an xs:element/@ref='prefix:name' is located in the schema.
	/// </summary>
	[TestFixture]
	public class ElementReferenceWithPrefixSelectedTestFixture : SchemaTestFixtureBase
	{
		XmlSchemaElement referencedSchemaElement;
		
		public override void FixtureInit()
		{
			XmlSchemaCompletionDataCollection schemas = new XmlSchemaCompletionDataCollection();
			schemas.Add(SchemaCompletionData);
			XmlSchemaCompletionData xsdSchemaCompletionData = new XmlSchemaCompletionData(ResourceManager.GetXsdSchema());
			schemas.Add(xsdSchemaCompletionData);
			XmlCompletionDataProvider provider = new XmlCompletionDataProvider(schemas, xsdSchemaCompletionData, String.Empty, null);
			
			string xml = GetSchema();
			
			int index = xml.IndexOf("ref=\"xs:list");
			index = xml.IndexOf("xs", index);
			referencedSchemaElement = (XmlSchemaElement)XmlEditorView.GetSchemaObjectSelected(xml, index, provider, SchemaCompletionData);
		}
		
		[Test]
		public void ReferencedElementName()
		{
			Assert.AreEqual("list", referencedSchemaElement.QualifiedName.Name);
		}
		
		[Test]
		public void ReferencedElementNamespace()
		{
			Assert.AreEqual("http://www.w3.org/2001/XMLSchema", referencedSchemaElement.QualifiedName.Namespace);
		}

		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://www.w3schools.com\"  xmlns=\"http://www.w3schools.com\">\r\n" +
				"\r\n" +
				"<!-- definition of simple elements -->\r\n" +
				"<xs:element name=\"name\" type=\"xs:string\"/>\r\n" +
				"<xs:element name=\"address\" type=\"xs:string\"/>\r\n" +
				"\r\n" +
				"<!-- definition of complex elements -->\r\n" +
				"<xs:element name=\"shipto\">\r\n" +
				" <xs:complexType>\r\n" +
				"  <xs:sequence>\r\n" +
				"   <xs:element ref=\"name\"/>\r\n" +
				"   <xs:element ref=\"xs:list\"/>\r\n" +
				"  </xs:sequence>\r\n" +
				"  <xs:attribute name=\"address\"/>\r\n" +
				" </xs:complexType>\r\n" +
				"</xs:element>\r\n" +
				"\r\n" +
				"<xs:element name=\"shiporder\">\r\n" +
				" <xs:complexType>\r\n" +
				"  <xs:sequence>\r\n" +
				"   <xs:element ref=\"shipto\"/>\r\n" +
				"  </xs:sequence>\r\n" +
				"  <xs:attribute name=\"id\"/>\r\n" +
				" </xs:complexType>\r\n" +
				"</xs:element>\r\n" +
				"\r\n" +
				"</xs:schema>";
		}
	}*/
}
