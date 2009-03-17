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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Moonlight
{
	
	
	public class MoonlightParser : AbstractParser
	{
		
		public MoonlightParser () : base (null, "application/xaml+xml")
		{
		}
		
		public override bool CanParse (string fileName)
		{
			return fileName.EndsWith (".xaml");
		}
		
		public override ParsedDocument Parse (ProjectDom dom, string fileName, string fileContent)
		{
			XmlParsedDocument doc = new XmlParsedDocument (fileName);
			TextReader tr = new StringReader (fileContent);
			try {
				Parser xmlParser = new Parser (new XmlFreeState (), true);
				xmlParser.Parse (tr);
				doc.XDocument = xmlParser.Nodes.GetRoot ();
				doc.Add (xmlParser.Errors);
				
				if (doc.XDocument != null || doc.XDocument.RootElement != null) {
					if (!doc.XDocument.RootElement.IsEnded)
						doc.XDocument.RootElement.End (xmlParser.Location);
					GenerateCU (doc);
				}
			}
			catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Unhandled error parsing xaml document", ex);
			}
			finally {
				if (tr != null)
					tr.Dispose ();
			}
			return doc;
		}
		
		static void GenerateCU (XmlParsedDocument doc)
		{
			if (doc.XDocument == null || doc.XDocument.RootElement == null) {
				doc.Add (new Error (ErrorType.Error, 1, 1, "No root node found."));
				return;
			}

			XAttribute rootClass = doc.XDocument.RootElement.Attributes [new XName ("x", "Class")];
			if (rootClass == null) {
				doc.Add (new Error (ErrorType.Error, 1, 1, "Root node does not contain an x:Class attribute."));
				return;
			}

			bool isApplication = doc.XDocument.RootElement.Name.Name == "Application";
			
			string rootNamespace, rootType, rootAssembly;
			XamlG.ParseXmlns (rootClass.Value, out rootType, out rootNamespace, out rootAssembly);
			
			CompilationUnit cu = new CompilationUnit (doc.FileName);
			doc.CompilationUnit = cu;

			DomRegion rootRegion = doc.XDocument.RootElement.Region;
			if (doc.XDocument.RootElement.IsClosed)
				rootRegion.End = doc.XDocument.RootElement.ClosingTag.Region.End;
			
			DomType declType = new DomType (cu, ClassType.Class, Modifiers.Partial | Modifiers.Public, rootType,
			                                doc.XDocument.RootElement.Region.Start, rootNamespace, rootRegion);
			cu.Add (declType);
			
			DomMethod initcomp = new DomMethod ();
			initcomp.Name = "InitializeComponent";
			initcomp.Modifiers = Modifiers.Public;
			initcomp.ReturnType = DomReturnType.Void;
			declType.Add (initcomp);
			
			DomField _contentLoaded = new DomField ("_contentLoaded");
			_contentLoaded.ReturnType = new DomReturnType ("System.Boolean");

			if (isApplication)
				return;
			
			cu.Add (new DomUsing (new DomRegion (), "System"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows.Controls"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows.Documents"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows.Input"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows.Media"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows.Media.Animation"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows.Shapes"));
			
//			Dictionary<string,string> namespaceMap = new Dictionary<string, string> ();
//			namespaceMap["x"] = "http://schemas.microsoft.com/winfx/2006/xaml";
			
			XName nameAtt = new XName ("x", "Name");
			
			IEnumerable<DomField> fields =
				from XElement el in doc.XDocument.RootElement.AllDescendentElements
				let name = el.Attributes [nameAtt]
				where name != null
				select new DomField (name.Value, Modifiers.Internal, el.Region.Start,
					             new DomReturnType (ResolveType (el)));
			
			foreach (DomField f in fields)
				declType.Add (f);
		}
		
		static string ResolveType (XElement el)
		{
			//FIXME implement
//			string name = attr.Value;
//			string ns = XamlG.GetNamespace (node);
//			string type = node.LocalName;
//			
//			if (ns != null)
//				type = String.Concat (ns, ".", type);
			return el.Name.Name;
		}
	}
}
