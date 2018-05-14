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

using MonoDevelop.Core.Assemblies;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Xml.Dom;
using MonoDevelop.AspNet.Projects;
using MonoDevelop.AspNet.WebForms.Dom;
using System.Reflection;
using MonoDevelop.Ide.Editor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.AspNet.WebForms
{
	class WebFormsTypeContext
	{
		Compilation compilation;
		DotNetProject project;
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

		public DotNetProject Project {
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

		public AspNetAppProjectFlavor ProjectFlavor {
			get {
				return project != null ? project.GetFlavor<AspNetAppProjectFlavor> () : null;
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

		public async Task CreateCompilation (CancellationToken token)
		{
			if (compilation != null)
				return;

			const string dummyAsmName = "CompiledAspNetPage";
			compilation = CSharpCompilation.Create (dummyAsmName)
				.AddReferences (await GetReferencedAssemblies (token));
		}
		
		public INamedTypeSymbol GetType (string tagPrefix, string tagName, string htmlTypeAttribute)
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
		
		public IEnumerable<CompletionData> GetControlCompletionData (INamedTypeSymbol baseType)
		{
			var names = new HashSet<string> ();

			foreach (var rd in GetControls ()) {
				var ard = rd as WebFormsPageInfo.AssemblyRegisterDirective;
				if (ard != null) {
					string prefix = ard.TagPrefix + ":";
					foreach (var cls in ListControlClasses (baseType, ard.Namespace, compilation))
						if (names.Add (prefix + cls.Name))
							yield return new AspTagCompletionData (prefix, cls);
					continue;
				}
				
				var cd = rd as WebFormsPageInfo.ControlRegisterDirective;
				if (cd != null && names.Add (cd.TagPrefix + cd.TagName))
					yield return new CompletionData (string.Concat (cd.TagPrefix, ":", cd.TagName), Gtk.Stock.GoForward);
			}
		}
		
		public INamedTypeSymbol GetControlType (string tagPrefix, string tagName)
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
		
		public string GetTagPrefix (INamedTypeSymbol control)
		{
			if (control.ContainingNamespace.GetFullName () == "System.Web.UI.HtmlControls")
				return string.Empty;
			
			foreach (var rd in GetControls ()) {
				var ard = rd as WebFormsPageInfo.AssemblyRegisterDirective;
				if (ard != null && ard.Namespace == control.ContainingNamespace.GetFullName ())
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
		public string GetTagPrefixWithNewDirective (INamedTypeSymbol control, string assemblyName, string desiredPrefix, 
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
			
			directiveNeededToAdd = new WebFormsPageInfo.AssemblyRegisterDirective (prefix, control.ContainingNamespace.GetFullName (), an.Name);
			
			return prefix;
		}
		
		#region "Refactoring" operations -- things that modify the file
		
		string GetPrefix (INamedTypeSymbol control)
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
			string[] namespaces = control.ContainingNamespace.GetFullName ().Split ('.');
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
		
		public void AddRegisterDirective (WebFormsPageInfo.RegisterDirective directive, TextEditor editor, bool preserveCaretPosition)
		{
			if (doc == null)
				return;

			var node = GetRegisterInsertionPointNode ();
			if (node == null)
				return;
			
			doc.Info.RegisteredTags.Add (directive);
			
			var line = Math.Max (node.Region.EndLine, node.Region.BeginLine);
			var pos = editor.LocationToOffset (line, editor.GetLine (line - 1).Length);
			if (pos < 0)
				return;
			
			using (var undo = editor.OpenUndoGroup ()) {
				var oldCaret = editor.CaretOffset;
				var text = editor.FormatString (pos, editor.EolMarker + directive);
				var inserted = text.Length;
				editor.InsertText (pos, text);
				if (preserveCaretPosition) {
					editor.CaretOffset = (pos < oldCaret)? oldCaret + inserted : oldCaret;
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
			if (ProjectFlavor != null && doc != null)
				return ProjectFlavor.RegistrationCache.GetInfosForPath (Path.GetDirectoryName (doc.FileName));
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

		async Task<IEnumerable<MetadataReference>> GetReferencedAssemblies (CancellationToken token)
		{
			var references = new HashSet<MetadataReference> ();

			if (project != null) {
				var result = await TypeSystemService.GetCompilationAsync (project, token);
				if (result != null)
					references.Add (result.ToMetadataReference ());
			}

			var tasks = new List<Task<MetadataReference>> ();
			if (doc != null)
				foreach (var t in doc.Info.Assemblies.Select (a => a.Name).Select (name => GetReferencedAssembly (name, token)))
					tasks.Add (t);

			foreach (var t in GetRegisteredAssemblies ().Select (name => GetReferencedAssembly (name, token)))
				tasks.Add (t);

			MetadataReference[] assemblies = await Task.WhenAll (tasks);
			foreach (var asm in assemblies)
				references.Add (asm);

			references.Remove (null);

			return references;
		}

		async Task<MetadataReference> GetReferencedAssembly (string assemblyName, CancellationToken token)
		{
			var parsed = SystemAssemblyService.ParseAssemblyName (assemblyName);
			if (string.IsNullOrEmpty (parsed.Name))
				return null;

			var r = await GetProjectReference (parsed, token);
			if (r != null)
				return r;

			string path = GetAssemblyPath (assemblyName);
			if (path != null)
				return LoadMetadataReference (path);

			return null;
		}

		async Task<MetadataReference> GetProjectReference (AssemblyName parsed, CancellationToken token)
		{
			if (project == null)
				return null;

			var dllName = parsed.Name + ".dll";

			foreach (var reference in project.References) {
				if (reference.ReferenceType == ReferenceType.Package || reference.ReferenceType == ReferenceType.Assembly) {
					foreach (string refPath in reference.GetReferencedFileNames (null))
						if (Path.GetFileName (refPath) == dllName)
							return LoadMetadataReference (refPath);
				}
				else
					if (reference.ReferenceType == ReferenceType.Project && parsed.Name == reference.Reference) {
						var p = reference.ResolveProject (project.ParentSolution) as DotNetProject;
						if (p == null)
							continue;
						var result = await TypeSystemService.GetCompilationAsync (p);
						if (result != null) {
							return result.ToMetadataReference ();
						}
						return null;
					}
			}

			return null;
		}

		MetadataReference LoadMetadataReference (string path)
		{
			var roslynProject = TypeSystemService.GetProject (Project);
			var workspace = (MonoDevelopWorkspace)roslynProject.Solution.Workspace;
			var reference = workspace.MetadataReferenceManager.GetOrCreateMetadataReferenceSnapshot (path, MetadataReferenceProperties.Assembly);

			return reference;
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

		INamedTypeSymbol HtmlControlTypeLookup (string tagName, string typeAttribute)
		{
			var str = HtmlControlLookup (tagName, typeAttribute);
			if (str != null)
				return AssemblyTypeLookup ("System.Web.UI.HtmlControls", str);
			return null;
		}

		static IEnumerable<INamedTypeSymbol> ListControlClasses (INamedTypeSymbol baseType, string namespac, Compilation compilation)
		{
			var baseTypeDefinition = baseType;
			if (baseTypeDefinition == null)
				yield break;

			//return classes if they derive from system.web.ui.control
			foreach (var type in GetSubTypes (baseTypeDefinition, compilation).Where (t => TypeHasNamespace (t, namespac)))
				if (!type.IsAbstract && type.DeclaredAccessibility == Accessibility.Public)
					yield return type;

			if (!baseTypeDefinition.IsAbstract && baseTypeDefinition.DeclaredAccessibility == Accessibility.Public && TypeHasNamespace (baseTypeDefinition, namespac)) {
				yield return baseType;
			}
		}

		static IEnumerable<INamedTypeSymbol> GetSubTypes (INamedTypeSymbol baseType, Compilation compilation)
		{
			return compilation.GlobalNamespace.GetAllTypes().Where (t => t.IsDerivedFromClass (baseType));
		}

		static bool TypeHasNamespace (INamedTypeSymbol type, string namespac)
		{
			return type.ContainingNamespace != null && type.ContainingNamespace.GetFullName () == namespac;
		}

		INamedTypeSymbol AssemblyTypeLookup (string namespac, string tagName)
		{
			var fullName = namespac + "." + tagName;
			var type = compilation.GetTypeByMetadataName (fullName);
			if (type == null || type.Kind == SymbolKind.ErrorType)
				return null;
			return type;
		}

		public string GetControlPrefix (INamedTypeSymbol control)
		{
			if (control.ContainingNamespace.ToDisplayString (SymbolDisplayFormat.CSharpErrorMessageFormat) == "System.Web.UI.WebControls")
				return "asp";
			if (control.ContainingNamespace.ToDisplayString (SymbolDisplayFormat.CSharpErrorMessageFormat) == "System.Web.UI.HtmlControls")
				return string.Empty;

			//todo: handle user controls
			foreach (var info in GetControls ().OfType<WebFormsPageInfo.AssemblyRegisterDirective> ()) {
				if (info.Namespace == control.ContainingNamespace.ToDisplayString (SymbolDisplayFormat.CSharpErrorMessageFormat)) {
					if (AssemblyTypeLookup (info.Namespace, control.Name) != null)
						return info.TagPrefix;
				}
			}

			return null;
		}

		public string GetUserControlTypeName (string virtualPath)
		{
			string typeName = null;
			if (ProjectFlavor != null && doc != null) {
				string absolute = ProjectFlavor.VirtualToLocalPath (virtualPath, doc.FileName);
				typeName = ProjectFlavor.GetCodebehindTypeName (absolute);
			}
			return typeName ?? "System.Web.UI.UserControl";
		}

		INamedTypeSymbol GetUserControlType (string virtualPath)
		{
			var name = GetUserControlTypeName (virtualPath);
			var type = compilation.GetTypeByMetadataName (name);
			if (type.Kind == SymbolKind.ErrorType)
				return null;
			return type;
		}

		public INamedTypeSymbol GetTypeByMetadataName (string fullyQualifiedMetadataName)
		{
			return compilation.GetTypeByMetadataName (fullyQualifiedMetadataName);
		}
	}

	class AspTagCompletionData : CompletionData
	{
		readonly INamedTypeSymbol cls;

		public AspTagCompletionData (string prefix, INamedTypeSymbol cls)
			: base (prefix + cls.Name, Gtk.Stock.GoForward)
		{
			this.cls = cls;
		}

		public override async Task<TooltipInformation> CreateTooltipInformation (bool smartWrap, CancellationToken token)
		{
			var tt = await base.CreateTooltipInformation (smartWrap, token);
			tt.SignatureMarkup = cls.GetFullName ();
			tt.SummaryMarkup = await Task.Run (() => Ambience.GetSummaryMarkup (cls));
			return tt;
		}
	}

	class AspAttributeCompletionData : CompletionData
	{
		readonly Microsoft.CodeAnalysis.ISymbol member;

		public AspAttributeCompletionData (Microsoft.CodeAnalysis.ISymbol member, string name = null)
			: base (name ?? member.Name, member.GetStockIcon ())
		{
			this.member = member;
		}

		public override async Task<TooltipInformation> CreateTooltipInformation (bool smartWrap, CancellationToken token)
		{
			var tt = await base.CreateTooltipInformation (smartWrap, token);
			tt.SignatureMarkup = member.Name;
			tt.SummaryMarkup = await Task.Run (() => Ambience.GetSummaryMarkup (member));
			return tt;
		}
	}
}
