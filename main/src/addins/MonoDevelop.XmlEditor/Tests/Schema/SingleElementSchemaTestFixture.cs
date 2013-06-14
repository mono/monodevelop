using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Retrieve completion data for an xml schema that specifies only one 
	/// element.
	/// </summary>
	[TestFixture]
	public class SingleElementSchemaTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList childElementCompletionData;
		CompletionDataList attributeCompletionData;
		
		public override void FixtureInit()
		{
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("note", "http://www.w3schools.com"));

			attributeCompletionData = 
				SchemaCompletionData.GetAttributeCompletionData(path);

			childElementCompletionData = 
				SchemaCompletionData.GetChildElementCompletionData(path);
		}
		
		[Test]
		public void NamespaceUri()
		{
			Assert.AreEqual("http://www.w3schools.com", 
			                SchemaCompletionData.NamespaceUri,
			                "Unexpected namespace.");
		}
		
		[Test]
		public void NoteElementHasNoAttributes()
		{
			Assert.AreEqual(0, attributeCompletionData.Count, 
			                "Not expecting any attributes.");
		}
		
		[Test]
		public void NoteElementHasNoChildElements()
		{
			Assert.AreEqual(0, childElementCompletionData.Count, "" +
			                "Not expecting any child elements.");
		}
		
		protected override string GetSchema()
		{
			return "<?xml version=\"1.0\"?>\r\n" +
				"<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\r\n" +
				"targetNamespace=\"http://www.w3schools.com\"\r\n" +
				"xmlns=\"http://www.w3schools.com\"\r\n" +
				"elementFormDefault=\"qualified\">\r\n" +
				"<xs:element name=\"note\">\r\n" +
				"</xs:element>\r\n" +
				"</xs:schema>";
		}
	}
}
