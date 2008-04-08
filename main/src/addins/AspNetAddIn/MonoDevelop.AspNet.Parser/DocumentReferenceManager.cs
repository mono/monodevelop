//
// DocumentReferenceManager.cs: Handles web type lookups for ASP.NET documents.
//
// Authors:
//   Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Globalization;

using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AspNet.Parser
{
	
	
	public class DocumentReferenceManager
	{
		protected List<RegisterDirective> pageRefsList = new List<RegisterDirective> ();
		protected Document doc;
		
		public DocumentReferenceManager (Document doc)
		{
			this.doc = doc;
			updateList ();
		}
		
		void updateList ()
		{
			ReferenceVisitor visitor = new ReferenceVisitor (this);
			pageRefsList.Clear ();
			doc.RootNode.AcceptVisit (visitor);
		}
		
		public string GetTypeName (string tagPrefix, string tagName)
		{
			return GetTypeName (tagPrefix, tagName, null);
		}
		
		public string GetTypeName (string tagPrefix, string tagName, string htmlTypeAttribute)
		{
			if (tagPrefix == null || tagPrefix.Length < 1)
				return WebTypeManager.HtmlControlLookup (tagName, htmlTypeAttribute);
			
			if (0 == string.Compare (tagPrefix, "asp", true, CultureInfo.InvariantCulture))
				return WebTypeManager.SystemWebControlLookup (tagName,
				    doc.Project == null? MonoDevelop.Core.ClrVersion.Default : doc.Project.ClrVersion);
			
			foreach (RegisterDirective directive in pageRefsList) {
				AssemblyRegisterDirective ard = directive as AssemblyRegisterDirective;
				if (ard != null && ard.TagPrefix == tagPrefix) {
					string fullName;
					if (doc.Project != null) 
						fullName = doc.Project.WebTypeManager.ProjectTypeNameLookup (tagName, ard.Namespace, ard.Assembly);
					else
						fullName = WebTypeManager.AssemblyTypeNameLookup (tagName, ard.Namespace, ard.Assembly);
					
					if (fullName != null)
						return fullName;
				}
				
				ControlRegisterDirective crd = directive as ControlRegisterDirective;
				if (crd != null && crd.TagPrefix == tagPrefix) {
					string fullName =  doc.Project.WebTypeManager.GetControlTypeName (doc.FilePath, crd.Src);
					if (fullName != null)
						return fullName;
				}
			}
			
			string globalLookup = doc.Project.WebTypeManager.GetGloballyRegisteredTypeName 
					(System.IO.Path.GetDirectoryName (doc.FilePath), tagPrefix, tagName);
			
			//returns null if type not found
			return globalLookup;
		}
		
		public IEnumerable<IClass> ListControlClasses ()
		{
			MonoDevelop.Core.ClrVersion clrVersion = 
				doc.Project == null? MonoDevelop.Core.ClrVersion.Default : doc.Project.ClrVersion;
			foreach (IClass cls in WebTypeManager.ListSystemControlClasses (clrVersion))
			    yield return cls;
			
			//FIXME: return other refernced controls
		}
		
		public IClass GetControlType (string tagPrefix, string tagName)
		{
			if (0 == string.Compare (tagPrefix, "asp", true, CultureInfo.InvariantCulture))
				return WebTypeManager.AssemblyTypeLookup (tagName, "System.Web.UI.WebControls", "System.Web");
			
			//FIXME: Implement for non-builtins
			return null;
		}
		
		#region directive classes
		
		protected abstract class RegisterDirective
		{
			private DirectiveNode node;
			
			public RegisterDirective (DirectiveNode node)
			{
				this.node = node;
			}
			
			public DirectiveNode Node {
				get { return node; }
			}
			
			public string TagPrefix {
				get { return (string) node.Attributes ["TagPrefix"]; }
				set { node.Attributes ["TagPrefix"] = value; }
			}
		}
		
		protected class AssemblyRegisterDirective : RegisterDirective
		{
			public AssemblyRegisterDirective (DirectiveNode node)
				: base (node)
			{
			}
			
			public string Namespace {
				get { return (string) Node.Attributes ["Namespace"]; }
				set { Node.Attributes ["Namespace"] = value; }
			}
			
			public string Assembly {
				get { return (string) Node.Attributes ["Assembly"]; }
				set { Node.Attributes ["Assembly"] = value; }
			}
			
			public override string ToString ()
			{	
				return String.Format ("<%@ Register {0}=\"{1}\" {2}=\"{3}\" {4}=\"{5}\" %>", "TagPrefix", TagPrefix, "Namespace", Namespace, "Assembly", Assembly);
			}
		}
		
		protected class ControlRegisterDirective : RegisterDirective
		{			
			public ControlRegisterDirective (DirectiveNode node)
				: base (node)
			{
			}
			
			public string TagName {
				get { return (string) Node.Attributes ["TagName"]; }
				set { Node.Attributes ["TagName"] = value; }
			}
			
			public string Src {
				get { return (string) Node.Attributes ["Src"]; }
				set { Node.Attributes ["Src"] = value; }
			}
			
			public override string ToString ()
			{	
				return String.Format ("<%@ Register {0}=\"{1}\" {2}=\"{3}\" {4}=\"{5}\" %>", "TagPrefix", TagPrefix, "TagName", TagName, "Src", Src);
			}
		}
		
		private class ReferenceVisitor : Visitor
		{
			DocumentReferenceManager parent;
			
			public ReferenceVisitor (DocumentReferenceManager parent)
			{
				this.parent = parent;
			}
			
			public override void Visit (DirectiveNode node)
			{
				if ((String.Compare (node.Name, "register", true) != 0) || (node.Attributes ["TagPrefix"] == null))
					return;
				
				if ((node.Attributes ["TagName"] != null) && (node.Attributes ["Src"] != null))
					parent.pageRefsList.Add (new ControlRegisterDirective (node));
				else if ((node.Attributes ["Namespace"] != null) && (node.Attributes ["Assembly"] != null))
					parent.pageRefsList.Add (new AssemblyRegisterDirective (node));
			}	
		}
		
		#endregion classes
	}
}
