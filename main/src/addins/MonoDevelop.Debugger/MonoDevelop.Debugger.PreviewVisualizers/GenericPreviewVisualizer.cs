//
// GenericPreviewVisualizer.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Components;
using Mono.Debugging.Client;
using Gtk;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Debugger.PreviewVisualizers
{
	public class GenericPreviewVisualizer : PreviewVisualizer
	{
		Label label;
		string value;

		public void Copy()
		{
			string text;
			if (label.GetSelectionBounds (out int start, out int end))
				text = label.Text.Substring (start, end - start);
			else
				text = value;//put full value into clipboard, not ellipsized one
			Clipboard.Get (Gdk.Selection.Clipboard).Text = text;
		}

		#region implemented abstract members of PreviewVisualizer

		public override bool CanVisualize (ObjectValue val)
		{
			throw new InvalidOperationException ();//Should never be called since this is special PreviewVisualizer
		}

		public override Control GetVisualizerWidget (ObjectValue val)
		{
			var ops = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
			ops.AllowTargetInvoke = true;
			ops.ChunkRawStrings = true;
			ops.EllipsizedLength = 5000;//Preview window can hold aprox. 4700 chars
			val.Refresh (ops);//Refresh DebuggerDisplay/String value with full length instead of ellipsized
			value = val.Value;
			Gdk.Color col = Styles.PreviewVisualizerTextColor.ToGdkColor ();

			if (DebuggingService.HasInlineVisualizer (val))
				value = DebuggingService.GetInlineVisualizer (val).InlineVisualize (val);
			
			label = new Label ();
			label.Text = value;
			label.ModifyFont (FontService.SansFont.CopyModified (Ide.Gui.Styles.FontScale11));
			label.ModifyFg (StateType.Normal, col);
			label.SetPadding (4, 4);

			if (label.SizeRequest ().Width > 500) {
				label.WidthRequest = 500;
				label.Wrap = true;
				label.LineWrapMode = Pango.WrapMode.WordChar;
			} else {
				label.Justify = Gtk.Justification.Center;
			}

			if (label.Layout.GetLine (1) != null) {
				label.Justify = Gtk.Justification.Left;
				var trimmedLine = label.Layout.GetLine (50);
				if (trimmedLine != null) {
					label.Text = value.Substring (0, trimmedLine.StartIndex).TrimEnd ('\r', '\n') + "\n…";
				}
			}
			label.Selectable = true;
			label.CanFocus = false;
			label.Show ();

			return label;
		}

		#endregion
	}
}

