using System.Threading;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Completion;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MonoDevelop.Xml.Tests.Schema
{
	[TestFixture]
	public class ReferencedElementsTestFixture : SchemaTestFixtureBase
	{
		CompletionDataList shipOrderAttributes;
		CompletionDataList shipToAttributes;
		XmlElementPath shipToPath;
		XmlElementPath shipOrderPath;
		
		public override void FixtureInit()
		{
			// Get shipto attributes.
			shipToPath = new XmlElementPath();
			QualifiedName shipOrderName = new QualifiedName("shiporder", "http://www.w3schools.com");
			shipToPath.Elements.Add(shipOrderName);
			shipToPath.Elements.Add(new QualifiedName("shipto", "http://www.w3schools.com"));

			shipToAttributes = SchemaCompletionData.GetAttributeCompletionData(shipToPath, CancellationToken.None).Result;
			
			// Get shiporder attributes.
			shipOrderPath = new XmlElementPath();
			shipOrderPath.Elements.Add(shipOrderName);
			
			shipOrderAttributes = SchemaCompletionData.GetAttributeCompletionData(shipOrderPath, CancellationToken.None).Result;
			
		}
		
		[Test]
		public void OneShipOrderAttribute()
		{
			Assert.AreEqual(1, shipOrderAttributes.Count, "Should only have one shiporder attribute.");
		}		
		
		[Test]
		public void ShipOrderAttributeName()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(shipOrderAttributes,"id"),
			                "Incorrect shiporder attribute name.");
		}

		[Test]
		public void OneShipToAttribute()
		{
			Assert.AreEqual(1, shipToAttributes.Count, "Should only have one shipto attribute.");
		}
		
		[Test]
		public void ShipToAttributeName()
		{
			Assert.IsTrue(SchemaTestFixtureBase.Contains(shipToAttributes, "address"),
			                "Incorrect shipto attribute name.");
		}					
		
		[Test]
		public async Task ShipOrderChildElementsCount()
		{
			Assert.AreEqual(1, (await SchemaCompletionData.GetChildElementCompletionData(shipOrderPath, CancellationToken.None)).Count, 
			                "Should be one child element.");
		}
		
		[Test]
		public async Task ShipOrderHasShipToChildElement()
		{
			CompletionDataList data = await SchemaCompletionData.GetChildElementCompletionData(shipOrderPath, CancellationToken.None);
			Assert.IsTrue(SchemaTestFixtureBase.Contains(data, "shipto"), 
			                "Incorrect child element name.");
		}
		
		[Test]
		public async Task ShipToChildElementsCount()
		{
			Assert.AreEqual(2, (await SchemaCompletionData.GetChildElementCompletionData(shipToPath, CancellationToken.None)).Count, 
			                "Should be 2 child elements.");
		}		
		
		[Test]
		public async Task ShipToHasNameChildElement()
		{
			CompletionDataList data = await SchemaCompletionData.GetChildElementCompletionData(shipToPath, CancellationToken.None);
			Assert.IsTrue(SchemaTestFixtureBase.Contains(data, "name"), 
			                "Incorrect child element name.");
		}		
		
		[Test]
		public async Task ShipToHasAddressChildElement()
		{
			CompletionDataList data = await SchemaCompletionData.GetChildElementCompletionData(shipToPath, CancellationToken.None);
			Assert.IsTrue(SchemaTestFixtureBase.Contains(data, "address"), 
			                "Incorrect child element name.");
		}		
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://www.w3schools.com\"  xmlns=\"http://www.w3schools.com\">\r\n" +
				"\r\n" +
				"<!-- definition of simple elements -->\r\n" +
				"<xs:element name=\"name\" type=\"xs:string\"/>\r\n" +
				"<xs:element name=\"address\" type=\"xs:string\"/>\r\n" +
				"\r\n" +
				"<!-- definition of complex elements -->\r\n" +
				"<xs:element name=\"shipto\">\r\n" +
				" <xs:complexType>\r\n" +
				"  <xs:sequence>\r\n" +
				"   <xs:element ref=\"name\"/>\r\n" +
				"   <xs:element ref=\"address\"/>\r\n" +
				"  </xs:sequence>\r\n" +
				"  <xs:attribute name=\"address\"/>\r\n" +
				" </xs:complexType>\r\n" +
				"</xs:element>\r\n" +
				"\r\n" +
				"<xs:element name=\"shiporder\">\r\n" +
				" <xs:complexType>\r\n" +
				"  <xs:sequence>\r\n" +
				"   <xs:element ref=\"shipto\"/>\r\n" +
				"  </xs:sequence>\r\n" +
				"  <xs:attribute name=\"id\"/>\r\n" +
				" </xs:complexType>\r\n" +
				"</xs:element>\r\n" +
				"\r\n" +
				"</xs:schema>";
		}
	}
}
