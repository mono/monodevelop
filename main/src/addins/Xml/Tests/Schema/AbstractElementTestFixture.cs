using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Xml.Tests.Schema
{
	/// <summary>
	/// Tests elements that are abstract and require substitution groups.
	/// </summary>
	[TestFixture]
	public class AbstractElementTestFixture : SchemaTestFixtureBase
	{		
		CompletionDataList itemsElementChildren;
		CompletionDataList fileElementAttributes;
		CompletionDataList fileElementChildren;
		
		async Task Init ()
		{
			if (fileElementAttributes != null)
				return;
			
			var path = new XmlElementPath();
			
			path.Elements.Add(new QualifiedName("project", "http://foo"));
			path.Elements.Add(new QualifiedName("items", "http://foo"));

			itemsElementChildren = await SchemaCompletionData.GetChildElementCompletionData(path, CancellationToken.None);
			
			path.Elements.Add(new QualifiedName("file", "http://foo"));
			
			fileElementAttributes = await SchemaCompletionData.GetAttributeCompletionData(path, CancellationToken.None);
			fileElementChildren = await SchemaCompletionData.GetChildElementCompletionData(path, CancellationToken.None);
		}
		
		[Test]
		public async Task ItemsElementHasTwoChildElements()
		{
			await Init ();
			Assert.AreEqual(2, itemsElementChildren.Count, 
			                "Should be 2 child elements.");
		}
		
		[Test]
		public async Task ReferenceElementIsChildOfItemsElement()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(itemsElementChildren, "reference"));
		}
		
		[Test]
		public async Task FileElementIsChildOfItemsElement()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(itemsElementChildren, "file"));
		}
		
		[Test]
		public async Task FileElementHasAttributeNamedType()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(fileElementAttributes, "type"));
		}
		
		[Test]
		public async Task FileElementHasTwoChildElements()
		{
			await Init ();
			Assert.AreEqual(2, fileElementChildren.Count, "Should be 2 child elements.");
		}
		
		protected override string GetSchema()
		{
			return "<xs:schema targetNamespace=\"http://foo\" xmlns:foo=\"http://foo\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" elementFormDefault=\"qualified\">\r\n" +
					"    <xs:element name=\"project\">\r\n" +
					"        <xs:complexType>\r\n" +
					"            <xs:sequence>\r\n" +
					"                <xs:group ref=\"foo:projectItems\" minOccurs=\"0\" maxOccurs=\"unbounded\"/>\r\n" +
					"            </xs:sequence>\r\n" +
					"        </xs:complexType>\r\n" +
					"    </xs:element>\r\n" +
					"\r\n" +
					"    <xs:group name=\"projectItems\">\r\n" +
					"        <xs:choice>\r\n" +
					"            <xs:element name=\"items\" type=\"foo:itemGroupType\"/>\r\n" +
					"            <xs:element name=\"message\" type=\"xs:string\"/>\r\n" +
					"        </xs:choice>\r\n" +
					"    </xs:group>\r\n" +
					"\r\n" +
					"    <xs:complexType name=\"itemGroupType\">\r\n" +
					"        <xs:sequence minOccurs=\"0\" maxOccurs=\"unbounded\">\r\n" +
					"            <xs:element ref=\"foo:item\"/>\r\n" +
					"        </xs:sequence>\r\n" +
					"        <xs:attribute name=\"name\" type=\"xs:string\" use=\"optional\"/>            \r\n" +
					"    </xs:complexType>\r\n" +
					"\r\n" +
					"    <xs:element name=\"item\" type=\"foo:itemType\" abstract=\"true\"/>\r\n" +
					"\r\n" +
					"<xs:complexType name=\"itemType\">\r\n" +
					"        <xs:attribute name=\"name\" type=\"xs:string\" use=\"optional\"/>                        \r\n" +
					"    </xs:complexType>\r\n" +
					"\r\n" +
					"    <xs:element name=\"reference\" substitutionGroup=\"foo:item\">\r\n" +
					"        <xs:complexType>\r\n" +
					"            <xs:complexContent>\r\n" +
					"                <xs:extension base=\"foo:itemType\">\r\n" +
					"                    <xs:sequence minOccurs=\"0\" maxOccurs=\"unbounded\">\r\n" +
					"                        <xs:choice>\r\n" +
					"                            <xs:element name=\"name\"/>\r\n" +
					"                            <xs:element name=\"location\"/>                             \r\n" +
					"                        </xs:choice>\r\n" +
					"                    </xs:sequence>\r\n" +
					"                    <xs:attribute name=\"description\" type=\"xs:string\"/>\r\n" +
					"                 </xs:extension>\r\n" +
					"            </xs:complexContent>\r\n" +
					"        </xs:complexType>\r\n" +
					"    </xs:element>\r\n" +
					"\r\n" +
					"    <xs:element name=\"file\" substitutionGroup=\"foo:item\">\r\n" +
					"        <xs:complexType>\r\n" +
					"            <xs:complexContent>\r\n" +
					"                <xs:extension base=\"foo:itemType\">\r\n" +
					"                    <xs:sequence minOccurs=\"0\" maxOccurs=\"unbounded\">\r\n" +
					"                        <xs:choice>\r\n" +
					"                            <xs:element name=\"name\"/>\r\n" +
					"                            <xs:element name=\"attributes\"/>\r\n" +
					"                         </xs:choice>\r\n" +
					"                    </xs:sequence>\r\n" +
					"                    <xs:attribute name=\"type\" type=\"xs:string\"/>\r\n" +
					"                </xs:extension>\r\n" +
					"            </xs:complexContent>\r\n" +
					"        </xs:complexType>\r\n" +
					"    </xs:element>\r\n" +
					"</xs:schema>";
		}
	}
}
