using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Xml.Tests.Schema
{
	[TestFixture]
	public class MissingSchemaElementTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList barElementAttributes;
		
		async Task Init ()
		{
			if (barElementAttributes != null)
				return;
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("root", "http://foo"));
			path.Elements.Add(new QualifiedName("bar", "http://foo"));
			barElementAttributes = await SchemaCompletionData.GetAttributeCompletionData(path, CancellationToken.None);
		}
		
		[Test]
		public async Task BarHasOneAttribute()
		{
			await Init ();
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
