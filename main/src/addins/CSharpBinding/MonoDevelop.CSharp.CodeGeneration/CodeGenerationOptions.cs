// 
// CodeGenerationOptions.cs
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
using System.Linq;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System;
using System.Threading;

namespace MonoDevelop.CodeGeneration
{
	public class CodeGenerationOptions
	{
		public Document Document {
			get;
			private set;
		}
		
		public ITypeDefinition EnclosingType {
			get;
			private set;
		}
		
		public IUnresolvedTypeDefinition EnclosingPart {
			get;
			private set;
		}
		
		public IMember EnclosingMember {
			get;
			private set;
		}
		
		public string MimeType {
			get {
				return DesktopService.GetMimeTypeForUri (Document.FileName);
			}
		}
		
		public CSharpFormattingOptions FormattingOptions {
			get {
				var doc = Document;
				var policyParent = doc.Project != null ? doc.Project.Policies : null;
				var types = DesktopService.GetMimeTypeInheritanceChain (doc.Editor.MimeType);
				var codePolicy = policyParent != null ? policyParent.Get<MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy> (types);
				return codePolicy.CreateOptions ();
			}
		}
		
		static AstNode FirstExpressionChild (AstNode parent)
		{
			AstNode node = parent.FirstChild;
			if (node == null)
				return null;
			while (node != null && !(node is Expression || node is Statement)) {
				node = node.NextSibling;
			}
			return node;
		}
		
		static AstNode NextExpression (AstNode parent)
		{
			AstNode node = parent.GetNextNode ();
			if (node == null)
				return null;
			while (node != null && !(node is Expression || node is Statement)) {
				node = node.GetNextNode ();
			}
			return node;
		}
		
		readonly Lazy<CSharpResolver> currentState;
		public CSharpResolver CurrentState {
			get {
				return currentState.Value;
			}
		}
		
		public CodeGenerationOptions ()
		{
			currentState = new Lazy<CSharpResolver> (() => {
				var parsedDocument = Document.ParsedDocument;
				if (parsedDocument == null)
					return null;
				var unit = parsedDocument.GetAst<SyntaxTree> ().Clone ();
				var file = parsedDocument.ParsedFile as CSharpUnresolvedFile;
				
				var resolvedNode = unit.GetNodeAt<BlockStatement> (Document.Editor.Caret.Location);
				if (resolvedNode == null)
					return null;
				
				var expr = new IdentifierExpression ("foo");
				resolvedNode.Add (expr);
				
				var ctx = file.GetTypeResolveContext (Document.Compilation, Document.Editor.Caret.Location);
				
				var resolver = new CSharpResolver (ctx);
				
				var astResolver = new CSharpAstResolver (resolver, unit, file);
				astResolver.ApplyNavigator (new NodeListResolveVisitorNavigator (expr), CancellationToken.None);
				astResolver.Resolve (expr);
				return astResolver.GetResolverStateBefore (expr);
			});
		}
		
		public AstType CreateShortType (IType fullType)
		{
			var parsedFile = Document.ParsedDocument.ParsedFile as CSharpUnresolvedFile;
			
			var compilation = Document.Compilation;
			fullType = compilation.Import (fullType);
			var csResolver = parsedFile.GetResolver (compilation, Document.Editor.Caret.Location);
			
			var builder = new ICSharpCode.NRefactory.CSharp.Refactoring.TypeSystemAstBuilder (csResolver);
			return builder.ConvertType (fullType);
		}
		
		public CodeGenerator CreateCodeGenerator ()
		{
			var result = CodeGenerator.CreateGenerator (Document);
			if (result == null)
				LoggingService.LogError ("Generator can't be generated for : " + Document.Editor.MimeType);
			return result;
		}
		
		public static CodeGenerationOptions CreateCodeGenerationOptions (Document document)
		{
			document.UpdateParseDocument ();
			var options = new CodeGenerationOptions {
				Document = document
			};
			if (document.ParsedDocument != null && document.ParsedDocument.ParsedFile != null) {
				options.EnclosingPart = document.ParsedDocument.ParsedFile.GetInnermostTypeDefinition (document.Editor.Caret.Location);
				if (options.EnclosingPart != null)
					options.EnclosingType = options.EnclosingPart.Resolve (document.Project).GetDefinition ();
				if (options.EnclosingType != null) {
					options.EnclosingMember = options.EnclosingType.Members.FirstOrDefault (m => !m.IsSynthetic && m.Region.FileName == document.FileName && m.Region.IsInside (document.Editor.Caret.Location));
				}
			}
			return options;
		}
		
	}
}
