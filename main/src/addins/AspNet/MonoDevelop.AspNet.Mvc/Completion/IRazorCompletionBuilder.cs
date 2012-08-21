//
// IRazorCompletionBuilder.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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
using System.Linq;
using System.Text;
using MonoDevelop.Ide.CodeCompletion;
using ICSharpCode.NRefactory.Completion;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Gui;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.AspNet.Mvc.Completion
{
	// Based on MonoDevelop.AspNet.Gui.ILanguageCompletionBuilder

	public interface IRazorCompletionBuilder
	{
		bool SupportsLanguage (string language);
		ICompletionWidget CreateCompletionWidget (Document document, UnderlyingDocumentInfo docInfo);
		ICompletionDataList HandlePopupCompletion (Document realDocument, UnderlyingDocumentInfo docInfo);
		ICompletionDataList HandleCompletion (Document realDocument, CodeCompletionContext completionContext,
			UnderlyingDocumentInfo docInfo, char currentChar, ref int triggerWordLength);
		ParameterDataProvider HandleParameterCompletion (Document realDocument, CodeCompletionContext completionContext,
			UnderlyingDocumentInfo docInfo, char completionChar);
		bool GetParameterCompletionCommandOffset (Document realDocument, UnderlyingDocumentInfo docInfo, out int cpos);
		int GetCurrentParameterIndex (Document realDocument, UnderlyingDocumentInfo docInfo, int startOffset);
	}

	public class UnderlyingDocument : Document
	{
		internal ParsedDocument HiddenParsedDocument;
		internal ICompilation HiddenCompilation;

		public override ParsedDocument ParsedDocument {
			get	{ return HiddenParsedDocument; }
		}

		public override ICompilation Compilation {
			get { return HiddenCompilation; }
		}

		public UnderlyingDocument (IWorkbenchWindow window)
			: base (window)
		{
		}
	}

	public class UnderlyingDocumentInfo
	{
		public int CaretPosition { get; set; }
		public int OriginalCaretPosition { get; set; }
		public UnderlyingDocument UnderlyingDocument { get; set; }
	}

	public static class RazorCompletionBuilderService
	{
		static List<IRazorCompletionBuilder> builder = new List<IRazorCompletionBuilder> ();

		public static IEnumerable<IRazorCompletionBuilder> Builder {
			get	{ return builder; }
		}

		static RazorCompletionBuilderService ()
		{
			Mono.Addins.AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Razor/CompletionBuilders", delegate (object sender, Mono.Addins.ExtensionNodeEventArgs args)
			{
				switch (args.Change) {
					case Mono.Addins.ExtensionChange.Add:
						builder.Add ((IRazorCompletionBuilder)args.ExtensionObject);
						break;
					case Mono.Addins.ExtensionChange.Remove:
						builder.Remove ((IRazorCompletionBuilder)args.ExtensionObject);
						break;
				}
			});
		}

		public static IRazorCompletionBuilder GetBuilder (string language)
		{
			foreach (IRazorCompletionBuilder b in Builder) {
				if (b.SupportsLanguage (language))
					return b;
			}
			return null;
		}
	}
}
