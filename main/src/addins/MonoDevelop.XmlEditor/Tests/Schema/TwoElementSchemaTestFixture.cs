using MonoDevelop.Ide.CodeCompletion;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Two elements defined in a schema, one uses the 'type' attribute to
	/// link to the complex type definition.
	/// </summary>
	[TestFixture]
	public class TwoElementSchemaTestFixture : SchemaTestFixtureBase
	{
		XmlElementPath noteElementPath;
		XmlElementPath textElementPath;
		
		public override void FixtureInit()
		{
			// Note element path.
			noteElementPath = new XmlElementPath();
			QualifiedName noteQualifiedName = new QualifiedName("note", "http://www.w3schools.com");
			noteElementPath.Elements.Add(noteQualifiedName);
		
			// Text element path.
			textElementPath = new XmlElementPath();
			textElementPath.Elements.Add(noteQualifiedName);
			textElementPath.Elements.Add(new QualifiedName("text", "http://www.w3schools.com"));
		}	
		
		[Test]
		public void TextElementHasOneAttribute()
		{
			CompletionDataList attributesCompletionData = SchemaCompletionData.GetAttributeCompletionData(textElementPath);
			
			Assert.AreEqual(1, attributesCompletionData.Count, 
			                "Should have 1 text attribute.");
		}
		
		[Test]
		public void TextElementAttributeName()
		{
			CompletionDataList attributesCompletionData = SchemaCompletionData.GetAttributeCompletionData(textElementPath);
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributesCompletionData, "foo"),
			              "Unexpected text attribute name.");
		}

		[Test]
		public void NoteElementHasChildElement()
		{
			CompletionDataList childElementCompletionData
				= SchemaCompletionData.GetChildElementCompletionData(noteElementPath);
			
			Assert.AreEqual(1, childElementCompletionData.Count,
			                "Should be one child.");
		}
		
		[Test]
		public void NoteElementHasNoAttributes()
		{	
			CompletionDataList attributeCompletionData
				= SchemaCompletionData.GetAttributeCompletionData(noteElementPath);
			
			Assert.AreEqual(0, attributeCompletionData.Count,
			                "Should no attributes.");
		}

		[Test]
		public void OneRootElement()
		{
			CompletionDataList elementCompletionData
				= SchemaCompletionData.GetElementCompletionData();
			
			Assert.AreEqual(1, elementCompletionData.Count, "Should be 1 root element.");
		}
		
		[Test]
		public void RootElementIsNote()
		{
			CompletionDataList elementCompletionData
				= SchemaCompletionData.GetElementCompletionData();
			
			Assert.IsTrue(Contains(elementCompletionData, "note"), 
			              "Should be called note.");
		}
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://www.w3schools.com\" xmlns=\"http://www.w3schools.com\" elementFormDefault=\"qualified\">\r\n" +
				"\t<xs:element name=\"note\">\r\n" +
				"\t\t<xs:complexType> \r\n" +
				"\t\t\t<xs:sequence>\r\n" +
				"\t\t\t\t<xs:element name=\"text\" type=\"text-type\"/>\r\n" +
				"\t\t\t</xs:sequence>\r\n" +
				"\t\t</xs:complexType>\r\n" +
				"\t</xs:element>\r\n" +
				"\t<xs:complexType name=\"text-type\">\r\n" +
				"\t\t<xs:attribute name=\"foo\"/>\r\n" +
				"\t</xs:complexType>\r\n" +
				"</xs:schema>";
		}
	}
}
