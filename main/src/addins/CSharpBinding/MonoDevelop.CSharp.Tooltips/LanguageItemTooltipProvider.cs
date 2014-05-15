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
using Mono.TextEditor;
using MonoDevelop.Ide;
using MonoDevelop.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace MonoDevelop.SourceEditor
{
	class LanguageItemTooltipProvider: TooltipProvider, IDisposable
	{
		class ToolTipData
		{
			public readonly SymbolInfo SymbolInfo;
			public ISymbol Symbol { get { return SymbolInfo.Symbol; } }
			public readonly SyntaxToken Token;

			public ToolTipData (SymbolInfo symbol, SyntaxToken token)
			{
				Token = token;
				SymbolInfo = symbol;
			}
			
			public override string ToString ()
			{
				return string.Format ("[ToolTipData: Symbol={0}, Token={1}]", Symbol, Token);
			}
		}

		#region ITooltipProvider implementation 
		
		public override TooltipItem GetItem (TextEditor editor, int offset)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.AnalysisDocument == null)
				return null;
			var unit = doc.AnalysisDocument.GetSemanticModelAsync ().Result;
			if (unit == null)
				return null;
			
			var root = unit.SyntaxTree.GetRoot ();
			
			var token = root.FindToken (offset);
			if (token == lastNode)
				return lastResult;
			lastNode = token;
			var symbolInfo = unit.GetSymbolInfo (token.Parent); 
			return lastResult = new TooltipItem (new ToolTipData (symbolInfo, token), token.FullSpan.Start, token.FullSpan.Length);
		}
		
		SyntaxToken lastNode;
		static TooltipInformationWindow lastWindow = null;
		TooltipItem lastResult;

		static void DestroyLastTooltipWindow ()
		{
			if (lastWindow != null) {
				lastWindow.Destroy ();
				lastWindow = null;
			}
		}

		#region IDisposable implementation

		public void Dispose ()
		{
			DestroyLastTooltipWindow ();
			lastNode = new SyntaxToken ();
			lastResult = null;
		}

		#endregion

		protected override Gtk.Window CreateTooltipWindow (TextEditor editor, int offset, Gdk.ModifierType modifierState, TooltipItem item)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return null;

			var titem = (ToolTipData)item.Item;

			var tooltipInformation = CreateTooltip (titem, offset, modifierState);
			if (tooltipInformation == null || string.IsNullOrEmpty (tooltipInformation.SignatureMarkup))
				return null;

			var result = new TooltipInformationWindow ();
			result.ShowArrow = true;
			result.AddOverload (tooltipInformation);
			result.RepositionWindow ();
			return result;
		}

		public override Gtk.Window ShowTooltipWindow (TextEditor editor, int offset, Gdk.ModifierType modifierState, int mouseX, int mouseY, TooltipItem item)
		{
			var titem = (ToolTipData)item.Item;
			if (lastWindow != null && lastWindow.IsRealized && lastNode == titem.Token)
				return lastWindow;
			
			DestroyLastTooltipWindow ();

			var tipWindow = CreateTooltipWindow (editor, offset, modifierState, item) as TooltipInformationWindow;
			if (tipWindow == null)
				return null;

			var hoverNode = titem.Token;
			var startLoc = editor.OffsetToLocation (hoverNode.Span.Start);
			var endLoc = editor.OffsetToLocation (hoverNode.Span.End);
			var p1 = editor.LocationToPoint (startLoc);
			var p2 = editor.LocationToPoint (endLoc);
			var positionWidget = editor.TextArea;
			var caret = new Gdk.Rectangle ((int)p1.X - positionWidget.Allocation.X, (int)p2.Y - positionWidget.Allocation.Y, (int)(p2.X - p1.X), (int)editor.LineHeight);

			tipWindow.ShowPopup (positionWidget, caret, PopupPosition.Top);
			tipWindow.EnterNotifyEvent += delegate {
				editor.HideTooltip (false);
			};
			lastWindow = tipWindow;
			lastNode = titem.Token;
			return tipWindow;
		}

		TooltipInformation CreateTooltip (ToolTipData data, int offset, Gdk.ModifierType modifierState)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return null;
			bool createFooter = (modifierState & Gdk.ModifierType.Mod1Mask) != 0;
			try {
				TooltipInformation result;
				var sig = new SignatureMarkupCreator (doc);
				sig.BreakLineAfterReturnType = false;
				
				var typeOfExpression = data.Token.Parent as TypeOfExpressionSyntax;
				if (typeOfExpression != null && data.Symbol is ITypeSymbol)
					return sig.GetTypeOfTooltip (typeOfExpression, (ITypeSymbol)data.Symbol);
				
//				var parentKind = data.Token.Parent != null ? data.Token.Parent.CSharpKind () : SyntaxKind.None;
//				switch (parentKind) {
//					case SyntaxKind.ConstructorConstraint:
//					case SyntaxKind.ClassConstraint:
//					case SyntaxKind.StructConstraint:
//						return sig.GetConstraintTooltip (data.Token);
//				}
//
//				if (data.Node is ThisReferenceExpression && result is ThisResolveResult) {
//					var resolver = file.GetResolver (doc.Compilation, doc.Editor.Caret.Location);
//					var sig = new SignatureMarkupCreator (resolver, doc.GetFormattingPolicy ().CreateOptions ());
//					sig.BreakLineAfterReturnType = false;
//					
//					return sig.GetKeywordTooltip ("this", data.Node);
//				}
//				
//				if (data.Node is TypeOfExpression) {
//					var resolver = file.GetResolver (doc.Compilation, doc.Editor.Caret.Location);
//					var sig = new SignatureMarkupCreator (resolver, doc.GetFormattingPolicy ().CreateOptions ());
//					sig.BreakLineAfterReturnType = false;
//					return sig.GetTypeOfTooltip ((TypeOfExpression)data.Node, result as TypeOfResolveResult);
//				}
//				if (data.Node is PrimitiveType && data.Node.Parent is Constraint) {
//					var t = (PrimitiveType)data.Node;
//					if (t.Keyword == "class" || t.Keyword == "new" || t.Keyword == "struct") {
//						var resolver = file.GetResolver (doc.Compilation, doc.Editor.Caret.Location);
//						var sig = new SignatureMarkupCreator (resolver, doc.GetFormattingPolicy ().CreateOptions ());
//						sig.BreakLineAfterReturnType = false;
//						return sig.GetConstraintTooltip (t.Keyword);
//					}
//					return null;
//				}
				result = sig.GetKeywordTooltip (data.Token); 
				if (result != null)
					return result;
				
				if (data.Symbol != null) {
					result = RoslynSymbolCompletionData.CreateTooltipInformation (doc, data.Symbol, false, createFooter);
				}
				
//				if (result == null && parentKind == SyntaxKind.IdentifierName) {
//					if (data.SymbolInfo.CandidateReason == CandidateReason.None) {
//						if (data.Token.Parent.Parent.CSharpKind () == SyntaxKind.SimpleMemberAccessExpression ||
//							data.Token.Parent.Parent.CSharpKind () == SyntaxKind.PointerMemberAccessExpression) {
//							var ma = (MemberAccessExpressionSyntax)data.Token.Parent.Parent;
//							return new TooltipInformation {
//								SignatureMarkup = string.Format ("error CS0117: `{0}' does not contain a definition for `{1}'", ma.Expression, ma.Name)
//							};
//						}
//						return new TooltipInformation {
//							SignatureMarkup = string.Format ("error CS0103: The name `{0}' does not exist in the current context", data.Token)
//						};
//					}
//				}
				
				return result;

//				if (result is AliasNamespaceResolveResult) {
//					var resolver = file.GetResolver (doc.Compilation, doc.Editor.Caret.Location);
//					var sig = new SignatureMarkupCreator (doc);
//					sig.BreakLineAfterReturnType = false;
//					return sig.GetAliasedNamespaceTooltip ((AliasNamespaceResolveResult)result);
//				}
//				
//				if (result is AliasTypeResolveResult) {
//					var resolver = file.GetResolver (doc.Compilation, doc.Editor.Caret.Location);
//					var sig = new SignatureMarkupCreator (doc);
//					sig.BreakLineAfterReturnType = false;
//					return sig.GetAliasedTypeTooltip ((AliasTypeResolveResult)result);
//				}
//				
//				
//				if (data.Node is ExternAliasDeclaration) {
//					var resolver = file.GetResolver (doc.Compilation, doc.Editor.Caret.Location);
//					var sig = new SignatureMarkupCreator (resolver, doc.GetFormattingPolicy ().CreateOptions ());
//					sig.BreakLineAfterReturnType = false;
//					return sig.GetExternAliasTooltip ((ExternAliasDeclaration)data.Node, doc.Project as DotNetProject);
//				}
//				if (result is MethodGroupResolveResult) {
//					var mrr = (MethodGroupResolveResult)result;
//					var allMethods = new List<IMethod> (mrr.Methods);
//					foreach (var l in mrr.GetExtensionMethods ()) {
//						allMethods.AddRange (l);
//					}
//				
//					var method = allMethods.FirstOrDefault ();
//					if (method != null) {
////						return MemberCompletionData.CreateTooltipInformation (
////							doc.Compilation,
////							file,
////							doc.Editor,
////							doc.GetFormattingPolicy (),
////							method,
////							false,
////							createFooter);
//					}
//				} else if (result is CSharpInvocationResolveResult) {
//					var invocationResult = (CSharpInvocationResolveResult)result;
//					var member = (IMember)invocationResult.ReducedMethod ?? invocationResult.Member;
////						return MemberCompletionData.CreateTooltipInformation (
////							doc.Compilation,
////							file,
////							doc.Editor,
////							doc.GetFormattingPolicy (),
////							member, 
////							false,
////							createFooter);
//				} else if (result is MemberResolveResult) {
//					var member = ((MemberResolveResult)result).Member;
////					return MemberCompletionData.CreateTooltipInformation (
////						doc.Compilation,
////						file,
////						doc.Editor,
////						doc.GetFormattingPolicy (),
////						member, 
////						false,
////						createFooter);
//				} else {
//					return MemberCompletionData.CreateTooltipInformation (
//						doc.Compilation,
//						file,
//						doc.Editor,
//						doc.GetFormattingPolicy (),
//						result.Type, 
//						false,
//						createFooter);
//				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while creating tooltip.", e);
				return null;
			}
		}
		
//		class ErrorVisitor : DepthFirstAstVisitor
//		{
//			readonly CSharpAstResolver resolver;
//			readonly CancellationToken cancellationToken;
//			ResolveResult errorResolveResult;
//
//			public ResolveResult ErrorResolveResult {
//				get {
//					return errorResolveResult;
//				}
//			}
//
//			AstNode errorNode;
//
//			public AstNode ErrorNode {
//				get {
//					return errorNode;
//				}
//			}
//
//			public ErrorVisitor (CSharpAstResolver resolver, CancellationToken cancellationToken = default(CancellationToken))
//			{
//				this.resolver = resolver;
//				this.cancellationToken = cancellationToken;
//			}
//			
//			protected override void VisitChildren (AstNode node)
//			{
//				if (ErrorResolveResult != null || cancellationToken.IsCancellationRequested)
//					return;
//				if (node is Expression) {
//					var rr = resolver.Resolve (node, cancellationToken);
//					if (rr.IsError) {
//						errorResolveResult = rr;
//						errorNode = node;
//					}
//				}
//				base.VisitChildren (node);
//			}
//		}

		
		protected override void GetRequiredPosition (Mono.TextEditor.TextEditor editor, Gtk.Window tipWindow, out int requiredWidth, out double xalign)
		{
			var win = (TooltipInformationWindow)tipWindow;
			requiredWidth = win.Allocation.Width;
			xalign = 0.5;
		}

		#endregion 

	}
}
