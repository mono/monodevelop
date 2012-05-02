// 
// MoonlightParser.cs
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
using System.Linq;
using System.Xml;

using MonoDevelop.Xml.StateEngine;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using MonoDevelop.Projects;

namespace MonoDevelop.Moonlight
{
	public class MoonlightParser : AbstractTypeSystemParser
	{
		public override ParsedDocument Parse (bool storeAst, string fileName, TextReader tr, Project project = null)
		{
			XmlParsedDocument doc = new XmlParsedDocument (fileName);
			try {
				Parser xmlParser = new Parser (new XmlFreeState (), true);
				xmlParser.Parse (tr);
				doc.XDocument = xmlParser.Nodes.GetRoot ();
				doc.Add (xmlParser.Errors);
				
				if (doc.XDocument != null && doc.XDocument.RootElement != null) {
					if (!doc.XDocument.RootElement.IsEnded)
						doc.XDocument.RootElement.End (xmlParser.Location);
					GenerateCU (doc);
				}
			}
			catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Unhandled error parsing xaml document", ex);
			}
			return doc;
		}
		
		static void GenerateCU (XmlParsedDocument doc)
		{
			if (doc.XDocument == null || doc.XDocument.RootElement == null) {
				doc.Add (new Error (ErrorType.Error, "No root node found.", 1, 1));
				return;
			}

			XAttribute rootClass = doc.XDocument.RootElement.Attributes [new XName ("x", "Class")];
			if (rootClass == null) {
				doc.Add (new Error (ErrorType.Error, "Root node does not contain an x:Class attribute.", 1, 1));
				return;
			}

			bool isApplication = doc.XDocument.RootElement.Name.Name == "Application";
			
			string rootNamespace, rootType, rootAssembly;
			XamlG.ParseXmlns (rootClass.Value, out rootType, out rootNamespace, out rootAssembly);

			var cu = new DefaultParsedDocument (doc.FileName);

			DomRegion rootRegion = doc.XDocument.RootElement.Region;
			if (doc.XDocument.RootElement.IsClosed)
				rootRegion = new DomRegion (doc.XDocument.RootElement.Region.FileName, doc.XDocument.RootElement.Region.Begin, doc.XDocument.RootElement.ClosingTag.Region.End); 
			
			var declType = new DefaultUnresolvedTypeDefinition (rootNamespace, rootType) {
				Kind = TypeKind.Class,
				Accessibility = Accessibility.Public,
				Region = rootRegion
			};
			cu.TopLevelTypeDefinitions.Add (declType);
			
			var initcomp = new DefaultUnresolvedMethod (declType, "InitializeComponent") {
				ReturnType = KnownTypeReference.Void,
				Accessibility = Accessibility.Public
			};
			declType.Members.Add (initcomp);
			
			var _contentLoaded = new DefaultUnresolvedField (declType, "_contentLoaded") {
				ReturnType = KnownTypeReference.Boolean
			};
// was missing in the original code: correct ? 
//			declType.Fields.Add (_contentLoaded);

			if (isApplication)
				return;
			
//			cu.Add (new DomUsing (DomRegion.Empty, "System"));
//			cu.Add (new DomUsing (DomRegion.Empty, "System.Windows"));
//			cu.Add (new DomUsing (DomRegion.Empty, "System.Windows.Controls"));
//			cu.Add (new DomUsing (DomRegion.Empty, "System.Windows.Documents"));
//			cu.Add (new DomUsing (DomRegion.Empty, "System.Windows.Input"));
//			cu.Add (new DomUsing (DomRegion.Empty, "System.Windows.Media"));
//			cu.Add (new DomUsing (DomRegion.Empty, "System.Windows.Media.Animation"));
//			cu.Add (new DomUsing (DomRegion.Empty, "System.Windows.Shapes"));
//			cu.Add (new DomUsing (DomRegion.Empty, "System.Windows.Controls.Primitives"));
			
	//		Dictionary<string,string> namespaceMap = new Dictionary<string, string> ();
	//		namespaceMap["x"] = "http://schemas.microsoft.com/winfx/2006/xaml";
			
			XName nameAtt = new XName ("x", "Name");
			
			foreach (XElement el in doc.XDocument.RootElement.AllDescendentElements) {
				XAttribute name = el.Attributes [nameAtt];
				if (name != null && name.IsComplete) {
					string type = ResolveType (el);
					if (type == null || type.Length == 0)
						cu.Add (new Error (ErrorType.Error, "Could not find namespace for '" + el.Name.FullName + "'.", el.Region.Begin));
					else
						declType.Members.Add (new DefaultUnresolvedField (declType, name.Value) {
							Accessibility = Accessibility.Internal,
							Region = el.Region,
							ReturnType = new DefaultUnresolvedTypeDefinition (type)
						});
				}
			}
		}
		
		static string GetNamespace (XElement el)
		{
			XName attName;
			if (el.Name.HasPrefix) {
				attName = new XName ("xmlns", el.Name.Prefix);
		 	} else {
				attName = new XName ("xmlns");
				XAttribute att = el.Attributes[attName];
				if (att != null)
						return att.Value;
			}
			
			foreach (XNode node in el.Parents) {
				XElement parentElement = node as XElement;
				if (parentElement != null) {
					XAttribute att = parentElement.Attributes[attName];
					if (att != null)
						return att.Value;
				}
			}
			return null;
		}
		
		static string PresentationNS = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
		
		static string ResolveType (XElement el)
		{
			string name = el.Name.Name;
			string ns = GetNamespace (el);
			if (ns != null) {
				if (ns.StartsWith ("clr-namespace:")) {
					int end = ns.IndexOf (';', 14);
					if (end > 0)
						return ns.Substring (14, ns.Length - end + 1);
					else
						return (ns.Substring (14));
				} else if (ns == PresentationNS) {
					return el.Name.Name;
				}
			}
			return null;
		}
	}
}
