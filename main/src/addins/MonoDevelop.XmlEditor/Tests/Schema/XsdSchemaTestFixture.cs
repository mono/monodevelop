using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using MonoDevelop.XmlEditor.Completion;
using NUnit.Framework;
using System.Xml;
using MonoDevelop.XmlEditor.Tests.Utils;

namespace MonoDevelop.XmlEditor.Tests.Schema
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
		
		[TestFixtureSetUp]
		public void FixtureInit()
		{
			XmlTextReader reader = ResourceManager.GetXsdSchema();
			schemaCompletionData = new XmlSchemaCompletionData(reader);
			
			// Set up choice element's path.
			choicePath = new XmlElementPath();
			choicePath.Elements.Add(new QualifiedName("schema", namespaceURI, prefix));
			choicePath.Elements.Add(new QualifiedName("element", namespaceURI, prefix));
			choicePath.Elements.Add(new QualifiedName("complexType", namespaceURI, prefix));
			
			mixedAttributeValues = schemaCompletionData.GetAttributeValueCompletionData(choicePath, "mixed");

			choicePath.Elements.Add(new QualifiedName("choice", namespaceURI, prefix));
			
			// Get choice element info.
			choiceAttributes = schemaCompletionData.GetAttributeCompletionData(choicePath);
			maxOccursAttributeValues = schemaCompletionData.GetAttributeValueCompletionData(choicePath, "maxOccurs");
			
			// Set up element path.
			elementPath = new XmlElementPath();
			elementPath.Elements.Add(new QualifiedName("schema", namespaceURI, prefix));
			
			elementFormDefaultAttributeValues = schemaCompletionData.GetAttributeValueCompletionData(elementPath, "elementFormDefault");
			blockDefaultAttributeValues = schemaCompletionData.GetAttributeValueCompletionData(elementPath, "blockDefault");
			finalDefaultAttributeValues = schemaCompletionData.GetAttributeValueCompletionData(elementPath, "finalDefault");
			
			elementPath.Elements.Add(new QualifiedName("element", namespaceURI, prefix));
				
			// Get element attribute info.
			elementAttributes = schemaCompletionData.GetAttributeCompletionData(elementPath);

			// Set up simple enum type path.
			simpleEnumPath = new XmlElementPath();
			simpleEnumPath.Elements.Add(new QualifiedName("schema", namespaceURI, prefix));
			simpleEnumPath.Elements.Add(new QualifiedName("simpleType", namespaceURI, prefix));
			simpleEnumPath.Elements.Add(new QualifiedName("restriction", namespaceURI, prefix));
			
			// Get child elements.
			simpleEnumElements = schemaCompletionData.GetChildElementCompletionData(simpleEnumPath);

			// Set up enum path.
			enumPath = new XmlElementPath();
			enumPath.Elements.Add(new QualifiedName("schema", namespaceURI, prefix));
			enumPath.Elements.Add(new QualifiedName("simpleType", namespaceURI, prefix));
			enumPath.Elements.Add(new QualifiedName("restriction", namespaceURI, prefix));
			enumPath.Elements.Add(new QualifiedName("enumeration", namespaceURI, prefix));
			
			// Get attributes.
			enumAttributes = schemaCompletionData.GetAttributeCompletionData(enumPath);
			
			// Set up xs:all path.
			allElementPath = new XmlElementPath();
			allElementPath.Elements.Add(new QualifiedName("schema", namespaceURI, prefix));
			allElementPath.Elements.Add(new QualifiedName("element", namespaceURI, prefix));
			allElementPath.Elements.Add(new QualifiedName("complexType", namespaceURI, prefix));
			allElementPath.Elements.Add(new QualifiedName("all", namespaceURI, prefix));
		
			// Get child elements of the xs:all element.
			allElementChildElements = schemaCompletionData.GetChildElementCompletionData(allElementPath);
			
			// Set up the path to the annotation element that is a child of xs:all.
			allElementAnnotationPath = new XmlElementPath();
			allElementAnnotationPath.Elements.Add(new QualifiedName("schema", namespaceURI, prefix));
			allElementAnnotationPath.Elements.Add(new QualifiedName("element", namespaceURI, prefix));
			allElementAnnotationPath.Elements.Add(new QualifiedName("complexType", namespaceURI, prefix));
			allElementAnnotationPath.Elements.Add(new QualifiedName("all", namespaceURI, prefix));
			allElementAnnotationPath.Elements.Add(new QualifiedName("annotation", namespaceURI, prefix));
			
			// Get the xs:all annotation child element.
			allElementAnnotationChildElements = schemaCompletionData.GetChildElementCompletionData(allElementAnnotationPath);
		}
		
		[Test]
		public void ChoiceHasAttributes()
		{
			Assert.IsTrue(choiceAttributes.Count > 0, "Should have at least one attribute.");
		}
		
		[Test]
		public void ChoiceHasMinOccursAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(choiceAttributes, "minOccurs"),
			              "Attribute minOccurs missing.");
		}
		
		[Test]
		public void ChoiceHasMaxOccursAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(choiceAttributes, "maxOccurs"),
			              "Attribute maxOccurs missing.");
		}
		
		/// <summary>
		/// Tests that prohibited attributes are not added to the completion data.
		/// </summary>
		[Test]
		public void ChoiceDoesNotHaveNameAttribute()
		{
			Assert.IsFalse(SchemaTestFixtureBase.Contains(choiceAttributes, "name"),
			               "Attribute name should not exist.");
		}
		
		/// <summary>
		/// Tests that prohibited attributes are not added to the completion data.
		/// </summary>
		[Test]
		public void ChoiceDoesNotHaveRefAttribute()
		{
			Assert.IsFalse(SchemaTestFixtureBase.Contains(choiceAttributes, "ref"),
			               "Attribute ref should not exist.");
		}	
		
		/// <summary>
		/// Duplicate attribute test.
		/// </summary>
		[Test]
		public void ElementNameAttributeAppearsOnce()
		{
			int nameAttributeCount = SchemaTestFixtureBase.GetItemCount(elementAttributes, "name");
			Assert.AreEqual(1, nameAttributeCount, "Should be only one name attribute.");
		}
		
		[Test]
		public void ElementHasIdAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(elementAttributes, "id"), 
			              "id attribute missing.");
		}		
		
		[Test]
		public void SimpleRestrictionTypeHasEnumChildElement()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(simpleEnumElements, "xs:enumeration"),
			              "enumeration element missing.");			
		}
		
		[Test]
		public void EnumHasValueAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(enumAttributes, "value"),
			              "Attribute value missing.");			
		}
		
		[Test]
		public void ElementFormDefaultAttributeHasValueQualified()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(elementFormDefaultAttributeValues, "qualified"),
			              "Attribute value 'qualified' missing.");
		}
		
		[Test]
		public void BlockDefaultAttributeHasValueAll()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(blockDefaultAttributeValues, "#all"),
			              "Attribute value '#all' missing.");
		}		
		
		[Test]
		public void BlockDefaultAttributeHasValueExtension()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(blockDefaultAttributeValues, "extension"),
			              "Attribute value 'extension' missing.");
		}		
		
		[Test]
		public void FinalDefaultAttributeHasValueList()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(finalDefaultAttributeValues, "list"),
			              "Attribute value 'list' missing.");
		}
		
		/// <summary>
		/// xs:boolean tests.
		/// </summary>
		[Test]
		[Ignore]
		public void MixedAttributeHasValueTrue()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(mixedAttributeValues, "true"),
			              "Attribute value 'true' missing.");
		}
		
		[Test]
		public void MaxOccursAttributeHasValueUnbounded()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(maxOccursAttributeValues, "unbounded"),
			              "Attribute value 'unbounded' missing.");
		}
		
		[Test]
		public void AllElementHasAnnotationChildElement()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(allElementChildElements, "xs:annotation"),
			              "Should have an annotation child element.");
		}
		
		[Test]
		public void AllElementHasElementChildElement()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(allElementChildElements, "xs:element"),
			              "Should have an child element called 'element'.");
		}
		
		[Test]
		public void AllElementAnnotationHasDocumentationChildElement()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(allElementAnnotationChildElements, "xs:documentation"),
			              "Should have documentation child element.");
		}				
	}
}
