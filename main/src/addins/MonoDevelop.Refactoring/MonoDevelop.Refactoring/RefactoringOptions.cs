// 
// RefactoringOptions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Ide.Gui;
 
using System.Text;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MonoDevelop.Refactoring
{
	public class RefactoringOptions
	{
		public TextEditor Editor {
			get;
			private set;
		}

		public DocumentContext DocumentContext {
			get;
			private set;
		}

		public object SelectedItem {
			get;
			set;
		}
		
		// file provider for unit test purposes.
		public ITextFileProvider TestFileProvider {
			get;
			set;
		}
		
		public string MimeType {
			get {
				return DesktopService.GetMimeTypeForUri (DocumentContext.Name);
			}
		}
		
		public DocumentLocation Location {
			get {
				return Editor.CaretLocation;
			}
		}


		public RefactoringOptions ()
		{
		}

		public RefactoringOptions (Document doc) : this(doc.Editor, doc)
		{
		}

		public RefactoringOptions (TextEditor editor, DocumentContext doc)
		{
			this.DocumentContext = doc;
			this.Editor = editor;
			/*if (doc != null && doc.ParsedDocument != null) {
				var sharedResolver = doc.GetSharedResolver ();
				if (sharedResolver == null)
					return;
				resolver = sharedResolver;
				//Unit = resolver != null ? resolver.RootNode as SyntaxTree : null;
			}*/
		}

		public TextEditor GetTextEditorData ()
		{
			return Editor;
		}
		
		public static string GetWhitespaces (TextEditor editor, int insertionOffset)
		{
			StringBuilder result = new StringBuilder ();
			for (int i = insertionOffset; i < editor.Length; i++) {
				char ch = editor.GetCharAt (i);
				if (ch == ' ' || ch == '\t') {
					result.Append (ch);
				} else {
					break;
				}
			}
			return result.ToString ();
		}

		public CodeGenerator CreateCodeGenerator ()
		{
			var result = CodeGenerator.CreateGenerator (Editor, DocumentContext);
			if (result == null)
				LoggingService.LogError ("Generator can't be generated for : " + Editor.MimeType);
			return result;
		}
		
		public static string GetIndent (TextEditor editor, Microsoft.CodeAnalysis.SyntaxNode member)
		{
			return GetWhitespaces (editor, member.SpanStart);
		}
		
		public string GetWhitespaces (int insertionOffset)
		{
			return GetWhitespaces (Editor, insertionOffset);
		}
		
		public Task<ImmutableArray<string>> GetUsedNamespacesAsync (CancellationToken cancellationToken = default (CancellationToken))
		{
			return GetUsedNamespacesAsync (Editor, DocumentContext,  Editor.LocationToOffset (Location));
		}
		
		public static async Task<ImmutableArray<string>> GetUsedNamespacesAsync (TextEditor editor, DocumentContext doc, int offset, CancellationToken cancellationToken = default (CancellationToken))
		{
			if (editor == null)
				throw new System.ArgumentNullException ("editor");
			var analysisDocument = doc.AnalysisDocument;
			if (analysisDocument == null)
				return ImmutableArray<string>.Empty;
			var result = ImmutableArray<string>.Empty.ToBuilder ();
			var sm = await analysisDocument.GetSyntaxRootAsync (cancellationToken); 
			var node = sm.FindNode (TextSpan.FromBounds (offset, offset)); 
			
			while (node != null) {
				var cu = node as CompilationUnitSyntax;
				if (cu != null) {
					foreach (var u in cu.Usings) {
						if (u.CSharpKind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.UsingDirective)
							result.Add (u.Name.ToString ());
					}
				}
				var ns = node as NamespaceDeclarationSyntax;
				if (ns != null) {
					var name = ns.Name.ToString ();
					result.Add (name);
					foreach (var u in ns.Usings) {
						if (u.CSharpKind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.UsingDirective) 
							result.Add (u.Name.ToString ());
					}
				}

				node = node.Parent;
			}
			
			return result.ToImmutable ();
		}
		
//		public List<string> GetResolveableNamespaces (RefactoringOptions options, out bool resolveDirect)
//		{
//			IReturnType returnType = null; 
//			INRefactoryASTProvider astProvider = RefactoringService.GetASTProvider (DesktopService.GetMimeTypeForUri (options.Document.FileName));
//			
//			if (options.ResolveResult != null && options.ResolveResult.ResolvedExpression != null) {
//				if (astProvider != null) 
//					returnType = astProvider.ParseTypeReference (options.ResolveResult.ResolvedExpression.Expression).ConvertToReturnType ();
//				if (returnType == null)
//					returnType = DomReturnType.GetSharedReturnType (options.ResolveResult.ResolvedExpression.Expression);
//			}
//			
//			List<string> namespaces;
//			if (options.ResolveResult is UnresolvedMemberResolveResult) {
//				namespaces = new List<string> ();
//				UnresolvedMemberResolveResult unresolvedMemberResolveResult = options.ResolveResult as UnresolvedMemberResolveResult;
//				IType type = unresolvedMemberResolveResult.TargetResolveResult != null ? options.Dom.GetType (unresolvedMemberResolveResult.TargetResolveResult.ResolvedType) : null;
//				if (type != null) {
//					List<IType> allExtTypes = DomType.GetAccessibleExtensionTypes (options.Dom, null);
//					foreach (ExtensionMethod method in type.GetExtensionMethods (allExtTypes, unresolvedMemberResolveResult.MemberName)) {
//						string ns = method.OriginalMethod.DeclaringType.Namespace;
//						if (!namespaces.Contains (ns) && !options.Document.CompilationUnit.Usings.Any (u => u.Namespaces.Contains (ns)))
//							namespaces.Add (ns);
//					}
//				}
//				resolveDirect = false;
//			} else {
//				namespaces = new List<string> (options.Dom.ResolvePossibleNamespaces (returnType));
//				resolveDirect = true;
//			}
//			for (int i = 0; i < namespaces.Count; i++) {
//				for (int j = i + 1; j < namespaces.Count; j++) {
//					if (namespaces[j] == namespaces[i]) {
//						namespaces.RemoveAt (j);
//						j--;
//					}
//				}
//			}
//			return namespaces;
//		}
	}
}
