// 
// BaseHtmlEditorExtension.cs
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
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.AspNet;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.AspNet.StateEngine;
using MonoDevelop.Html;
using MonoDevelop.DesignerSupport;
using MonoDevelop.Xml.StateEngine; 

namespace MonoDevelop.AspNet.Gui
{
	
	public abstract class BaseHtmlEditorExtension : MonoDevelop.XmlEditor.Gui.BaseXmlEditorExtension
	{
		HtmlSchema schema;
		bool resolvedDocType;
		
		protected HtmlSchema Schema {
			get {
				if (resolvedDocType)
					return schema;
				
				resolvedDocType = true;
				
				if (DocType == null || String.IsNullOrEmpty (DocType.PublicFpi)) {
					LoggingService.LogDebug ("HTML completion found no doctype, using default");
					schema = HtmlSchemaService.DefaultDocType;
					return schema;
				}
				
				schema = HtmlSchemaService.GetSchema (DocType.PublicFpi, true);
				if (schema != null) {
					LoggingService.LogDebug ("HTML completion using doctype {0}", schema.Name);
				} else {
					LoggingService.LogDebug ("HTML completion could not find schema for doctype {0} so is falling back to default", DocType);
					schema = HtmlSchemaService.DefaultDocType;
				}
				return schema;
			}
		}
		
		protected override void OnDocTypeChanged ()
		{
			resolvedDocType = false;
		}
		
		#region Setup and teardown
		
		protected override RootState CreateRootState ()
		{
			return new XmlFreeState (new HtmlTagState (true), new HtmlClosingTagState (true));
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			
			//ensure that the schema service is initialised, or code completion may take a couple of seconds to trigger
			HtmlSchemaService.Initialise ();
		}
		
		#endregion
		
		protected override void GetElementCompletions (CompletionDataList list)
		{
			XName parentName = GetParentElementName (0);
			AddHtmlTagCompletionData (list, Schema, parentName.ToLower ());
			AddMiscBeginTags (list);
			
			//FIXME: don't show this after any elements
			if (DocType == null)
				list.Add ("!DOCTYPE", "md-literal", GettextCatalog.GetString ("Document type"));
		}
		
		protected override CompletionDataList GetDocTypeCompletions ()
		{
			return new CompletionDataList (from DocTypeCompletionData dat
			                               in HtmlSchemaService.DocTypeCompletionData
			                               select (CompletionData) dat);
		}
		
		protected override CompletionDataList GetAttributeCompletions (IAttributedXObject attributedOb,
			Dictionary<string, string> existingAtts)
		{
			if (attributedOb is XElement && !attributedOb.Name.HasPrefix) {
				var list = new CompletionDataList ();
				AddHtmlAttributeCompletionData (list, Schema, attributedOb.Name, existingAtts);
				return list;
			}
			return null;
		}
		
		protected override CompletionDataList GetAttributeValueCompletions (IAttributedXObject ob, XAttribute att)
		{
			if (ob is XElement && !ob.Name.HasPrefix) {
				var list = new CompletionDataList ();
				AddHtmlAttributeValueCompletionData (list, Schema, ob.Name, att.Name);
				return list;
			}
			return null;
		}
		
		#region HTML data
		
		protected static void AddHtmlTagCompletionData (CompletionDataList list, HtmlSchema schema, XName parentName)
		{
			if (schema == null)
				return;
			
			if (parentName.IsValid) {
				list.AddRange (schema.CompletionProvider.GetChildElementCompletionData (parentName.FullName));
			} else {
				list.AddRange (schema.CompletionProvider.GetElementCompletionData ());
			}			
		}
		
		protected void AddHtmlAttributeCompletionData (CompletionDataList list, HtmlSchema schema, 
		    XName tagName, Dictionary<string, string> existingAtts)
		{
			//add atts only if they're not aready in the tag
			foreach (CompletionData datum in schema.CompletionProvider.GetAttributeCompletionData (tagName.FullName))
				if (existingAtts == null || !existingAtts.ContainsKey (datum.DisplayText))
					list.Add (datum);
		}
		
		protected void AddHtmlAttributeValueCompletionData (CompletionDataList list, HtmlSchema schema, 
		    XName tagName, XName attributeName)
		{
			list.AddRange (schema.CompletionProvider.GetAttributeValueCompletionData (tagName.FullName, 
			                                                                          attributeName.FullName));
		}
		
		#endregion
	}
}
