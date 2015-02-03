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
		CompletionTextEditorExtension projectedExtension;

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
		//	projectedExtension = ProjectedDocumentContext.ProjectedEditor.GetContent<CompletionTextEditorExtension> ();
		}

		public override bool CanRunCompletionCommand ()
		{
			return projectedExtension.CanRunCompletionCommand ();
		}

		public override MonoDevelop.Ide.CodeCompletion.ICompletionDataList CodeCompletionCommand (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext)
		{
			return projectedExtension.CodeCompletionCommand (completionContext);
		}

		public override bool CanRunParameterCompletionCommand ()
		{
			return projectedExtension.CanRunParameterCompletionCommand ();
		}

		public override string CompletionLanguage {
			get {
				return projectedExtension.CompletionLanguage;
			}
		}

		public override bool GetCompletionCommandOffset (out int cpos, out int wlen)
		{
			return projectedExtension.GetCompletionCommandOffset (out cpos, out wlen);
		}

		public override int GetCurrentParameterIndex (int startOffset)
		{
			return projectedExtension.GetCurrentParameterIndex (startOffset);
		}

		public override int GuessBestMethodOverload (ICSharpCode.NRefactory6.CSharp.Completion.ParameterHintingResult provider, int currentOverload)
		{
			return projectedExtension.GuessBestMethodOverload (provider, currentOverload);
		}

		public override System.Threading.Tasks.Task<MonoDevelop.Ide.CodeCompletion.ICompletionDataList> HandleCodeCompletionAsync (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext, char completionChar, System.Threading.CancellationToken token)
		{
			return projectedExtension.HandleCodeCompletionAsync (completionContext, completionChar, token);
		}

		public override System.Threading.Tasks.Task<ICSharpCode.NRefactory6.CSharp.Completion.ParameterHintingResult> HandleParameterCompletionAsync (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext, char completionChar, System.Threading.CancellationToken token)
		{
			return projectedExtension.HandleParameterCompletionAsync (completionContext, completionChar, token);
		}

		public override bool KeyPress (KeyDescriptor descriptor)
		{
			return projectedExtension.KeyPress (descriptor);
		}

		public override ICSharpCode.NRefactory6.CSharp.Completion.ParameterHintingResult ParameterCompletionCommand (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext)
		{
			return projectedExtension.ParameterCompletionCommand (completionContext);
		}

		public override void RunCompletionCommand ()
		{
			projectedExtension.RunCompletionCommand ();
		}

		public override void RunParameterCompletionCommand ()
		{
			projectedExtension.RunParameterCompletionCommand ();
		}

		public override void RunShowCodeTemplatesWindow ()
		{
			projectedExtension.RunShowCodeTemplatesWindow ();
		}

		public override MonoDevelop.Ide.CodeCompletion.ICompletionDataList ShowCodeSurroundingsCommand (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext)
		{
			return projectedExtension.ShowCodeSurroundingsCommand (completionContext);
		}

		public override MonoDevelop.Ide.CodeCompletion.ICompletionDataList ShowCodeTemplatesCommand (MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext)
		{
			return projectedExtension.ShowCodeTemplatesCommand (completionContext);
		}
	}
}