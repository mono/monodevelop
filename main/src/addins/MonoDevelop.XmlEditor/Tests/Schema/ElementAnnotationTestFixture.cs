using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Tests that the completion data retrieves the annotation documentation
	/// that an element may have.
	/// </summary>
	[TestFixture]
	public class ElementAnnotationTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList fooChildElementCompletionData;
		CompletionDataList rootElementCompletionData;
		
		public override void FixtureInit()
		{
			rootElementCompletionData = SchemaCompletionData.GetElementCompletionData();
			
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("foo", "http://foo.com"));
			
			fooChildElementCompletionData = SchemaCompletionData.GetChildElementCompletionData(path);
		}
				
		[Test]
		public void RootElementDocumentation()
		{
			Assert.AreEqual("Documentation for foo element.", rootElementCompletionData[0].Description);
		}
		
		[Test]
		public void FooChildElementDocumentation()
		{
			Assert.AreEqual("Documentation for bar element.", fooChildElementCompletionData[0].Description);
		}
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://foo.com\" xmlns=\"http://foo.com\" elementFormDefault=\"qualified\">\r\n" +
				"\t<xs:element name=\"foo\">\r\n" +
				"\t\t<xs:annotation>\r\n" +
				"\t\t\t<xs:documentation>Documentation for foo element.</xs:documentation>\r\n" +
				"\t\t</xs:annotation>\r\n" +
				"\t\t<xs:complexType>\r\n" +
				"\t\t\t<xs:sequence>\t\r\n" +
				"\t\t\t\t<xs:element name=\"bar\" type=\"bar\">\r\n" +
				"\t\t\t\t\t<xs:annotation>\r\n" +
				"\t\t\t\t\t\t<xs:documentation>Documentation for bar element.</xs:documentation>\r\n" +
				"\t\t\t\t</xs:annotation>\t\r\n" +
				"\t\t\t</xs:element>\r\n" +
				"\t\t\t</xs:sequence>\r\n" +
				"\t\t</xs:complexType>\r\n" +
				"\t</xs:element>\r\n" +
				"\t<xs:complexType name=\"bar\">\r\n" +
				"\t\t<xs:annotation>\r\n" +
				"\t\t\t<xs:documentation>Documentation for bar element.</xs:documentation>\r\n" +
				"\t\t</xs:annotation>\t\r\n" +
				"\t\t<xs:attribute name=\"id\"/>\r\n" +
				"\t</xs:complexType>\r\n" +
				"</xs:schema>";
		}		
	}
}
