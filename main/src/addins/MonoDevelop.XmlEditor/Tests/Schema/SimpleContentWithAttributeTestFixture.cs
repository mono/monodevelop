using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Element that is a simple content type.
	/// </summary>
	[TestFixture]
	public class SimpleContentWithAttributeSchemaTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList attributeCompletionData;
		
		public override void FixtureInit()
		{
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("foo", "http://foo.com"));
			
			attributeCompletionData = SchemaCompletionData.GetAttributeCompletionData(path);
		}
		
		[Test]
		public void BarAttributeExists()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeCompletionData, "bar"),
			              "Attribute bar does not exist.");
		}		
	
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\r\n" +
				"\ttargetNamespace=\"http://foo.com\"\r\n" +
				"\txmlns=\"http://foo.com\">\r\n" +
				"\t<xs:element name=\"foo\">\r\n" +
				"\t\t<xs:complexType>\r\n" +
				"\t\t\t<xs:simpleContent>\r\n" +
				"\t\t\t\t<xs:extension base=\"xs:string\">\r\n" +
				"\t\t\t\t\t<xs:attribute name=\"bar\">\r\n" +
				"\t\t\t\t\t\t<xs:simpleType>\r\n" +
				"\t\t\t\t\t\t\t<xs:restriction base=\"xs:NMTOKEN\">\r\n" +
				"\t\t\t\t\t\t\t\t<xs:enumeration value=\"default\"/>\r\n" +
				"\t\t\t\t\t\t\t\t<xs:enumeration value=\"enable\"/>\r\n" +
				"\t\t\t\t\t\t\t\t<xs:enumeration value=\"disable\"/>\r\n" +
				"\t\t\t\t\t\t\t\t<xs:enumeration value=\"hide\"/>\r\n" +
				"\t\t\t\t\t\t\t\t<xs:enumeration value=\"show\"/>\r\n" +
				"\t\t\t\t\t\t\t</xs:restriction>\r\n" +
				"\t\t\t\t\t\t</xs:simpleType>\r\n" +
				"\t\t\t\t\t</xs:attribute>\r\n" +
				"\t\t\t\t\t<xs:attribute name=\"id\" type=\"xs:string\"/>\r\n" +
				"\t\t\t\t\t<xs:attribute name=\"msg\" type=\"xs:string\"/>\r\n" +
				"\t\t\t\t</xs:extension>\r\n" +
				"\t\t\t</xs:simpleContent>\r\n" +
				"\t\t</xs:complexType>\r\n" +
				"\t</xs:element>\r\n" +
				"</xs:schema>";
		}
	}
}
