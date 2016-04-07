//
// AnonymousMethodCompletionData.cs
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
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.CSharp.Formatting;

namespace MonoDevelop.CSharp.Completion
{
	class AnonymousMethodCompletionData : RoslynCompletionData
	{
		RoslynCodeCompletionFactory factory;
		public override int PriorityGroup { get { return 2; } }

		public AnonymousMethodCompletionData (RoslynCodeCompletionFactory factory, ICompletionDataKeyHandler keyHandler) : base (keyHandler)
		{
			this.factory = factory;
			this.Icon = "md-newmethod";
		}

		public override int CompareTo (object obj)
		{
			var anonymousMethodCompletionData = obj as AnonymousMethodCompletionData;
			if (anonymousMethodCompletionData == null)
				return -1;

			return DisplayText.CompareTo(anonymousMethodCompletionData.DisplayText);
		}

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Ide.Editor.Extension.KeyDescriptor descriptor)
		{
			base.InsertCompletionText (window, ref ka, descriptor);
			factory.Ext.Editor.GetContent<CSharpTextEditorIndentation> ().DoReSmartIndent ();
			if (this.CompletionText.Contains ("\n")) {
				
				factory.Ext.Editor.GetContent<CSharpTextEditorIndentation> ().DoReSmartIndent (factory.Ext.Editor.GetLine (factory.Ext.Editor.CaretLine).NextLine.Offset);
			}
		}


	}
}

