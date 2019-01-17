using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Schema
{
	/// <summary>
	/// Tests that the completion data retrieves the annotation documentation
	/// that an element ref may have.
	/// </summary>
	[TestFixture]
	public class ElementRefAnnotationTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList fooChildElementCompletionData;
		
		async Task Init ()
		{
			if (fooChildElementCompletionData != null)
				return;
			
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("foo", "http://foo.com"));
			
			fooChildElementCompletionData = await SchemaCompletionData.GetChildElementCompletionData(path, CancellationToken.None);
		}
				
		[Test]
		public async Task BarElementDocumentation()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.ContainsDescription(fooChildElementCompletionData, "bar", "Documentation for bar element."),
			              "Missing documentation for bar element");
		}
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://foo.com\" xmlns=\"http://foo.com\">\r\n" +
				"\t<xs:element name=\"foo\">\r\n" +
				"\t\t<xs:complexType>\r\n" +
				"\t\t\t<xs:sequence>\r\n" +
				"\t\t\t\t<xs:element ref=\"bar\"/>\r\n" +
				"\t\t\t</xs:sequence>\r\n" +
				"\t\t</xs:complexType>\r\n" +
				"\t</xs:element>\r\n" +
				"\r\n" +
				"\t<xs:element name=\"bar\">\r\n" +
				"\t\t<xs:annotation>\r\n" +
				"\t\t\t<xs:documentation>Documentation for bar element.</xs:documentation>\r\n" +
				"\t\t</xs:annotation>\r\n" +
				"\t\t<xs:complexType>\r\n" +
				"\t\t\t<xs:attribute name=\"id\"/>\r\n" +
				"\t\t</xs:complexType>\r\n" +
				"\t</xs:element>\r\n" +
				"</xs:schema>";
		}		
	}
}
