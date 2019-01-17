using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Schema
{
	/// <summary>
	/// Element that uses an attribute group ref.
	/// </summary>
	[TestFixture]
	public class AttributeGroupRefTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList attributeCompletionData;
		
		async Task Init ()
		{
			if (attributeCompletionData != null)
				return;
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("note", "http://www.w3schools.com"));
			attributeCompletionData = await SchemaCompletionData.GetAttributeCompletionData(path, CancellationToken.None);
		}
		
		[Test]
		public async Task AttributeCount()
		{
			await Init ();
			Assert.AreEqual(4, attributeCompletionData.Count, "Should be 4 attributes.");
		}
		
		[Test]
		public async Task NameAttribute()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeCompletionData, "name"), 
			              "Attribute name does not exist.");
		}		
		
		[Test]
		public async Task IdAttribute()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeCompletionData, "id"), 
			              "Attribute id does not exist.");
		}		
		
		[Test]
		public async Task StyleAttribute()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeCompletionData, "style"), 
			              "Attribute style does not exist.");
		}	
		
		[Test]
		public async Task TitleAttribute()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeCompletionData, "title"), 
			              "Attribute title does not exist.");
		}		
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://www.w3schools.com\" xmlns=\"http://www.w3schools.com\" elementFormDefault=\"qualified\">\r\n" +
				"<xs:attributeGroup name=\"coreattrs\">" +
				"\t<xs:attribute name=\"id\" type=\"xs:string\"/>" +
				"\t<xs:attribute name=\"style\" type=\"xs:string\"/>" +
				"\t<xs:attribute name=\"title\" type=\"xs:string\"/>" +
				"</xs:attributeGroup>" +
				"\t<xs:element name=\"note\">\r\n" +
				"\t\t<xs:complexType>\r\n" +
				"\t\t\t<xs:attributeGroup ref=\"coreattrs\"/>" +
				"\t\t\t<xs:attribute name=\"name\" type=\"xs:string\"/>\r\n" +
				"\t\t</xs:complexType>\r\n" +
				"\t</xs:element>\r\n" +
				"</xs:schema>";
		}
	}
}
