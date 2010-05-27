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
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using System.IO;
using System.Linq;

namespace MonoDevelop.AspNet.Parser
{
	
	
	public class DocumentReferenceManager
	{
		public DocumentReferenceManager ()
		{
		}
		
		protected IEnumerable<RegisterDirective> RegisteredTags {
			get { return Doc.Info.RegisteredTags; }
		}
		
		public AspNetParsedDocument Doc { get; set; }
		public AspNetAppProject Project { get; set; }
		string DirectoryPath { get { return Path.GetDirectoryName (Doc.FileName); } }
		
		public string GetTypeName (string tagPrefix, string tagName)
		{
			return GetTypeName (tagPrefix, tagName, null);
		}
		
		public string GetTypeName (string tagPrefix, string tagName, string htmlTypeAttribute)
		{
			if (tagPrefix == null || tagPrefix.Length < 1)
				return WebTypeManager.HtmlControlLookup (tagName, htmlTypeAttribute);
			
			if (0 == string.Compare (tagPrefix, "asp", StringComparison.OrdinalIgnoreCase)) {
				string systemType = WebTypeManager.SystemTypeNameLookup (tagName, Project);
				if (!string.IsNullOrEmpty (systemType))
					return systemType;
			}
			
			foreach (var rd in RegisteredTags) {
				if (string.Compare (rd.TagPrefix, tagPrefix, StringComparison.OrdinalIgnoreCase) != 0)
					continue;
				
				var ard = rd as AssemblyRegisterDirective;
				if (ard != null) {
					ProjectDom dom = WebTypeManager.ResolveAssembly (Project, ard.Assembly);
					if (dom == null)
						continue;
					
					string fullName = WebTypeManager.AssemblyTypeNameLookup (dom, ard.Namespace, tagName);
					if (fullName != null)
						return fullName;
				}
				
				var crd = rd as ControlRegisterDirective;
				if (crd != null && string.Compare (crd.TagName, tagName, StringComparison.OrdinalIgnoreCase) == 0) {
					string fullName =  WebTypeManager.GetUserControlTypeName (Project, crd.Src, Doc.FileName);
					if (fullName != null)
						return fullName;
				}
			}
			
			//returns null if type not found
			return WebTypeManager.GetRegisteredTypeName (Project, DirectoryPath, tagPrefix, tagName);
		}
		
		public IType GetType (string tagPrefix, string tagName, string htmlTypeAttribute)
		{
			if (tagPrefix == null || tagPrefix.Length < 1) 
				return WebTypeManager.HtmlControlTypeLookup (Project, tagName, htmlTypeAttribute);
			
			if (0 == string.Compare (tagPrefix, "asp", StringComparison.OrdinalIgnoreCase)) {
				var systemType = WebTypeManager.SystemTypeLookup (tagName, Project);
				if (systemType != null)
					return systemType;
			}
			
			foreach (var rd in RegisteredTags) {
				if (string.Compare (rd.TagPrefix, tagPrefix, StringComparison.OrdinalIgnoreCase) != 0)
					continue;
				
				var ard = rd as AssemblyRegisterDirective;
				if (ard != null) {
					var dom = WebTypeManager.ResolveAssembly (Project, ard.Assembly);
					if (dom != null) {
						var type = WebTypeManager.AssemblyTypeLookup (dom, ard.Namespace, tagName);
						if (type != null)
							return type;
					}
					continue;
				}
				
				var crd = rd as ControlRegisterDirective;
				if (crd != null && string.Compare (crd.TagName, tagName, StringComparison.OrdinalIgnoreCase) == 0) {
					var type = WebTypeManager.GetUserControlType (Project, crd.Src, Doc.FileName);
					if (type != null)
						return type;
				}
			}
			
			//returns null if type not found
			return WebTypeManager.GetRegisteredType (Project, DirectoryPath, tagPrefix, tagName);
		}
		
		public IEnumerable<CompletionData> GetControlCompletionData ()
		{
			return GetControlCompletionData (new DomType ("System.Web.UI.Control"));
		}
		
		public IEnumerable<CompletionData> GetControlCompletionData (IType baseType)
		{
			bool isSWC = baseType.FullName == "System.Web.UI.Control";
			
			string aspPrefix = "asp:";
			foreach (IType cls in WebTypeManager.ListSystemControlClasses (baseType, Project))
				yield return new AspTagCompletionData (aspPrefix, cls);
			
			foreach (var rd in RegisteredTags) {
				if (!rd.IsValid ())
					continue;
				
				AssemblyRegisterDirective ard = rd as AssemblyRegisterDirective;
				if (ard != null) {
					ProjectDom dom = WebTypeManager.ResolveAssembly (Project, ard.Assembly);
					if (dom == null)
						continue;
					
					string prefix = ard.TagPrefix + ":";
					foreach (IType cls in WebTypeManager.ListControlClasses (baseType, dom, ard.Namespace))
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
			
			//return controls from web.config
			foreach (var cd in WebTypeManager.GetRegisteredTypeCompletionData (Project, DirectoryPath, baseType))
				yield return cd;
		}
		
		public IType GetControlType (string tagPrefix, string tagName)
		{
			if (String.IsNullOrEmpty (tagPrefix))
				return null;
			
			IType type = null;
			if (0 == string.Compare (tagPrefix, "asp", StringComparison.OrdinalIgnoreCase)) {
				type = WebTypeManager.SystemTypeLookup (tagName, Project);
				if (type != null)
					return type;
			}
			
			foreach (var rd in RegisteredTags) {
				if (string.Compare (rd.TagPrefix, tagPrefix, StringComparison.OrdinalIgnoreCase) != 0)
					continue;
				
				AssemblyRegisterDirective ard = rd as AssemblyRegisterDirective;
				if (ard != null) {
					string assembly = ard.Assembly;
					ProjectDom dom = WebTypeManager.ResolveAssembly (Project, ard.Assembly);
					if (dom == null)
						continue;
					type = WebTypeManager.AssemblyTypeLookup (dom, ard.Namespace, tagName);
					if (type != null)
						return type;
					continue;
				}
				
				ControlRegisterDirective crd = rd as ControlRegisterDirective;
				if (crd != null && string.Compare (crd.TagName, tagName, StringComparison.OrdinalIgnoreCase) == 0) {
					return WebTypeManager.GetUserControlType (Project, crd.Src, Doc.FileName);
				}	
			}
			
			//returns null if type not found
			return WebTypeManager.GetRegisteredType (Project, DirectoryPath, tagPrefix, tagName);
		}
		
		public string GetTagPrefix (IType control)
		{
			if (control.Namespace == "System.Web.UI.WebControls")
				return "asp";
			else if (control.Namespace == "System.Web.UI.HtmlControls")
				return string.Empty;
			
			foreach (var rd in RegisteredTags) {
				var ard = rd as AssemblyRegisterDirective;
				if (ard != null && ard.Namespace == control.Namespace)
					return ard.TagPrefix;
			}
			
			// returns null if no result found
			return WebTypeManager.GetControlPrefix (Project, DirectoryPath, control);
		}
		
		IEnumerable<RegisterDirective> GetDirectivesForPrefix (string prefix)
		{
			return RegisteredTags.Where (t => string.Equals (t.TagPrefix, prefix, StringComparison.OrdinalIgnoreCase));
		}
		
		/// <summary>
		/// Gets a tag prefix, also returning the directive that would have to be added if necessary.
		/// </summary>
		public string GetTagPrefixWithNewDirective (IType control, string assemblyName, string desiredPrefix, 
		                                            out RegisterDirective directiveNeededToAdd)
		{
			directiveNeededToAdd = null;
			string existingPrefix = GetTagPrefix (control);
			if (existingPrefix != null)
				return existingPrefix;
			
			//TODO: detect control name conflicts 
			string prefix = desiredPrefix;
			if (desiredPrefix == null)
				prefix = GetPrefix (control);
			
			var an = MonoDevelop.Core.Assemblies.SystemAssemblyService.ParseAssemblyName (assemblyName);
			
			directiveNeededToAdd = new AssemblyRegisterDirective (prefix, control.Namespace, an.Name);
			
			return prefix;
		}
		
		#region "Refactoring" operations -- things that modify the file
		
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
			foreach (var p in Project.References)
				if (p.Equals (pr))
					match = true;
			if (!match)
				Project.References.Add (pr);
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
		
		public void AddRegisterDirective (RegisterDirective directive, TextEditor editor, bool preserveCaretPosition)
		{
			var node = GetRegisterInsertionPointNode ();
			if (node == null)
				return;
			
			Doc.Info.RegisteredTags.Add (directive);
			
			var line = Math.Max (node.Location.EndLine, node.Location.BeginLine);
			var pos = editor.GetPositionFromLineColumn (line, editor.GetLineLength (line) + 1);
			if (pos < 0)
				return;
			
			editor.BeginAtomicUndo ();
			var oldCaret = editor.CursorPosition;
			
			var inserted = editor.InsertText (pos, editor.NewLine + directive.ToString ());
			if (preserveCaretPosition) {
				editor.CursorPosition = (pos < oldCaret)? oldCaret + inserted : oldCaret;
			}
			editor.EndAtomicUndo ();
		}
		
		DirectiveNode GetRegisterInsertionPointNode ()
		{
			var v = new RegisterDirectiveInsertionPointVisitor ();
			Doc.RootNode.AcceptVisit (v);
			return v.Node;
		}
		
		class RegisterDirectiveInsertionPointVisitor: Visitor
		{
			public DirectiveNode Node { get; private set; }
			
			public override void Visit (DirectiveNode node)
			{
				switch (node.Name.ToLowerInvariant ()) {
				case "page": case "control": case "master": case "register":
					Node = node;
					return;
				}
			}
			
			public override void Visit (TagNode node)
			{
				QuickExit = true;
			}
		}
		
		#endregion
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
