//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.TextEditor
{
	[Export (typeof (IObscuringTipManager))]
	class WpfObscuringTipManager : IObscuringTipManager
	{
		readonly Dictionary<ITextView, List<IObscuringTip>> tipStacks
			= new Dictionary<ITextView, List<IObscuringTip>> ();

		public void PushTip (ITextView view, IObscuringTip tip)
		{
			if (!(view is IWpfTextView)) {
				return;
			}

			if (!tipStacks.TryGetValue (view, out var stack)) {
				view.Closed += OnTextViewClosed;
				tipStacks[view] = stack = new List<IObscuringTip> ();
			}

			stack.Add (tip);
		}

		public void RemoveTip (ITextView view, IObscuringTip tip)
		{
			if (!(view is IWpfTextView)) {
				return;
			}

			if (tipStacks.TryGetValue (view, out var stack)) {
				stack.Remove (tip);
			}
		}

		public bool HasStack (ITextView view)
			=> tipStacks.TryGetValue (view, out var stack) && stack.Count > 0;

		public bool DismissTopOfStack (ITextView view)
		{
			if (tipStacks.TryGetValue (view, out var stack)) {
				if (stack.Count > 0) {
					// NOTE: Tip is responsible for removing itself.
					stack[stack.Count - 1].Dismiss ();
					return true;
				}
			}

			return false;
		}

		void OnTextViewClosed (object sender, EventArgs e)
		{
			var textView = (ITextView)sender;
			tipStacks.Remove (textView);
			textView.Closed -= OnTextViewClosed;
		}
	}
}