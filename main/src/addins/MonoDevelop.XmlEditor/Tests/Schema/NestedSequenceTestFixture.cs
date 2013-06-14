using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Tests that nested schema sequence elements are handled.  This 
	/// happens in the NAnt schema 0.84.
	/// </summary>
	[TestFixture]
	public class NestedSequenceSchemaTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList noteChildElements;
		
		public override void FixtureInit()
		{
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("note", "http://www.w3schools.com"));
			noteChildElements = SchemaCompletionData.GetChildElementCompletionData(path);
		}
		
		[Test]
		public void TitleHasNoChildElements()
		{
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("note", "http://www.w3schools.com"));
			path.Elements.Add(new QualifiedName("title", "http://www.w3schools.com"));
			Assert.AreEqual(0, SchemaCompletionData.GetChildElementCompletionData(path).Count,
			                "Should be no child elements.");
		}
		
		[Test]
		public void TextHasNoChildElements()
		{
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("note", "http://www.w3schools.com"));
			path.Elements.Add(new QualifiedName("text", "http://www.w3schools.com"));
			Assert.AreEqual(0, SchemaCompletionData.GetChildElementCompletionData(path).Count, 
			                "Should be no child elements.");
		}		
		
		[Test]
		public void NoteHasTwoChildElements()
		{
			Assert.AreEqual(2, noteChildElements.Count, 
			                "Should be two child elements.");
		}
		
		[Test]
		public void NoteChildElementIsText()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(noteChildElements, "text"), 
			              "Should have a child element called text.");
		}
		
		[Test]
		public void NoteChildElementIsTitle()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(noteChildElements, "title"), 
			              "Should have a child element called title.");
		}		
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://www.w3schools.com\" xmlns=\"http://www.w3schools.com\" elementFormDefault=\"qualified\">\r\n" +
				"\t<xs:element name=\"note\">\r\n" +
				"\t\t<xs:complexType> \r\n" +
				"\t\t\t<xs:sequence>\r\n" +
				"\t\t\t\t<xs:sequence>\r\n" +
				"\t\t\t\t\t<xs:sequence>\r\n" +
				"\t\t\t\t\t\t<xs:element name=\"title\" type=\"xs:string\"/>\r\n" +
				"\t\t\t\t\t\t<xs:element name=\"text\" type=\"xs:string\"/>\r\n" +
				"\t\t\t\t\t</xs:sequence>\r\n" +
				"\t\t\t\t</xs:sequence>\r\n" +
				"\t\t\t</xs:sequence>\r\n" +
				"\t\t</xs:complexType>\r\n" +
				"\t</xs:element>\r\n" +
				"</xs:schema>";
		}
	}
}
