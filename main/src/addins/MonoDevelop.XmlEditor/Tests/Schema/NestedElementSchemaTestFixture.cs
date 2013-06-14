using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	[TestFixture]
	public class NestedElementSchemaTestFixture : SchemaTestFixtureBase
	{
		XmlElementPath noteElementPath;
		CompletionDataList elementData;
		
		public override void FixtureInit()
		{			
			noteElementPath = new XmlElementPath();
			noteElementPath.Elements.Add(new QualifiedName("note", "http://www.w3schools.com"));

			elementData = SchemaCompletionData.GetChildElementCompletionData(noteElementPath); 
		}
		
		[Test]
		public void NoteHasOneChildElementCompletionDataItem()
		{
			Assert.AreEqual(1, elementData.Count, "Should be one child element completion data item.");
		}
		
		[Test]
		public void NoteChildElementCompletionDataText()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(elementData, "text"),
			              "Should be one child element called text.");
		}		

		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://www.w3schools.com\" xmlns=\"http://www.w3schools.com\" elementFormDefault=\"qualified\">\r\n" +
				"\t<xs:element name=\"note\">\r\n" +
				"\t\t<xs:complexType> \r\n" +
				"\t\t\t<xs:sequence>\r\n" +
				"\t\t\t\t<xs:element name=\"text\" type=\"xs:string\"/>\r\n" +
				"\t\t\t</xs:sequence>\r\n" +
				"\t\t</xs:complexType>\r\n" +
				"\t</xs:element>\r\n" +
				"</xs:schema>";
		}
	}
}
