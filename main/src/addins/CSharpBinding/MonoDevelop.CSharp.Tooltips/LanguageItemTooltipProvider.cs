// LanguageItemTooltipProvider.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//
using System;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide;
using MonoDevelop.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Projects;
using Mono.Cecil.Cil;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using Gtk;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.SourceEditor
{
	class LanguageItemTooltipProvider: TooltipProvider, IDisposable
	{
		#region ITooltipProvider implementation 
		public override async Task<TooltipItem> GetItem (TextEditor editor, DocumentContext ctx, int offset, CancellationToken token = default(CancellationToken))
		{
			if (ctx == null)
				return null;
			var analysisDocument = ctx.ParsedDocument;
			if (analysisDocument == null)
				return null;
			var unit = analysisDocument.GetAst<SemanticModel> ();
			if (unit == null)
				return null;
			
			var root = unit.SyntaxTree.GetRoot (token);
			SyntaxToken syntaxToken;
			try {
				syntaxToken = root.FindToken (offset);
			} catch (ArgumentOutOfRangeException) {
				return null;
			}
			if (!syntaxToken.Span.IntersectsWith (offset))
				return null;
			var node = GetBestFitResolveableNode (syntaxToken.Parent);
			var symbolInfo = unit.GetSymbolInfo (node, token);
			var symbol = symbolInfo.Symbol ?? unit.GetDeclaredSymbol (node, token);
			var tooltipInformation = await CreateTooltip (symbol, syntaxToken, editor, ctx, offset);
			if (tooltipInformation == null || string.IsNullOrEmpty (tooltipInformation.SignatureMarkup))
				return null;
			return new TooltipItem (tooltipInformation, syntaxToken.Span.Start, syntaxToken.Span.Length);
		}

		static SyntaxNode GetBestFitResolveableNode (SyntaxNode node)
		{
			// case constructor name : new Foo (); 'Foo' only resolves to the type not to the constructor
			if (node.Parent.IsKind (SyntaxKind.ObjectCreationExpression)) {
				var oce = (ObjectCreationExpressionSyntax)node.Parent;
				if (oce.Type == node)
					return oce;
			}
			return node;
		}

		static TooltipInformationWindow lastWindow = null;

		static void DestroyLastTooltipWindow ()
		{
			if (lastWindow != null) {
				lastWindow.Destroy ();
				lastWindow = null;
			}
		}

		#region IDisposable implementation

		public override void Dispose ()
		{
			if (IsDisposed)
				return;
			DestroyLastTooltipWindow ();
			base.Dispose ();
		}

		#endregion


		public override Control CreateTooltipWindow (TextEditor editor, DocumentContext ctx, TooltipItem item, int offset, Xwt.ModifierKeys modifierState)
		{
			var doc = ctx;
			if (doc == null)
				return null;

			var result = new TooltipInformationWindow ();
			result.ShowArrow = true;
			result.AddOverload ((TooltipInformation)item.Item);
			result.RepositionWindow ();
			return result;
		}

		async Task<TooltipInformation> CreateTooltip (ISymbol symbol, SyntaxToken token, TextEditor editor, DocumentContext doc, int offset)
		{
			try {
				TooltipInformation result;
				var sig = new SignatureMarkupCreator (doc, offset);
				sig.BreakLineAfterReturnType = false;
				
				var typeOfExpression = token.Parent as TypeOfExpressionSyntax;
				if (typeOfExpression != null && symbol is ITypeSymbol)
					return sig.GetTypeOfTooltip (typeOfExpression, (ITypeSymbol)symbol);

				result = sig.GetKeywordTooltip (token); 
				if (result != null)
					return result;
				
				if (symbol != null) {
					result = await RoslynSymbolCompletionData.CreateTooltipInformation (CancellationToken.None, editor, doc, symbol, false, true);
				}
				
				return result;
			} catch (Exception e) {
				LoggingService.LogError ("Error while creating tooltip.", e);
				return null;
			}
		}

		public override void GetRequiredPosition (TextEditor editor, Control tipWindow, out int requiredWidth, out double xalign)
		{
			var win = (TooltipInformationWindow)tipWindow;
			requiredWidth = win.Allocation.Width;
			xalign = 0.5;
		}

		#endregion 

	}
}
