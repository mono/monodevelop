//
// ProjectedFilterCompletionTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Threading.Tasks;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.Ide.Editor.Projection
{
	sealed class ProjectedFilterCompletionTextEditorExtension : CompletionTextEditorExtension, IProjectionExtension
	{
		CompletionTextEditorExtension completionTextEditorExtension;
		IReadOnlyList<Projection> projections;

		IReadOnlyList<Projection> IProjectionExtension.Projections {
			get {
				return projections;
			}
			set {
				projections = value;
			}
		}

		internal override CodeCompletion.ICompletionWidget CompletionWidget {
			get {
				return completionTextEditorExtension.CompletionWidget;
			}
			set {
				completionTextEditorExtension.CompletionWidget = value;
				completionTextEditorExtension.UnsubscribeCompletionContextChanged ();
			}
		}

		public ProjectedFilterCompletionTextEditorExtension (CompletionTextEditorExtension completionTextEditorExtension, IReadOnlyList<Projection> projections)
		{
			this.completionTextEditorExtension = completionTextEditorExtension;
			this.projections = projections;
			completionTextEditorExtension.UnsubscribeCompletionContextChanged ();
		}

		internal protected override bool IsActiveExtension ()
		{
			return !IsInProjection ();
		}

		bool IsInProjection ()
		{
			int offset = Editor.CaretOffset;
			foreach (var p in projections) {
				foreach (var seg in p.ProjectedSegments) {
					if (seg.ContainsOriginal (offset))
						return true;
				}
			}
			return false;
		}

		public override bool KeyPress (KeyDescriptor descriptor)
		{
			if (!IsActiveExtension())
				return Next == null || Next.KeyPress (descriptor);
			return completionTextEditorExtension.KeyPress (descriptor);
		}

		public override bool IsValidInContext (DocumentContext context)
		{
			if (!IsActiveExtension())
				return false;
			return completionTextEditorExtension.IsValidInContext (context);
		}

		public override Task<int> GetCurrentParameterIndex (int startOffset, System.Threading.CancellationToken token)
		{
			if (!IsActiveExtension())
				return Task.FromResult (-1);
			return completionTextEditorExtension.GetCurrentParameterIndex (startOffset, token);
		}

		public override string CompletionLanguage {
			get {
				return completionTextEditorExtension.CompletionLanguage;
			}
		}

		public override void RunCompletionCommand ()
		{
			if (!IsActiveExtension())
				return;
			completionTextEditorExtension.RunCompletionCommand ();
		}

		public override void RunShowCodeTemplatesWindow ()
		{
			if (!IsActiveExtension())
				return;
			completionTextEditorExtension.RunShowCodeTemplatesWindow ();
		}

		public override void RunParameterCompletionCommand ()
		{
			if (!IsActiveExtension())
				return;
			completionTextEditorExtension.RunParameterCompletionCommand ();
		}

		public override bool CanRunCompletionCommand ()
		{
			if (!IsActiveExtension ())
				return false;
			return completionTextEditorExtension.CanRunCompletionCommand ();
		}

		public override bool CanRunParameterCompletionCommand ()
		{
			if (!IsActiveExtension ())
				return false;
			return completionTextEditorExtension.CanRunParameterCompletionCommand ();
		}

		public override System.Threading.Tasks.Task<CodeCompletion.ICompletionDataList> HandleCodeCompletionAsync (CodeCompletion.CodeCompletionContext completionContext, CompletionTriggerInfo triggerInfo, System.Threading.CancellationToken token)
		{
			return completionTextEditorExtension.HandleCodeCompletionAsync (completionContext, triggerInfo, token);
		}

		public override System.Threading.Tasks.Task<CodeCompletion.ParameterHintingResult> HandleParameterCompletionAsync (CodeCompletion.CodeCompletionContext completionContext, char completionChar, System.Threading.CancellationToken token)
		{
			return completionTextEditorExtension.HandleParameterCompletionAsync (completionContext, completionChar, token);
		}

		public override bool GetCompletionCommandOffset (out int cpos, out int wlen)
		{
			if (!IsActiveExtension()) {
				cpos = 0; wlen = 0;
				return false;
			}
			return completionTextEditorExtension.GetCompletionCommandOffset (out cpos, out wlen);
		}

		public override CodeCompletion.ICompletionDataList ShowCodeSurroundingsCommand (CodeCompletion.CodeCompletionContext completionContext)
		{
			if (!IsActiveExtension()) return null;
			return completionTextEditorExtension.ShowCodeSurroundingsCommand (completionContext);
		}

		public override CodeCompletion.ICompletionDataList ShowCodeTemplatesCommand (CodeCompletion.CodeCompletionContext completionContext)
		{
			if (!IsActiveExtension()) return null;
			return completionTextEditorExtension.ShowCodeTemplatesCommand (completionContext);
		}

		public override Task<CodeCompletion.ParameterHintingResult> ParameterCompletionCommand (CodeCompletion.CodeCompletionContext completionContext)
		{
			if (!IsActiveExtension()) return null;
			return completionTextEditorExtension.ParameterCompletionCommand (completionContext);
		}

		public override Task<int> GuessBestMethodOverload (CodeCompletion.ParameterHintingResult provider, int currentOverload, System.Threading.CancellationToken token)
		{
			if (!IsActiveExtension()) return Task.FromResult (-1);
			return completionTextEditorExtension.GuessBestMethodOverload (provider, currentOverload, token);
		}

		internal protected override void OnCompletionContextChanged (object o, EventArgs a)
		{
			if (!IsActiveExtension()) return;
			completionTextEditorExtension.OnCompletionContextChanged (o, a);
		}

		internal protected override void HandlePositionChanged (object sender, EventArgs e)
		{
			if (!IsActiveExtension ())
				return;
			completionTextEditorExtension.HandlePositionChanged (sender, e);
		}

		protected override void Initialize ()
		{
		}

		public override void Dispose ()
		{
			completionTextEditorExtension.Dispose ();
		}
	}
}

