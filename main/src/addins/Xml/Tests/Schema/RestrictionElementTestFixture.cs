using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Xml.Tests.Schema
{
	/// <summary>
	/// Tests complex content restriction elements.
	/// </summary>
	[TestFixture]
	public class RestrictionElementTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList childElements;
		CompletionDataList attributes;
		CompletionDataList annotationChildElements;
		CompletionDataList choiceChildElements;

		async Task Init ()
		{
			if (childElements != null)
				return;

			XmlElementPath path = new XmlElementPath ();
			path.Elements.Add (new QualifiedName ("group", "http://www.w3.org/2001/XMLSchema"));
			childElements = await SchemaCompletionData.GetChildElementCompletionData (path, CancellationToken.None);
			attributes = await SchemaCompletionData.GetAttributeCompletionData (path, CancellationToken.None);

			// Get annotation child elements.
			path.Elements.Add (new QualifiedName ("annotation", "http://www.w3.org/2001/XMLSchema"));
			annotationChildElements = await SchemaCompletionData.GetChildElementCompletionData (path, CancellationToken.None);

			// Get choice child elements.
			path.Elements.RemoveAt (path.Elements.Count - 1);
			path.Elements.Add (new QualifiedName ("choice", "http://www.w3.org/2001/XMLSchema"));
			choiceChildElements = await SchemaCompletionData.GetChildElementCompletionData (path, CancellationToken.None);
		}

		[Test]
		public async Task GroupChildElementIsAnnotation()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(childElements, "annotation"), 
			              "Should have a child element called annotation.");
		}
		
		[Test]
		public async Task GroupChildElementIsChoice()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(childElements, "choice"), 
			              "Should have a child element called choice.");
		}		
		
		[Test]
		public async Task GroupChildElementIsSequence()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(childElements, "sequence"), 
			              "Should have a child element called sequence.");
		}		
		
		[Test]
		public async Task GroupAttributeIsName()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributes, "name"),
			              "Should have an attribute called name.");			
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
			              "Should have a child element called appinfo.");
		}	
		
		[Test]
		public async Task ChoiceChildElementIsSequence()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(choiceChildElements, "element"), 
			              "Should have a child element called element.");
		}	
		
		protected override string GetSchema()
		{
			return "<xs:schema targetNamespace=\"http://www.w3.org/2001/XMLSchema\" blockDefault=\"#all\" elementFormDefault=\"qualified\" version=\"1.0\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xml:lang=\"EN\" xmlns:hfp=\"http://www.w3.org/2001/XMLSchema-hasFacetAndProperty\">\r\n" +
					"\r\n" +
					" <xs:element name=\"group\" type=\"xs:namedGroup\" id=\"group\">\r\n" +
					" </xs:element>\r\n" +
					"\r\n" +
					" <xs:element name=\"annotation\" id=\"annotation\">\r\n" +
					"   <xs:complexType>\r\n" +
					"      <xs:choice minOccurs=\"0\" maxOccurs=\"unbounded\">\r\n" +
					"       <xs:element name=\"appinfo\"/>\r\n" +
					"       <xs:element name=\"documentation\"/>\r\n" +
					"      </xs:choice>\r\n" +
					"      <xs:attribute name=\"id\" type=\"xs:ID\"/>\r\n" +
					"   </xs:complexType>\r\n" +
					" </xs:element>\r\n" +
					"\r\n" +
					"\r\n" +
					" <xs:complexType name=\"namedGroup\">\r\n" +
					"  <xs:complexContent>\r\n" +
					"   <xs:restriction base=\"xs:realGroup\">\r\n" +
					"    <xs:sequence>\r\n" +
					"     <xs:element ref=\"xs:annotation\" minOccurs=\"0\"/>\r\n" +
					"     <xs:choice minOccurs=\"1\" maxOccurs=\"1\">\r\n" +
					"      <xs:element ref=\"xs:choice\"/>\r\n" +
					"      <xs:element name=\"sequence\"/>\r\n" +
					"     </xs:choice>\r\n" +
					"    </xs:sequence>\r\n" +
					"    <xs:attribute name=\"name\" use=\"required\" type=\"xs:NCName\"/>\r\n" +
					"    <xs:attribute name=\"ref\" use=\"prohibited\"/>\r\n" +
					"    <xs:attribute name=\"minOccurs\" use=\"prohibited\"/>\r\n" +
					"    <xs:attribute name=\"maxOccurs\" use=\"prohibited\"/>\r\n" +
					"    <xs:anyAttribute namespace=\"##other\" processContents=\"lax\"/>\r\n" +
					"   </xs:restriction>\r\n" +
					"  </xs:complexContent>\r\n" +
					" </xs:complexType>\r\n" +
					"\r\n" +
					" <xs:complexType name=\"realGroup\">\r\n" +
					"    <xs:sequence>\r\n" +
					"     <xs:element ref=\"xs:annotation\" minOccurs=\"0\"/>\r\n" +
					"     <xs:choice minOccurs=\"0\" maxOccurs=\"1\">\r\n" +
					"      <xs:element name=\"all\"/>\r\n" +
					"      <xs:element ref=\"xs:choice\"/>\r\n" +
					"      <xs:element name=\"sequence\"/>\r\n" +
					"     </xs:choice>\r\n" +
					"    </xs:sequence>\r\n" +
					"    <xs:anyAttribute namespace=\"##other\" processContents=\"lax\"/>\r\n" +
					" </xs:complexType>\r\n" +
					"\r\n" +
					" <xs:element name=\"choice\" id=\"choice\">\r\n" +
					"   <xs:complexType>\r\n" +
					"     <xs:choice minOccurs=\"0\" maxOccurs=\"1\">\r\n" +
					"       <xs:element name=\"element\"/>\r\n" +
				    "       <xs:element name=\"sequence\"/>\r\n" +
					"     </xs:choice>\r\n" +					
					"   </xs:complexType>\r\n" +
					" </xs:element>\r\n" +
					"</xs:schema>";
		}
	}
}
