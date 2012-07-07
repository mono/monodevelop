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

		#region XDocument parsing

		public void Populate (XDocument xDoc, List<Error> errors)
		{
			foreach (XNode node in xDoc.AllDescendentNodes) {
				if (node is AspNetDirective) {
					HandleDirective (node as AspNetDirective, errors);
				} else if (node is XDocType) {
					HandleDocType (node as XDocType);
				} else if (node is XElement) {
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

		void HandleDirective (AspNetDirective directive, List<Error> errors)
		{
			switch (directive.Name.Name.ToLowerInvariant ()) {
			case "page":
				MasterPageFile = GetAttributeValueCI (directive.Attributes, "masterpagefile");
				SetSubtype (WebSubtype.WebForm, directive, errors);
				break;
			case "control":
				SetSubtype (WebSubtype.WebControl, directive, errors);
				break;
			case "webservice":
				SetSubtype (WebSubtype.WebService, directive, errors);
				break;
			case "webhandler":
				SetSubtype (WebSubtype.WebHandler, directive, errors);
				break;
			case "application":
				SetSubtype (WebSubtype.Global, directive, errors);
				break;
			case "master":
				SetSubtype (WebSubtype.MasterPage, directive, errors);
				break;
			case "mastertype":
				if (MasterPageTypeVPath != null || MasterPageTypeName != null) {
					errors.Add (new Error (ErrorType.Error, "Unexpected second mastertype directive", directive.Region));
					return;
				}
				MasterPageTypeName = GetAttributeValueCI (directive.Attributes, "typename");
				MasterPageTypeVPath = GetAttributeValueCI (directive.Attributes, "virtualpath");
				if (string.IsNullOrEmpty (MasterPageTypeName) == string.IsNullOrEmpty (MasterPageTypeVPath))
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
						registeredTags.Add (new ControlRegisterDirective (tagPrefix, tagName, src));
					else if (!string.IsNullOrEmpty (nspace) && !string.IsNullOrEmpty (assembly))
						registeredTags.Add (new AssemblyRegisterDirective (tagPrefix, nspace, assembly));
				}
				break;
			case "assembly":
				var assm = new AssemblyDirective (
					GetAttributeValueCI (directive.Attributes, "name"),
					GetAttributeValueCI (directive.Attributes, "src"));
				if (assm.IsValid ())
					assemblies.Add (assm);
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
					imports.Add (ns);
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
					implements.Add (interf);
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

		void SetSubtype (WebSubtype type, AspNetDirective directive, List<Error> errors)
		{
			if (Subtype != WebSubtype.None) {
				errors.Add (new Error (ErrorType.Error, "Unexpected directive " + directive.Name.FullName, directive.Region));
				return;
			}
			
			Subtype = type;
						
			InheritedClass = GetAttributeValueCI (directive.Attributes, "inherits");
			if (ClassName == null)
				ClassName = GetAttributeValueCI (directive.Attributes, "classname");
			CodeBehindFile = GetAttributeValueCI (directive.Attributes, "codebehind");
			Language = GetAttributeValueCI (directive.Attributes, "language");
			CodeFile = GetAttributeValueCI (directive.Attributes, "codefile");
		}

		void HandleDocType (XDocType docType)
		{
			DocType = "<!DOCTYPE html";
			if (!string.IsNullOrEmpty (docType.PublicFpi))
				DocType += " \"" + docType.PublicFpi + "\"";
			if (!string.IsNullOrEmpty (docType.Uri))
				DocType += " \"" + docType.Uri + "\"";
			DocType += ">";
		}

		#endregion
	}
	
	public abstract class RegisterDirective
	{
		public RegisterDirective (string tagPrefix)
		{
			this.TagPrefix = tagPrefix;
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
