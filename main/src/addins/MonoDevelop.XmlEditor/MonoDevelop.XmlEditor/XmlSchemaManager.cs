//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2007 Matthew Ward
//

using MonoDevelop.Core;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using MonoDevelop.XmlEditor.Completion;

namespace MonoDevelop.XmlEditor
{
	// Keeps track of all the schemas that the Xml Editor is aware of.
	public static class XmlSchemaManager
	{
		public const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";
		
		public static event EventHandler UserSchemaAdded;
		public static event EventHandler UserSchemaRemoved;
		
		static XmlSchemaCompletionDataCollection userSchemas;
		static XmlSchemaCompletionDataCollection builtinSchemas;
		static IXmlSchemaCompletionDataCollection mergedSchemas;
	
		// Gets the schemas that MonoDevelop knows about.
		public static IXmlSchemaCompletionDataCollection SchemaCompletionDataItems {
			get {
				if (mergedSchemas == null) {
					mergedSchemas = new MergedXmlSchemaCompletionDataCollection (BuiltinSchemas, UserSchemas);
				}
				return mergedSchemas;
			}
		}
		
		internal static XmlSchemaCompletionDataCollection UserSchemas {
			get {
				if (userSchemas == null) {
					userSchemas = new XmlSchemaCompletionDataCollection ();
					LoadSchemas (userSchemas, UserSchemaFolder, false);
				}
				return userSchemas;
			}
		}
		
		internal static XmlSchemaCompletionDataCollection BuiltinSchemas {
			get {
				if (builtinSchemas == null) {
					builtinSchemas  = new XmlSchemaCompletionDataCollection ();
					LoadSchemas (builtinSchemas, SchemaFolder, true);
				}
				return builtinSchemas;
			}
		}
		
		public static XmlSchemaCompletionData GetSchemaCompletionData (string fileExtension)
		{
			XmlSchemaCompletionData data = null;
			
			XmlSchemaAssociation association = XmlEditorAddInOptions.GetSchemaAssociation (fileExtension);
			if (association != null)
				if (association.NamespaceUri.Length > 0)
					data = SchemaCompletionDataItems [association.NamespaceUri];
			
			return data;
		}
		
		/// <summary>
		/// Gets the namespace prefix that is associated with the
		/// specified file extension.
		/// </summary>
		public static string GetNamespacePrefix(string extension)
		{
			XmlSchemaAssociation association = XmlEditorAddInOptions.GetSchemaAssociation(extension);
			if (association != null) {
				return association.NamespacePrefix;
			}
			return String.Empty;
		}
		
		/// <summary>
		/// Removes the schema with the specified namespace from the
		/// user schemas folder and removes the completion data.
		/// </summary>
		public static void RemoveUserSchema (string namespaceUri)
		{
			XmlSchemaCompletionData schemaData = UserSchemas [namespaceUri];
			if (schemaData != null) {
				if (File.Exists(schemaData.FileName)) {
					File.Delete(schemaData.FileName);
				}
				UserSchemas.Remove (schemaData);
				OnUserSchemaRemoved ();
			}
		}
		
		/// <summary>
		/// Adds the schema to the user schemas folder and makes the
		/// schema available to the xml editor.
		/// </summary>
		public static void AddUserSchema(XmlSchemaCompletionData schemaData)
		{
			if (UserSchemas [schemaData.NamespaceUri] == null) {

				if (!Directory.Exists(UserSchemaFolder)) {
					Directory.CreateDirectory (UserSchemaFolder);
				}			
	
				string fileName = Path.GetFileName (schemaData.FileName);
				string destinationFileName = Path.Combine (UserSchemaFolder, fileName);
				File.Copy (schemaData.FileName, destinationFileName);
				schemaData.FileName = destinationFileName;
				UserSchemas.Add (schemaData);
				OnUserSchemaAdded ();
			} else {
				LoggingService.LogWarning ("XmlSchemaManager cannot register two schemas with the same namespace '{0}'.", schemaData.NamespaceUri);
			}
		}	
		
		/// <summary>
		/// Determines whether the specified namespace is actually the W3C namespace for
		/// XSD files.
		/// </summary>
		public static bool IsXmlSchemaNamespace(string schemaNamespace)
		{
			return schemaNamespace == XmlSchemaNamespace;
		}
		
		// Reads all .xsd files in the specified folder.
		static void LoadSchemas (List<XmlSchemaCompletionData> list, string folder, bool readOnly)
		{
			LoggingService.LogInfo ("Reading schemas from: " + folder);
			if (Directory.Exists(folder)) {
				int count = 0;
				foreach (string fileName in Directory.GetFiles(folder, "*.xsd")) {
					LoadSchema (list, fileName, readOnly);
					++count;
				}
				LoggingService.LogInfo ("XmlSchemaManager found {0} schemas.", count);
			}
		}
		
		/// <summary>
		/// Reads an individual schema and adds it to the collection.
		/// </summary>
		/// <remarks>
		/// If the schema namespace exists in the collection it is not added.
		/// </remarks>
		static void LoadSchema (List<XmlSchemaCompletionData> list, string fileName, bool readOnly)
		{
			try {
				string baseUri = XmlSchemaCompletionData.GetUri (fileName);
				XmlSchemaCompletionData data = new XmlSchemaCompletionData (baseUri, fileName);
				
				if (data.NamespaceUri == null) {
					LoggingService.LogWarning (
					    "XmlSchemaManager is ignoring schema with no namespace, from file '{0}'.",
					    data.FileName);
					return;
				}
				
				foreach (XmlSchemaCompletionData d in list) {
					if (d.NamespaceUri == data.NamespaceUri) {
						LoggingService.LogWarning (
						    "XmlSchemaManager is ignoring schema with duplicate namespace '{0}'.",
						    data.NamespaceUri);
						return;
					}
				}
				
				data.ReadOnly = readOnly;
				list.Add (data);
				
			} catch (Exception ex) {
				LoggingService.LogWarning (
				    "XmlSchemaManager is unable to read schema '{0}', because of the following error: {1}",
				    fileName, ex.Message);
			}
		}
		
		/// <summary>
		/// Gets the folder where the schemas for all users on the
		/// local machine are stored.
		/// </summary>
		static string SchemaFolder {
			get {
				string location = Assembly.GetAssembly (typeof(XmlSchemaManager)).Location;
				return Path.GetFullPath (Path.Combine (Path.GetDirectoryName (location), "schemas"));
			}
		}
		
		// Gets the folder where schemas are stored for an individual user.
		static string UserSchemaFolder {
			get { return Path.Combine (PropertyService.ConfigPath, "schemas"); }
		}
		
		// FIXME: Should really pass schema info with the event.
		static void OnUserSchemaAdded()
		{
			if (UserSchemaAdded != null)
				UserSchemaAdded (null, EventArgs.Empty);
		}
		
		// FIXME: Should really pass schema info with the event.
		static void OnUserSchemaRemoved()
		{
			if (UserSchemaRemoved != null)
				UserSchemaRemoved (null, EventArgs.Empty);
		}
	}
}
