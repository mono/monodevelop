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

		public ProjectedFilterCompletionTextEditorExtension (CompletionTextEditorExtension completionTextEditorExtension, IReadOnlyList<Projection> projections)
		{
			this.completionTextEditorExtension = completionTextEditorExtension;
			this.projections = projections;
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
			if (IsInProjection())
				return Next == null || Next.KeyPress (descriptor);
			return completionTextEditorExtension.KeyPress (descriptor);
		}

		public override bool IsValidInContext (DocumentContext context)
		{
			if (IsInProjection())
				return false;
			return completionTextEditorExtension.IsValidInContext (context);
		}

		public override int GetCurrentParameterIndex (int startOffset)
		{
			if (IsInProjection())
				return -1;
			return completionTextEditorExtension.GetCurrentParameterIndex (startOffset);
		}

		public override string CompletionLanguage {
			get {
				return completionTextEditorExtension.CompletionLanguage;
			}
		}

		public override void RunCompletionCommand ()
		{
			if (IsInProjection ())
				return;
			completionTextEditorExtension.RunCompletionCommand ();
		}

		public override void RunShowCodeTemplatesWindow ()
		{
			if (IsInProjection ())
				return;
			completionTextEditorExtension.RunShowCodeTemplatesWindow ();
		}

		public override void RunParameterCompletionCommand ()
		{
			if (IsInProjection ())
				return;
			completionTextEditorExtension.RunParameterCompletionCommand ();
		}

		public override bool CanRunCompletionCommand ()
		{
			if (IsInProjection ())
				return false;
			return completionTextEditorExtension.CanRunCompletionCommand ();
		}

		public override bool CanRunParameterCompletionCommand ()
		{
			if (IsInProjection ())
				return false;
			return completionTextEditorExtension.CanRunParameterCompletionCommand ();
		}

		public override System.Threading.Tasks.Task<CodeCompletion.ICompletionDataList> HandleCodeCompletionAsync (CodeCompletion.CodeCompletionContext completionContext, char completionChar, System.Threading.CancellationToken token)
		{
			return completionTextEditorExtension.HandleCodeCompletionAsync (completionContext, completionChar, token);
		}

		public override System.Threading.Tasks.Task<CodeCompletion.ParameterHintingResult> HandleParameterCompletionAsync (CodeCompletion.CodeCompletionContext completionContext, char completionChar, System.Threading.CancellationToken token)
		{
			return completionTextEditorExtension.HandleParameterCompletionAsync (completionContext, completionChar, token);
		}

		public override bool GetCompletionCommandOffset (out int cpos, out int wlen)
		{
			if (IsInProjection ()) {
				cpos = 0; wlen = 0;
				return false;
			}
			return completionTextEditorExtension.GetCompletionCommandOffset (out cpos, out wlen);
		}

		public override CodeCompletion.ICompletionDataList ShowCodeSurroundingsCommand (CodeCompletion.CodeCompletionContext completionContext)
		{
			if (IsInProjection ()) return null;
			return completionTextEditorExtension.ShowCodeSurroundingsCommand (completionContext);
		}

		public override CodeCompletion.ICompletionDataList ShowCodeTemplatesCommand (CodeCompletion.CodeCompletionContext completionContext)
		{
			if (IsInProjection ()) return null;
			return completionTextEditorExtension.ShowCodeTemplatesCommand (completionContext);
		}

		public override CodeCompletion.ICompletionDataList CodeCompletionCommand (CodeCompletion.CodeCompletionContext completionContext)
		{
			if (IsInProjection ()) return null;
			return completionTextEditorExtension.CodeCompletionCommand (completionContext);
		}

		public override CodeCompletion.ParameterHintingResult ParameterCompletionCommand (CodeCompletion.CodeCompletionContext completionContext)
		{
			if (IsInProjection ()) return null;
			return completionTextEditorExtension.ParameterCompletionCommand (completionContext);
		}

		public override int GuessBestMethodOverload (CodeCompletion.ParameterHintingResult provider, int currentOverload)
		{
			if (IsInProjection ()) return -1;
			return completionTextEditorExtension.GuessBestMethodOverload (provider, currentOverload);
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

