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
using System.IO;
using System.Linq;

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using Mono.TextEditor;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Xml.Dom;
using MonoDevelop.AspNet.Projects;
using MonoDevelop.AspNet.WebForms.Dom;
using System.Reflection;

namespace MonoDevelop.AspNet.WebForms
{
	public class WebFormsTypeContext
	{
		ICompilation compilation;
		AspNetAppProject project;
		WebFormsParsedDocument doc;

		public WebFormsParsedDocument Doc {
			get {
				return doc;
			}
			set {
				if (doc == value)
					return;
				doc = value;
				compilation = null;
			}
		}

		public AspNetAppProject Project {
			get {
				return project;
			}
			set {
				if (project == value)
					return;
				project = value;
				compilation = null;
			}
		}

		public ICompilation Compilation {
			get {
				if (compilation == null)
					UpdateCompilation ();
				return compilation;
			}
		}

		TargetFramework TargetFramework {
			get {
				return project != null
					? project.TargetFramework
					: MonoDevelop.Core.Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.NET_4_5);
			}
		}

		TargetRuntime TargetRuntime {
			get {
				return project != null ? project.TargetRuntime : MonoDevelop.Core.Runtime.SystemAssemblyService.DefaultRuntime;
			}
		}

		void UpdateCompilation ()
		{
			const string dummyAsmName = "CompiledAspNetPage";
			IUnresolvedAssembly asm = new DefaultUnresolvedAssembly (dummyAsmName);
			compilation = new SimpleCompilation (asm, GetReferencedAssemblies ());
		}
		
		public IType GetType (string tagPrefix, string tagName, string htmlTypeAttribute)
		{
			if (tagPrefix == null || tagPrefix.Length < 1)
				return HtmlControlTypeLookup (tagName, htmlTypeAttribute);

			foreach (var rd in GetControls ()) {
				if (string.Compare (rd.TagPrefix, tagPrefix, StringComparison.OrdinalIgnoreCase) != 0)
					continue;
				
				var ard = rd as WebFormsPageInfo.AssemblyRegisterDirective;
				if (ard != null) {
					var type = AssemblyTypeLookup (ard.Namespace, tagName);
					if (type != null)
						return type;
					continue;
				}
				
				var crd = rd as WebFormsPageInfo.ControlRegisterDirective;
				if (crd != null && string.Compare (crd.TagName, tagName, StringComparison.OrdinalIgnoreCase) == 0) {
					var type = GetUserControlType (crd.Src);
					if (type != null)
						return type;
				}
			}

			return null;
		}
		
		public IEnumerable<CompletionData> GetControlCompletionData ()
		{
			return GetControlCompletionData (AssemblyTypeLookup ("System.Web.UI", "Control"));
		}
		
		public IEnumerable<CompletionData> GetControlCompletionData (IType baseType)
		{
			var names = new HashSet<string> ();

			foreach (var rd in GetControls ()) {
				var ard = rd as WebFormsPageInfo.AssemblyRegisterDirective;
				if (ard != null) {
					string prefix = ard.TagPrefix + ":";
					foreach (IType cls in ListControlClasses (baseType, ard.Namespace))
						if (names.Add (prefix + cls.Name))
							yield return new AspTagCompletionData (prefix, cls);
					continue;
				}
				
				var cd = rd as WebFormsPageInfo.ControlRegisterDirective;
				if (cd != null && names.Add (cd.TagPrefix + cd.TagName))
					yield return new CompletionData (string.Concat (cd.TagPrefix, ":", cd.TagName), Gtk.Stock.GoForward);
			}
		}
		
		public IType GetControlType (string tagPrefix, string tagName)
		{
			if (String.IsNullOrEmpty (tagPrefix))
				return null;

			foreach (var rd in GetControls ()) {
				if (string.Compare (rd.TagPrefix, tagPrefix, StringComparison.OrdinalIgnoreCase) != 0)
					continue;
				
				var ard = rd as WebFormsPageInfo.AssemblyRegisterDirective;
				if (ard != null) {
					var type = AssemblyTypeLookup (ard.Namespace, tagName);
					if (type != null)
						return type;
					continue;
				}
				
				var crd = rd as WebFormsPageInfo.ControlRegisterDirective;
				if (crd != null && string.Compare (crd.TagName, tagName, StringComparison.OrdinalIgnoreCase) == 0) {
					return GetUserControlType (crd.Src);
				}	
			}

			return AssemblyTypeLookup ("System.Web.UI", "Control");
		}
		
		public string GetTagPrefix (IType control)
		{
			if (control.Namespace == "System.Web.UI.HtmlControls")
				return string.Empty;
			
			foreach (var rd in GetControls ()) {
				var ard = rd as WebFormsPageInfo.AssemblyRegisterDirective;
				if (ard != null && ard.Namespace == control.Namespace)
					return ard.TagPrefix;
			}

			return null;
		}
		
		IEnumerable<WebFormsPageInfo.RegisterDirective> GetDirectivesForPrefix (string prefix)
		{
			return GetControls ().Where (t => string.Equals (t.TagPrefix, prefix, StringComparison.OrdinalIgnoreCase));
		}
		
		/// <summary>
		/// Gets a tag prefix, also returning the directive that would have to be added if necessary.
		/// </summary>
		public string GetTagPrefixWithNewDirective (IType control, string assemblyName, string desiredPrefix, 
			out WebFormsPageInfo.RegisterDirective directiveNeededToAdd)
		{
			directiveNeededToAdd = null;
			string existingPrefix = GetTagPrefix (control);
			if (existingPrefix != null)
				return existingPrefix;
			
			//TODO: detect control name conflicts 
			string prefix = desiredPrefix;
			if (desiredPrefix == null)
				prefix = GetPrefix (control);
			
			var an = SystemAssemblyService.ParseAssemblyName (assemblyName);
			
			directiveNeededToAdd = new WebFormsPageInfo.AssemblyRegisterDirective (prefix, control.Namespace, an.Name);
			
			return prefix;
		}
		
		#region "Refactoring" operations -- things that modify the file
		
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
				using (var en = GetDirectivesForPrefix (trial).GetEnumerator())
					if (!en.MoveNext ())
						return trial;
				trial = trialPrefix + trialSuffix;
			}
			throw new InvalidOperationException ("Ran out of integer suffixes for tag prefixes");
		}
		
		string ExprToStr (System.CodeDom.CodeExpression expr)
		{
			var p = expr as System.CodeDom.CodePrimitiveExpression;
			return p != null? p.Value as string : null;
		}
		
		public void AddRegisterDirective (WebFormsPageInfo.RegisterDirective directive, TextEditorData editor, bool preserveCaretPosition)
		{
			if (doc == null)
				return;

			var node = GetRegisterInsertionPointNode ();
			if (node == null)
				return;
			
			doc.Info.RegisteredTags.Add (directive);
			
			var line = Math.Max (node.Region.EndLine, node.Region.BeginLine);
			var pos = editor.Document.LocationToOffset (line, editor.Document.GetLine (line - 1).Length);
			if (pos < 0)
				return;
			
			using (var undo = editor.OpenUndoGroup ()) {
				var oldCaret = editor.Caret.Offset;
				
				var inserted = editor.Insert (pos, editor.EolMarker + directive);
				if (preserveCaretPosition) {
					editor.Caret.Offset = (pos < oldCaret)? oldCaret + inserted : oldCaret;
				}
			}
		}
		
		WebFormsDirective GetRegisterInsertionPointNode ()
		{
			foreach (XNode node in doc.XDocument.AllDescendentNodes) {
				var directive = node as WebFormsDirective;
				if (directive != null) {
					switch (directive.Name.Name.ToLower ()) {
					case "page":
					case "control":
					case "master":
					case "register":
						return directive;
					}
				}
			}
			return null;
		}
		
		#endregion

		IList<RegistrationInfo> GetRegistrationInfos ()
		{
			if (project != null && doc != null)
				return project.RegistrationCache.GetInfosForPath (Path.GetDirectoryName (doc.FileName));
			return new[] { WebFormsRegistrationCache.MachineRegistrationInfo };
		}

		IEnumerable<ControlRegistration> GetRegisteredControls ()
		{
			//FIXME: handle removes and clears as well as adds
			return GetRegistrationInfos ().SelectMany (x => x.Controls).Where (c => c.Add);
		}

		IEnumerable<string> GetRegisteredAssemblies ()
		{
			//FIXME: handle removes and clears as well as adds
			//FIXME: wildcards
			return GetRegistrationInfos ().SelectMany (x => x.Assemblies).Where (c => c.Add).Select (c => c.Name);
		}

		public IEnumerable<string> GetRegisteredNamespaces ()
		{
			//FIXME: handle removes and clears as well as adds
			return GetRegistrationInfos ().SelectMany (x => x.Namespaces).Where (c => c.Add).Select (c => c.Namespace);
		}

		public IEnumerable<string> GetUsings ()
		{
			var usings = new HashSet<string> ();

			if (doc != null)
				foreach (var u in doc.Info.Imports)
					usings.Add (u);

			foreach (var u in GetRegisteredNamespaces ())
				usings.Add (u);

			return usings;
		}

		IEnumerable<IAssemblyReference> GetReferencedAssemblies ()
		{
			var references = new HashSet<IAssemblyReference> ();

			if (project != null)
				references.Add (TypeSystemService.GetCompilation (project).MainAssembly.UnresolvedAssembly);

			if (doc != null)
				foreach (var asm in doc.Info.Assemblies.Select (a => a.Name).Select (name => GetReferencedAssembly (name)))
					references.Add (asm);

			foreach (var asm in GetRegisteredAssemblies ().Select (name => GetReferencedAssembly (name)))
				references.Add (asm);

			references.Remove (null);

			return references;
 		}

		IAssemblyReference GetReferencedAssembly (string assemblyName)
		{
			var parsed = SystemAssemblyService.ParseAssemblyName (assemblyName);
			if (string.IsNullOrEmpty (parsed.Name))
				return null;

			var r = GetProjectReference (parsed);
			if (r != null)
				return r;

			string path = GetAssemblyPath (assemblyName);
			if (path != null)
				return TypeSystemService.LoadAssemblyContext (TargetRuntime, TargetFramework, path);

			return null;
		}

		IAssemblyReference GetProjectReference (AssemblyName parsed)
		{
			if (project == null)
				return null;

			var dllName = parsed.Name + ".dll";

			foreach (var reference in project.References) {
				if (reference.ReferenceType == ReferenceType.Package || reference.ReferenceType == ReferenceType.Assembly) {
					foreach (string refPath in reference.GetReferencedFileNames (null))
						if (Path.GetFileName (refPath) == dllName)
							return TypeSystemService.LoadAssemblyContext (project.TargetRuntime, project.TargetFramework, refPath);
				}
				else
					if (reference.ReferenceType == ReferenceType.Project && parsed.Name == reference.Reference) {
						var p = project.ParentSolution.FindProjectByName (reference.Reference) as DotNetProject;
						if (p == null)
							continue;
						return TypeSystemService.GetCompilation (p).MainAssembly.UnresolvedAssembly;
					}
			}

			return null;
		}

		string GetAssemblyPath (string assemblyName)
		{
			var parsed = SystemAssemblyService.ParseAssemblyName (assemblyName);
			if (string.IsNullOrEmpty (parsed.Name))
				return null;

			if (project != null) {
				string localName = Path.Combine (Path.Combine (project.BaseDirectory, "bin"), parsed.Name + ".dll");
				if (File.Exists (localName))
					return localName;
			}

			assemblyName = TargetRuntime.AssemblyContext.GetAssemblyFullName (assemblyName, TargetFramework);
			if (assemblyName == null)
				return null;
			assemblyName = TargetRuntime.AssemblyContext.GetAssemblyNameForVersion (assemblyName, TargetFramework);
			if (assemblyName == null)
				return null;
			return TargetRuntime.AssemblyContext.GetAssemblyLocation (assemblyName, TargetFramework);
		}

		IEnumerable<WebFormsPageInfo.RegisterDirective> GetControls ()
		{
			yield return new WebFormsPageInfo.AssemblyRegisterDirective ("asp", "System.Web.UI.WebControls", "System.Web");

			if (doc != null) {
				foreach (var c in doc.Info.RegisteredTags.OfType<WebFormsPageInfo.ControlRegisterDirective> ())
					yield return c;

				foreach (var c in doc.Info.RegisteredTags.OfType<WebFormsPageInfo.AssemblyRegisterDirective> ())
					yield return c;
			}

			foreach (var c in GetRegisteredControls ()
				.Where (c => c.IsUserControl)
				.Select (r => new WebFormsPageInfo.ControlRegisterDirective (r.TagPrefix, r.TagName, r.Source)))
				yield return c;

			foreach (var c in GetRegisteredControls ()
				.Where (c => c.IsAssembly)
				.Select (r => new WebFormsPageInfo.AssemblyRegisterDirective (r.TagPrefix, r.Namespace, r.Assembly)))
			yield return c;
		}

		static string HtmlControlLookup (string tagName, string typeAttribute)
		{
			switch (tagName.ToLower ()) {
			case "a":
				return "HtmlAnchor";
			case "button":
				return "HtmlButton";
			case "form":
				return "HtmlForm";
			case "head":
				return "HtmlHead";
			case "img":
				return "HtmlImage";
			case "input":
				string val = LookupHtmlInput (typeAttribute);
				return val;
			case "link":
				return "HtmlLink";
			case "meta":
				return "HtmlMeta";
			case "select":
				return "HtmlSelect";
			case "table":
				return "HtmlTable";
			case "th":
			case "td":
				return "HtmlTableCell";
			case "tr":
				return "HtmlTableRow";
			case "textarea":
				return "HtmlTextArea";
			case "title":
				return "HtmlTitle";
			default:
				return "HtmlGenericControl";
			}
		}

		static string LookupHtmlInput (string type)
		{
			switch (type != null? type.ToLower () : null)
			{
			case "button":
			case "reset":
			case "submit":
				return "HtmlInputButton";
			case "checkbox":
				return "HtmlInputCheckBox";
			case "file":
				return "HtmlInputFile";
			case "hidden":
				return "HtmlInputHidden";
			case "image":
				return "HtmlInputImage";
			case "password":
				return "HtmlInputText";
			case "radio":
				return "HtmlInputRadioButton";
			case "text":
				return "HtmlInputText";
			default:
				return "HtmlInputControl";
			}
		}

		IType HtmlControlTypeLookup (string tagName, string typeAttribute)
		{
			var str = HtmlControlLookup (tagName, typeAttribute);
			if (str != null)
				return AssemblyTypeLookup ("System.Web.UI.HtmlControls", str);
			return null;
		}

		static IEnumerable<IType> ListControlClasses (IType baseType, string namespac)
		{
			var baseTypeDefinition = baseType.GetDefinition ();
			if (baseTypeDefinition == null)
				yield break;

			//return classes if they derive from system.web.ui.control
			foreach (var type in baseTypeDefinition.GetSubTypeDefinitions ().Where (t => t.Namespace == namespac))
				if (!type.IsAbstract && type.IsPublic)
					yield return type;

			if (!baseTypeDefinition.IsAbstract && baseTypeDefinition.IsPublic && baseTypeDefinition.Namespace == namespac) {
				yield return baseType;
			}
		}

		IType AssemblyTypeLookup (string namespac, string tagName)
		{
			var fullName = namespac + "." + tagName;
			var type = ReflectionHelper.ParseReflectionName (fullName).Resolve (Compilation);
			if (type.Kind == TypeKind.Unknown)
				return null;
			return type;
		}

		public string GetControlPrefix (IType control)
		{
			if (control.Namespace == "System.Web.UI.WebControls")
				return "asp";
			if (control.Namespace == "System.Web.UI.HtmlControls")
				return string.Empty;

			//todo: handle user controls
			foreach (var info in GetControls ().OfType<WebFormsPageInfo.AssemblyRegisterDirective> ()) {
				if (info.Namespace == control.Namespace) {
					if (AssemblyTypeLookup (info.Namespace, control.Name) != null)
						return info.TagPrefix;
				}
			}

			return null;
		}

		public string GetUserControlTypeName (string virtualPath)
		{
			string typeName = null;
			if (project != null && doc != null) {
				string absolute = project.VirtualToLocalPath (virtualPath, doc.FileName);
				typeName = project.GetCodebehindTypeName (absolute);
			}
			return typeName ?? "System.Web.UI.UserControl";
		}

		IType GetUserControlType (string virtualPath)
		{
			var name = GetUserControlTypeName (virtualPath);
			var type = ReflectionHelper.ParseReflectionName (name).Resolve (Compilation);
			if (type.Kind == TypeKind.Unknown)
				return null;
			return type;
		}
	}

	class AspTagCompletionData : CompletionData
	{
		readonly IType cls;

		public AspTagCompletionData (string prefix, IType cls)
			: base (prefix + cls.Name, Gtk.Stock.GoForward)
		{
			this.cls = cls;
		}

		public override TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			var tt = base.CreateTooltipInformation (smartWrap);
			tt.SignatureMarkup = cls.FullName;
			tt.SummaryMarkup = AmbienceService.GetSummaryMarkup (cls.GetDefinition ());
			return tt;
		}
	}

	class AspAttributeCompletionData : CompletionData
	{
		readonly IMember member;

		public AspAttributeCompletionData (IMember member, string name = null)
			: base (name ?? member.Name, member.GetStockIcon ())
		{
			this.member = member;
		}

		public override TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			var tt = base.CreateTooltipInformation (smartWrap);
			tt.SignatureMarkup = member.Name;
			tt.SummaryMarkup = AmbienceService.GetSummaryMarkup (member);
			return tt;
		}
	}
}
