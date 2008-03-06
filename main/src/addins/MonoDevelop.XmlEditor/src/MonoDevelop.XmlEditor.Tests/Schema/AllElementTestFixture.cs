
using MonoDevelop.XmlEditor;
using MonoDevelop.Projects.Gui.Completion;
using NUnit.Framework;
using System;
using System.IO;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	/// <summary>
	/// Tests that element completion works for any child elements
	/// inside an xs:all schema element.
	/// </summary>
	[TestFixture]
	public class AllElementTestFixture : SchemaTestFixtureBase
	{		
		ICompletionData[] personElementChildren;
		
		public override void FixtureInit()
		{
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("person", "http://foo"));
			
			personElementChildren = SchemaCompletionData.GetChildElementCompletionData(path);
		}
		
		[Test]
		public void PersonElementHasTwoChildElements()
		{
			Assert.AreEqual(2, personElementChildren.Length, 
			                "Should have 2 child elements.");
		}
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" elementFormDefault=\"qualified\" targetNamespace=\"http://foo\">\r\n" +
				"    <xs:element name=\"person\">\r\n" +
				"      <xs:complexType>\r\n" +
				"        <xs:all>\r\n" +
				"          <xs:element name=\"firstname\" type=\"xs:string\"/>\r\n" +
				"          <xs:element name=\"lastname\" type=\"xs:string\"/>\r\n" +
				"        </xs:all>\r\n" +
				"      </xs:complexType>\r\n" +
				"    </xs:element>\r\n" +
				"</xs:schema>";
		}
	}
}
