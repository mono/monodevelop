using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.XmlEditor;
using NUnit.Framework;

namespace MonoDevelop.XmlEditor.Tests.Schema
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

			shipToAttributes = SchemaCompletionData.GetAttributeCompletionData(shipToPath);
			
			// Get shiporder attributes.
			shipOrderPath = new XmlElementPath();
			shipOrderPath.Elements.Add(shipOrderName);
			
			shipOrderAttributes = SchemaCompletionData.GetAttributeCompletionData(shipOrderPath);
			
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
		public void ShipOrderChildElementsCount()
		{
			Assert.AreEqual(1, SchemaCompletionData.GetChildElementCompletionData(shipOrderPath).Count, 
			                "Should be one child element.");
		}
		
		[Test]
		public void ShipOrderHasShipToChildElement()
		{
			CompletionDataList data = SchemaCompletionData.GetChildElementCompletionData(shipOrderPath);
			Assert.IsTrue(SchemaTestFixtureBase.Contains(data, "shipto"), 
			                "Incorrect child element name.");
		}
		
		[Test]
		public void ShipToChildElementsCount()
		{
			Assert.AreEqual(2, SchemaCompletionData.GetChildElementCompletionData(shipToPath).Count, 
			                "Should be 2 child elements.");
		}		
		
		[Test]
		public void ShipToHasNameChildElement()
		{
			CompletionDataList data = SchemaCompletionData.GetChildElementCompletionData(shipToPath);
			Assert.IsTrue(SchemaTestFixtureBase.Contains(data, "name"), 
			                "Incorrect child element name.");
		}		
		
		[Test]
		public void ShipToHasAddressChildElement()
		{
			CompletionDataList data = SchemaCompletionData.GetChildElementCompletionData(shipToPath);
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
