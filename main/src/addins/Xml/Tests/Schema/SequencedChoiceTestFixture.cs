using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Xml.Tests.Schema
{
	/// <summary>
	/// Tests that nested schema choice elements inside a sequence are handled.
	/// This happens in the NAnt schema 0.85.
	/// </summary>
	[TestFixture]
	public class SequencedChoiceTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList noteChildElements;
		
		async Task Init ()
		{
			if (noteChildElements != null)
				return;
			
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("note", "http://www.w3schools.com"));
			
			noteChildElements = await SchemaCompletionData.GetChildElementCompletionData(path, CancellationToken.None);
		}
		
		[Test]
		public async Task TitleHasNoChildElements()
		{
			await Init ();
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("note", "http://www.w3schools.com"));
			path.Elements.Add(new QualifiedName("title", "http://www.w3schools.com"));
			
			Assert.AreEqual(0, (await SchemaCompletionData.GetChildElementCompletionData(path, CancellationToken.None)).Count, 
			                "Should be no child elements.");
		}
		
		[Test]
		public async Task TextHasNoChildElements()
		{
			await Init ();
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("note", "http://www.w3schools.com"));
			path.Elements.Add(new QualifiedName("title", "http://www.w3schools.com"));

			Assert.AreEqual(0, (await SchemaCompletionData.GetChildElementCompletionData(path, CancellationToken.None)).Count,
			                "Should be no child elements.");
		}		
		
		[Test]
		public async Task NoteHasTwoChildElements()
		{
			await Init ();
			Assert.AreEqual(2, noteChildElements.Count, 
			                "Should be two child elements.");
		}
		
		[Test]
		public async Task NoteChildElementIsText()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(noteChildElements, "text"), 
			              "Should have a child element called text.");
		}
		
		[Test]
		public async Task NoteChildElementIsTitle()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(noteChildElements, "title"), 
			              "Should have a child element called title.");
		}		
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://www.w3schools.com\" xmlns=\"http://www.w3schools.com\" elementFormDefault=\"qualified\">\r\n" +
				"\t<xs:element name=\"note\">\r\n" +
				"\t\t<xs:complexType> \r\n" +
				"\t\t\t<xs:sequence>\r\n" +
				"\t\t\t\t<xs:choice>\r\n" +
				"\t\t\t\t\t<xs:element name=\"title\" type=\"xs:string\"/>\r\n" +
				"\t\t\t\t\t<xs:element name=\"text\" type=\"xs:string\"/>\r\n" +
				"\t\t\t\t</xs:choice>\r\n" +
				"\t\t\t</xs:sequence>\r\n" +
				"\t\t</xs:complexType>\r\n" +
				"\t</xs:element>\r\n" +
				"</xs:schema>";
		}
	}
}
