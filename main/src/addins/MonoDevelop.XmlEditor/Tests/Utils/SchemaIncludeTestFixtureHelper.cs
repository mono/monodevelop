using MonoDevelop.XmlEditor.Completion;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace MonoDevelop.XmlEditor.Tests.Utils
{
	/// <summary>
	/// Helper class when testing a schema which includes 
	/// another schema.
	/// </summary>
	public class SchemaIncludeTestFixtureHelper
	{
		static string mainSchemaFileName = "main.xsd";
		static string includedSchemaFileName = "include.xsd";
		static readonly string schemaPath;

		SchemaIncludeTestFixtureHelper()
		{
		}
		
		static SchemaIncludeTestFixtureHelper()
		{
			schemaPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XmlEditorTests");
		}
		
		/// <summary>
		/// Creates a schema with the given filename
		/// </summary>
		/// <param name="fileName">Filename of the schema that will be 
		/// generated.</param>
		/// <param name="xml">The schema xml</param>
		public static void CreateSchema(string fileName, string xml)
		{
			XmlTextWriter writer = new XmlTextWriter(fileName, Encoding.UTF8);
			writer.WriteRaw(xml);
			writer.Close();
		}
		
		/// <summary>
		/// Creates two schemas, one which references the other via an
		/// xs:include.  Both schemas will exist in the same folder.
		/// </summary>
		/// <param name="mainSchema">The main schema's xml.</param>
		/// <param name="includedSchema">The included schema's xml.</param>
		public static XmlSchemaCompletionData CreateSchemaCompletionDataObject(string mainSchema, string includedSchema)
		{	
			if (!Directory.Exists(schemaPath)) {
				Directory.CreateDirectory(schemaPath);
			}
			
			CreateSchema(Path.Combine(schemaPath, mainSchemaFileName), mainSchema);
			CreateSchema(Path.Combine(schemaPath, includedSchemaFileName), includedSchema);
			
			// Parse schema.
			string schemaFileName = Path.Combine(schemaPath, mainSchemaFileName);
			string baseUri = XmlSchemaCompletionData.GetUri(schemaFileName);
			return new XmlSchemaCompletionData(baseUri, schemaFileName);
		}
		
		/// <summary>
		/// Removes any files generated for the test fixture.
		/// </summary>
		public static void FixtureTearDown()
		{
			// Delete the created schemas.
			string fileName = Path.Combine(schemaPath, mainSchemaFileName);
			if (File.Exists(fileName)) {
				File.Delete(fileName);
			}
			
			fileName = Path.Combine(schemaPath, includedSchemaFileName);
			if (File.Exists(fileName)) {
				File.Delete(fileName);
			}
			
			if (Directory.Exists(schemaPath)) {
				Directory.Delete(schemaPath);
			}
		}
	}
}
