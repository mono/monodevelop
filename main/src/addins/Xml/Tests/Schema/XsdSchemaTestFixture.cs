using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;
using System.Xml;
using MonoDevelop.Xml.Tests.Utils;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace MonoDevelop.Xml.Tests.Schema
{
	/// <summary>
	/// Tests the xsd schema.
	/// </summary>
	[TestFixture]
	public class XsdSchemaTestFixture
	{
		XmlSchemaCompletionData schemaCompletionData;
		XmlElementPath choicePath;
		XmlElementPath elementPath;
		XmlElementPath simpleEnumPath;
		XmlElementPath enumPath;
		XmlElementPath allElementPath;
		XmlElementPath allElementAnnotationPath;
		CompletionDataList choiceAttributes;
		CompletionDataList elementAttributes;
		CompletionDataList simpleEnumElements;
		CompletionDataList enumAttributes;
		CompletionDataList elementFormDefaultAttributeValues;
		CompletionDataList blockDefaultAttributeValues;
		CompletionDataList finalDefaultAttributeValues;
		CompletionDataList mixedAttributeValues;
		CompletionDataList maxOccursAttributeValues;
		CompletionDataList allElementChildElements;
		CompletionDataList allElementAnnotationChildElements;
		
		string namespaceURI = "http://www.w3.org/2001/XMLSchema";
		string prefix = "xs";
		
		async Task Init ()
		{
			if (schemaCompletionData != null)
				return;
			
			using (var reader = new StreamReader (ResourceManager.GetXsdSchema(), true)) {
				schemaCompletionData = new XmlSchemaCompletionData(reader);
			}
			
			// Set up choice element's path.
			choicePath = new XmlElementPath();
			choicePath.Elements.Add(new QualifiedName("schema", namespaceURI, prefix));
			choicePath.Elements.Add(new QualifiedName("element", namespaceURI, prefix));
			choicePath.Elements.Add(new QualifiedName("complexType", namespaceURI, prefix));
			
			mixedAttributeValues = await schemaCompletionData.GetAttributeValueCompletionData(choicePath, "mixed", CancellationToken.None);

			choicePath.Elements.Add(new QualifiedName("choice", namespaceURI, prefix));
			
			// Get choice element info.
			choiceAttributes = await schemaCompletionData.GetAttributeCompletionData(choicePath, CancellationToken.None);
			maxOccursAttributeValues = await schemaCompletionData.GetAttributeValueCompletionData(choicePath, "maxOccurs", CancellationToken.None);
			
			// Set up element path.
			elementPath = new XmlElementPath();
			elementPath.Elements.Add(new QualifiedName("schema", namespaceURI, prefix));

			elementFormDefaultAttributeValues = await schemaCompletionData.GetAttributeValueCompletionData (elementPath, "elementFormDefault", CancellationToken.None);
			blockDefaultAttributeValues = await schemaCompletionData.GetAttributeValueCompletionData(elementPath, "blockDefault", CancellationToken.None);
			finalDefaultAttributeValues = await schemaCompletionData.GetAttributeValueCompletionData(elementPath, "finalDefault", CancellationToken.None);
			
			elementPath.Elements.Add(new QualifiedName("element", namespaceURI, prefix));
				
			// Get element attribute info.
			elementAttributes = await schemaCompletionData.GetAttributeCompletionData(elementPath, CancellationToken.None);

			// Set up simple enum type path.
			simpleEnumPath = new XmlElementPath();
			simpleEnumPath.Elements.Add(new QualifiedName("schema", namespaceURI, prefix));
			simpleEnumPath.Elements.Add(new QualifiedName("simpleType", namespaceURI, prefix));
			simpleEnumPath.Elements.Add(new QualifiedName("restriction", namespaceURI, prefix));
			
			// Get child elements.
			simpleEnumElements = await schemaCompletionData.GetChildElementCompletionData(simpleEnumPath, CancellationToken.None);

			// Set up enum path.
			enumPath = new XmlElementPath();
			enumPath.Elements.Add(new QualifiedName("schema", namespaceURI, prefix));
			enumPath.Elements.Add(new QualifiedName("simpleType", namespaceURI, prefix));
			enumPath.Elements.Add(new QualifiedName("restriction", namespaceURI, prefix));
			enumPath.Elements.Add(new QualifiedName("enumeration", namespaceURI, prefix));
			
			// Get attributes.
			enumAttributes = await schemaCompletionData.GetAttributeCompletionData(enumPath, CancellationToken.None);
			
			// Set up xs:all path.
			allElementPath = new XmlElementPath();
			allElementPath.Elements.Add(new QualifiedName("schema", namespaceURI, prefix));
			allElementPath.Elements.Add(new QualifiedName("element", namespaceURI, prefix));
			allElementPath.Elements.Add(new QualifiedName("complexType", namespaceURI, prefix));
			allElementPath.Elements.Add(new QualifiedName("all", namespaceURI, prefix));
		
			// Get child elements of the xs:all element.
			allElementChildElements = await schemaCompletionData.GetChildElementCompletionData(allElementPath, CancellationToken.None);
			
			// Set up the path to the annotation element that is a child of xs:all.
			allElementAnnotationPath = new XmlElementPath();
			allElementAnnotationPath.Elements.Add(new QualifiedName("schema", namespaceURI, prefix));
			allElementAnnotationPath.Elements.Add(new QualifiedName("element", namespaceURI, prefix));
			allElementAnnotationPath.Elements.Add(new QualifiedName("complexType", namespaceURI, prefix));
			allElementAnnotationPath.Elements.Add(new QualifiedName("all", namespaceURI, prefix));
			allElementAnnotationPath.Elements.Add(new QualifiedName("annotation", namespaceURI, prefix));
			
			// Get the xs:all annotation child element.
			allElementAnnotationChildElements = await schemaCompletionData.GetChildElementCompletionData(allElementAnnotationPath, CancellationToken.None);
		}
		
		[Test]
		public async Task ChoiceHasAttributes()
		{
			await Init ();
			Assert.IsTrue(choiceAttributes.Count > 0, "Should have at least one attribute.");
		}
		
		[Test]
		public async Task ChoiceHasMinOccursAttribute()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(choiceAttributes, "minOccurs"),
			              "Attribute minOccurs missing.");
		}
		
		[Test]
		public async Task ChoiceHasMaxOccursAttribute()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(choiceAttributes, "maxOccurs"),
			              "Attribute maxOccurs missing.");
		}
		
		/// <summary>
		/// Tests that prohibited attributes are not added to the completion data.
		/// </summary>
		[Test]
		public async Task ChoiceDoesNotHaveNameAttribute()
		{
			await Init ();
			Assert.IsFalse(SchemaTestFixtureBase.Contains(choiceAttributes, "name"),
			               "Attribute name should not exist.");
		}
		
		/// <summary>
		/// Tests that prohibited attributes are not added to the completion data.
		/// </summary>
		[Test]
		public async Task ChoiceDoesNotHaveRefAttribute()
		{
			await Init ();
			Assert.IsFalse(SchemaTestFixtureBase.Contains(choiceAttributes, "ref"),
			               "Attribute ref should not exist.");
		}	
		
		/// <summary>
		/// Duplicate attribute test.
		/// </summary>
		[Test]
		public async Task ElementNameAttributeAppearsOnce()
		{
			await Init ();
			int nameAttributeCount = SchemaTestFixtureBase.GetItemCount(elementAttributes, "name");
			Assert.AreEqual(1, nameAttributeCount, "Should be only one name attribute.");
		}
		
		[Test]
		public async Task ElementHasIdAttribute()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(elementAttributes, "id"), 
			              "id attribute missing.");
		}		
		
		[Test]
		public async Task SimpleRestrictionTypeHasEnumChildElement()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(simpleEnumElements, "xs:enumeration"),
			              "enumeration element missing.");			
		}
		
		[Test]
		public async Task EnumHasValueAttribute()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(enumAttributes, "value"),
			              "Attribute value missing.");			
		}
		
		[Test]
		public async Task ElementFormDefaultAttributeHasValueQualified()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(elementFormDefaultAttributeValues, "qualified"),
			              "Attribute value 'qualified' missing.");
		}
		
		[Test]
		public async Task BlockDefaultAttributeHasValueAll()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(blockDefaultAttributeValues, "#all"),
			              "Attribute value '#all' missing.");
		}		
		
		[Test]
		public async Task BlockDefaultAttributeHasValueExtension()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(blockDefaultAttributeValues, "extension"),
			              "Attribute value 'extension' missing.");
		}		
		
		[Test]
		public async Task FinalDefaultAttributeHasValueList()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(finalDefaultAttributeValues, "list"),
			              "Attribute value 'list' missing.");
		}
		
		/// <summary>
		/// xs:boolean tests.
		/// </summary>
		[Test]
		[Ignore]
		public async Task MixedAttributeHasValueTrue()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(mixedAttributeValues, "true"),
			              "Attribute value 'true' missing.");
		}
		
		[Test]
		public async Task MaxOccursAttributeHasValueUnbounded()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(maxOccursAttributeValues, "unbounded"),
			              "Attribute value 'unbounded' missing.");
		}
		
		[Test]
		public async Task AllElementHasAnnotationChildElement()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(allElementChildElements, "xs:annotation"),
			              "Should have an annotation child element.");
		}
		
		[Test]
		public async Task AllElementHasElementChildElement()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(allElementChildElements, "xs:element"),
			              "Should have an child element called 'element'.");
		}
		
		[Test]
		public async Task AllElementAnnotationHasDocumentationChildElement()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(allElementAnnotationChildElements, "xs:documentation"),
			              "Should have documentation child element.");
		}				
	}
}
