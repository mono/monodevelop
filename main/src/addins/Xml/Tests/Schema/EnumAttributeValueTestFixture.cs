using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Schema
{
	/// <summary>
	/// Tests attribute refs
	/// </summary>
	[TestFixture]
	public class EnumAttributeValueTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList attributeValues;
		
		async Task Init ()
		{
			if (attributeValues != null)
				return;
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("foo", "http://foo.com"));
			attributeValues = await SchemaCompletionData.GetAttributeValueCompletionData(path, "id", CancellationToken.None);
		}
		
		[Test]
		public async Task IdAttributeHasValueOne()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeValues, "one"),
			              "Missing attribute value 'one'");
		}
		
		[Test]
		public async Task IdAttributeHasValueTwo()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeValues, "two"),
			              "Missing attribute value 'two'");
		}		
		
		[Test]
		public async Task IdAttributeValueCount()
		{
			await Init ();
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
