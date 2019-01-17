using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Schema
{
	/// <summary>
	/// Tests element group refs
	/// </summary>
	[TestFixture]
	public class GroupRefTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList childElements;
		CompletionDataList paraAttributes;
		
		async Task Init ()
		{
			if (childElements != null)
				return;
			
			XmlElementPath path = new XmlElementPath();
			
			path.Elements.Add(new QualifiedName("html", "http://foo/xhtml"));
			path.Elements.Add(new QualifiedName("body", "http://foo/xhtml"));
			
			childElements = await SchemaCompletionData.GetChildElementCompletionData(path, CancellationToken.None);
			
			path.Elements.Add(new QualifiedName("p", "http://foo/xhtml"));
			paraAttributes = await SchemaCompletionData.GetAttributeCompletionData(path, CancellationToken.None);
		}
		
		[Test]
		public async Task BodyHasFourChildElements()
		{
			await Init ();
			Assert.AreEqual(4, childElements.Count, 
			                "Should be 4 child elements.");
		}
		
		[Test]
		public async Task BodyChildElementForm()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(childElements, "form"), 
			              "Should have a child element called form.");
		}
		
		[Test]
		public async Task BodyChildElementPara()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(childElements, "p"), 
			              "Should have a child element called p.");
		}		
		
		[Test]
		public async Task BodyChildElementTest()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(childElements, "test"), 
			              "Should have a child element called test.");
		}		
		
		[Test]
		public async Task BodyChildElementId()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(childElements, "id"), 
			              "Should have a child element called id.");
		}		
		
		[Test]
		public async Task ParaElementHasIdAttribute()
		{
			await Init ();
			Assert.IsTrue(SchemaTestFixtureBase.Contains(paraAttributes, "id"), 
			              "Should have an attribute called id.");			
		}
		
		protected override string GetSchema()
		{
			return "<xs:schema version=\"1.0\" xml:lang=\"en\"\r\n" +
				"    xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\r\n" +
				"    targetNamespace=\"http://foo/xhtml\"\r\n" +
				"    xmlns=\"http://foo/xhtml\"\r\n" +
				"    elementFormDefault=\"qualified\">\r\n" +
				"\r\n" +
				"  <xs:element name=\"html\">\r\n" +
				"    <xs:complexType>\r\n" +
				"      <xs:sequence>\r\n" +
				"        <xs:element ref=\"head\"/>\r\n" +
				"        <xs:element ref=\"body\"/>\r\n" +
				"      </xs:sequence>\r\n" +
				"    </xs:complexType>\r\n" +
				"  </xs:element>\r\n" +
				"\r\n" +
				"  <xs:element name=\"head\" type=\"xs:string\"/>\r\n" +
				"  <xs:element name=\"body\">\r\n" +
				"    <xs:complexType>\r\n" +
				"      <xs:sequence>\r\n" +
				"        <xs:group ref=\"block\"/>\r\n" +
				"        <xs:element name=\"form\"/>\r\n" +
				"      </xs:sequence>\r\n" +
				"    </xs:complexType>\r\n" +
				"  </xs:element>\r\n" +
				"\r\n" +
				"\r\n" +
				"  <xs:group name=\"block\">\r\n" +
				"    <xs:choice>\r\n" +
				"      <xs:element ref=\"p\"/>\r\n" +
				"      <xs:group ref=\"heading\"/>\r\n" +
				"    </xs:choice>\r\n" +
				"  </xs:group>\r\n" +
				"\r\n" +
				"  <xs:element name=\"p\">\r\n" +
				"    <xs:complexType>\r\n" +
				"      <xs:attribute name=\"id\"/>" +
				"    </xs:complexType>\r\n" +
				"  </xs:element>\r\n" +				
				"\r\n" +
				"  <xs:group name=\"heading\">\r\n" +
				"    <xs:choice>\r\n" +
				"      <xs:element name=\"test\"/>\r\n" +
				"      <xs:element name=\"id\"/>\r\n" +
				"    </xs:choice>\r\n" +
				"  </xs:group>\r\n" +
				"\r\n" +
				"</xs:schema>";
		}
	}
}
