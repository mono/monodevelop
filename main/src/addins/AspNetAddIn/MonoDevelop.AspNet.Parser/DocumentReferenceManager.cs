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

using MonoDevelop.Core;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Gui.Completion;
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
			
			if (0 == string.Compare (tagPrefix, "asp", StringComparison.OrdinalIgnoreCase)) {
				string systemType = WebTypeManager.SystemWebControlLookup (tagName,
				    doc.Project == null? MonoDevelop.Core.TargetFramework.Default : doc.Project.TargetFramework);
				if (!string.IsNullOrEmpty (systemType))
					return systemType;
			}
			
			foreach (RegisterDirective directive in pageRefsList) {
				AssemblyRegisterDirective ard = directive as AssemblyRegisterDirective;
				if (ard != null && ard.TagPrefix == tagPrefix) {
					string fullName;
					fullName = WebTypeManager.TypeNameLookup (doc.Project, tagName, ard.Namespace, ard.Assembly);
					
					if (fullName != null)
						return fullName;
				}
				
				ControlRegisterDirective crd = directive as ControlRegisterDirective;
				if (crd != null && crd.TagPrefix == tagPrefix) {
					string fullName =  WebTypeManager.GetControlTypeName (doc.FilePath, crd.Src);
					if (fullName != null)
						return fullName;
				}
			}
			
			string globalLookup = WebTypeManager.GetRegisteredTypeName (doc.Project, 
			    System.IO.Path.GetDirectoryName (doc.FilePath), tagPrefix, tagName);
			
			
			//returns null if type not found
			return globalLookup;
		}
		
		public IEnumerable<CompletionData> GetControlCompletionData ()
		{
			return GetControlCompletionData (new DomType ("System.Web.UI.Control"));
		}
		
		public IEnumerable<CompletionData> GetControlCompletionData (IType baseType)
		{
			bool isSWC = baseType.FullName == "System.Web.UI.Control";
			
			string aspPrefix = "asp:";
			foreach (IType cls in WebTypeManager.ListSystemControlClasses (baseType, doc.Project))
				yield return new AspTagCompletionData (aspPrefix, cls);
			
			foreach (RegisterDirective rd in pageRefsList) {
				if (!rd.IsValid ())
					continue;
				
				AssemblyRegisterDirective ard = rd as AssemblyRegisterDirective;
				if (ard != null) {
					string prefix = ard.TagPrefix + ":";
					foreach (IType cls in WebTypeManager.ListControlClasses (doc.Project, baseType, ard.Assembly, ard.Namespace))
						yield return new AspTagCompletionData (prefix, cls);
					continue;
				}
				
				if (!isSWC)
					continue;
				
				ControlRegisterDirective cd = rd as ControlRegisterDirective;
				if (cd != null) {
					yield return new CompletionData (string.Concat (cd.TagPrefix, ":", cd.TagName),
					                                 Gtk.Stock.GoForward);
				}
			}
			
			//FIXME: return controls from web.config
		}
		
		public IType GetControlType (string tagPrefix, string tagName)
		{
			if (String.IsNullOrEmpty (tagPrefix))
				return null;
			
			IType type = null;
			if (0 == string.Compare (tagPrefix, "asp", StringComparison.InvariantCultureIgnoreCase)) {
				type = WebTypeManager.AssemblyTypeLookup (doc.Project, "System.Web", "System.Web.UI.WebControls", tagName);
				if (type != null)
					return type;
			}
			
			foreach (RegisterDirective rd in pageRefsList) {
				if (string.Compare (rd.TagPrefix, tagPrefix, StringComparison.InvariantCultureIgnoreCase) != 0)
					continue;
				
				AssemblyRegisterDirective ard = rd as AssemblyRegisterDirective;
				if (ard != null) {
					type = WebTypeManager.AssemblyTypeLookup (doc.Project, ard.Assembly, ard.Namespace, tagName);
					if (type != null)
						return type;
					continue;
				}
				
				ControlRegisterDirective crd = rd as ControlRegisterDirective;
				if (crd != null && string.Compare (crd.TagName, tagName, StringComparison.InvariantCultureIgnoreCase) != 0) {
					//FIXME: return the codebehind class's IType
					type = WebTypeManager.AssemblyTypeLookup (doc.Project, "System.Web", "System.Web.UI", "UserControl");
					if (type == null)
						LoggingService.LogWarning ("Could not obtain IType for System.Web.UI.WebControls.WebControl");
					return type;
				
				}	
			}
			
			//FIXME: return controls from web.config
			return null;
		}
		
		public string GetTagPrefix (IType control)
		{
			if (control.Namespace == "System.Web.UI.WebControls")
				return "asp";
			else if (control.Namespace == "System.Web.UI.HtmlControls")
				return string.Empty;
			
			foreach (RegisterDirective rd in pageRefsList) {
				AssemblyRegisterDirective ard = rd as AssemblyRegisterDirective;
				if (ard != null && ard.Namespace == control.Namespace)
					return ard.TagPrefix;
			}
			
			string globalPrefix = WebTypeManager.GetControlPrefix (doc.Project, control);
			if (globalPrefix != null)
				return globalPrefix;
			
			return null;
		}
		
		IEnumerable<RegisterDirective> GetDirectivesForPrefix (string prefix)
		{
			foreach (RegisterDirective rd in pageRefsList)
				if (string.Equals (rd.TagPrefix, prefix, StringComparison.InvariantCultureIgnoreCase))
					yield return rd;
		}
		
		#region "Refactoring" operations -- things that modify the file
		
		public string AddAssemblyReferenceToDocument (IType control, string assemblyName)
		{
			return AddAssemblyReferenceToDocument (control, assemblyName, null);
		}
		
		public string AddAssemblyReferenceToDocument (IType control, string assemblyName, string desiredPrefix)
		{
			string existingPrefix = GetTagPrefix (control);
			if (existingPrefix != null)
				return existingPrefix;
			
			//TODO: detect control name conflicts 
			string prefix = desiredPrefix;
			if (desiredPrefix == null)
				prefix = GetPrefix (control);
			
			System.Reflection.AssemblyName an = MonoDevelop.Core.Runtime.SystemAssemblyService.ParseAssemblyName (assemblyName);
			
			string directive = string.Format ("{0}<%@ Register TagPrefix=\"{1}\" Namespace=\"{2}\" Assembly=\"{3}\" %>",
			    Environment.NewLine, prefix, control.Namespace, an.Name);
			
			//inset a directive into the document
			InsertDirective (directive);
			
			return prefix;
		}
		
		public void AddAssemblyReferenceToProject (string assemblyName, string assemblyLocation)
		{
			//build an reference to the assembly
			MonoDevelop.Projects.ProjectReference pr;
			if (string.IsNullOrEmpty (assemblyLocation)) {
				pr = new MonoDevelop.Projects.ProjectReference
					(MonoDevelop.Projects.ReferenceType.Gac, assemblyName);
			} else {
				pr =  new MonoDevelop.Projects.ProjectReference
					(MonoDevelop.Projects.ReferenceType.Assembly, assemblyLocation);
			}
			
			//add the reference if it doesn't match an existing one
			bool match = false;
			foreach (MonoDevelop.Projects.ProjectReference p in doc.Project.References)
				if (p.Equals (pr))
					match = true;
			if (!match)
				doc.Project.References.Add (pr);
		}
		
		string GetPrefix (IType control)
		{
			//FIXME: make this work 
			/*
			foreach (IAttributeSection attSec in control.CompilationUnit.Attributes) {
				foreach (IAttribute att in attSec.Attributes) {
					if (att.PositionalArguments != null && att.PositionalArguments.Length == 2
					    && ExprToStr (att.PositionalArguments[0]) == control.Namespace) {
						string prefix = ExprToStr (att.PositionalArguments [1]);
						if (prefix != null)
							return prefix;
					}
					
					if (att.Name == "System.Web.UI.TagPrefixAttribute") {
						bool match = false;
						foreach (NamedAttributeArgument arg in att.NamedArguments) {
							if (arg.Name == "NamespaceName"
							    && ExprToStr (arg.Expression) == control.Namespace) {
								match = true;
								break;
							}
						}
						foreach (NamedAttributeArgument arg in att.NamedArguments) {
							if (arg.Name == "TagPrefix") {
								string prefix = ExprToStr (arg.Expression);
								if (prefix != null)
									return prefix;
							}
						}
					}
				}
			}
			*/
			//generate a new prefix base on initials of namespace
			string[] namespaces = control.Namespace.Split ('.');
			char[] charr = new char[namespaces.Length];
			for (int i = 0; i < charr.Length; i++)
				charr[i] = char.ToLower (namespaces[i][0]);
			
			//find a variant that doesn't match an existing prefix
			string trialPrefix = new string (charr);
			string trial = trialPrefix;
			
			for (int trialSuffix = 1; trialSuffix < int.MaxValue; trialSuffix++) {
				using (IEnumerator<RegisterDirective> en = GetDirectivesForPrefix (trial).GetEnumerator())
					if (!en.MoveNext ())
						return trial;
				trial = trialPrefix + trialSuffix;
			}
			throw new InvalidOperationException ("Ran out of integer suffixes for tag prefixes");
		}
		
		string ExprToStr (System.CodeDom.CodeExpression expr)
		{
			System.CodeDom.CodePrimitiveExpression p = expr as System.CodeDom.CodePrimitiveExpression;
			return p != null? p.Value as string : null;
		}
		
		void InsertDirective (string directive)
		{
			DirectiveNode node = GetPageDirective ();
			if (node == null)
				return;
			
			IEditableTextFile textFile = 
				MonoDevelop.DesignerSupport.OpenDocumentFileProvider.Instance.GetEditableTextFile (doc.FilePath);
			if (textFile == null)
				textFile = new TextFile (doc.FilePath);
			
			int pos = textFile.GetPositionFromLineColumn (node.Location.EndLine, node.Location.EndColumn);
			textFile.InsertText (pos, directive);
		}
		
		DirectiveNode GetPageDirective ()
		{
			PageDirectiveVisitor v = new PageDirectiveVisitor ();
			doc.RootNode.AcceptVisit (v);
			return v.DirectiveNode;
		}
		
		#endregion
		
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
			
			public override bool IsValid ()
			{
				if (string.IsNullOrEmpty (Assembly) || string.IsNullOrEmpty (Namespace) || !base.IsValid ())
					return false;
				return true;
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
			
			public override bool IsValid ()
			{
				if (string.IsNullOrEmpty (TagName) || string.IsNullOrEmpty (Src) || !base.IsValid ())
					return false;
				return true;
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
	
	//lazily loads docs
	class AspTagCompletionData : CompletionData
	{
		IType cls;
		
		public AspTagCompletionData (string prefix, IType cls)
			: base (prefix + cls.Name, Gtk.Stock.GoForward)
		{
			this.cls = cls;
		}
		
		public override string Description {
			get {
				if (base.Description == null && cls != null)
					base.Description = DocumentationService.GetCodeCompletionDescription (
						cls, AmbienceService.DefaultAmbience);
				return base.Description;
			}
			set { base.Description = value;	}
		}
	}
}
