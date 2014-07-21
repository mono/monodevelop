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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.CSharp;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Refactoring
{
	public class ResolveCommandHandler : CommandHandler
	{
		/*
		public static bool ResolveAt (Document doc, out ResolveResult resolveResult, out AstNode node, CancellationToken token = default (CancellationToken))
		{
			if (doc == null)
				throw new ArgumentNullException ("doc");
			var editor = doc.Editor;
			if (editor == null || editor.MimeType != "text/x-csharp") {
				node = null;
				resolveResult = null;
				return false;
			}
			if (!InternalResolveAt (doc, out resolveResult, out node)) {
				var location = RefactoringService.GetCorrectResolveLocation (doc, editor.Caret.Location);
				resolveResult = GetHeuristicResult (doc, location, ref node);
				if (resolveResult == null)
					return false;
			}
			var oce = node as ObjectCreateExpression;
			if (oce != null)
				node = oce.Type;
			return true;
		}

		static bool InternalResolveAt (Document doc, out ResolveResult resolveResult, out AstNode node, CancellationToken token = default (CancellationToken))
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

		static string CreateStub (Document doc, int offset)
		{
			if (offset <= 0)
				return "";
			string text = doc.Editor.GetTextAt (0, Math.Min (doc.Editor.Length, offset));
			var stub = new StringBuilder (text);
			CSharpCompletionEngine.AppendMissingClosingBrackets (stub, false);
			return stub.ToString ();
		}
*/
		protected override void Update (CommandArrayInfo ainfo)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || doc.AnalysisDocument == null)
				return;

			var resolveMenu = new CommandInfoSet ();
			resolveMenu.Text = GettextCatalog.GetString ("Resolve");

			var possibleNamespaces = GetPossibleNamespaces (doc.Editor, doc, doc.Editor.CaretLocation);

			foreach (var t in possibleNamespaces.Where (tp => tp.OnlyAddReference)) {
				var reference = t.Reference;
				var info = resolveMenu.CommandInfos.Add (
					t.GetImportText (),
					new System.Action (new AddImport (doc.Editor, doc, possibleNamespaces.ResolveResult, null, reference, true, possibleNamespaces.Node).Run)
				);
				info.Icon = MonoDevelop.Ide.Gui.Stock.AddNamespace;

			}

			if (possibleNamespaces.AddUsings) {
				foreach (var t in possibleNamespaces.Where (tp => tp.IsAccessibleWithGlobalUsing)) {
					string ns = t.Namespace;
					var reference = t.Reference;
					var info = resolveMenu.CommandInfos.Add (
						t.GetImportText (),
						new System.Action (new AddImport (doc.Editor, doc, possibleNamespaces.ResolveResult, ns, reference, true, possibleNamespaces.Node).Run)
					);
					info.Icon = MonoDevelop.Ide.Gui.Stock.AddNamespace;
				}
			}

			if (possibleNamespaces.AddFullyQualifiedName) {
				if (resolveMenu.CommandInfos.Count > 0)
					resolveMenu.CommandInfos.AddSeparator ();
				var node = possibleNamespaces.Node;
				foreach (var t in possibleNamespaces) {
					string ns = t.Namespace;
					var reference = t.Reference;
					resolveMenu.CommandInfos.Add (t.GetInsertNamespaceText (doc.Editor.GetTextBetween (node.Span.Start, node.Span.End)), new System.Action (new AddImport (doc.Editor, doc, possibleNamespaces.ResolveResult, ns, reference, false, node).Run));
				}
			}

			if (resolveMenu.CommandInfos.Count > 0)
				ainfo.Insert (0, resolveMenu);
		}


		/*
		static ResolveResult GetHeuristicResult (Document doc, DocumentLocation location, ref AstNode node)
		{
			var editor = doc.Editor;
			if (editor == null || editor.Caret == null)
				return null;
			int offset = editor.Caret.Offset;
			bool wasLetter = false, wasWhitespaceAfterLetter = false;
			while (offset < editor.Length) {
				char ch = editor.GetCharAt (offset);
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
						ch = editor.GetCharAt (offset - 1);
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
		}*/


		public static PossibleNamespaceResult GetPossibleNamespaces (IReadonlyTextDocument editor, DocumentContext doc, DocumentLocation loc, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (doc == null)
				throw new ArgumentNullException ("doc");
			var analysisDocument = doc.AnalysisDocument;
			if (analysisDocument == null)
				return new PossibleNamespaceResult (new List<PossibleNamespace> (), null, new SymbolInfo (), false, false);
			var semanticModel = analysisDocument.GetSemanticModelAsync (cancellationToken).Result; 
			var location = RefactoringService.GetCorrectResolveLocation (editor, loc);
			var offset = editor.LocationToOffset (location);
			bool addUsings = true;
			bool fullyQualify = true;

			var token = semanticModel.SyntaxTree.GetRoot (cancellationToken).FindToken (offset);
			var node = token.Parent;
			var resolveResult = semanticModel.GetSymbolInfo (node, cancellationToken); 


			// if (resolveResult == null || resolveResult.Type.FullName == "System.Void")
			//	resolveResult = GetHeuristicResult (doc, location, ref node) ?? resolveResult;
			List<PossibleNamespace> foundNamespaces;
			if (node.Parent.CSharpKind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression &&
				node.Parent.Parent.CSharpKind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression) {
				foundNamespaces = GetPossibleNamespacesForExtensionMethods (editor, doc, semanticModel, node, resolveResult, offset).ToList ();
			} else {
				foundNamespaces = GetPossibleNamespacesForTypes (editor, doc, semanticModel, node, resolveResult, offset).ToList ();
			}

			//			if (!(resolveResult is AmbiguousTypeResolveResult)) {
			//				var usedNamespaces = RefactoringOptions.GetUsedNamespacesAsync (doc, doc.Editor.LocationToOffset (location)).Result;
			//				foundNamespaces = foundNamespaces.Where (n => !usedNamespaces.Contains (n.Namespace));
			//			}
			var result = new List<PossibleNamespace> ();
			foreach (var ns in foundNamespaces) {
				if (result.Any (n => n.Namespace == ns.Namespace))
					continue;
				result.Add (ns); 
			}
			return new PossibleNamespaceResult (result, node, resolveResult, addUsings, fullyQualify);
		}

		static int GetTypeParameterCount (SyntaxNode node, out SyntaxToken identifier)
		{
			var generic = node as GenericNameSyntax;
			if (generic != null) {
				identifier = generic.Identifier;
				return generic.Arity;
			}
			identifier = node.ChildTokens ().First ();
			return 0;
		}

		public class PossibleNamespaceResult : IReadOnlyList<PossibleNamespace>
		{
			readonly IReadOnlyList<PossibleNamespace> namespaces;

			public readonly SyntaxNode Node;
			public readonly SymbolInfo ResolveResult;

			public readonly bool AddUsings;
			public readonly bool AddFullyQualifiedName;

			internal PossibleNamespaceResult (IReadOnlyList<PossibleNamespace> namespaces, SyntaxNode node, SymbolInfo resolveResult, bool addUsings, bool addFullyQualifiedName)
			{
				this.namespaces = namespaces;
				this.Node = node;
				this.ResolveResult = resolveResult;
				this.AddUsings = addUsings;
				this.AddFullyQualifiedName = addFullyQualifiedName;
			}

			#region IReadOnlyList implementation

			public IEnumerator<PossibleNamespace> GetEnumerator ()
			{
				return namespaces.GetEnumerator ();
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
			{
				return namespaces.GetEnumerator ();
			}

			public PossibleNamespace this [int index] {
				get {
					return namespaces [index];
				}
			}

			public int Count {
				get {
					return namespaces.Count;
				}
			}
			#endregion
		}

		public class PossibleNamespace
		{
			public string Namespace { get; private set; }
			public bool IsAccessibleWithGlobalUsing { get; private set; }

			public bool OnlyAddReference { get { return !IsAccessibleWithGlobalUsing && Reference != null; } }
			public MonoDevelop.Projects.ProjectReference Reference { get; private set; }

			internal PossibleNamespace (string @namespace, bool isAccessibleWithGlobalUsing, MonoDevelop.Projects.ProjectReference reference = null)
			{
				this.Namespace = @namespace;
				this.IsAccessibleWithGlobalUsing = isAccessibleWithGlobalUsing;
				this.Reference = reference;
			}

			string GetLibraryName ()
			{
				var txt = Reference.Reference;
				int idx = txt.IndexOf (',');
				if (idx >= 0)
					return txt.Substring (0, idx);
				return txt;
			}

			public string GetImportText ()
			{
				if (OnlyAddReference)
					return GettextCatalog.GetString (
						"Reference '{0}'", 
						GetLibraryName ());
				if (Reference != null) 
					return GettextCatalog.GetString (
						"Reference '{0}' and use '{1}'", 
						GetLibraryName (),
						string.Format ("using {0};", Namespace));

				return string.Format ("using {0};", Namespace);
			}

			public string GetInsertNamespaceText (string member)
			{
				if (Reference != null) 
					return GettextCatalog.GetString (
						"Reference '{0}' and use '{1}'", 
						GetLibraryName (),
						Namespace + "." + member
					);
				return Namespace + "." + member;
			}


			internal static PossibleNamespace Create (INamespaceSymbol containingNamespace, bool isAccessibleWithGlobalUsing = true, MonoDevelop.Projects.ProjectReference reference = null)
			{
				return new PossibleNamespace (ImportSymbolCache.GetNamespaceString (containingNamespace), isAccessibleWithGlobalUsing, reference);
			}
		}

		internal static bool CanBeReferenced (MonoDevelop.Projects.Project project, SystemAssembly systemAssembly)
		{
			var netProject = project as DotNetProject;
			if (netProject == null)
				return false;
			var result = netProject.TargetRuntime.AssemblyContext.GetAssemblyNameForVersion(systemAssembly.FullName, netProject.TargetFramework);
			return !string.IsNullOrEmpty (result);
		}

		static string GetNestedTypeString (INamedTypeSymbol type)
		{
			var sb = new StringBuilder ();
			while (type != null) {
				if (sb.Length > 0) {
					sb.Insert (0, type.Name + ".");
				} else {
					sb.Append (type.Name);
				}
				type = type.ContainingType;
			}
			return sb.ToString ();
		}

		static bool CanReference (DocumentContext doc, MonoDevelop.Projects.ProjectReference projectReference)
		{
			var project = doc.Project as DotNetProject;
			if (project == null || projectReference == null || project.ParentSolution == null)
				return true;
			switch (projectReference.ReferenceType) {
			case ReferenceType.Project:
				var referenceProject = projectReference.ResolveProject (project.ParentSolution) as DotNetProject;
				if (referenceProject == null)
					return true;
				string reason;
				return project.CanReferenceProject (referenceProject, out reason);
			}
			return true;

		}

		static IEnumerable<PossibleNamespace> GetPossibleNamespacesForTypes (IReadonlyTextDocument editor, DocumentContext doc, SemanticModel semanticModel, SyntaxNode node, SymbolInfo resolveResult, int location)
		{
			if (resolveResult.Symbol != null)
				yield break;
			var netProject = doc.Project as DotNetProject;
			if (netProject == null)
				yield break;

			if (resolveResult.CandidateReason == CandidateReason.Ambiguous) {
				foreach (var candidate in resolveResult.CandidateSymbols) {
					if (candidate is INamedTypeSymbol)
						yield return PossibleNamespace.Create (((INamedTypeSymbol)candidate).ContainingNamespace);
				}
			}

			SyntaxToken identifier;
			int tc = GetTypeParameterCount (node, out identifier);
			bool isInsideAttributeType = identifier.CSharpKind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.IdentifierToken &&
				identifier.Parent.CSharpKind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.IdentifierName &&
				identifier.Parent.Parent.CSharpKind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.Attribute;

			var compilations = new List<Tuple<Compilation, MonoDevelop.Projects.ProjectReference>> ();
			compilations.Add (Tuple.Create (doc.GetCompilationAsync ().Result, (MonoDevelop.Projects.ProjectReference)null));
			var referencedItems = IdeApp.Workspace != null ? netProject.GetReferencedItems (IdeApp.Workspace.ActiveConfiguration).ToList () : (IEnumerable<SolutionItem>)new SolutionItem[0];
			var solution = netProject != null ? netProject.ParentSolution : null;
			if (solution != null) {
				foreach (var curProject in solution.GetAllProjects ()) {
					if (curProject == netProject || referencedItems.Contains (curProject))
						continue;

					var otherRefes = IdeApp.Workspace != null ? curProject.GetReferencedItems (IdeApp.Workspace.ActiveConfiguration).ToList () : (IEnumerable<SolutionItem>)new SolutionItem[0];
					if (otherRefes.Contains (netProject))
						continue;

					var comp = RoslynTypeSystemService.GetProject (curProject).GetCompilationAsync ().Result;
					if (comp == null)
						continue;
					compilations.Add (Tuple.Create (comp, new MonoDevelop.Projects.ProjectReference (curProject)));
				}
			}

			//			FrameworkLookup frameworkLookup;
			//			if (!TypeSystemService.TryGetFrameworkLookup (netProject, out frameworkLookup))
			//				frameworkLookup = null;
			//			if (frameworkLookup != null && resolveResult is UnknownMemberResolveResult) {
			//				var umResult = (UnknownMemberResolveResult)resolveResult;
			//				try {
			//					foreach (var r in frameworkLookup.GetExtensionMethodLookups (umResult)) {
			//						var systemAssembly = netProject.AssemblyContext.GetAssemblyFromFullName (r.FullName, r.Package, netProject.TargetFramework);
			//						if (systemAssembly == null)
			//							continue;
			//						if (CanBeReferenced (doc.Project, systemAssembly))
			//							compilations.Add (Tuple.Create (TypeSystemService.GetCompilation (systemAssembly, doc.Compilation), new MonoDevelop.Projects.ProjectReference (systemAssembly)));
			//					}
			//				}

			bool foundIdentifier = false;
			var name = identifier.ToString ();
			string possibleAttributeName = isInsideAttributeType ? name + "Attribute" : null;
			var typeStack = new Stack<INamedTypeSymbol> ();

			foreach (var comp in compilations) {
				var compilation = comp.Item1;
				var requiredReference = comp.Item2;
				var ns = new Stack<INamespaceSymbol> ();
				ns.Push (semanticModel.Compilation.GlobalNamespace);
				while (ns.Count > 0) {
					var curNs = ns.Pop ();

					foreach (var type in curNs.GetTypeMembers (name, tc)) {
						if (!semanticModel.IsAccessible (location, type))
							continue;
						yield return new PossibleNamespace (ImportSymbolCache.GetNamespaceString (curNs), true, requiredReference);
					} 
					if (possibleAttributeName != null) {
						foreach (var type in curNs.GetTypeMembers (possibleAttributeName, tc)) {
							if (!semanticModel.IsAccessible (location, type))
								continue;
							yield return new PossibleNamespace (ImportSymbolCache.GetNamespaceString (curNs), true, requiredReference);
						} 
					}

					// Search nested types.
					foreach (var type in curNs.GetTypeMembers ()) {
						if (!semanticModel.IsAccessible (location, type))
							continue;
						typeStack.Push (type);
						while (typeStack.Count > 0) {
							var nested = typeStack.Pop ();
							foreach (var childType in nested.GetTypeMembers ()) {
								if (!semanticModel.IsAccessible (location, childType))
									continue;
								if (childType.Arity == tc && (childType.Name == name || childType.Name == possibleAttributeName)) {
									if (CanReference(doc, requiredReference))
										yield return new PossibleNamespace (ImportSymbolCache.GetNamespaceString (curNs) + "." + GetNestedTypeString(nested), false, requiredReference);
								}
								typeStack.Push (childType);
							}
						}
					}
					foreach (var childNs in curNs.GetNamespaceMembers ()) {
						ns.Push (childNs);
					}
				}
				/*
				
				var allTypes =  compilation == doc.Compilation ? compilation.GetAllTypeDefinitions () : compilation.MainAssembly.GetAllTypeDefinitions ();
				if (resolveResult is UnknownIdentifierResolveResult) {
					var uiResult = resolveResult as UnknownIdentifierResolveResult;
					string possibleAttributeName = isInsideAttributeType ? uiResult.Identifier + "Attribute" : uiResult.Identifier;
					foreach (var typeDefinition in allTypes) {
						if ((typeDefinition.Name == possibleAttributeName || typeDefinition.Name == uiResult.Identifier) && typeDefinition.TypeParameterCount == tc &&
						    lookup.IsAccessible (typeDefinition, false)) {
							if (CanReference (doc, requiredReference)) {
								if (typeDefinition.DeclaringTypeDefinition != null) {
									var builder = new TypeSystemAstBuilder (new CSharpResolver (doc.Compilation));
									foundIdentifier = true;
									yield return new PossibleNamespace (builder.ConvertType (typeDefinition.DeclaringTypeDefinition).ToString (), false, requiredReference);
								} else {
									foundIdentifier = true;
									yield return new PossibleNamespace (typeDefinition.Namespace, true, requiredReference);
								}
							}
						}
					}
				}

				if (resolveResult is UnknownMemberResolveResult) {
					var umResult = (UnknownMemberResolveResult)resolveResult;
					string possibleAttributeName = isInsideAttributeType ? umResult.MemberName + "Attribute" : umResult.MemberName;
					foreach (var typeDefinition in allTypes.Where (t => t.HasExtensionMethods)) {
						if (!lookup.IsAccessible (typeDefinition, false))
							continue;
						foreach (var method in typeDefinition.Methods.Where (m => m.IsExtensionMethod && (m.Name == possibleAttributeName || m.Name == umResult.MemberName))) {
							if (!lookup.IsAccessible (method, false))
								continue;
							IType[] inferredTypes;
							if (CSharpResolver.IsEligibleExtensionMethod (
								compilation.Import (umResult.TargetType),
								method,
								true,
								out inferredTypes
							)) {
								if (CanReference(doc, requiredReference))
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
							string possibleAttributeName = isInsideAttributeType ? uiResult.Identifier + "Attribute" : uiResult.Identifier;
							foreach (var typeDefinition in allTypes) {
								if ((identifier.Name == possibleAttributeName || identifier.Name == uiResult.Identifier) && 
									typeDefinition.TypeParameterCount == tc && 
									lookup.IsAccessible (typeDefinition, false))
									if (CanReference(doc, requiredReference))
										yield return new PossibleNamespace (typeDefinition.Namespace, true, requiredReference);
							}
						}
					}
				}
*/
			}
			//
			//			// Try to search framework types
			//			if (!foundIdentifier && frameworkLookup != null && resolveResult is UnknownIdentifierResolveResult && node is AstType) {
			//				var uiResult = resolveResult as UnknownIdentifierResolveResult;
			//				if (uiResult != null) {
			//					var lookups = new List<Tuple<FrameworkLookup.AssemblyLookup, SystemAssembly>> ();
			//					try {
			//						foreach (var r in frameworkLookup.GetLookups (uiResult, tc, isInsideAttributeType)) {
			//							var systemAssembly = netProject.AssemblyContext.GetAssemblyFromFullName (r.FullName, r.Package, netProject.TargetFramework);
			//							if (systemAssembly == null)
			//								continue;
			//							if (CanBeReferenced (doc.Project, systemAssembly))
			//								lookups.Add (Tuple.Create (r, systemAssembly));
			//						}
			//					} catch (Exception e) {
			//						if (!TypeSystemService.RecreateFrameworkLookup (netProject))
			//							LoggingService.LogError (string.Format ("Error while looking up identifier {0}", uiResult.Identifier), e);
			//					}
			//					foreach(var kv in lookups)
			//						yield return new PossibleNamespace (kv.Item1.Namespace, true, new MonoDevelop.Projects.ProjectReference (kv.Item2));
			//
			//				}
			//			}
			//			if (!foundIdentifier && frameworkLookup != null && resolveResult is UnknownMemberResolveResult) {
			//				var uiResult = resolveResult as UnknownMemberResolveResult;
			//				if (uiResult != null) {
			//					var lookups = new List<Tuple<FrameworkLookup.AssemblyLookup, SystemAssembly>> ();
			//					try {
			//						foreach (var r in frameworkLookup.GetLookups (uiResult, node.ToString (), tc, isInsideAttributeType)) {
			//							var systemAssembly = netProject.AssemblyContext.GetAssemblyFromFullName (r.FullName, r.Package, netProject.TargetFramework);
			//							if (systemAssembly == null)
			//								continue;
			//							if (CanBeReferenced (doc.Project, systemAssembly))
			//								lookups.Add (Tuple.Create (r, systemAssembly));
			//						}
			//					} catch (Exception e) {
			//						if (!TypeSystemService.RecreateFrameworkLookup (netProject))
			//							LoggingService.LogError (string.Format ("Error while looking up member resolve result {0}", node), e);
			//					}
			//					foreach(var kv in lookups)
			//						yield return new PossibleNamespace (kv.Item1.Namespace, true, new MonoDevelop.Projects.ProjectReference (kv.Item2));
			//				}
			//			}

		}

		static ITypeSymbol GetReturnType (ISymbol symbol)
		{
			switch (symbol.Kind) {
			case SymbolKind.Local:
				return ((ILocalSymbol)symbol).Type;
			case SymbolKind.Parameter:
				return ((IParameterSymbol)symbol).Type;
			case SymbolKind.Field:
				return ((IFieldSymbol)symbol).Type;
			case SymbolKind.Property:
				return ((IPropertySymbol)symbol).Type;
			case SymbolKind.Method:
				return ((IMethodSymbol)symbol).ReturnType;
			}
			return null;
		}

		static IEnumerable<PossibleNamespace> GetPossibleNamespacesForExtensionMethods (IReadonlyTextDocument editor, DocumentContext doc, SemanticModel semanticModel, SyntaxNode node, SymbolInfo resolveResult, int location)
		{
			if (resolveResult.Symbol != null)
				yield break;
			var netProject = doc.Project as DotNetProject;
			if (netProject == null)
				yield break;
			var ma = node.Parent as MemberAccessExpressionSyntax;
			if (ma == null)
				yield break;
			var targetExpression = semanticModel.GetSymbolInfo (ma.Expression);

			SyntaxToken identifier;
			int tc = GetTypeParameterCount (node, out identifier);

			var compilations = new List<Tuple<Compilation, MonoDevelop.Projects.ProjectReference>> ();
			compilations.Add (Tuple.Create (doc.GetCompilationAsync ().Result, (MonoDevelop.Projects.ProjectReference)null));
			var referencedItems = IdeApp.Workspace != null ? netProject.GetReferencedItems (IdeApp.Workspace.ActiveConfiguration).ToList () : (IEnumerable<SolutionItem>)new SolutionItem[0];
			var solution = netProject != null ? netProject.ParentSolution : null;
			if (solution != null) {
				foreach (var curProject in solution.GetAllProjects ()) {
					if (curProject == netProject || referencedItems.Contains (curProject))
						continue;

					var otherRefes = IdeApp.Workspace != null ? curProject.GetReferencedItems (IdeApp.Workspace.ActiveConfiguration).ToList () : (IEnumerable<SolutionItem>)new SolutionItem[0];
					if (otherRefes.Contains (netProject))
						continue;

					var comp = RoslynTypeSystemService.GetProject (curProject).GetCompilationAsync ().Result;
					if (comp == null)
						continue;
					if (CanReference(doc, new MonoDevelop.Projects.ProjectReference (curProject)))
						compilations.Add (Tuple.Create (comp, new MonoDevelop.Projects.ProjectReference (curProject)));
				}
			}

			var name = identifier.ToString ();
			ITypeSymbol tsym = GetReturnType(targetExpression.Symbol);
			foreach (var comp in compilations) {
				var compilation = comp.Item1;
				var requiredReference = comp.Item2;
				var ns = new Stack<INamespaceSymbol> ();
				ns.Push (semanticModel.Compilation.GlobalNamespace);
				while (ns.Count > 0) {
					var curNs = ns.Pop ();


					foreach (var type in curNs.GetTypeMembers ()) {
						if (!type.MightContainExtensionMethods || !semanticModel.IsAccessible (location, type))
							continue;
						foreach (IMethodSymbol member in type.GetMembers (name).OfType<IMethodSymbol> ()) {
							if (!member.IsExtensionMethod)
								continue;
							if (!semanticModel.IsAccessible (location, member))
								continue;
							if (member.ReduceExtensionMethod (tsym)  != null) {
								if (CanReference(doc, requiredReference))
									yield return PossibleNamespace.Create (type.ContainingNamespace, false, requiredReference);
							}
						}
					} 
					foreach (var childNs in curNs.GetNamespaceMembers ()) {
						ns.Push (childNs);
					}
				}
			}
		}


		internal class AddImport
		{
			readonly TextEditor editor;
			readonly DocumentContext doc;
			readonly SymbolInfo resolveResult;
			readonly string ns;
			readonly bool addUsing;
			readonly SyntaxNode node;
			readonly MonoDevelop.Projects.ProjectReference reference;

			public AddImport (TextEditor editor, DocumentContext doc, SymbolInfo resolveResult, string ns, MonoDevelop.Projects.ProjectReference reference, bool addUsing, SyntaxNode node)
			{
				this.editor = editor;
				this.doc = doc;
				this.resolveResult = resolveResult;
				this.ns = ns;
				this.reference = reference;
				this.addUsing = addUsing;
				this.node = node;
			}

			public void Run ()
			{
				var loc = editor.CaretLocation;

				if (reference != null) {
					var project = doc.Project;
					project.Items.Add (reference);
					IdeApp.ProjectOperations.Save (project);
				}

				if (string.IsNullOrEmpty (ns))
					return;

				if (!addUsing) {
					//					var unit = doc.ParsedDocument.GetAst<SyntaxTree> ();
					int offset = node.Span.Start;
					editor.InsertText (offset, ns + ".");
					return;
				}

				var generator = doc.CreateCodeGenerator (editor);

				//				if (resolveResult is NamespaceResolveResult) {
				//					generator.AddLocalNamespaceImport (doc, ns, loc);
				//				} else {
				generator.AddGlobalNamespaceImport (editor, doc, ns);
				//				}
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

