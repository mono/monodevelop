using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	[TestFixture]
	public class MissingSchemaElementTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList barElementAttributes;
		
		public override void FixtureInit()
		{
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("root", "http://foo"));
			path.Elements.Add(new QualifiedName("bar", "http://foo"));
			barElementAttributes = SchemaCompletionData.GetAttributeCompletionData(path);
		}
		
		[Test]
		public void BarHasOneAttribute()
		{
			Assert.AreEqual(1, barElementAttributes.Count, "Should have 1 attribute.");
		}
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\r\n" +
				"           targetNamespace=\"http://foo\"\r\n" +
				"           xmlns=\"http://foo\"\r\n" +
				"           elementFormDefault=\"qualified\">\r\n" +
				"\t<xs:complexType name=\"root\">\r\n" +
				"\t\t<xs:choice minOccurs=\"0\" maxOccurs=\"unbounded\">\r\n" +
				"\t\t\t<xs:element ref=\"foo\"/>\r\n" +
				"\t\t\t<xs:element ref=\"bar\"/>\r\n" +
				"\t\t</xs:choice>\r\n" +
				"\t\t<xs:attribute name=\"id\" type=\"xs:string\" use=\"required\"/>\r\n" +
				"\t</xs:complexType>\r\n" +
				"\t<xs:element name=\"root\" type=\"root\"/>\r\n" +
				"\t<xs:complexType name=\"bar\">\r\n" +
				"\t\t<xs:attribute name=\"id\" type=\"xs:string\" use=\"required\"/>\r\n" +
				"\t</xs:complexType>\r\n" +
				"\t<xs:element name=\"bar\" type=\"bar\"/>\r\n" +
				"</xs:schema>";
		}
	}
}
