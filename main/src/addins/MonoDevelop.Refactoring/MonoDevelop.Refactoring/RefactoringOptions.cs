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
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.Ide.Editor;
using System.Threading.Tasks;

namespace MonoDevelop.Refactoring
{
	public class RefactoringOptions
	{
		readonly Task<CSharpAstResolver> resolver;

		public TextEditor Editor {
			get;
			private set;
		}

		public DocumentContext EditContext {
			get;
			private set;
		}

		public object SelectedItem {
			get;
			set;
		}
		
		public ResolveResult ResolveResult {
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
				return DesktopService.GetMimeTypeForUri (EditContext.Name);
			}
		}
		
		public TextLocation Location {
			get {
				return new TextLocation (Editor.CaretLine, Editor.CaretColumn);
			}
		}
		//public readonly SyntaxTree Unit;

		public RefactoringOptions ()
		{
		}

		public RefactoringOptions (Document doc) : this(doc.Editor, doc)
		{
		}

		public RefactoringOptions (TextEditor editor, DocumentContext doc)
		{
			this.EditContext = doc;
			this.Editor = editor;
			if (doc != null && doc.ParsedDocument != null) {
				var sharedResolver = doc.GetSharedResolver ();
				if (sharedResolver == null)
					return;
				resolver = sharedResolver;
				//Unit = resolver != null ? resolver.RootNode as SyntaxTree : null;
			}
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
		
		public string OutputNode (AstNode node)
		{
			using (var stringWriter = new System.IO.StringWriter ()) {
				var formatter = new TextWriterTokenWriter (stringWriter);
//				formatter.Indentation = indentLevel;
				stringWriter.NewLine = Editor.EolMarker;
				
				var visitor = new CSharpOutputVisitor (formatter, FormattingOptionsFactory.CreateMono ());
				node.AcceptVisitor (visitor);
				return stringWriter.ToString ();
			}
		}
		
		public CodeGenerator CreateCodeGenerator ()
		{
			var result = CodeGenerator.CreateGenerator (Editor, EditContext);
			if (result == null)
				LoggingService.LogError ("Generator can't be generated for : " + Editor.MimeType);
			return result;
		}
		
		public static string GetIndent (TextEditor editor, IEntity member)
		{
			return GetWhitespaces (editor, editor.LocationToOffset (member.Region.BeginLine, 1));
		}
		
		public string GetWhitespaces (int insertionOffset)
		{
			return GetWhitespaces (Editor, insertionOffset);
		}
		
		public string GetIndent (IEntity member)
		{
			return GetIndent (Editor, member);
		}
//		
//		public IReturnType ShortenTypeName (IReturnType fullyQualifiedTypeName)
//		{
//			return Document.ParsedDocument.CompilationUnit.ShortenTypeName (fullyQualifiedTypeName, Document.Editor.Caret.Line, Document.Editor.Caret.Column);
//		}
//		
//		public ParsedDocument ParseDocument ()
//		{
//			return ProjectDomService.Parse (Dom.Project, Document.FileName, Document.Editor.Text);
//		}
		
		public List<string> GetUsedNamespaces ()
		{
			return GetUsedNamespaces (EditContext, Location);
		}
		
		public static List<string> GetUsedNamespaces (DocumentContext doc, TextLocation loc)
		{
			var result = new List<string> ();
			var pf = doc.ParsedDocument.ParsedFile as CSharpUnresolvedFile;
			if (pf == null)
				return result;
			var scope = pf.GetUsingScope (loc);
			if (scope == null)
				return result;
			var resolver = pf.GetResolver (doc.Compilation, loc);
			for (var n = scope; n != null; n = n.Parent) {
				result.Add (n.NamespaceName);
				result.AddRange (n.Usings.Select (u => u.ResolveNamespace (resolver))
					.Where (nr => nr != null)
					.Select (nr => nr.FullName));
			}
			return result;
		}
		
		public ResolveResult Resolve (AstNode node)
		{
			if (!resolver.IsCompleted)
				resolver.Wait (2000);
			if (!resolver.IsCompleted)
				return null;
			return resolver.Result.Resolve (node);
		}
		
		public AstType CreateShortType (IType fullType)
		{
			var parsedFile = EditContext.ParsedDocument.ParsedFile as CSharpUnresolvedFile;
			
			var csResolver = parsedFile.GetResolver (EditContext.Compilation, Editor.CaretLocation);
			
			var builder = new ICSharpCode.NRefactory.CSharp.Refactoring.TypeSystemAstBuilder (csResolver);
			return builder.ConvertType (fullType);
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
