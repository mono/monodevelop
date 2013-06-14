using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Child element attributes.
	/// </summary>
	[TestFixture]
	public class ChildElementAttributesTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList attributes;
		
		public override void FixtureInit()
		{
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("project", "http://nant.sf.net//nant-0.84.xsd"));
			path.Elements.Add(new QualifiedName("attrib", "http://nant.sf.net//nant-0.84.xsd"));
			
			attributes = SchemaCompletionData.GetAttributeCompletionData(path);
		}

		[Test]
		public void AttributeCount()
		{
			Assert.AreEqual(10, attributes.Count, "Should be one attribute.");
		}
		
		[Test]
		public void FileAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(attributes, "file"),
			              "Attribute file does not exist.");
		}		
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:vs=\"urn:schemas-microsoft-com:HTML-Intellisense\" xmlns:nant=\"http://nant.sf.net//nant-0.84.xsd\" targetNamespace=\"http://nant.sf.net//nant-0.84.xsd\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\r\n" +
					"  <xs:element name=\"project\">\r\n" +
					"    <xs:complexType>\r\n" +
					"      <xs:sequence minOccurs=\"0\" maxOccurs=\"unbounded\">\r\n" +
					"        <xs:sequence minOccurs=\"0\" maxOccurs=\"unbounded\">\r\n" +
					"          <xs:sequence minOccurs=\"0\" maxOccurs=\"unbounded\">\r\n" +
					"            <xs:element name=\"attrib\" type=\"nant:attrib\" />\r\n" +
					"          </xs:sequence>\r\n" +
					"        </xs:sequence>\r\n" +
					"      </xs:sequence>\r\n" +
					"      <xs:attribute name=\"name\" use=\"required\" />\r\n" +
					"      <xs:attribute name=\"default\" use=\"optional\" />\r\n" +
					"      <xs:attribute name=\"basedir\" use=\"optional\" />\r\n" +
					"    </xs:complexType>\r\n" +
					"  </xs:element>\r\n" +
					"\r\n" +
					"  <xs:complexType id=\"NAnt.Core.Tasks.AttribTask\" name=\"attrib\">\r\n" +
					"    <xs:attribute name=\"file\" use=\"optional\" />\r\n" +
					"    <xs:attribute name=\"archive\" use=\"optional\" />\r\n" +
					"    <xs:attribute name=\"hidden\" use=\"optional\" />\r\n" +
					"    <xs:attribute name=\"normal\" use=\"optional\" />\r\n" +
					"    <xs:attribute name=\"readonly\" use=\"optional\" />\r\n" +
					"    <xs:attribute name=\"system\" use=\"optional\" />\r\n" +
					"    <xs:attribute name=\"failonerror\" use=\"optional\" />\r\n" +
					"    <xs:attribute name=\"verbose\" use=\"optional\" />\r\n" +
					"    <xs:attribute name=\"if\" use=\"optional\" />\r\n" +
					"    <xs:attribute name=\"unless\" use=\"optional\" />\r\n" +
					"  </xs:complexType>\r\n" +
					"</xs:schema>";
		}
	}
}
