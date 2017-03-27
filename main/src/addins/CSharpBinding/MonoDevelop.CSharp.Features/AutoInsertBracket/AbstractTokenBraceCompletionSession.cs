//
// AbstractTokenBraceCompletionSession.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Editor;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Ide;
using MonoDevelop.Core.Text;

namespace MonoDevelop.CSharp.Features.AutoInsertBracket
{
	abstract class AbstractTokenBraceCompletionSession : EditSession, ICheckPointEditSession
	{
		DocumentContext ctx;

		public Document Document { get { return ctx.AnalysisDocument; } }

		public SyntaxTree CurrentSnapshot {
			get {
				return ctx.AnalysisDocument.GetSyntaxTreeAsync ().Result;
			}
		}

		protected int OpeningTokenKind { get; }
		protected int ClosingTokenKind { get; }

		readonly char closingChar;

		protected AbstractTokenBraceCompletionSession (DocumentContext ctx,
													   int openingTokenKind, int closingTokenKind, char ch)
		{
			this.closingChar = ch;
			this.ctx = ctx;
			this.OpeningTokenKind = openingTokenKind;
			this.ClosingTokenKind = closingTokenKind;
		}

		public virtual bool CheckOpeningPoint (TextEditor editor, DocumentContext ctx, CancellationToken cancellationToken)
		{
			var snapshot = CurrentSnapshot;
			var position = StartOffset;
			var token = FindToken (snapshot, position, cancellationToken);

			if (!IsValidToken (token)) {
				return false;
			}

			return token.RawKind == OpeningTokenKind && token.SpanStart == position;
		}

		ITextSourceVersion version;
		protected override void OnEditorSet ()
		{
			version = Editor.Version;
			this.startOffset = Editor.CaretOffset - 1;
			this.endOffset = startOffset + 1;
		}

		public override void BeforeType (char ch, out bool handledCommand)
		{
			handledCommand = false;
			if (!CheckIsValid () || ch != this.closingChar) {
				return;
			}
			if (AllowOverType (default (CancellationToken))) {
				Editor.CaretOffset++;
				this.endOffset = this.startOffset = 0;
				handledCommand = true;
				Editor.EndSession ();
			}
		}

		public override void BeforeBackspace (out bool handledCommand)
		{
			base.BeforeBackspace (out handledCommand);
			if (Editor.CaretOffset <= StartOffset + 1 || Editor.CaretOffset > EndOffset) {
				Editor.EndSession ();
			}
		}

		public override void BeforeDelete (out bool handledCommand)
		{
			base.BeforeDelete (out handledCommand);
			if (Editor.CaretOffset <= StartOffset || Editor.CaretOffset >= EndOffset) {
				Editor.EndSession ();
			}
		}

		protected bool IsValidToken (SyntaxToken token)
		{
			return token.Parent != null && !(token.Parent is SkippedTokensTriviaSyntax);
		}

		public virtual void AfterStart (CancellationToken cancellationToken)
		{
		}

		public virtual void AfterReturn (CancellationToken cancellationToken)
		{
		}

		public virtual bool AllowOverType (CancellationToken cancellationToken)
		{
			return CheckCurrentPosition (cancellationToken) && CheckClosingTokenKind (cancellationToken);
		}

		protected bool CheckClosingTokenKind (CancellationToken cancellationToken)
		{
			var document = Document;
			if (document != null) {
				var root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
				var position = EndOffset + 1;

				return root.FindTokenFromEnd (position, includeZeroWidth: false, findInsideTrivia: true).RawKind == this.ClosingTokenKind;
			}

			return true;
		}

		protected bool CheckCurrentPosition (CancellationToken cancellationToken)
		{
			var document = Document;
			if (document != null) {
				// make sure auto closing is called from a valid position
				var tree = document.GetSyntaxTreeAsync (cancellationToken).WaitAndGetResult (cancellationToken);
				return !tree.IsInNonUserCode (Editor.CaretOffset, cancellationToken);
			}

			return true;
		}

		internal static SyntaxToken FindToken (SyntaxTree snapshot, int position, CancellationToken cancellationToken)
		{
			var root = snapshot.GetRootAsync (cancellationToken).WaitAndGetResult (CancellationToken.None);
			return root.FindToken (position, findInsideTrivia: true);
		}
	}
}

