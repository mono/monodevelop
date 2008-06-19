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
		
		#region Service loading
		
		static object initLock = new object ();
		static System.Threading.Thread loadingThread;
		
		public static void Initialise ()
		{
			if (schemas != null)
				return;
			
			schemas = new Dictionary<string, HtmlSchema> ();
			
			//load all the schemas from addin points
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
			
			//initialise the default backup schema if it doesn't exist already
			if (!schemas.ContainsKey (DefaultDocTypeName)) {
				HtmlSchema defaultSubstProvider = schemas["XHTML 1.0 Transitional"];
				IXmlCompletionProvider provider;
				if (defaultSubstProvider != null) {
					//start the threaded schema loading
					LoadSchema (defaultSubstProvider, true);
					provider = defaultSubstProvider.CompletionProvider;
				} else {
					LoggingService.LogWarning ("Completion schema for default HTML doctype not found.");
					provider = new EmptyXmlCompletionProvider ();
				}
				
				schemas[DefaultDocTypeName] = new HtmlSchema ("HTML 4.01 Transitional",
				    "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\"\n"
				    + "\"http://www.w3.org/TR/html4/loose.dtd\">",
				    provider);
			}
			
			MonoDevelop.Core.LoggingService.LogDebug ("HtmlSchemaService initialised");
		}
		
		#endregion
		
		public static string DefaultDocTypeName {
			get { return "HTML 4.01 Transitional"; }
		}
		
		public static HtmlSchema DefaultDocType {
			get {
				if (schemas == null)
					Initialise ();
				return schemas[DefaultDocTypeName];
			}
		}
		
		public static HtmlSchema GetSchema (string docType)
		{
			return GetSchema (docType, false);
		}
		
		//if lazy==true, then if the schema is lazily compiled, it gets force-compiled in a thread and null is returned
		public static HtmlSchema GetSchema (string docType, bool lazy)
		{
			if (schemas == null)
				Initialise ();
			
			if (!string.IsNullOrEmpty (docType))
				foreach (HtmlSchema schema in schemas.Values)
					if (docType.Contains (schema.Name))
						return LoadSchema (schema, lazy);
			return null;
		}
		
		//if lazy == true, returns null if the schema isn't loaded yet
		static HtmlSchema LoadSchema (HtmlSchema schema, bool lazy)
		{
			ILazilyLoadedProvider lazyProv = schema.CompletionProvider as ILazilyLoadedProvider;
			if (lazyProv == null || lazyProv.IsLoaded) {
				return schema;
			} else {
				//FIXME: actually implement threaded loading and return null if loading && lazy
				lazyProv.EnsureLoaded ();
				return schema;
			}
		}
		
		public static IXmlCompletionProvider LazyGetProvider (HtmlSchema schema)
		{
			//get the provided schema, if it's loaded
			HtmlSchema hs = LoadSchema (schema, true);
			//fall back to the defaul provider
			if (hs == null)
				hs = LoadSchema (DefaultDocType, true);
			//fall back to a blank provider
			return new EmptyXmlCompletionProvider ();
		}
		
		public static IEnumerable<DocTypeCompletionData> DocTypeCompletionData {
			get {
				if (schemas == null)
					Initialise ();
				
				foreach (HtmlSchema item in schemas.Values)
					yield return new DocTypeCompletionData (item.Name, item.DocType);
			}
		}
	}
}
