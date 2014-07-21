//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2007 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Xml.Completion;

namespace MonoDevelop.Xml.Editor
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
					var nodes = Mono.Addins.AddinManager.GetExtensionNodes ("/MonoDevelop/Xml/Editor/XmlSchemas");
					foreach (XmlSchemaNode node in nodes)
						LoadSchema (builtinSchemas, node.File, true);
				}
				return builtinSchemas;
			}
		}
		
		public static XmlSchemaCompletionData GetSchemaCompletionDataForFileName (string filename)
		{
			var association = XmlFileAssociationManager.GetAssociationForFileName (filename);
			if (association == null || association.NamespaceUri.Length == 0)
				return null;
			var u = new Uri (association.NamespaceUri);
			if (u.IsFile) {
				return ReadLocalSchema (u);
			} else {
				return SchemaCompletionDataItems [association.NamespaceUri];
			}
		}
		
		/// <summary>
		/// Gets the namespace prefix that is associated with the
		/// specified file extension.
		/// </summary>
		public static string GetNamespacePrefixForFileName (string filename)
		{
			var association = XmlFileAssociationManager.GetAssociationForFileName (filename);
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
		public static void AddUserSchema (XmlSchemaCompletionData schemaData)
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
		
		//FIXME: cache and re-use these instances using a weak reference table
		static XmlSchemaCompletionData ReadLocalSchema (Uri uri)
		{
			try {
				return new XmlSchemaCompletionData (uri.ToString (), uri.LocalPath);
			} catch (Exception ex) {
				LoggingService.LogWarning (
				    "XmlSchemaManager is unable to read schema '{0}', because of the following error: {1}",
				    uri, ex.Message);
				return null;
			}
		}
		
		// Gets the folder where schemas are stored for an individual user.
		static string UserSchemaFolder {
			get { return UserProfile.Current.UserDataRoot.Combine ("XmlSchemas"); }
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
