using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Tests that autocompletion data is correct for an xml schema containing:
	/// <![CDATA[ <xs:element name="foo">
	/// 	<xs:group ref="myGroup"/>
	///   </xs:element>
	/// </]]>
	/// </summary>
	[TestFixture]
	public class GroupRefAsCompositorTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList rootChildElements;
		CompletionDataList fooAttributes;
		
		public override void FixtureInit()
		{
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("root", "http://foo"));
			
			rootChildElements = SchemaCompletionData.GetChildElementCompletionData(path);
		
			path.Elements.Add(new QualifiedName("foo", "http://foo"));
			
			fooAttributes = SchemaCompletionData.GetAttributeCompletionData(path);
		}
				
		[Test]
		public void RootHasTwoChildElements()
		{
			Assert.AreEqual(2, rootChildElements.Count, 
			                "Should be two child elements.");
		}
		
		[Test]
		public void RootChildElementIsFoo()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(rootChildElements, "foo"), 
			              "Should have a child element called foo.");
		}
		
		[Test]
		public void RootChildElementIsBar()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(rootChildElements, "bar"), 
			              "Should have a child element called bar.");
		}		
		
		[Test]
		public void FooElementHasIdAttribute()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(fooAttributes, "id"),
			              "Should have an attribute called id.");
		}
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://foo\" xmlns=\"http://foo\" elementFormDefault=\"qualified\">\r\n" +
				"\t<xs:element name=\"root\">\r\n" +
				"\t\t<xs:complexType>\r\n" +
				"\t\t\t<xs:group ref=\"fooGroup\"/>\r\n" +
				"\t\t</xs:complexType>\r\n" +
				"\t</xs:element>\r\n" +
				"\t<xs:group name=\"fooGroup\">\r\n" +
				"\t\t<xs:choice>\r\n" +
				"\t\t\t<xs:element name=\"foo\">\r\n" +
				"\t\t\t\t<xs:complexType>\r\n" +
				"\t\t\t\t\t<xs:attribute name=\"id\" type=\"xs:string\"/>\r\n" +
				"\t\t\t\t</xs:complexType>\r\n" +
				"\t\t\t</xs:element>\r\n" +
				"\t\t\t<xs:element name=\"bar\" type=\"xs:string\"/>\r\n" +
				"\t\t</xs:choice>\r\n" +
				"\t</xs:group>\r\n" +
				"</xs:schema>";
		}
	}
}
