using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Tests attribute refs
	/// </summary>
	[TestFixture]
	public class EnumAttributeValueTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList attributeValues;
		
		public override void FixtureInit()
		{
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("foo", "http://foo.com"));
			attributeValues = SchemaCompletionData.GetAttributeValueCompletionData(path, "id");
		}
		
		[Test]
		public void IdAttributeHasValueOne()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeValues, "one"),
			              "Missing attribute value 'one'");
		}
		
		[Test]
		public void IdAttributeHasValueTwo()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeValues, "two"),
			              "Missing attribute value 'two'");
		}		
		
		[Test]
		public void IdAttributeValueCount()
		{
			Assert.AreEqual(2, attributeValues.Count, "Expecting 2 attribute values.");
		}
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://foo.com\" targetNamespace=\"http://foo.com\" elementFormDefault=\"qualified\">\r\n" +
				"\t<xs:element name=\"foo\">\r\n" +
				"\t\t<xs:complexType>\r\n" +
				"\t\t\t<xs:attribute name=\"id\">\r\n" +
				"\t\t\t\t<xs:simpleType>\r\n" +
				"\t\t\t\t\t<xs:restriction base=\"xs:string\">\r\n" +
				"\t\t\t\t\t\t<xs:enumeration value=\"one\"/>\r\n" +
				"\t\t\t\t\t\t<xs:enumeration value=\"two\"/>\r\n" +
				"\t\t\t\t\t</xs:restriction>\r\n" +
				"\t\t\t\t</xs:simpleType>\r\n" +
				"\t\t\t</xs:attribute>\r\n" +
				"\t\t</xs:complexType>\r\n" +
				"\t</xs:element>\r\n" +
				"</xs:schema>";
		}
	}
}
