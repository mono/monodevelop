using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Schema
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
		
		async Task Init ()
		{
			if (rootElementCompletionData != null)
				return;
			
			rootElementCompletionData = await SchemaCompletionData.GetElementCompletionData(CancellationToken.None);
			
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("foo", "http://foo.com"));
			
			fooChildElementCompletionData = await SchemaCompletionData.GetChildElementCompletionData(path, CancellationToken.None);
		}
				
		[Test]
		public async Task RootElementDocumentation()
		{
			await Init ();
			Assert.AreEqual("Documentation for foo element.", ((MonoDevelop.Ide.CodeCompletion.CompletionData)rootElementCompletionData[0]).Description);
		}
		
		[Test]
		public async Task FooChildElementDocumentation()
		{
			await Init ();
			Assert.AreEqual("Documentation for bar element.", ((MonoDevelop.Ide.CodeCompletion.CompletionData)fooChildElementCompletionData[0]).Description);
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
