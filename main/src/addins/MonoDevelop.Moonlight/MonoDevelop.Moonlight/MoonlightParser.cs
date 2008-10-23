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
using System.IO;
using System.Xml;

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
		
		public override ParsedDocument Parse (string fileName, string fileContent)
		{
			using (TextReader tr = new StringReader (fileContent))
			{
				ParsedDocument doc = new ParsedDocument (fileName);
				try {
					GenerateCU (fileName, tr, doc);
				} catch (Exception ex) {
					MonoDevelop.Core.LoggingService.LogError ("Unhandled error parsing xaml document", ex);
				}
				return doc;
			}
		}
		
		static ParsedDocument GenerateCU (string fileName, TextReader fileContents, ParsedDocument doc)
		{
			XmlDocument xmldoc = new XmlDocument ();
			try {
				xmldoc.Load (fileContents);
			} catch (XmlException ex) {
				doc.Add (new Error (ErrorType.Error, ex.LineNumber, ex.LinePosition, ex.Message));
				return doc;
			}

			XmlNamespaceManager nsmgr = new XmlNamespaceManager (xmldoc.NameTable);
			nsmgr.AddNamespace("x", "http://schemas.microsoft.com/winfx/2006/xaml");

			XmlNode root = xmldoc.SelectSingleNode ("/*", nsmgr);
			if (root == null) {
				doc.Add (new Error (ErrorType.Error, 1, 1, "No root node found."));
				return doc;
			}

			XmlAttribute root_class = root.Attributes ["x:Class"];
			if (root_class == null) {
				doc.Add (new Error (ErrorType.Error, 1, 1, "Root node does not contain an x:Class attribute."));
				return doc;
			}

			bool is_application = root.LocalName == "Application";
			string root_ns;
			string root_type;
			string root_asm;

			XamlG.ParseXmlns (root_class.Value, out root_type, out root_ns, out root_asm);
			
			CompilationUnit cu = new CompilationUnit (fileName);
			doc.CompilationUnit = cu;
			
			cu.Add (new DomUsing (new DomRegion (), "System"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows.Controls"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows.Documents"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows.Input"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows.Media"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows.Media.Animation"));
			cu.Add (new DomUsing (new DomRegion (), "System.Windows.Shapes"));

			DomType decl_type = new DomType (cu, ClassType.Class, Modifiers.Partial | Modifiers.Public,
			                                 root_type, new DomLocation (1, 1), root_ns, new DomRegion (1, 1));
			cu.Add (decl_type);
			
			DomMethod initcomp = new DomMethod ();
			initcomp.Name = "InitializeComponent";
			initcomp.Modifiers = Modifiers.Public;
			initcomp.ReturnType = DomReturnType.Void;
			decl_type.Add (initcomp);
			
			DomField _contentLoaded = new DomField ("_contentLoaded");
			_contentLoaded.ReturnType = new DomReturnType ("System.Boolean");

			if (is_application)
				return doc;
			
			XmlNodeList names = root.SelectNodes ("//*[@x:Name]", nsmgr);
			foreach (XmlNode node in names)	{
				// Don't take the root canvas
				if (node == root)
					continue;

				XmlAttribute attr = node.Attributes ["x:Name"];
				string name = attr.Value;
				string ns = XamlG.GetNamespace (node);
				string type = node.LocalName;

				if (ns != null)
					type = String.Concat (ns, ".", type);
				
				DomField field = new DomField (name, Modifiers.Internal, DomLocation.Empty,
					                       new DomReturnType (type));
				decl_type.Add (field);
			}
			
			return doc;
		}
	}
}
