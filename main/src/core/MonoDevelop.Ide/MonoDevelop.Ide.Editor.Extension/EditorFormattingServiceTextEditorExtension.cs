//
// EditorFormattingServiceTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using Microsoft.CodeAnalysis.Editor;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Editor.Extension
{
	partial class EditorFormattingServiceTextEditorExtension : TextEditorExtension
	{
        protected override void Initialize()
        {
			base.Initialize();
        }

        public override bool KeyPress(KeyDescriptor descriptor)
		{
			var result = base.KeyPress (descriptor);
			if (!DefaultSourceEditorOptions.Instance.OnTheFlyFormatting)
				return result;
			
			var doc = DocumentContext.AnalysisDocument;
			if (doc == null)
				return result;
			
			var formattingService = doc.GetLanguageService<IEditorFormattingService> ();
			if (formattingService == null)
				return result;

			if (descriptor.SpecialKey == SpecialKey.Return) {
				if (formattingService.SupportsFormatOnReturn)
					TryFormat (formattingService, descriptor.KeyChar, Editor.CaretOffset, true, default (CancellationToken));
			} else if (formattingService.SupportsFormattingOnTypedCharacter(doc, descriptor.KeyChar)) {
				TryFormat (formattingService, descriptor.KeyChar, Editor.CaretOffset, false, default (CancellationToken));
			}
			return result;
		}

		bool TryFormat (IEditorFormattingService formattingService, char typedChar, int position, bool formatOnReturn, CancellationToken cancellationToken)
		{
			var document = DocumentContext.AnalysisDocument;
			var changes = formatOnReturn
				? formattingService.GetFormattingChangesOnReturnAsync (document, position, cancellationToken).WaitAndGetResult (cancellationToken)
				: formattingService.GetFormattingChangesAsync (document, typedChar, position, cancellationToken).WaitAndGetResult (cancellationToken);
			var options = document.GetOptionsAsync (cancellationToken).WaitAndGetResult (cancellationToken);

			if (changes == null || changes.Count == 0) {
				return false;
			}

			Editor.ApplyTextChanges (changes);
			return true;
		}
	}
}