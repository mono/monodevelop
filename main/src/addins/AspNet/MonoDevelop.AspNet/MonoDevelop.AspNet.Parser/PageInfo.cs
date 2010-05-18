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
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.AspNet.Parser
{
	
	public class PageInfo
	{
		public PageInfo ()
		{
		}
		
		protected List<RegisterDirective> registeredTags = new List<RegisterDirective> ();
		protected List<string> imports = new List<string> ();
		
		public string InheritedClass { get; private set; }
		public string CodeBehindFile { get; private set; }
		public string CodeFile { get; private set; }
		public string Language { get; private set; }
		public string DocType { get; private set; }
		public string MasterPageFile { get; private set; }
		public string MasterPageTypeName { get; private set; }
		public string MasterPageTypeVPath { get; private set; }
		public WebSubtype Subtype { get; private set; }
		public IEnumerable<RegisterDirective> RegisteredTags { get { return registeredTags; } }
		public IEnumerable<string> Imports { get { return imports; } }
		
		public IEnumerable<Error> Populate (RootNode node, List<Error> errors)
		{
			var visitor = new PageInfoVisitor (this, errors);
			node.AcceptVisit (visitor);
			return visitor.Errors;
		}
		
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
				switch (node.Name.ToLowerInvariant ()) {
				case "page":
					SetSubtype (WebSubtype.WebForm, node);
					info.MasterPageFile = node.Attributes ["masterpagefile"] as string;
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
					info.MasterPageTypeName = node.Attributes["typename"] as string;
					info.MasterPageTypeVPath = node.Attributes["virtualpath"] as string;
					break;
				case "register":
					if (node.Attributes ["TagPrefix"] != null) {
						if ((node.Attributes ["TagName"] != null) && (node.Attributes ["Src"] != null))
							info.registeredTags.Add (new ControlRegisterDirective (node));
						else if ((node.Attributes ["Namespace"] != null) && (node.Attributes ["Assembly"] != null))
							info.registeredTags.Add (new AssemblyRegisterDirective (node));
					}
					break;
				case "assembly":
					break;
				case "import":
					break;
				default:
					break;
				}
			}
			
			void Add (ErrorType type, Node node, string message, params object[] args)
			{
				errors.Add (new Error (type, node.Location.BeginLine, node.Location.BeginColumn, string.Format (message, args)));
			}
			
			void SetSubtype (WebSubtype type, DirectiveNode node)
			{
				if (info.Subtype != WebSubtype.None) {
					Add (ErrorType.Error, node, "Unexpected directive {0}", node.Name);
					return;
				}
				
				info.Subtype = type;
				info.InheritedClass = node.Attributes ["inherits"] as string;
				if (info.InheritedClass == null)
					info.InheritedClass = node.Attributes ["class"] as string;
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
		private DirectiveNode node;
		
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
			return String.Format ("<%@ Register {0}=\"{1}\" {2}=\"{3}\" {4}=\"{5}\" %>", "TagPrefix", TagPrefix, "Namespace", Namespace, "Assembly", Assembly);
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
		
}
