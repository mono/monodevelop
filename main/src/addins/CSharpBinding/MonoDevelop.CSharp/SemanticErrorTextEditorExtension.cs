//
// CSharpQuickTaskProviderTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Threading;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Refactoring;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.Refactoring;

namespace MonoDevelop.CSharp
{
	class SemanticErrorTextEditorExtension : TextEditorExtension, IQuickTaskProvider
	{
		CancellationTokenSource src;

		protected override void Initialize ()
		{
			DocumentContext.DocumentParsed  += HandleDocumentParsed;
		}

		public override void Dispose ()
		{
			DocumentContext.DocumentParsed  -= HandleDocumentParsed;
			base.Dispose ();
		}

		void HandleDocumentParsed (object sender, EventArgs e)
		{
			if (src != null)
				src.Cancel ();
			if (DocumentContext.IsProjectContextInUpdate)
				return;
			var newResolverTask = DocumentContext.GetSharedResolver ();
			src = new CancellationTokenSource ();
			var cancellationToken = src.Token;
			System.Threading.Tasks.Task.Factory.StartNew (delegate {
				if (newResolverTask == null)
					return;
				var newResolver = newResolverTask.Result;
				if (newResolver == null)
					return;
				var visitor = new QuickTaskVisitor (newResolver, cancellationToken);
				try {
					newResolver.RootNode.AcceptVisitor (visitor);
				} catch (Exception ex) {
					LoggingService.LogError ("Error while analyzing the file for the semantic highlighting.", ex);
					return;
				}
				if (!cancellationToken.IsCancellationRequested) {
					Gtk.Application.Invoke (delegate {
						if (cancellationToken.IsCancellationRequested)
							return;
						quickTasks = visitor.QuickTasks;
						OnTasksUpdated (EventArgs.Empty);
					});
				}
			});
		}

		#region IQuickTaskProvider implementation
		public event EventHandler TasksUpdated;

		protected virtual void OnTasksUpdated (EventArgs e)
		{
			var handler = TasksUpdated;
			if (handler != null)
				handler (this, e);
		}

		List<QuickTask> quickTasks;
		public IEnumerable<QuickTask> QuickTasks {
			get {
				return quickTasks;
			}
		}
		#endregion

		class QuickTaskVisitor : DepthFirstAstVisitor
		{
			internal List<QuickTask> QuickTasks = new List<QuickTask> ();
			readonly CSharpAstResolver resolver;
			readonly CancellationToken cancellationToken;

			public QuickTaskVisitor (CSharpAstResolver resolver, CancellationToken cancellationToken)
			{
				this.resolver = resolver;
				this.cancellationToken = cancellationToken;
			}

			protected override void VisitChildren (AstNode node)
			{
				if (cancellationToken.IsCancellationRequested)
					return;
				base.VisitChildren (node);
			}

			public override void VisitIdentifierExpression (IdentifierExpression identifierExpression)
			{
				base.VisitIdentifierExpression (identifierExpression);
				var result = resolver.Resolve (identifierExpression, cancellationToken);
				if (result.IsError) {
					QuickTasks.Add (new QuickTask (() => string.Format ("error CS0103: The name `{0}' does not exist in the current context", identifierExpression.Identifier), identifierExpression.StartLocation, Severity.Error));
				}
			}

			public override void VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression)
			{
				base.VisitMemberReferenceExpression (memberReferenceExpression);
				var result = resolver.Resolve (memberReferenceExpression, cancellationToken) as UnknownMemberResolveResult;
				if (result != null && result.TargetType.Kind != TypeKind.Unknown) {
					QuickTasks.Add (new QuickTask (string.Format ("error CS0117: `{0}' does not contain a definition for `{1}'", result.TargetType.FullName, memberReferenceExpression.MemberName), memberReferenceExpression.MemberNameToken.StartLocation, Severity.Error));
				}
			}

			public override void VisitSimpleType (SimpleType simpleType)
			{
				base.VisitSimpleType (simpleType);
				var result = resolver.Resolve (simpleType, cancellationToken);
				if (result.IsError) {
					QuickTasks.Add (new QuickTask (string.Format ("error CS0246: The type or namespace name `{0}' could not be found. Are you missing an assembly reference?", simpleType.Identifier), simpleType.StartLocation, Severity.Error));
				}
			}

			public override void VisitMemberType (MemberType memberType)
			{
				base.VisitMemberType (memberType);
				var result = resolver.Resolve (memberType, cancellationToken);
				if (result.IsError) {
					QuickTasks.Add (new QuickTask (string.Format ("error CS0246: The type or namespace name `{0}' could not be found. Are you missing an assembly reference?", memberType.MemberName), memberType.StartLocation, Severity.Error));
				}
			}

			public override void VisitComment (Comment comment)
			{
			}
		}
	}
}