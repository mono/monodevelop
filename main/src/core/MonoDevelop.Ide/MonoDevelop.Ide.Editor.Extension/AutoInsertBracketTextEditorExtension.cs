//
// AutoInsertBracketTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using Mono.Addins;

namespace MonoDevelop.Ide.Editor.Extension
{
	public abstract class AutoInsertBracketHandler
	{
		public abstract bool CanHandle (TextEditor editor);

		public abstract bool Handle (TextEditor editor, DocumentContext ctx, KeyDescriptor descriptor);
	}

	class AutoInsertBracketTextEditorExtension : TextEditorExtension
	{
		static List<AutoInsertBracketHandler> allHandlers = new List<AutoInsertBracketHandler> ();

		public AutoInsertBracketTextEditorExtension ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/AutoInsertBracketHandler", delegate (object sender, ExtensionNodeEventArgs args) {
				var newHandler = (AutoInsertBracketHandler)args.ExtensionObject;
				switch (args.Change) {
				case ExtensionChange.Add:
					allHandlers.Add (newHandler);
					break;
				case ExtensionChange.Remove:
					allHandlers.Remove (newHandler);
					break;
				}
			});
		}

		public override bool KeyPress (KeyDescriptor descriptor)
		{
			var result = base.KeyPress (descriptor);

			if (DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket && !Editor.IsSomethingSelected) {
				var handler = allHandlers.FirstOrDefault(h => h.CanHandle (Editor));
				if (handler != null && handler.Handle (Editor, DocumentContext, descriptor))
					return false;
			}
			return result;
		}
	}
}

