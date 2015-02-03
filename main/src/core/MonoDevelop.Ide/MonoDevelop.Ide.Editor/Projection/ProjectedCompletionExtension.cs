//
// ProjectedCompletionExtension.cs
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
using MonoDevelop.Ide.Editor.Extension;
using System.Collections.Generic;


namespace MonoDevelop.Ide.Editor.Projection
{
	sealed class ProjectedCompletionExtension : CompletionTextEditorExtension
	{
		IReadOnlyList<Projection> projections;

		public ProjectedCompletionExtension (IReadOnlyList<Projection> projections)
		{
			if (projections == null)
				throw new ArgumentNullException ("projections");
			this.projections = projections;
		}
		

		public override bool IsValidInContext (DocumentContext context)
		{
			var pctx = context as ProjectedDocumentContext;
			if (pctx == null)
				return false;
			return pctx.ProjectedEditor.GetContent<CompletionTextEditorExtension> () != null;
		}

		protected override void Initialize ()
		{
		}

		Projection GetProjectionAt (int offset)
		{
			foreach (var projection in projections) {
				foreach (var seg in projection.ProjectedSegments) {
					if (seg.ContainsOriginal (offset)) {
						projection.ProjectedEditor.CaretOffset = seg.FromOriginalToProjected (offset);
						return projection;
					}
				}
			}
			return null;
		}

		CompletionTextEditorExtension GetExtensionAt (int offset)
		{
			var projection = GetProjectionAt (offset);
			if (projection != null)			
				return projection.ProjectedEditor.GetContent<CompletionTextEditorExtension> ();
			return null;
		}

		CompletionTextEditorExtension GetCurrentExtension ()
		{
			return GetExtensionAt (Editor.CaretOffset);
		}

		public override bool CanRunCompletionCommand ()
		{
			var projectedExtension = GetCurrentExtension ();
			if (projectedExtension == null)
				return false;
			return projectedExtension.CanRunCompletionCommand ();
		}

		public override MonoDevelop.Ide.CodeCompletion.ICompletionDataList CodeCompletionCommand (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext)
		{
			var projectedExtension = GetExtensionAt (completionContext.TriggerOffset);
			if (projectedExtension == null)
				return null;
			return projectedExtension.CodeCompletionCommand (ConvertContext (completionContext));
		}

		public override bool CanRunParameterCompletionCommand ()
		{
			var projectedExtension = GetCurrentExtension ();
			if (projectedExtension == null)
				return false;
			return projectedExtension.CanRunParameterCompletionCommand ();
		}

		public override string CompletionLanguage {
			get {
				var projectedExtension = GetCurrentExtension ();
				if (projectedExtension == null)
					return base.CompletionLanguage;
				return projectedExtension.CompletionLanguage;
			}
		}

		public override bool GetCompletionCommandOffset (out int cpos, out int wlen)
		{
			var projectedExtension = GetCurrentExtension ();
			if (projectedExtension == null) {
				cpos = 0;
				wlen = 0;
				return false;
			}
			return projectedExtension.GetCompletionCommandOffset (out cpos, out wlen);
		}

		public override int GetCurrentParameterIndex (int startOffset)
		{
			var projectedExtension = GetExtensionAt (startOffset);
			if (projectedExtension == null)
				return -1;
			return projectedExtension.GetCurrentParameterIndex (startOffset);
		}

		public override int GuessBestMethodOverload (ICSharpCode.NRefactory6.CSharp.Completion.ParameterHintingResult provider, int currentOverload)
		{
			var projectedExtension = GetCurrentExtension ();
			if (projectedExtension == null)
				return -1;
			return projectedExtension.GuessBestMethodOverload (provider, currentOverload);
		}

		public override System.Threading.Tasks.Task<MonoDevelop.Ide.CodeCompletion.ICompletionDataList> HandleCodeCompletionAsync (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext, char completionChar, System.Threading.CancellationToken token)
		{
			var projectedExtension = GetExtensionAt (completionContext.TriggerOffset);
			if (projectedExtension == null)
				return null;

			return projectedExtension.HandleCodeCompletionAsync (ConvertContext (completionContext), completionChar, token);
		}

		public override System.Threading.Tasks.Task<ICSharpCode.NRefactory6.CSharp.Completion.ParameterHintingResult> HandleParameterCompletionAsync (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext, char completionChar, System.Threading.CancellationToken token)
		{
			var projectedExtension = GetExtensionAt (completionContext.TriggerOffset);
			if (projectedExtension == null)
				return null;
			return projectedExtension.HandleParameterCompletionAsync (ConvertContext (completionContext), completionChar, token);
		}

		public override bool KeyPress (KeyDescriptor descriptor)
		{
			var projectedExtension = GetCurrentExtension();
			if (projectedExtension != null)
				projectedExtension.KeyPress (descriptor);
			return base.KeyPress (descriptor);
		}

		public override ICSharpCode.NRefactory6.CSharp.Completion.ParameterHintingResult ParameterCompletionCommand (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext)
		{
			var projectedExtension = GetExtensionAt (completionContext.TriggerOffset);
			if (projectedExtension == null)
				return null;
			return projectedExtension.ParameterCompletionCommand (ConvertContext (completionContext));
		}

		public override void RunCompletionCommand ()
		{
			Console.WriteLine ("run completion command !!!!");
			var projectedExtension = GetCurrentExtension();
			if (projectedExtension == null)
				return;
			Console.WriteLine ("found ext : "+ projectedExtension);
			projectedExtension.RunCompletionCommand ();
		}

		public override void RunParameterCompletionCommand ()
		{
			var projectedExtension = GetCurrentExtension();
			if (projectedExtension == null)
				return;
			
			projectedExtension.RunParameterCompletionCommand ();
		}

		public override void RunShowCodeTemplatesWindow ()
		{
			var projectedExtension = GetCurrentExtension();
			if (projectedExtension == null)
				return;
			projectedExtension.RunShowCodeTemplatesWindow ();
		}

		public override MonoDevelop.Ide.CodeCompletion.ICompletionDataList ShowCodeSurroundingsCommand (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext)
		{
			var projectedExtension = GetExtensionAt (completionContext.TriggerOffset);
			if (projectedExtension == null)
				return null;
			return projectedExtension.ShowCodeSurroundingsCommand (ConvertContext (completionContext));
		}

		public override MonoDevelop.Ide.CodeCompletion.ICompletionDataList ShowCodeTemplatesCommand (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext)
		{
			var projectedExtension = GetExtensionAt (completionContext.TriggerOffset);
			if (projectedExtension == null)
				return null;
			return projectedExtension.ShowCodeTemplatesCommand (ConvertContext (completionContext));
		}

		MonoDevelop.Ide.CodeCompletion.CodeCompletionContext ConvertContext (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext)
		{
			var projection = GetProjectionAt (completionContext.TriggerOffset);

			int offset = completionContext.TriggerOffset;
			int line = completionContext.TriggerLine;
			int lineOffset = completionContext.TriggerLineOffset;

			if (projection != null) {
				foreach (var seg in projection.ProjectedSegments) {
					if (seg.ContainsOriginal (offset)) {
						offset = seg.FromOriginalToProjected (offset);
						var loc = projection.ProjectedEditor.OffsetToLocation (offset);
						line = loc.Line;
						lineOffset = loc.Column - 1;
					}
				}
			}

			return new MonoDevelop.Ide.CodeCompletion.CodeCompletionContext {
				TriggerOffset = offset,
				TriggerLine = line,
				TriggerLineOffset  = lineOffset,
				TriggerXCoord  = completionContext.TriggerXCoord,
				TriggerYCoord  = completionContext.TriggerYCoord,
				TriggerTextHeight  = completionContext.TriggerTextHeight,
				TriggerWordLength  = completionContext.TriggerWordLength
			};
		}
	}
}