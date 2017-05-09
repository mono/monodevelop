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
using MonoDevelop.Ide.CodeCompletion;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Editor.Projection
{
	sealed class ProjectedCompletionExtension : CompletionTextEditorExtension, IProjectionExtension
	{
		DocumentContext ctx;
		IReadOnlyList<Projection> projections;

		public IReadOnlyList<Projection> Projections {
			get {
				return projections;
			}
			set {
				projections = value;
			}
		}

		public ProjectedCompletionExtension (DocumentContext ctx, IReadOnlyList<Projection> projections)
		{
			if (projections == null)
				throw new ArgumentNullException ("projections");
			this.ctx = ctx;
			this.projections = projections;
		}

		public override bool IsValidInContext (DocumentContext context)
		{
			var pctx = context as ProjectedDocumentContext;
			if (pctx == null)
				return false;
			return pctx.ProjectedEditor.GetContent<CompletionTextEditorExtension> () != null;
		}

		internal protected override bool IsActiveExtension ()
		{
			return IsInProjection ();
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
			if (projection != null) {
				var result = projection.ProjectedEditor.GetContent<CompletionTextEditorExtension> ();
				if (result != null && CompletionWidget != null) {
					result.CompletionWidget = new ProjectedCompletionWidget (CompletionWidget, projection);
					if (result.CurrentCompletionContext == null)
						result.CurrentCompletionContext = result.CompletionWidget.CurrentCodeCompletionContext;
				}
				result.Next = Next;
				return result;
			}
			return null;
		}

		class ProjectedCompletionWidget : ICompletionWidget
		{
			readonly ICompletionWidget completionWidget;
			readonly Projection projection;

			public ProjectedCompletionWidget (ICompletionWidget completionWidget, Projection projection)
			{
				if (completionWidget == null)
					throw new ArgumentNullException ("completionWidget");
				if (projection == null)
					throw new ArgumentNullException ("projection");
				this.projection = projection;
				this.completionWidget = completionWidget;
			}

			#region ICompletionWidget implementation
			public double ZoomLevel {
				get {
					return completionWidget.ZoomLevel;
				}
			}

			event EventHandler ICompletionWidget.CompletionContextChanged {
				add {
					completionWidget.CompletionContextChanged += value;
				}
				remove {
					completionWidget.CompletionContextChanged -= value;
				}
			}

			string ICompletionWidget.GetText (int startOffset, int endOffset)
			{
				return completionWidget.GetText(startOffset, endOffset);
			}

			char ICompletionWidget.GetChar (int offset)
			{
				return projection.ProjectedEditor.GetCharAt (offset);
			}

			void ICompletionWidget.Replace (int offset, int count, string text)
			{
				completionWidget.Replace (offset, count, text);
			}

			int ConvertOffset (int triggerOffset)
			{
				int result = triggerOffset;
				foreach (var seg in projection.ProjectedSegments) {
					if (seg.ContainsProjected (result)) {
						result = seg.FromProjectedToOriginal (result);
						break;
					}
				}
				return result;
			}

			int ProjectOffset (int offset)
			{
				int result = offset;
				foreach (var seg in projection.ProjectedSegments) {
					if (seg.ContainsOriginal (result)) {
						result = seg.FromOriginalToProjected (result);
						break;
					}
				}
				return result;

			}

			CodeCompletionContext ICompletionWidget.CreateCodeCompletionContext (int triggerOffset)
			{
				var originalTriggerOffset = ConvertOffset (triggerOffset);
				var completionContext = completionWidget.CreateCodeCompletionContext (originalTriggerOffset);
				return ConvertContext (completionContext, projection);
			}

			string ICompletionWidget.GetCompletionText (CodeCompletionContext ctx)
			{
				return completionWidget.GetCompletionText (ImportContext (ctx, projection));
			}

			void ICompletionWidget.SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
			{
				completionWidget.SetCompletionText (ImportContext (ctx, projection), partial_word, complete_word);
			}

			void ICompletionWidget.SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int completeWordOffset)
			{
				completionWidget.SetCompletionText (ImportContext (ctx, projection), partial_word, complete_word, completeWordOffset);
			}

			CodeCompletionContext ICompletionWidget.CurrentCodeCompletionContext {
				get {
					return ConvertContext (completionWidget.CurrentCodeCompletionContext, projection);
				}
			}

			int ICompletionWidget.CaretOffset {
				get {
					return projection.ProjectedEditor.CaretOffset = ConvertOffset (completionWidget.CaretOffset);
				}
				set {
					completionWidget.CaretOffset = ProjectOffset (value);
				}
			}

			int ICompletionWidget.TextLength {
				get {
					return projection.ProjectedEditor.Length;
				}
			}

			int ICompletionWidget.SelectedLength {
				get {
					return completionWidget.SelectedLength;
				}
			}

			Gtk.Style ICompletionWidget.GtkStyle {
				get {
					return completionWidget.GtkStyle;
				}
			}
			#endregion
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

		public override Task<int> GetCurrentParameterIndex (int startOffset, System.Threading.CancellationToken token)
		{
			var projectedExtension = GetExtensionAt (startOffset);
			if (projectedExtension == null)
				return Task.FromResult (-1);
			return projectedExtension.GetCurrentParameterIndex (startOffset, token);
		}

		public override Task<int> GuessBestMethodOverload (ParameterHintingResult provider, int currentOverload, System.Threading.CancellationToken token)
		{
			var projectedExtension = GetCurrentExtension ();
			if (projectedExtension == null)
				return Task.FromResult (-1);
			return projectedExtension.GuessBestMethodOverload (provider, currentOverload, token);
		}

		public override System.Threading.Tasks.Task<MonoDevelop.Ide.CodeCompletion.ICompletionDataList> HandleCodeCompletionAsync (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext, CompletionTriggerInfo triggerInfo, System.Threading.CancellationToken token)
		{
			var projectedExtension = GetExtensionAt (completionContext.TriggerOffset);
			if (projectedExtension == null)
				return null;

			return projectedExtension.HandleCodeCompletionAsync (ConvertContext (completionContext), triggerInfo, token);
		}

		public override Task<ParameterHintingResult> HandleParameterCompletionAsync (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext, char completionChar, System.Threading.CancellationToken token)
		{
			var projectedExtension = GetExtensionAt (completionContext.TriggerOffset);
			if (projectedExtension == null)
				return Task.FromResult<ParameterHintingResult> (null);
			return projectedExtension.HandleParameterCompletionAsync (ConvertContext (completionContext), completionChar, token);
		}

		public override bool KeyPress (KeyDescriptor descriptor)
		{
			var task = ctx.GetPartialProjectionsAsync ();
			if (task != null)
				projections = task.Result;
			var projectedExtension = GetCurrentExtension();
			if (projectedExtension != null) {
				return projectedExtension.KeyPress (descriptor);
			}
			return base.KeyPress (descriptor);
		}

		public override Task<ParameterHintingResult> ParameterCompletionCommand (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext)
		{
			var projectedExtension = GetExtensionAt (completionContext.TriggerOffset);
			if (projectedExtension == null)
				return null;
			return projectedExtension.ParameterCompletionCommand (ConvertContext (completionContext));
		}

		public override void RunCompletionCommand ()
		{
			var projectedExtension = GetCurrentExtension();
			if (projectedExtension == null)
				return;
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

		CodeCompletionContext ConvertContext (CodeCompletionContext completionContext)
		{
			var projection = GetProjectionAt (completionContext.TriggerOffset);
			return ConvertContext (completionContext, projection);
		}
			
		static CodeCompletionContext ConvertContext (CodeCompletionContext completionContext, Projection projection)
		{
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

		static CodeCompletionContext ImportContext (CodeCompletionContext completionContext, Projection projection)
		{
			int offset = completionContext.TriggerOffset;
			int line = completionContext.TriggerLine;
			int lineOffset = completionContext.TriggerLineOffset;

			if (projection != null) {
				foreach (var seg in projection.ProjectedSegments) {
					if (seg.ContainsProjected (offset)) {
						offset = seg.FromProjectedToOriginal (offset);
						var loc = projection.ProjectedEditor.OffsetToLocation (offset);
						line = loc.Line;
						lineOffset = loc.Column - 1;
						break;
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
	
		internal protected override void OnCompletionContextChanged (object o, EventArgs a)
		{
			if (!IsActiveExtension ())
				return;
			var ext = GetExtensionAt (Editor.CaretOffset);
			if (ext != null) {
				ext.CurrentCompletionContext = ext.CompletionWidget.CurrentCodeCompletionContext;
			}
		}

		internal protected override void HandlePositionChanged (object sender, EventArgs e)
		{
			if (!IsActiveExtension ())
				return;
			var ext = GetExtensionAt (Editor.CaretOffset);
			if (ext != null)
				ext.HandlePositionChanged (sender, e);
		}
	}
}