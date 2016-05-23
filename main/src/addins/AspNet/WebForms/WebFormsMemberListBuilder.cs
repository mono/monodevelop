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
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.AspNet.WebForms;
using MonoDevelop.AspNet.WebForms.Dom;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.AspNet.WebForms
{
	//purpose is to find all named tags for code completion and compilation of base class
	class WebFormsMemberListBuilder
	{
		readonly WebFormsTypeContext docRefMan;
		XDocument xDocument;
		
		public IDictionary<string,CodeBehindMember> Members { get; private set; }
		public IList<Error> Errors { get; private set; }
		
		public WebFormsMemberListBuilder (WebFormsTypeContext refMan, XDocument xDoc)
		{
			docRefMan = refMan;
			xDocument = xDoc;
			
			Errors = new List<Error> ();
			Members = new Dictionary<string,CodeBehindMember> ();
		}
		
		public void Build ()
		{
			try {
				foreach (XElement el in xDocument.Nodes.OfType<XElement> ())
					AddMember (el);
			} catch (Exception ex) {
				LoggingService.LogError ("Unknown parser error: {0}", ex);
				Errors.Add (new Error (ErrorType.Error, GettextCatalog.GetString ("Unknown parser error: {0}", ex.Message)));
			}
		}
		
		void AddMember (XElement element)
		{
			string id;
			if (element.IsRunatServer () && !string.IsNullOrEmpty (id = element.GetId ())) {
				
				if (Members.ContainsKey (id)) {
					Errors.Add (new Error (
						ErrorType.Error,
						GettextCatalog.GetString ("Tag ID must be unique within the document: '{0}'.", id),
						element.Region
					)
					);
				} else {
					string controlType = element.Attributes.GetValue (new XName ("type"), true);
					var type = docRefMan.GetType (element.Name.Prefix, element.Name.Name, controlType);
	
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
			foreach (XElement child in element.Nodes.OfType<XElement> ())
				AddMember (child);
		}
	}
	
	public class CodeBehindMember
	{
		public CodeBehindMember (string name, INamedTypeSymbol type, DocumentLocation location)
		{
			this.Name = name;
			this.Type = type;
			this.Location = location;
		}		
		
		public string Name { get; private set; }
		public INamedTypeSymbol Type { get; private set; }
		public DocumentLocation Location { get; private set; }
	}
}
