// 
// PageInfo.cs
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
using MonoDevelop.AspNet.Parser.Dom;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

using MonoDevelop.AspNet.StateEngine;
using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.AspNet.Parser
{
	
	public class PageInfo
	{
		public PageInfo ()
		{
		}
		
		List<RegisterDirective> registeredTags = new List<RegisterDirective> ();
		List<string> imports = new List<string> ();
		List<string> implements = new List<string> ();
		List<AssemblyDirective> assemblies = new List<AssemblyDirective> ();
		
		public string InheritedClass { get; private set; }
		public string ClassName { get; private set; }
		public string CodeBehindFile { get; private set; }
		public string CodeFile { get; private set; }
		public string Language { get; private set; }
		public string DocType { get; private set; }
		public string MasterPageFile { get; private set; }
		public string MasterPageTypeName { get; private set; }
		public string MasterPageTypeVPath { get; private set; }
		public WebSubtype Subtype { get; private set; }
		public IList<RegisterDirective> RegisteredTags { get { return registeredTags; } }
		public IList<string> Imports { get { return imports; } }
		public IList<string> Implements { get { return imports; } }
		public IList<AssemblyDirective> Assemblies { get { return assemblies; } }
		
		public IEnumerable<Error> Populate (RootNode node, List<Error> errors)
		{
			var visitor = new PageInfoVisitor (this, errors);
			node.AcceptVisit (visitor);
			return visitor.Errors;
		}

		#region XDocument parsing

		public void Populate (XDocument xDoc, List<Error> errors)
		{
			XDocDirectiveParser xDocParser = new XDocDirectiveParser (this, errors);
			xDocParser.Parse (xDoc);
		}

		private class XDocDirectiveParser
		{
			PageInfo info;
			List<Error> errors;

			public XDocDirectiveParser (PageInfo inf, List<Error> err)
			{
				info = inf;
				errors = err;
			}

			public void Parse (XDocument xDoc)
			{
				foreach (XNode node in xDoc.AllDescendentNodes) {
					if (node is AspNetDirective) {
						HandleDirective (node as AspNetDirective);
					} else if (node is XDocType) {
						HandleDocType (node as XDocType);
					} else {
						// quit the parsing when reached the html nodes
						return;
					}
				}
			}

			string GetAttributeValueCI (XAttributeCollection attributes, string key)
			{
				XName nameKey = new XName (key.ToLowerInvariant ());
	
				foreach (XAttribute attr in attributes) {
					if (attr.Name.ToLower () == nameKey)
						return attr.Value;
				}
				return string.Empty;
			}

			void HandleDirective (AspNetDirective directive)
			{
				switch (directive.Name.Name.ToLowerInvariant ()) {
				case "page":
					info.MasterPageFile = GetAttributeValueCI (directive.Attributes, "masterpagefile");
					SetSubtype (WebSubtype.WebForm, directive);
					break;
				case "control":
					SetSubtype (WebSubtype.WebControl, directive);
					break;
				case "webservice":
					SetSubtype (WebSubtype.WebService, directive);
					break;
				case "webhandler":
					SetSubtype (WebSubtype.WebHandler, directive);
					break;
				case "application":
					SetSubtype (WebSubtype.Global, directive);
					break;
				case "master":
					SetSubtype (WebSubtype.MasterPage, directive);
					break;
				case "mastertype":
					if (info.MasterPageTypeVPath != null || info.MasterPageTypeName != null) {
						errors.Add (new Error (ErrorType.Error, "Unexpected second mastertype directive", directive.Region));
						return;
					}
					info.MasterPageTypeName = GetAttributeValueCI (directive.Attributes, "typename");
					info.MasterPageTypeVPath = GetAttributeValueCI (directive.Attributes, "virtualpath");
					if (string.IsNullOrEmpty (info.MasterPageTypeName) == string.IsNullOrEmpty (info.MasterPageTypeVPath))
						errors.Add (new Error (
							ErrorType.Error,
							"Mastertype directive must have non-empty 'typename' or 'virtualpath' attribute",
							directive.Region
						)
						);
					break;
				case "register":
					string tagPrefix = GetAttributeValueCI (directive.Attributes, "tagprefix");
					string tagName = GetAttributeValueCI (directive.Attributes, "tagname");
					string src = GetAttributeValueCI (directive.Attributes, "src");
					string nspace = GetAttributeValueCI (directive.Attributes, "namespace");
					string assembly = GetAttributeValueCI (directive.Attributes, "assembly");
					if (!string.IsNullOrEmpty (tagPrefix)) {
						if (!string.IsNullOrEmpty (tagName) && !string.IsNullOrEmpty (src))
							info.registeredTags.Add (new ControlRegisterDirective (tagPrefix, tagName, src));
						else if (!string.IsNullOrEmpty (nspace) && !string.IsNullOrEmpty (assembly))
							info.registeredTags.Add (new AssemblyRegisterDirective (tagPrefix, nspace, assembly));
					}
					break;
				case "assembly":
					var assm = new AssemblyDirective (
						GetAttributeValueCI (directive.Attributes, "name"),
						GetAttributeValueCI (directive.Attributes, "src"));
					if (assm.IsValid ())
						info.assemblies.Add (assm);
					else
						errors.Add (new Error (
							ErrorType.Error,
							"Assembly directive must have non-empty 'name' or 'src' attribute",
							directive.Region
						)
						);
					break;
				case "import":
					string ns = GetAttributeValueCI (directive.Attributes, "namespace");
					if (!string.IsNullOrEmpty (ns))
						info.imports.Add (ns);
					else
						errors.Add (new Error (
							ErrorType.Error,
							"Import directive must have non-empty 'namespace' attribute",
							directive.Region
						)
						);
					break;
				case "implements":
					string interf = GetAttributeValueCI (directive.Attributes, "interface");
					if (!string.IsNullOrEmpty (interf))
						info.implements.Add (interf);
					else
						errors.Add (new Error (
							ErrorType.Error,
							"Implements directive must have non-empty 'interface' attribute",
							directive.Region
						)
						);
					break;
				default:
					break;
				}
			}

			void SetSubtype (WebSubtype type, AspNetDirective directive)
			{
				if (info.Subtype != WebSubtype.None) {
					errors.Add (new Error (ErrorType.Error, "Unexpected directive " + directive.Name.FullName, directive.Region));
					return;
				}
				
				info.Subtype = type;
							
				info.InheritedClass = GetAttributeValueCI (directive.Attributes, "inherits");
				if (info.ClassName == null)
					info.ClassName = GetAttributeValueCI (directive.Attributes, "classname");
				info.CodeBehindFile = GetAttributeValueCI (directive.Attributes, "codebehind");
				info.Language = GetAttributeValueCI (directive.Attributes, "language");
				info.CodeFile = GetAttributeValueCI (directive.Attributes, "codefile");
			}

			void HandleDocType (XDocType docType)
			{
				info.DocType = "<!DOCTYPE html";
				if (!string.IsNullOrEmpty (docType.PublicFpi))
					info.DocType += " \"" + docType.PublicFpi + "\"";
				if (!string.IsNullOrEmpty (docType.Uri))
					info.DocType += " \"" + docType.Uri + "\"";
				info.DocType += ">";
			}
		}

		#endregion

		private class PageInfoVisitor : Visitor
		{
			PageInfo info;
			List<Error> errors;
			
			public PageInfoVisitor (PageInfo info, List<Error> errors)
			{
				this.info = info;
				this.errors = errors;
			}
			
			public List<Error> Errors { get { return errors; } }
			
			public override void Visit (DirectiveNode node)
			{
				var atts = node.Attributes;
				
				switch (node.Name.ToLowerInvariant ()) {
				case "page":
					SetSubtype (WebSubtype.WebForm, node);
					if (atts == null)
						return;
					info.MasterPageFile = atts ["masterpagefile"] as string;
					break;
				case "control":
					SetSubtype (WebSubtype.WebControl, node);
					break;
				case "webservice":
					SetSubtype (WebSubtype.WebService, node);
					break;
				case "webhandler":
					SetSubtype (WebSubtype.WebHandler, node);
					break;
				case "application":
					SetSubtype (WebSubtype.Global, node);
					break;
				case "master":
					SetSubtype (WebSubtype.MasterPage, node);
					break;
				case "mastertype":
					if (info.MasterPageTypeVPath != null || info.MasterPageTypeName != null) {
						Add (ErrorType.Error, node, "Unexpected second mastertype directive", node.Name);
						return;
					}
					if (atts == null)
						return;
					info.MasterPageTypeName = atts["typename"] as string;
					info.MasterPageTypeVPath = atts["virtualpath"] as string;
					if (string.IsNullOrEmpty (info.MasterPageTypeName) == string.IsNullOrEmpty (info.MasterPageTypeVPath))
						Add (ErrorType.Error, node, "Mastertype directive must have non-empty 'typename' or 'virtualpath' attribute");
					break;
				case "register":
					if (atts == null)
						return;
					if (atts ["TagPrefix"] != null) {
						if ((atts ["TagName"] != null) && (atts ["Src"] != null))
							info.registeredTags.Add (new ControlRegisterDirective (node));
						else if ((atts ["Namespace"] != null) && (atts ["Assembly"] != null))
							info.registeredTags.Add (new AssemblyRegisterDirective (node));
					}
					break;
				case "assembly":
					if (atts == null)
						return;
					var assembly = new AssemblyDirective (atts ["name"] as string, atts ["src"] as string);
					if (assembly.IsValid ())
						info.assemblies.Add (assembly);
					else
						Add (ErrorType.Error, node, "Assembly directive must have non-empty 'name' or 'src' attribute");
					break;
				case "import":
					if (atts == null)
						return;
					var ns = atts ["namespace"] as string;
					if (!string.IsNullOrEmpty (ns))
						info.imports.Add (ns);
					else
						Add (ErrorType.Error, node, "Import directive must have non-empty 'namespace' attribute");
					break;
				case "implements":
					if (atts == null)
						return;
					var interf = atts ["interface"] as string;
					if (!string.IsNullOrEmpty (interf))
						info.implements.Add (interf);
					else
						Add (ErrorType.Error, node, "Implements directive must have non-empty 'interface' attribute");
					break;
				default:
					break;
				}
			}
			
			void Add (ErrorType type, Node node, string message, params object[] args)
			{
				errors.Add (new Error (type, string.Format (message, args), node.Location.BeginLine, node.Location.BeginColumn));
			}
			
			void SetSubtype (WebSubtype type, DirectiveNode node)
			{
				if (info.Subtype != WebSubtype.None) {
					Add (ErrorType.Error, node, "Unexpected directive {0}", node.Name);
					return;
				}
				
				info.Subtype = type;
				
				if (node.Attributes == null)
					return;
				
				info.InheritedClass = node.Attributes ["inherits"] as string;
				if (info.ClassName == null)
					info.ClassName = node.Attributes ["classname"] as string;
				info.CodeBehindFile = node.Attributes ["codebehind"] as string;
				info.Language = node.Attributes ["language"] as string;
				info.CodeFile = node.Attributes ["codefile"] as string;
			}
			
			public override void Visit (TextNode node)
			{
				int start = node.Text.IndexOf ("<!DOCTYPE");
				if (start < 0)
					return;
				int end = node.Text.IndexOf (">", start);
				info.DocType = node.Text.Substring (start, end - start + 1);
				QuickExit = true;
			}
	
			
			public override void Visit (TagNode node)
			{
				//as soon as tags are declared, doctypes and directives must been set
				QuickExit = true;
			}
		}
	}
	
	public abstract class RegisterDirective
	{
		public RegisterDirective (string tagPrefix)
		{
			this.TagPrefix = tagPrefix;
		}
		
		public RegisterDirective (DirectiveNode node)
		{
			TagPrefix = (string) node.Attributes ["TagPrefix"];
		}
		
		public string TagPrefix { get; private set; }
		
		public virtual bool IsValid ()
		{
			if (string.IsNullOrEmpty (TagPrefix))
				return false;
			
			foreach (char c in TagPrefix)
				if (!Char.IsLetterOrDigit (c))
					return false;
			
			return true;
		}
	}
	
	public class AssemblyRegisterDirective : RegisterDirective
	{
		public AssemblyRegisterDirective (string tagPrefix, string @namespace, string assembly) : base (tagPrefix)
		{
			this.Namespace = @namespace;
			this.Assembly = assembly;
		}
		
		public AssemblyRegisterDirective (DirectiveNode node)
			: base (node)
		{
			Namespace = (string) node.Attributes ["Namespace"];
			Assembly = (string) node.Attributes ["Assembly"];
		}
		
		public string Namespace { get; private set; }
		public string Assembly { get; private set; }
		
		public override string ToString ()
		{	
			return String.Format ("<%@ Register TagPrefix=\"{0}\" Namespace=\"{1}\" Assembly=\"{2}\" %>", TagPrefix, Namespace, Assembly);
		}
		
		public override bool IsValid ()
		{
			if (string.IsNullOrEmpty (Assembly) || string.IsNullOrEmpty (Namespace) || !base.IsValid ())
				return false;
			return true;
		}
	}
	
	public class ControlRegisterDirective : RegisterDirective
	{
		public ControlRegisterDirective (string tagPrefix, string tagName, string src) : base (tagPrefix)
		{
			this.TagName = tagName;
			this.Src = src;
		}
		
		public ControlRegisterDirective (DirectiveNode node)
			: base (node)
		{
			TagName = (string) node.Attributes ["TagName"];
			Src = (string) node.Attributes ["Src"];
		}
		
		public string TagName { get; private set; }
		public string Src { get; private set; }
		
		public override string ToString ()
		{	
			return String.Format ("<%@ Register {0}=\"{1}\" {2}=\"{3}\" {4}=\"{5}\" %>", "TagPrefix", TagPrefix, "TagName", TagName, "Src", Src);
		}
		
		public override bool IsValid ()
		{
			if (string.IsNullOrEmpty (TagName) || string.IsNullOrEmpty (Src) || !base.IsValid ())
				return false;
			return true;
		}
	}
	
	public class AssemblyDirective
	{			
		public AssemblyDirective (string name, string src)
		{
			this.Name = name;
			this.Src = src;
		}		
		
		public string Name { get; private set; }
		public string Src { get; private set; }
		
		public bool IsValid ()
		{
			return string.IsNullOrEmpty (Name) ^ string.IsNullOrEmpty (Src);
		}
	}
		
}
