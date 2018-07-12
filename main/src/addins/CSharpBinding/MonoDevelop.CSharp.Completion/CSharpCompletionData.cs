//
// CSharpCompletionData.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide;
using Microsoft.CodeAnalysis.CSharp.Completion;
using MonoDevelop.Ide.Gui;
using Microsoft.VisualStudio.Platform;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Ide.CodeTemplates;
using System.Linq;
using System.Text;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.CSharp.Formatting;

namespace MonoDevelop.CSharp.Completion
{
	class CSharpCompletionData  : RoslynCompletionData
	{
		Lazy<CompletionProvider> provider;

		public override CompletionProvider Provider => provider.Value;
		protected override string MimeType => CSharpFormatter.MimeType;


		public CSharpCompletionData (Microsoft.CodeAnalysis.Document document, ITextSnapshot triggerSnapshot, CompletionService completionService, CompletionItem completionItem) : base (document, triggerSnapshot, completionService, completionItem)
		{
			provider = new Lazy<CompletionProvider> (delegate {
				return ((CSharpCompletionService)completionService).GetProvider (CompletionItem);
			});
		}

		public override bool MuteCharacter (char keyChar, string partialWord)
		{
			return false;
		}

	}
}
