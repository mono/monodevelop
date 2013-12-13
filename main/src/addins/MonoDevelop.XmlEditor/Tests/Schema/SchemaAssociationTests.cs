using MonoDevelop.XmlEditor;
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
		public void Init ()
		{
			xml = new StringBuilder ();
			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.Indent = true;
			settings.OmitXmlDeclaration = true;
			settings.IndentChars = "\t";
			writer = XmlWriter.Create (xml, settings);
		}
				
		[Test]
		[Platform (Exclude = "Win")]
		public void ToXml()
		{
			XmlFileAssociation schema = new XmlFileAssociation (".xml", "http://mono-project.com", null);
			schema.WriteTo(writer);
			
			string expectedXml = "<SchemaAssociation extension=\".xml\" namespace=\"http://mono-project.com\" prefix=\"\" />";
			Assert.AreEqual(expectedXml, xml.ToString());
		}
		
		[Test]
		[Platform (Exclude = "Win")]
		public void FromXml()
		{
			XmlFileAssociation expectedSchema = new XmlFileAssociation (".xml", "http://mono-project.com", null);
			expectedSchema.WriteTo(writer);

			string propertiesXml = "<SerializedNode>" + xml.ToString() + "</SerializedNode>";
			XmlTextReader reader = new XmlTextReader (new StringReader(propertiesXml));
			XmlFileAssociation schema = new XmlFileAssociation ();
			schema = (XmlFileAssociation)schema.ReadFrom (reader);
			
			Assert.AreEqual(expectedSchema.Extension, schema.Extension);
			Assert.AreEqual(expectedSchema.NamespacePrefix, schema.NamespacePrefix);
			Assert.AreEqual(expectedSchema.NamespaceUri, schema.NamespaceUri);
		}
		
		[Test]
		public void FromXmlMissingSchemaAssociation()
		{
			string propertiesXml = "<SerializedNode/>";
			XmlTextReader reader = new XmlTextReader(new StringReader(propertiesXml));
			XmlFileAssociation schema = new XmlFileAssociation();
			Assert.IsNull(schema.ReadFrom (reader));
		}		
	}
}
