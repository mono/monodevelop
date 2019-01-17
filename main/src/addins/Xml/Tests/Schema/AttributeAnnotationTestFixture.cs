using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Schema
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
		
		async Task Init ()
		{
			if (fooAttributeCompletionData != null)
				return;
			
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("foo", "http://foo.com"));
			
			fooAttributeCompletionData = await SchemaCompletionData.GetAttributeCompletionData(path, CancellationToken.None);

			path.Elements.Add(new QualifiedName("bar", "http://foo.com"));
			barAttributeCompletionData = await SchemaCompletionData.GetAttributeCompletionData(path, CancellationToken.None);
		}
				
		[Test]
		public async Task FooAttributeDocumentation()
		{
			await Init ();
			Assert.AreEqual("Documentation for foo attribute.", ((MonoDevelop.Ide.CodeCompletion.CompletionData)fooAttributeCompletionData[0]).Description);
		}
		
		[Test]
		public async Task BarAttributeDocumentation()
		{
			await Init ();
			Assert.AreEqual("Documentation for bar attribute.", ((MonoDevelop.Ide.CodeCompletion.CompletionData)barAttributeCompletionData[0]).Description);
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
