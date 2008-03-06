using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace MonoDevelop.XmlEditor.Tests.Schema
{
	[TestFixture]
	public class SchemaAssociationTests
	{
		StringBuilder xml;
		XmlWriter writer;
		
		[SetUp]
		public void Init()
		{
			xml = new StringBuilder();
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.OmitXmlDeclaration = true;
			settings.IndentChars = "\t";
			writer = XmlWriter.Create(xml, settings);
		}
				
		[Test]
		public void ToXml()
		{
			XmlSchemaAssociation schema = new XmlSchemaAssociation(".xml", "http://mono-project.com");
			schema.WriteTo(writer);
			
			string expectedXml = "<SchemaAssociation extension=\".xml\" namespace=\"http://mono-project.com\" prefix=\"\" />";
			Assert.AreEqual(expectedXml, xml.ToString());
		}
		
		[Test]
		public void FromXml()
		{
			XmlSchemaAssociation expectedSchema = new XmlSchemaAssociation(".xml", "http://mono-project.com");
			expectedSchema.WriteTo(writer);

			string propertiesXml = "<SerializedNode>" + xml.ToString() + "</SerializedNode>";
			XmlTextReader reader = new XmlTextReader(new StringReader(propertiesXml));
			XmlSchemaAssociation schema = new MonoDevelop.XmlEditor.XmlSchemaAssociation(String.Empty);
			schema = (XmlSchemaAssociation)schema.ReadFrom(reader);
			
			Assert.AreEqual(expectedSchema.Extension, schema.Extension);
			Assert.AreEqual(expectedSchema.NamespacePrefix, schema.NamespacePrefix);
			Assert.AreEqual(expectedSchema.NamespaceUri, schema.NamespaceUri);
		}
		
		[Test]
		public void FromXmlMissingSchemaAssociation()
		{
			string propertiesXml = "<SerializedNode/>";
			XmlTextReader reader = new XmlTextReader(new StringReader(propertiesXml));
			XmlSchemaAssociation schema = new MonoDevelop.XmlEditor.XmlSchemaAssociation(String.Empty);
			Assert.IsNull(schema.ReadFrom(reader));
		}		
	}
}
