//
// NewViEditMode.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace Mono.TextEditor.Vi
{
	public class NewViEditMode : EditMode
	{
		protected ViEditor ViEditor { get ; private set ;}

		public NewViEditMode ()
		{
			ViEditor = new ViEditor (this);
		}

		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			ViEditor.ProcessKey (modifier, key, (char)unicodeKey);
		}

		public new TextEditor Editor { get { return base.Editor; } }
		public new TextEditorData Data { get { return base.Data; } }

		public override bool WantsToPreemptIM {
			get {
				switch (ViEditor.Mode) {
				case ViEditorMode.Insert:
				case ViEditorMode.Replace:
					return false;
				case ViEditorMode.Normal:
				case ViEditorMode.Visual:
				case ViEditorMode.VisualLine:
				default:
					return true;
				}
			}
		}

		protected override void OnAddedToEditor (TextEditorData data)
		{
			ViEditor.SetMode (ViEditorMode.Normal);
			SetCaretMode (CaretMode.Block, data);
			ViActions.RetreatFromLineEnd (data);
		}

		protected override void OnRemovedFromEditor (TextEditorData data)
		{
			SetCaretMode (CaretMode.Insert, data);
		}

		protected override void CaretPositionChanged ()
		{
			ViEditor.OnCaretPositionChanged ();
		}

		public void SetCaretMode (CaretMode mode)
		{
			SetCaretMode (mode, Data);
		}

		static void SetCaretMode (CaretMode mode, TextEditorData data)
		{
			if (data.Caret.Mode == mode)
				return;
			data.Caret.Mode = mode;
			data.Document.RequestUpdate (new SinglePositionUpdate (data.Caret.Line, data.Caret.Column));
			data.Document.CommitDocumentUpdate ();
		}
	}

}
