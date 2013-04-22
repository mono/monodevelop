// 
// ResolveCommand.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.TextEditor;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.Completion;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace MonoDevelop.Refactoring
{
	public class ResolveCommandHandler : CommandHandler
	{

		public static bool ResolveAt (Document doc, out ResolveResult resolveResult, out AstNode node, CancellationToken token = default (CancellationToken))
		{
			var parsedDocument = doc.ParsedDocument;
			resolveResult = null;
			node = null;
			if (parsedDocument == null)
				return false;
			var unit = parsedDocument.GetAst<SyntaxTree> ();
			var parsedFile = parsedDocument.ParsedFile as CSharpUnresolvedFile;
			if (unit == null || parsedFile == null)
				return false;
			try {
				var location = RefactoringService.GetCorrectResolveLocation (doc, doc.Editor.Caret.Location);
				resolveResult = ResolveAtLocation.Resolve (doc.Compilation, parsedFile, unit, location, out node, token);
				if (resolveResult == null || node is Statement)
					return false;
			} catch (OperationCanceledException) {
				return false;
			} catch (Exception e) {
				Console.WriteLine ("Got resolver exception:" + e);
				return false;
			}
			return true;
		}

		protected override void Update (CommandArrayInfo ainfo)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || doc.ParsedDocument == null)
				return;

			ResolveResult resolveResult;
			AstNode node;
			if (!ResolveAt (doc, out resolveResult, out node)) {
				var location = RefactoringService.GetCorrectResolveLocation (doc, doc.Editor.Caret.Location);
				resolveResult = GetHeuristicResult (doc, location, ref node);
				if (resolveResult == null)
					return;
			}
			var resolveMenu = new CommandInfoSet ();
			resolveMenu.Text = GettextCatalog.GetString ("Resolve");
			
			var possibleNamespaces = GetPossibleNamespaces (doc, node, ref resolveResult);

			bool addUsing = !(resolveResult is AmbiguousTypeResolveResult);
			if (addUsing) {
				foreach (var t in possibleNamespaces.Where (tp => tp.IsAccessibleWithGlobalUsing)) {
					string ns = t.Namespace;
					var reference = t.Reference;
					var info = resolveMenu.CommandInfos.Add (
						string.Format ("using {0};", ns),
						new System.Action (new AddImport (doc, resolveResult, ns, reference, true, node).Run)
					);
					info.Icon = MonoDevelop.Ide.Gui.Stock.AddNamespace;
				}
			}
			
			bool resolveDirect = !(resolveResult is UnknownMemberResolveResult);
			if (resolveDirect) {
				if (resolveMenu.CommandInfos.Count > 0)
					resolveMenu.CommandInfos.AddSeparator ();
				if (node is ObjectCreateExpression)
					node = ((ObjectCreateExpression)node).Type;
				foreach (var t in possibleNamespaces) {
					string ns = t.Namespace;
					var reference = t.Reference;
					resolveMenu.CommandInfos.Add (string.Format ("{0}", ns + "." + doc.Editor.GetTextBetween (node.StartLocation, node.EndLocation)), new System.Action (new AddImport (doc, resolveResult, ns, reference, false, node).Run));
				}
			}
			
			if (resolveMenu.CommandInfos.Count > 0)
				ainfo.Insert (0, resolveMenu);
		}

		static string CreateStub (Document doc, int offset)
		{
			if (offset <= 0)
				return "";
			string text = doc.Editor.GetTextAt (0, Math.Min (doc.Editor.Length, offset));
			var stub = new StringBuilder (text);
			CSharpCompletionEngine.AppendMissingClosingBrackets (stub, text, false);
			return stub.ToString ();
		}

		static ResolveResult GetHeuristicResult (Document doc, DocumentLocation location, ref AstNode node)
		{
			int offset = doc.Editor.Caret.Offset;
			bool wasLetter = false, wasWhitespaceAfterLetter = false;
			while (offset < doc.Editor.Length) {
				char ch = doc.Editor.GetCharAt (offset);
				bool isLetter = char.IsLetterOrDigit (ch) || ch == '_';
				bool isWhiteSpace = char.IsWhiteSpace (ch);
				bool isValidPunc = ch == '.' || ch == '<' || ch == '>';

				if (!(wasLetter && wasWhitespaceAfterLetter) && (isLetter || isWhiteSpace || isValidPunc)) {
					if (isValidPunc) {
						wasWhitespaceAfterLetter = false;
						wasLetter = false;
					}
					offset++;
				} else {
					offset--;
					while (offset > 1) {
						ch = doc.Editor.GetCharAt (offset - 1);
						if (!(ch == '.' || char.IsWhiteSpace (ch)))
							break;
						offset--;
					}
					break;
				}

				wasLetter |= isLetter;
				if (wasLetter)
					wasWhitespaceAfterLetter |= isWhiteSpace;
			}

			var unit = SyntaxTree.Parse (CreateStub (doc, offset), doc.FileName);

			return ResolveAtLocation.Resolve (
				doc.Compilation, 
				doc.ParsedDocument.ParsedFile as CSharpUnresolvedFile,
				unit,
				location, 
				out node);
		}

		public static HashSet<PossibleNamespace> GetPossibleNamespaces (Document doc, AstNode node, ref ResolveResult resolveResult)
		{
			var location = RefactoringService.GetCorrectResolveLocation (doc, doc.Editor.Caret.Location);

			if (resolveResult == null || resolveResult.Type.FullName == "System.Void")
				resolveResult = GetHeuristicResult (doc, location, ref node) ?? resolveResult;
			var foundNamespaces = GetPossibleNamespaces (doc, node, resolveResult, location);
			
			if (!(resolveResult is AmbiguousTypeResolveResult)) {
				var usedNamespaces = RefactoringOptions.GetUsedNamespaces (doc, location);
				foundNamespaces = foundNamespaces.Where (n => !usedNamespaces.Contains (n.Namespace));
			}

			return new HashSet<PossibleNamespace> (foundNamespaces);
		}

		static int GetTypeParameterCount (AstNode node)
		{
			if (node is ObjectCreateExpression)
				node = ((ObjectCreateExpression)node).Type;
			if (node is SimpleType)
				return ((SimpleType)node).TypeArguments.Count;
			if (node is MemberType)
				return ((MemberType)node).TypeArguments.Count;
			if (node is IdentifierExpression)
				return ((IdentifierExpression)node).TypeArguments.Count;
			return 0;
		}

		public class PossibleNamespace
		{
			public string Namespace { get; private set; }
			public bool IsAccessibleWithGlobalUsing { get; private set; }
			public MonoDevelop.Projects.ProjectReference Reference { get; private set; }

			public PossibleNamespace (string @namespace, bool isAccessibleWithGlobalUsing, MonoDevelop.Projects.ProjectReference reference = null)
			{
				this.Namespace = @namespace;
				this.IsAccessibleWithGlobalUsing = isAccessibleWithGlobalUsing;
				this.Reference = reference;
			}
		}

		static IEnumerable<PossibleNamespace> GetPossibleNamespaces (Document doc, AstNode node, ResolveResult resolveResult, DocumentLocation location)
		{
			var unit = doc.ParsedDocument.GetAst<SyntaxTree> ();
			if (unit == null)
				yield break;

			int tc = GetTypeParameterCount (node);
			var attribute = unit.GetNodeAt<ICSharpCode.NRefactory.CSharp.Attribute> (location);
			bool isInsideAttributeType = attribute != null && attribute.Type.Contains (location);

			var compilations = new List<Tuple<ICompilation, MonoDevelop.Projects.ProjectReference>> ();
			compilations.Add (Tuple.Create (doc.Compilation, (MonoDevelop.Projects.ProjectReference)null));
			var referencedItems = doc.Project.GetReferencedItems (IdeApp.Workspace.ActiveConfiguration).ToList ();
			foreach (var project in doc.Project.ParentSolution.GetAllProjects ()) {
				if (project == doc.Project || referencedItems.Contains (project))
					continue;
				var comp = TypeSystemService.GetCompilation (project);
				if (comp == null)
					continue;
				compilations.Add (Tuple.Create (comp, new MonoDevelop.Projects.ProjectReference (project)));
			}

			var netProject = doc.Project as DotNetProject;
			if (netProject == null) 
				yield break;
			var frameworkLookup = TypeSystemService.GetFrameworkLookup (netProject);

			if (resolveResult is UnknownMemberResolveResult) {
				var umResult = (UnknownMemberResolveResult)resolveResult;
				foreach (var r in frameworkLookup.LookupExtensionMethod (umResult.MemberName)) {
					var systemAssembly = netProject.AssemblyContext.GetAssemblyFromFullName (r.FullName, r.Package, netProject.TargetFramework);
					if (systemAssembly == null)
						continue;
					compilations.Add (Tuple.Create (TypeSystemService.GetCompilation (systemAssembly, doc.Compilation), new MonoDevelop.Projects.ProjectReference (systemAssembly)));
				}
			}

			var lookup = new MemberLookup (null, doc.Compilation.MainAssembly);
			foreach (var comp in compilations) {
				var compilation = comp.Item1;
				var requiredReference = comp.Item2;
				if (resolveResult is AmbiguousTypeResolveResult) {
					var aResult = resolveResult as AmbiguousTypeResolveResult;
					var file = doc.ParsedDocument.ParsedFile as CSharpUnresolvedFile;
					var scope = file.GetUsingScope (location).Resolve (compilation);
					while (scope != null) {
						foreach (var u in scope.Usings) {
							foreach (var typeDefinition in u.Types) {
								if (typeDefinition.Name == aResult.Type.Name && 
									typeDefinition.TypeParameterCount == tc &&
									lookup.IsAccessible (typeDefinition, false)) {
									yield return new PossibleNamespace (typeDefinition.Namespace, true, requiredReference);
								}
							}
						}
						scope = scope.Parent;
					}
				}

				if (resolveResult is UnknownIdentifierResolveResult) {
					var uiResult = resolveResult as UnknownIdentifierResolveResult;
					string possibleAttributeName = isInsideAttributeType ? uiResult.Identifier + "Attribute" : null;
					foreach (var typeDefinition in compilation.GetAllTypeDefinitions ()) {
						if ((typeDefinition.Name == uiResult.Identifier || typeDefinition.Name == possibleAttributeName) && typeDefinition.TypeParameterCount == tc && 
							lookup.IsAccessible (typeDefinition, false)) {
							if (typeDefinition.DeclaringTypeDefinition != null) {
								var builder = new TypeSystemAstBuilder (new CSharpResolver (doc.Compilation));
								yield return new PossibleNamespace (builder.ConvertType (typeDefinition.DeclaringTypeDefinition).ToString (), false, requiredReference);
							} else {
								yield return new PossibleNamespace (typeDefinition.Namespace, true, requiredReference);
							}
						}
					}
				}

				if (resolveResult is UnknownMemberResolveResult) {
					var umResult = (UnknownMemberResolveResult)resolveResult;
					string possibleAttributeName = isInsideAttributeType ? umResult.MemberName + "Attribute" : null;
					foreach (var typeDefinition in compilation.GetAllTypeDefinitions ().Where (t => t.HasExtensionMethods)) {
						foreach (var method in typeDefinition.Methods.Where (m => m.IsExtensionMethod && (m.Name == umResult.MemberName || m.Name == possibleAttributeName))) {
							IType[] inferredTypes;
							if (CSharpResolver.IsEligibleExtensionMethod (
								compilation.Import (umResult.TargetType),
								method,
								true,
								out inferredTypes
							)) {
								yield return new PossibleNamespace (typeDefinition.Namespace, true, requiredReference);
								goto skipType;
							}
						}
						skipType:
						;
					}
				}
				
				if (resolveResult is ErrorResolveResult) {
					var identifier = unit != null ? unit.GetNodeAt<Identifier> (location) : null;
					if (identifier != null) {
						var uiResult = resolveResult as UnknownIdentifierResolveResult;
						if (uiResult != null) {
							string possibleAttributeName = isInsideAttributeType ? uiResult.Identifier + "Attribute" : null;
							foreach (var typeDefinition in compilation.GetAllTypeDefinitions ()) {
								if ((identifier.Name == uiResult.Identifier || identifier.Name == possibleAttributeName) && 
								    typeDefinition.TypeParameterCount == tc && 
								    lookup.IsAccessible (typeDefinition, false))
									yield return new PossibleNamespace (typeDefinition.Namespace, true, requiredReference);
							}
						}
					}
				}
			}
		

			if (resolveResult is UnknownIdentifierResolveResult) {
				var uiResult = resolveResult as UnknownIdentifierResolveResult;

				foreach (var r in frameworkLookup.LookupIdentifier (uiResult.Identifier, tc)) {
					var systemAssembly = netProject.AssemblyContext.GetAssemblyFromFullName (r.FullName, r.Package, netProject.TargetFramework);
					if (systemAssembly == null)
						continue;
					yield return new PossibleNamespace (r.Namespace, true, new MonoDevelop.Projects.ProjectReference (systemAssembly));
				}
			}

		}

		internal class AddImport
		{
			readonly Document doc;
			readonly ResolveResult resolveResult;
			readonly string ns;
			readonly bool addUsing;
			readonly AstNode node;
			readonly MonoDevelop.Projects.ProjectReference reference;

			public AddImport (Document doc, ResolveResult resolveResult, string ns, MonoDevelop.Projects.ProjectReference reference, bool addUsing, AstNode node)
			{
				this.doc = doc;
				this.resolveResult = resolveResult;
				this.ns = ns;
				this.reference = reference;
				this.addUsing = addUsing;
				this.node = node;
			}
			
			public void Run ()
			{
				var loc = doc.Editor.Caret.Location;

				if (reference != null)
					doc.Project.Items.Add (reference);

				if (!addUsing) {
//					var unit = doc.ParsedDocument.GetAst<SyntaxTree> ();
					int offset = doc.Editor.LocationToOffset (node.StartLocation);
					doc.Editor.Insert (offset, ns + ".");
					doc.Editor.Document.CommitLineUpdate (loc.Line);
					return;
				}

				var generator = doc.CreateCodeGenerator ();

				if (resolveResult is NamespaceResolveResult) {
					generator.AddLocalNamespaceImport (doc, ns, loc);
				} else {
					generator.AddGlobalNamespaceImport (doc, ns);
				}
			}
		}
		
		protected override void Run (object data)
		{
			var del = (System.Action)data;
			if (del != null)
				del ();
		}
	}
}

