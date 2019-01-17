using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Xml.Tests.Schema
{
	/// <summary>
	/// Tests that element completion works for any child elements
	/// inside an xs:all schema element.
	/// </summary>
	[TestFixture]
	public class AllElementTestFixture : SchemaTestFixtureBase
	{		
		CompletionDataList personElementChildren;
		CompletionDataList firstNameAttributes;
		CompletionDataList firstNameElementChildren;
		
		async Task Init ()
		{
			if (personElementChildren != null)
				return;
			
			var path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("person", "http://foo"));
			personElementChildren = await SchemaCompletionData.GetChildElementCompletionData(path, CancellationToken.None);
			
			path.Elements.Add(new QualifiedName("firstname", "http://foo"));
			firstNameAttributes = await SchemaCompletionData.GetAttributeCompletionData(path, CancellationToken.None);
			firstNameElementChildren = await SchemaCompletionData.GetChildElementCompletionData(path, CancellationToken.None);
		}
		
		[Test]
		public async Task PersonElementHasTwoChildElements()
		{
			await Init ();
			Assert.AreEqual(2, personElementChildren.Count, 
			                "Should be 2 child elements.");
		}
		
		[Test]
		public async Task FirstNameElementHasAttribute()
		{
			await Init ();
			Assert.AreEqual(1, firstNameAttributes.Count, "Should have one attribute.");
		}
		
		[Test]
		public async Task FirstNameElementHasChildren()
		{
			await Init ();
			Assert.AreEqual(2, firstNameElementChildren.Count, 
			                "Should be 2 child elements.");
		}
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" elementFormDefault=\"qualified\" targetNamespace=\"http://foo\">\r\n" +
				"    <xs:element name=\"person\">\r\n" +
				"      <xs:complexType>\r\n" +
				"        <xs:all>\r\n" +
				"          <xs:element name=\"firstname\">\r\n" +
				"            <xs:complexType>\r\n" +
                "              <xs:sequence>\r\n" +
                "                <xs:element name=\"short\" type=\"xs:string\"/>\r\n" +
                "                <xs:element name=\"title\" type=\"xs:string\"/>\r\n" +
                "              </xs:sequence>\r\n" +
                "              <xs:attribute name=\"id\"/>\r\n" +
                "            </xs:complexType>\r\n" +
				"          </xs:element>\r\n" +
				"          <xs:element name=\"lastname\" type=\"xs:string\"/>\r\n" +
				"        </xs:all>\r\n" +
				"      </xs:complexType>\r\n" +
				"    </xs:element>\r\n" +
				"</xs:schema>";
		}
	}
}
