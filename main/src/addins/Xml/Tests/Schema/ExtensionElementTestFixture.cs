using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Xml.Tests.Schema
{
	/// <summary>
	/// Tests complex content extension elements.
	/// </summary>
	[TestFixture]
	public class ExtensionElementTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList schemaChildElements;
		CompletionDataList annotationChildElements;
		CompletionDataList annotationAttributes;
		CompletionDataList includeAttributes;
		CompletionDataList appInfoAttributes;
		//CompletionDataList schemaAttributes;
		CompletionDataList fooAttributes;
		
		async Task Init ()
		{
			if (schemaChildElements != null)
				return;
			
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("schema", "http://www.w3.org/2001/XMLSchema"));
			
			schemaChildElements = await SchemaCompletionData.GetChildElementCompletionData(path, CancellationToken.None);
			//schemaAttributes = SchemaCompletionData.GetAttributeCompletionData(path);
			
			// Get include elements attributes.
			path.Elements.Add(new QualifiedName("include", "http://www.w3.org/2001/XMLSchema"));
			includeAttributes = await SchemaCompletionData.GetAttributeCompletionData(path, CancellationToken.None);
		
			// Get annotation element info.
			path.Elements.RemoveAt(path.Elements.Count - 1);
			path.Elements.Add(new QualifiedName("annotation", "http://www.w3.org/2001/XMLSchema"));
			
			annotationChildElements = await SchemaCompletionData.GetChildElementCompletionData(path, CancellationToken.None);
			annotationAttributes = await SchemaCompletionData.GetAttributeCompletionData(path, CancellationToken.None);
		
			// Get app info attributes.
			path.Elements.Add(new QualifiedName("appinfo", "http://www.w3.org/2001/XMLSchema"));
			appInfoAttributes = await SchemaCompletionData.GetAttributeCompletionData(path, CancellationToken.None);
			
			// Get foo attributes.
			path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("foo", "http://www.w3.org/2001/XMLSchema"));
			fooAttributes = await SchemaCompletionData.GetAttributeCompletionData(path, CancellationToken.None);
		}
		
		[Test]
		public async Task SchemaHasSevenChildElements()
		{
			await Init ();
			Assert.AreEqual(7, schemaChildElements.Count, 
			                "Should be 7 child elements.");
		}
		
		[Test]
		public async Task SchemaChildElementIsInclude()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(schemaChildElements, "include"), 
			              "Should have a child element called include.");
		}
		
		[Test]
		public async Task SchemaChildElementIsImport()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(schemaChildElements, "import"), 
			              "Should have a child element called import.");
		}		
		
		[Test]
		public async Task SchemaChildElementIsNotation()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(schemaChildElements, "notation"), 
			              "Should have a child element called notation.");
		}		
		
		/// <summary>
		/// Tests that the extended element has the base type's attributes. 
		/// </summary>
		[Test]
		public async Task FooHasClassAttribute()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(fooAttributes, "class"), 
			              "Should have an attribute called class.");						
		}
		
		[Test]
		public async Task AnnotationElementHasOneAttribute()
		{
			await Init ();
			Assert.AreEqual(1, annotationAttributes.Count, "Should be one attribute.");
		}
		
		[Test]
		public async Task AnnotationHasIdAttribute()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(annotationAttributes, "id"), 
			              "Should have an attribute called id.");			
		}
		
		[Test]
		public async Task AnnotationHasTwoChildElements()
		{
			await Init ();
			Assert.AreEqual(2, annotationChildElements.Count, 
			                "Should be 2 child elements.");
		}
		
		[Test]
		public async Task AnnotationChildElementIsAppInfo()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(annotationChildElements, "appinfo"), 
			              "Should have a child element called appinfo.");
		}
		
		[Test]
		public async Task AnnotationChildElementIsDocumentation()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(annotationChildElements, "documentation"), 
			              "Should have a child element called documentation.");
		}		
		
		[Test]
		public async Task IncludeElementHasOneAttribute()
		{
			await Init ();
			Assert.AreEqual(1, includeAttributes.Count, "Should be one attribute.");
		}
		
		[Test]
		public async Task IncludeHasSchemaLocationAttribute()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(includeAttributes, "schemaLocation"), 
			              "Should have an attribute called schemaLocation.");			
		}	
		
		[Test]
		public async Task AppInfoElementHasOneAttribute()
		{
			await Init ();
			Assert.AreEqual(1, appInfoAttributes.Count, "Should be one attribute.");
		}
		
		[Test]
		public async Task AppInfoHasIdAttribute()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(appInfoAttributes, "id"), 
			              "Should have an attribute called id.");			
		}		
		
		protected override string GetSchema()
		{
			return "<xs:schema targetNamespace=\"http://www.w3.org/2001/XMLSchema\" elementFormDefault=\"qualified\" version=\"1.0\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xml:lang=\"EN\">\r\n" +
				"\r\n" +
				" <xs:element name=\"schema\" id=\"schema\">\r\n" +
				"  <xs:complexType>\r\n" +
				"   <xs:complexContent>\r\n" +
				"    <xs:extension base=\"xs:openAttrs\">\r\n" +
				"     <xs:sequence>\r\n" +
				"      <xs:choice minOccurs=\"0\" maxOccurs=\"unbounded\">\r\n" +
				"       <xs:element ref=\"xs:include\"/>\r\n" +
				"       <xs:element name=\"import\"/>\r\n" +
				"       <xs:element name=\"redefine\"/>\r\n" +
				"       <xs:element ref=\"xs:annotation\"/>\r\n" +
				"      </xs:choice>\r\n" +
				"      <xs:sequence minOccurs=\"0\" maxOccurs=\"unbounded\">\r\n" +
				"       <xs:group ref=\"xs:schemaTop\"/>\r\n" +
				"       <xs:element ref=\"xs:annotation\" minOccurs=\"0\" maxOccurs=\"unbounded\"/>\r\n" +
				"      </xs:sequence>\r\n" +
				"     </xs:sequence>\r\n" +
				"     <xs:attribute name=\"targetNamespace\" type=\"xs:anyURI\"/>\r\n" +
				"     <xs:attribute name=\"version\" type=\"xs:token\"/>\r\n" +
				"     <xs:attribute name=\"finalDefault\" type=\"xs:fullDerivationSet\" use=\"optional\" default=\"\"/>\r\n" +
				"     <xs:attribute name=\"blockDefault\" type=\"xs:blockSet\" use=\"optional\" default=\"\"/>\r\n" +
				"     <xs:attribute name=\"attributeFormDefault\" type=\"xs:formChoice\" use=\"optional\" default=\"unqualified\"/>\r\n" +
				"     <xs:attribute name=\"elementFormDefault\" type=\"xs:formChoice\" use=\"optional\" default=\"unqualified\"/>\r\n" +
				"     <xs:attribute name=\"id\" type=\"xs:ID\"/>\r\n" +
				"     <xs:attribute ref=\"xml:lang\"/>\r\n" +
				"    </xs:extension>\r\n" +
				"   </xs:complexContent>\r\n" +
				"  </xs:complexType>\r\n" +
				"\r\n" +
				"\r\n" +
				" </xs:element>\r\n" +
				"\r\n" +
				"<xs:complexType name=\"openAttrs\">\r\n" +
				"   <xs:complexContent>\r\n" +
				"     <xs:restriction base=\"xs:anyType\">\r\n" +
				"       <xs:anyAttribute namespace=\"##other\" processContents=\"lax\"/>\r\n" +
				"     </xs:restriction>\r\n" +
				"   </xs:complexContent>\r\n" +
				" </xs:complexType>\r\n" +
				"\r\n" +
				"<xs:complexType name=\"anyType\" mixed=\"true\">\r\n" +
				"  <xs:annotation>\r\n" +
				"   <xs:documentation>\r\n" +
				"   Not the real urType, but as close an approximation as we can\r\n" +
				"   get in the XML representation</xs:documentation>\r\n" +
				"  </xs:annotation>\r\n" +
				"  <xs:sequence>\r\n" +
				"   <xs:any minOccurs=\"0\" maxOccurs=\"unbounded\" processContents=\"lax\"/>\r\n" +
				"  </xs:sequence>\r\n" +
				"  <xs:anyAttribute processContents=\"lax\"/>\r\n" +
				" </xs:complexType>\r\n" +
				"\r\n" +
				"<xs:element name=\"include\" id=\"include\">\r\n" +
				"  <xs:complexType>\r\n" +
				"     <xs:attribute name=\"schemaLocation\" type=\"xs:anyURI\" use=\"required\"/>\r\n" +
				"  </xs:complexType>\r\n" +
				" </xs:element>\r\n" +
				"\r\n" +
				"<xs:element name=\"annotation\" id=\"annotation\">\r\n" +
				"   <xs:complexType>\r\n" +
				"    <xs:complexContent>\r\n" +
				"     <xs:extension base=\"xs:openAttrs\">\r\n" +
				"      <xs:choice minOccurs=\"0\" maxOccurs=\"unbounded\">\r\n" +
				"       <xs:element ref=\"xs:appinfo\"/>\r\n" +
				"       <xs:element name=\"documentation\"/>\r\n" +
				"      </xs:choice>\r\n" +
				"      <xs:attribute name=\"id\" type=\"xs:ID\"/>\r\n" +
				"     </xs:extension>\r\n" +
				"    </xs:complexContent>\r\n" +
				"   </xs:complexType>\r\n" +
				" </xs:element>\r\n" +
				"\r\n" +
				" <xs:group name=\"schemaTop\">\r\n" +
				"  <xs:choice>\r\n" +
				"   <xs:element name=\"element\"/>\r\n" +
				"   <xs:element name=\"attribute\"/>\r\n" +
				"   <xs:element name=\"notation\"/>\r\n" +
				"  </xs:choice>\r\n" +
				" </xs:group>\r\n" +
				"\r\n" +
				"\r\n" +
				"<xs:element name=\"appinfo\" id=\"appinfo\">\r\n" +
				"  <xs:complexType>\r\n" +
				"     <xs:attribute name=\"id\" type=\"xs:anyURI\" use=\"required\"/>\r\n" +
				"  </xs:complexType>\r\n" +
				" </xs:element>\r\n" +
				"\r\n" +
				"\r\n" +
				" <xs:element name=\"foo\">\r\n" +
				"  <xs:complexType>\r\n" +
				"   <xs:complexContent>\r\n" +
				"    <xs:extension base=\"xs:fooBase\">\r\n" +
				"      <xs:attribute name=\"id\" type=\"xs:ID\"/>\r\n" +
				"     </xs:extension>\r\n" +
				"    </xs:complexContent>\r\n" +
				"   </xs:complexType>\r\n" +
				" </xs:element>\r\n" +
				"\r\n" +
				"<xs:complexType name=\"fooBase\">\r\n" +
				"      <xs:attribute name=\"class\" type=\"xs:string\"/>\r\n" +
				" </xs:complexType>\r\n" +
				"</xs:schema>";
		}
	}
}
