using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor.Completion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;
using System.Xml;
using MonoDevelop.XmlEditor.Tests.Utils;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Tests the xhtml1-strict schema.
	/// </summary>
	[TestFixture]
	public class XhtmlStrictSchemaTestFixture
	{
		XmlSchemaCompletionData schemaCompletionData;
		XmlElementPath h1Path;
		CompletionDataList h1Attributes;
		string namespaceURI = "http://www.w3.org/1999/xhtml";
		
		[TestFixtureSetUp]
		public void FixtureInit()
		{
			XmlTextReader reader = ResourceManager.GetXhtmlStrictSchema();
			schemaCompletionData = new XmlSchemaCompletionData(reader);
			
			// Set up h1 element's path.
			h1Path = new XmlElementPath();
			h1Path.Elements.Add(new QualifiedName("html", namespaceURI));
			h1Path.Elements.Add(new QualifiedName("body", namespaceURI));
			h1Path.Elements.Add(new QualifiedName("h1", namespaceURI));
			
			// Get h1 element info.
			h1Attributes = schemaCompletionData.GetAttributeCompletionData(h1Path);
		}
		
		[Test]
		public void H1HasAttributes()
		{
			Assert.IsTrue(h1Attributes.Count > 0, "Should have at least one attribute.");
		}
	}
}
