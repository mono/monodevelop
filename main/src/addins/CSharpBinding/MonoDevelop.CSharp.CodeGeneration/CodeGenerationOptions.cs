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
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CodeGeneration
{
	sealed class CodeGenerationOptions
	{
		readonly int offset;

		public TextEditor Editor
		{
			get;
			private set;
		}

		public DocumentContext DocumentContext
		{
			get;
			private set;
		}

		public ITypeSymbol EnclosingType
		{
			get;
			private set;
		}

		public SyntaxNode EnclosingMemberSyntax
		{
			get;
			private set;
		}

		public TypeDeclarationSyntax EnclosingPart
		{
			get;
			private set;
		}

		public ISymbol EnclosingMember
		{
			get;
			private set;
		}

		public string MimeType
		{
			get
			{
				return DesktopService.GetMimeTypeForUri (DocumentContext.Name);
			}
		}

		public OptionSet FormattingOptions
		{
			get
			{
				var doc = DocumentContext;
				var policyParent = doc.Project != null ? doc.Project.Policies : null;
				var types = DesktopService.GetMimeTypeInheritanceChain (Editor.MimeType);
				var codePolicy = policyParent != null ? policyParent.Get<MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy> (types);
				var textPolicy = policyParent != null ? policyParent.Get<TextStylePolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> (types);
				return codePolicy.CreateOptions (textPolicy);
			}
		}

		public SemanticModel CurrentState
		{
			get;
			private set;
		}

		internal CodeGenerationOptions (TextEditor editor, DocumentContext ctx)
		{
			Editor = editor;
			DocumentContext = ctx;
			if (ctx.ParsedDocument != null)
				CurrentState = ctx.ParsedDocument.GetAst<SemanticModel> ();
			offset = editor.CaretOffset;
			var tree = CurrentState.SyntaxTree;
			EnclosingPart = tree.GetContainingTypeDeclaration (offset, default(CancellationToken));
			if (EnclosingPart != null) {
				EnclosingType = CurrentState.GetDeclaredSymbol (EnclosingPart) as ITypeSymbol;

				foreach (var member in EnclosingPart.Members) {
					if (member.Span.Contains (offset)) {
						EnclosingMemberSyntax = member;
						break;
					}

				}
				if (EnclosingMemberSyntax != null)
					EnclosingMember = CurrentState.GetDeclaredSymbol (EnclosingMemberSyntax);
			}
		}

		public string CreateShortType (ITypeSymbol fullType)
		{
			return RoslynCompletionData.SafeMinimalDisplayString (fullType, CurrentState, offset);
		}

		public static CodeGenerationOptions CreateCodeGenerationOptions (TextEditor document, DocumentContext ctx)
		{
			return new CodeGenerationOptions (document, ctx);
		}

		public async Task<string> OutputNode (SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
		{
			node = Formatter.Format (node, TypeSystemService.Workspace, FormattingOptions, cancellationToken);

			var text = Editor.Text;
			string nodeText = node.ToString ();
			text = text.Insert (offset, nodeText);

			var backgroundDocument = DocumentContext.AnalysisDocument.WithText (SourceText.From (text));

			var currentRoot = await backgroundDocument.GetSyntaxRootAsync (cancellationToken).ConfigureAwait (false);

			// add formatter & simplifier annotations 
			var oldNode = currentRoot.FindNode (TextSpan.FromBounds(offset, offset + nodeText.Length));
			currentRoot = currentRoot.ReplaceNode (oldNode, oldNode.WithAdditionalAnnotations (Formatter.Annotation, Simplifier.Annotation)); 

			// get updated node
			node = currentRoot.FindNode (TextSpan.FromBounds(offset, offset + nodeText.Length));
			currentRoot = currentRoot.TrackNodes (node);

			backgroundDocument = backgroundDocument.WithSyntaxRoot (currentRoot);
			backgroundDocument = await Formatter.FormatAsync (backgroundDocument, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
			backgroundDocument = await Simplifier.ReduceAsync (backgroundDocument, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
			var newRoot = await backgroundDocument.GetSyntaxRootAsync (cancellationToken).ConfigureAwait (false);

			var formattedNode = newRoot.GetCurrentNode (node);
			if (formattedNode == null) {
				LoggingService.LogError ("Fatal error: Can't find current formatted node in code generator document.");
				return nodeText;
			}
			return formattedNode.ToString ();
		}
	}
}
