// 
// XmlCompletionDataList.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using MonoDevelop.Ide.CodeCompletion;
using System.Text;
using System.Xml.Schema;
using System.Xml;

namespace MonoDevelop.Xml.Completion
{
	public class XmlCompletionDataList: CompletionDataList
	{
		HashSet<string> names = new HashSet<string> ();
		XmlNamespacePrefixMap nsMap;
		
		public XmlCompletionDataList (XmlNamespacePrefixMap nsMap)
		{
			this.nsMap = nsMap;
		}
		
		public XmlCompletionDataList ()
		{
			this.nsMap = new XmlNamespacePrefixMap ();
		}
		
		public void AddAttribute (XmlSchemaAttribute attribute)
		{
			string name = attribute.Name;
			if (name == null) {
				var ns = attribute.RefName.Namespace;
				if (string.IsNullOrEmpty (ns))
					return;
				var prefix = nsMap.GetPrefix (ns);
				if (prefix == null) {
					if (ns == "http://www.w3.org/XML/1998/namespace")
						prefix = "xml";
					else
						return;
				}
				name = attribute.RefName.Name;
				if (prefix.Length > 0)
					name = prefix + ":" + name;
			}
			if (!names.Add (name))
				return;
			string documentation = GetDocumentation (attribute.Annotation);
			Add (new XmlCompletionData (name, documentation, XmlCompletionData.DataType.XmlAttribute));
		}
		
		public void AddAttributeValue (string valueText)
		{
			Add (new XmlCompletionData (valueText, XmlCompletionData.DataType.XmlAttributeValue));
		}
		
		public void AddAttributeValue (string valueText, XmlSchemaAnnotation annotation)
		{
			string documentation = GetDocumentation (annotation);
			Add (new XmlCompletionData (valueText, documentation, XmlCompletionData.DataType.XmlAttributeValue));
		}		
		
		/// <summary>
		/// Adds an element completion data to the collection if it does not 
		/// already exist.
		/// </summary>
		public void AddElement (string name, string prefix, string documentation)
		{
			if (!names.Add (name))
				return;
			//FIXME: don't accept a prefix, accept a namespace and resolve it to a prefix
			if (prefix.Length > 0)
				name = String.Concat (prefix, ":", name);
			
			Add (new XmlCompletionData (name, documentation));				
		}
		
		/// <summary>
		/// Adds an element completion data to the collection if it does not 
		/// already exist.
		/// </summary>
		public void AddElement (string name, string prefix, XmlSchemaAnnotation annotation)
		{
			string documentation = GetDocumentation (annotation);
			AddElement (name, prefix, documentation);
		}
		
		/// <summary>
		/// Gets the documentation from the annotation element.
		/// </summary>
		/// <remarks>
		/// All documentation elements are added.  All text nodes inside
		/// the documentation element are added.
		/// </remarks>
		static string GetDocumentation (XmlSchemaAnnotation annotation)
		{
			if (annotation == null)
				return "";
			
			var documentationBuilder = new StringBuilder ();
			foreach (XmlSchemaObject schemaObject in annotation.Items) {
				var schemaDocumentation = schemaObject as XmlSchemaDocumentation;
				if (schemaDocumentation != null && schemaDocumentation.Markup != null) {
					foreach (XmlNode node in schemaDocumentation.Markup) {
						var textNode = node as XmlText;
						if (textNode != null && !string.IsNullOrEmpty (textNode.Data))
							documentationBuilder.Append (textNode.Data);
					}
				}
			}
			
			return documentationBuilder.ToString ();
		}
	}
}

