//
// WebFormReferenceManager.cs: Tracks references within an ASP.NET document, and 
//     resolves types from tag names
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
using System.Globalization;
using System.Collections;
using System.Web;
using System.Web.UI;
using System.Web.UI.Design;

using AspNetAddIn.Parser.Tree;
using MonoDevelop.Projects;

namespace AspNetAddIn.Parser
{
	public class WebFormReferenceManager : IWebFormReferenceManager
	{
		int prefixIndex = 0;
		ArrayList list = new ArrayList ();
		Document doc;
		
		public WebFormReferenceManager (Document parent)
		{
			this.doc = parent;	
		}
		
		#region IWebFormReferenceManager Members

		public Type GetObjectType (string tagPrefix, string typeName)
		{
			if (tagPrefix == null || tagPrefix.Length < 1)
				return typeof (System.Web.UI.HtmlControls.HtmlControl).Assembly.GetType ("System.Web.UI.HtmlControls.Html"+typeName, true, true);
			
			if (0 == string.Compare (tagPrefix, "asp", true, CultureInfo.InvariantCulture))
				return typeof (System.Web.UI.WebControls.WebControl).Assembly.GetType ("System.Web.UI.WebControls."+typeName, true, true);
			
			foreach (RegisterDirective directive in list) {
				AssemblyRegisterDirective ard = directive as AssemblyRegisterDirective;
				if (ard != null) {
					//TODO: look up from parent project's type resolution
					string fullName = ard.Namespace + "." + typeName;
					/* MonoDevelop.Projects.Parser.IProjectParserContext cxt =
					MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (pf.Project);
					MonoDevelop.Projects.Parser.IClass cls = cxt.GetClass (fullName); */
					
					//TODO: should load referenced assemblies out-of process, or in another appdomain
					
					System.Reflection.Assembly assem = System.Reflection.Assembly.Load (ard.Assembly);
					return assem.GetType (fullName, true, true);
				}
				
				ControlRegisterDirective crd = directive as ControlRegisterDirective;
				if (crd != null) {
					//TODO: UserControls - is this correct behaviour?
					ProjectFile pf = doc.ProjectFile.Project.GetProjectFile (crd.Src);
					if (pf != null) {
						string inheritsName = ((AspNetAppProject) pf.Project).GetDocument (pf).Info.InheritedClass;
						//TODO: look up from parent project's type resolution
						return Type.GetType (inheritsName, true, true);
					}
					return typeof (System.Web.UI.UserControl);
				}
			}
			
			throw new Exception ("The tag prefix \"" + tagPrefix + "\" has not been registered");
		}
		
		public string GetTypeName (string tagPrefix, string typeName)
		{
			if (tagPrefix == null || tagPrefix.Length < 1)
				return "System.Web.UI.HtmlControls."+typeName;
			
			if (0 == string.Compare (tagPrefix, "asp", true, CultureInfo.InvariantCulture))
				return "System.Web.UI.WebControls."+typeName;
			
			foreach (RegisterDirective directive in list) {
				AssemblyRegisterDirective ard = directive as AssemblyRegisterDirective;
				if (ard != null)
					return ard.Namespace + "." + typeName;
				
				ControlRegisterDirective crd = directive as ControlRegisterDirective;
				if (crd != null) {
					//TODO: UserControls - is this correct behaviour?
					ProjectFile pf = doc.ProjectFile.Project.GetProjectFile (crd.Src);
					if (pf != null)
						return crd.Src.Replace ('.', '_');
					else
						return "System.Web.UI.UserControl";
				}
			}
			
			throw new Exception ("The tag prefix \"" + tagPrefix + "\" has not been registered");
		}

		public string GetRegisterDirectives ()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder ();
			
			foreach (RegisterDirective directive in list) {
				AssemblyRegisterDirective ard = directive as AssemblyRegisterDirective;
				
				if (ard != null)
					sb.AppendFormat ("<%@ Register {0}=\"{1}\" {2}=\"{3}\" {4}=\"{5}\" %>", "TagPrefix", ard.TagPrefix, "Namespace", ard.Namespace, "Assembly", ard.Assembly);
				else {
					ControlRegisterDirective crd = (ControlRegisterDirective) directive;
					sb.AppendFormat ("<%@ Register {0}=\"{1}\" {2}=\"{3}\" {4}=\"{5}\" %>", "TagPrefix", crd.TagPrefix, "TagName", crd.TagName, "Src", crd.Src);
				}
			}
			
			return sb.ToString ();
		}

		public string GetTagPrefix (Type objectType)
		{
			if (objectType.Namespace.StartsWith ("System.Web.UI"))
				return "asp";
			
			foreach (RegisterDirective directive in list) {
				AssemblyRegisterDirective ard = directive as AssemblyRegisterDirective;
				
				if (ard != null)
					if (string.Compare (ard.Namespace, objectType.Namespace, true, CultureInfo.InvariantCulture) == 0)
						return directive.TagPrefix;
			}
			
			throw new Exception ("A tag prefix has not been registered for " + objectType.ToString ());
		}

		#endregion
		
		#region Add/Remove references

		public void AddReference (Type type)
		{
			RegisterTagPrefix (type);			
		}

		public void AddReference (Type type, string prefix)
		{
			if (type.Assembly == typeof(System.Web.UI.WebControls.WebControl).Assembly)
				return;

			//check namespace is not already registered			foreach (RegisterDirective directive in list) {
				AssemblyRegisterDirective ard = directive as AssemblyRegisterDirective;
				if (0 == string.Compare (ard.Namespace, type.Namespace, false, CultureInfo.InvariantCulture))
					throw new Exception ("That namespace is already registered with another prefix");
				
				if (0 == string.Compare (directive.TagPrefix, prefix, true, CultureInfo.InvariantCulture)) {
					//duplicate prefix; generate a new one.
					//FIXME: possibility of stack overflow with too many default prefixes in existing document
					AddReference (type);
					return;
				}
			}
			
			doc.ProjectFile.Project.ProjectReferences.Add (
			    new ProjectReference (ReferenceType.Assembly, type.Assembly.ToString ()));
		/*
		TODO: insert the reference into the document tree 
		*/
		}

		#endregion
		
		#region 2.0 WebFormsReferenceManager members
		
		public string RegisterTagPrefix (Type type)
		{
			if (type.Assembly == typeof(System.Web.UI.WebControls.WebControl).Assembly)
				return "asp";

			string prefix = null;

			//check if there's a prefix for this namespace in the assembly
			TagPrefixAttribute[] atts = (TagPrefixAttribute[]) type.Assembly.GetCustomAttributes (typeof (TagPrefixAttribute), true);
			foreach (TagPrefixAttribute tpa in atts)
					if (0 == string.Compare (tpa.NamespaceName, type.Namespace, false, CultureInfo.InvariantCulture))
						prefix = tpa.TagPrefix;
			
			//generate default prefix
			if (prefix == null) {
				prefix = "cc" + prefixIndex.ToString ();
				prefixIndex++;
			}
				
			AddReference (type, prefix);
			
			return prefix;
		}
		
		public string GetUserControlPath (string tagPrefix, string tagName)
		{
			foreach (RegisterDirective directive in list) {
				ControlRegisterDirective crd = directive as ControlRegisterDirective;
				
				if (crd != null)
					if ((string.Compare (crd.TagPrefix, tagPrefix, true, CultureInfo.InvariantCulture) == 0)
							&& (string.Compare (crd.TagName, tagName, true, CultureInfo.InvariantCulture) == 0))
						return crd.Src;
			}
			
			throw new Exception ("That tag has not been registered");
		}
		
		public Type GetType (string tagPrefix, string tagName)
		{
			return GetObjectType (tagPrefix, tagName);
		}
		
		#endregion 2.0 WebFormsReferenceManager members
		
		#region extra utility members
		
		
		#endregion
		
		#region directive classes
		
		private abstract class RegisterDirective
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
		
		private class AssemblyRegisterDirective : RegisterDirective
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
		
		private class ControlRegisterDirective : RegisterDirective
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
			WebFormReferenceManager parent;
			
			public ReferenceVisitor (WebFormReferenceManager parent)
			{
				this.parent = parent;
			}
			
			public override void Visit (DirectiveNode node)
			{
				if ((String.Compare (node.Name, "register", true) != 0) || (node.Attributes ["TagPrefix"] == null))
					return;
				
				if ((node.Attributes ["TagName"] != null) && (node.Attributes ["Src"] != null))
					parent.list.Add (new ControlRegisterDirective (node));
				else if ((node.Attributes ["Namespace"] != null) && (node.Attributes ["Assembly"] != null))
					parent.list.Add (new AssemblyRegisterDirective (node));
			}	
		}
		
		
		#endregion classes
	}
}
