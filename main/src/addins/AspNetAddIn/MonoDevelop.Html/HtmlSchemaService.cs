// 
// HtmlSchemaService.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.XmlEditor.Completion;

namespace MonoDevelop.Html
{
	
	public static class HtmlSchemaService
	{
		static Dictionary<string, HtmlSchema> schemas;
		
		static void Initialise ()
		{
			schemas = new Dictionary<string, HtmlSchema> ();
			
			//TODO: load all the schemas from addin points
			//NOTE: the first ([0]) schema must be the default schema (HTML4 transitional)
			string schemaDir = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location);
			schemaDir = Path.Combine (schemaDir, "Schemas");
			
			foreach (DocTypeExtensionNode node in Mono.Addins.AddinManager.GetExtensionNodes ("/MonoDevelop/Html/DocTypes")) {
				if (schemas.ContainsKey (node.Name))
					LoggingService.LogWarning (
					    "HtmlSchemaService cannot register duplicate doctype with the name '{0}'", node.Name);
				
				if (!string.IsNullOrEmpty (node.XsdFile)) {
					string path = Path.Combine (schemaDir, node.XsdFile);
					try {
						IXmlCompletionProvider provider = new XmlSchemaCompletionData (path);
						schemas.Add (node.Name, new HtmlSchema (node.Name, node.FullName, provider));
					} catch (Exception ex) {
						LoggingService.LogWarning (
						    "HtmlSchemaService encountered an error registering the schema '" + path + "'", ex);
					}
				} else {
					schemas.Add (node.Name, new HtmlSchema (node.Name, node.FullName, node.CompletionDocTypeName));
				}
			}
		}
		
		public static string DefaultDocTypeName {
			get { return "HTML 4.01 Transitional"; }
		}
		
		public static HtmlSchema DefaultDocType {
			get {
				if (schemas == null) Initialise ();
				return schemas[DefaultDocTypeName];
			}
		}
		
		public static IXmlCompletionProvider GetCompletionProvider (string docTypeName)
		{
			if (schemas == null) Initialise ();
			
			if (schemas.ContainsKey (docTypeName))
				return (schemas [docTypeName].CompletionProvider);
			return DefaultDocType.CompletionProvider;
		}
		
		public static HtmlSchema GetSchema (string docType)
		{
			if (schemas == null) Initialise ();
			
			if (!string.IsNullOrEmpty (docType))
				foreach (HtmlSchema schema in schemas.Values)
					if (docType.Contains (schema.Name))
						return schema;
			return null;
		}
		
		public static ICompletionData[] GetDocTypeCompletionData ()
		{
			if (schemas == null) Initialise ();
			
			List<ICompletionData> list = new List<ICompletionData> ();
			foreach (HtmlSchema item in schemas.Values)
				list.Add (new DocTypeCompletionData (item.Name, item.DocType));
			return list.ToArray ();
		}
	}
}
