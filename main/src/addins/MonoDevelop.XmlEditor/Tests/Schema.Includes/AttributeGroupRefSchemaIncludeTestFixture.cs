using NUnit.Framework;
using MonoDevelop.XmlEditor.Completion;
using MonoDevelop.XmlEditor.Tests.Utils;
using MonoDevelop.XmlEditor.Tests.Schema;

namespace MonoDevelop.XmlEditor.Tests.Schema.Includes
{
	/// <summary>
	/// Tests that schemas referenced via xs:include elements are used when
	/// generating completion data.
	/// </summary>
	[TestFixture]
	[Ignore]
	public class AttributeGroupRefSchemaIncludeTestFixture : AttributeGroupRefTestFixture
	{
		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			SchemaIncludeTestFixtureHelper.FixtureTearDown();
		}
		
		protected override XmlSchemaCompletionData CreateSchemaCompletionDataObject()
		{	
			return SchemaIncludeTestFixtureHelper.CreateSchemaCompletionDataObject(GetMainSchema(), GetSchema());
		}
		
		string GetMainSchema()
		{
			return "<xs:schema \r\n" +
				"targetNamespace=\"http://www.w3schools.com\" \r\n" +
				"xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" \r\n" +
				"elementFormDefault=\"qualified\">\r\n" +
				"\t<xs:include schemaLocation=\"include.xsd\"/>\r\n" +
				"</xs:schema>";
		}
	}
}
