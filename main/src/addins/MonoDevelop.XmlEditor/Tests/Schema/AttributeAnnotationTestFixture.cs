using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Tests that the completion data retrieves the annotation documentation
	/// that an attribute may have.
	/// </summary>
	[TestFixture]
	public class AttributeAnnotationTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList fooAttributeCompletionData;
		CompletionDataList barAttributeCompletionData;
		
		public override void FixtureInit()
		{			
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("foo", "http://foo.com"));
			
			fooAttributeCompletionData = SchemaCompletionData.GetAttributeCompletionData(path);

			path.Elements.Add(new QualifiedName("bar", "http://foo.com"));
			barAttributeCompletionData = SchemaCompletionData.GetAttributeCompletionData(path);
		}
				
		[Test]
		public void FooAttributeDocumentation()
		{
			Assert.AreEqual("Documentation for foo attribute.", fooAttributeCompletionData[0].Description);
		}
		
		[Test]
		public void BarAttributeDocumentation()
		{
			Assert.AreEqual("Documentation for bar attribute.", barAttributeCompletionData[0].Description);
		}
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://foo.com\" xmlns=\"http://foo.com\" elementFormDefault=\"qualified\">\r\n" +
				"\t<xs:element name=\"foo\">\r\n" +
				"\t\t<xs:complexType>\r\n" +
				"\t\t\t<xs:sequence>\t\r\n" +
				"\t\t\t\t<xs:element name=\"bar\" type=\"bar\">\r\n" +
				"\t\t\t</xs:element>\r\n" +
				"\t\t\t</xs:sequence>\r\n" +
				"\t\t\t<xs:attribute name=\"id\">\r\n" +
				"\t\t\t\t\t<xs:annotation>\r\n" +
				"\t\t\t\t\t\t<xs:documentation>Documentation for foo attribute.</xs:documentation>\r\n" +
				"\t\t\t\t</xs:annotation>\t\r\n" +
				"\t\t\t</xs:attribute>\t\r\n" +
				"\t\t</xs:complexType>\r\n" +
				"\t</xs:element>\r\n" +
				"\t<xs:complexType name=\"bar\">\r\n" +
				"\t\t<xs:attribute name=\"name\">\r\n" +
				"\t\t\t<xs:annotation>\r\n" +
				"\t\t\t\t<xs:documentation>Documentation for bar attribute.</xs:documentation>\r\n" +
				"\t\t\t</xs:annotation>\t\r\n" +
				"\t\t</xs:attribute>\t\r\n" +
				"\t</xs:complexType>\r\n" +
				"</xs:schema>";
		}		
	}
}
