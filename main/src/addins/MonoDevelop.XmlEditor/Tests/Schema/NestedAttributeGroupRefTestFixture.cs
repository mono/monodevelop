using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Element that uses an attribute group ref.
	/// </summary>
	[TestFixture]
	public class NestedAttributeGroupRefTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList attributeCompletionData;
		
		public override void FixtureInit()
		{
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("note", "http://www.w3schools.com"));
			attributeCompletionData = SchemaCompletionData.GetAttributeCompletionData(path);
		}
		
		[Test]
		public void AttributeCount()
		{
			Assert.AreEqual(7, attributeCompletionData.Count, "Should be 7 attributes.");
		}
		
		[Test]
		public void NameAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeCompletionData, "name"), 
			              "Attribute name does not exist.");
		}		
		
		[Test]
		public void IdAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeCompletionData, "id"), 
			              "Attribute id does not exist.");
		}		
		
		[Test]
		public void StyleAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeCompletionData, "style"), 
			              "Attribute style does not exist.");
		}	
		
		[Test]
		public void TitleAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeCompletionData, "title"), 
			              "Attribute title does not exist.");
		}		
		
		[Test]
		public void BaseIdAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeCompletionData, "baseid"), 
			              "Attribute baseid does not exist.");
		}		
		
		[Test]
		public void BaseStyleAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeCompletionData, "basestyle"), 
			              "Attribute basestyle does not exist.");
		}	
		
		[Test]
		public void BaseTitleAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributeCompletionData, "basetitle"), 
			              "Attribute basetitle does not exist.");
		}			
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://www.w3schools.com\" xmlns=\"http://www.w3schools.com\" elementFormDefault=\"qualified\">\r\n" +
				"<xs:attributeGroup name=\"coreattrs\">" +
				"\t<xs:attribute name=\"id\" type=\"xs:string\"/>" +
				"\t<xs:attribute name=\"style\" type=\"xs:string\"/>" +
				"\t<xs:attribute name=\"title\" type=\"xs:string\"/>" +
				"\t<xs:attributeGroup ref=\"baseattrs\"/>" +
				"</xs:attributeGroup>" +
				"<xs:attributeGroup name=\"baseattrs\">" +
				"\t<xs:attribute name=\"baseid\" type=\"xs:string\"/>" +
				"\t<xs:attribute name=\"basestyle\" type=\"xs:string\"/>" +
				"\t<xs:attribute name=\"basetitle\" type=\"xs:string\"/>" +
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
