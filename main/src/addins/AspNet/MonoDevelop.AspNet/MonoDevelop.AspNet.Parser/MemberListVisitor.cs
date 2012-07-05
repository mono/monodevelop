//
// MemberListVisitor.cs: Collects members from ASP.NET document tree, 
//     for code completion and other services.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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

using MonoDevelop.Core;
using MonoDevelop.AspNet.Parser.Dom;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory;
using MonoDevelop.Xml.StateEngine;
using MonoDevelop.AspNet.StateEngine;

namespace MonoDevelop.AspNet.Parser
{
	//purpose is to find all named tags for code completion and compilation of base class
	public class MemberListVisitor : Visitor
	{
		DocumentReferenceManager refMan;
		
		public MemberListVisitor (DocumentReferenceManager refMan)
		{
			this.refMan = refMan;
			this.Errors = new List<Error> ();
			this.Members = new Dictionary<string,CodeBehindMember> ();
		}
		
		public override void Visit (TagNode node)
		{
			if (!node.Attributes.IsRunAtServer ())
				return;
			
			string id = node.Attributes ["id"] as string;
			
			if (id == null)
				return;
			
			if (Members.ContainsKey (id)) {
				AddError (ErrorType.Error, node.Location, GettextCatalog.GetString ("Tag ID must be unique within the document: '{0}'.", id));
				return;
			}
			
			string [] s = node.TagName.Split (':');
			string prefix = (s.Length == 1)? "" : s[0];
			string name = (s.Length == 1)? s[0] : s[1];
			if (s.Length > 2) {
				AddError (ErrorType.Error, node.Location, GettextCatalog.GetString ("Malformed tag name: '{0}'.", node.TagName));
				return;
			}
			
			IType type = null;
			try {
				type = refMan.GetType (prefix, name, node.Attributes ["type"] as string);
			} catch (Exception e) {
				AddError (ErrorType.Error, node.Location, "Unknown parser error:" + e.ToString ());
				return;
			}
			
			if (type == null) {
				AddError (ErrorType.Error, node.Location, GettextCatalog.GetString ("The tag type '{0}{1}{2}' has not been registered.", prefix, string.IsNullOrEmpty(prefix)? string.Empty:":", name));
				return;
			}
			
			Members [id] = new CodeBehindMember (id, type, new TextLocation (node.Location.BeginLine, node.Location.BeginColumn));
		}
		
		internal void AddError (ErrorType type, ILocation location, string message)
		{
			Errors.Add (new Error (type, message, location.BeginLine, location.BeginColumn));
		}
		
		public IDictionary<string,CodeBehindMember> Members { get; private set; }
		public IList<Error> Errors { get; private set; }
	}
	
	public class MemberListBuilder
	{
		DocumentReferenceManager docRefMan;
		XDocument xDocument;
		
		public IDictionary<string,CodeBehindMember> Members { get; private set; }
		public IList<Error> Errors { get; private set; }
		
		public MemberListBuilder (DocumentReferenceManager refMan, XDocument xDoc)
		{
			docRefMan = refMan;
			xDocument = xDoc;
			
			Errors = new List<Error> ();
			Members = new Dictionary<string,CodeBehindMember> ();
		}
		
		public void Build ()
		{
			try {
				AddMember (xDocument.RootElement);
			} catch (Exception ex) {
				Errors.Add (new Error (ErrorType.Error, "Unknown parser error: " + ex.ToString ()));
			}
		}
		
		void AddMember (XElement element)
		{
			string id = GetAttributeValueCI (element.Attributes, "id");
			if (IsRunatServer (element) && (id != string.Empty)) {
				
				if (Members.ContainsKey (id)) {
					Errors.Add (new Error (
						ErrorType.Error,
						GettextCatalog.GetString ("Tag ID must be unique within the document: '{0}'.", id),
						element.Region
					)
					);
				} else {
					string controlType = GetAttributeValueCI (element.Attributes, "type");
					IType type = docRefMan.GetType (element.Name.Prefix, element.Name.Name, controlType);
	
					if (type == null) {
						Errors.Add (
							new Error (
								ErrorType.Error,
								GettextCatalog.GetString ("The tag type '{0}{1}{2}' has not been registered.", 
						                          element.Name.Prefix, 
						                          element.Name.HasPrefix ? string.Empty : ":", 
						                          element.Name.Name),
								element.Region
						)
						);
					} else
						Members [id] = new CodeBehindMember (id, type, element.Region.Begin);
				}

			}
			foreach (XNode node in element.Nodes) {
				if (node is XElement)
					AddMember (node as XElement);
			}
		}
		
		bool IsRunatServer (XElement el)
		{
			XName runat = new XName ("runat");
			foreach (XAttribute attr in el.Attributes) {
				if ((attr.Name.ToLower () == runat) && (attr.Value.ToLower () == "server"))
					return true;
			}
			return false;
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
	}
	
	public class CodeBehindMember
	{
		public CodeBehindMember (string name, IType type, TextLocation location)
		{
			this.Name = name;
			this.Type = type;
			this.Location = location;
		}		
		
		public string Name { get; private set; }
		public IType Type { get; private set; }
		public TextLocation Location { get; private set; }
	}
}
