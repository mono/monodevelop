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

namespace MonoDevelop.Debugger.PreviewVisualizers
{
	public class GenericPreviewVisualizer : PreviewVisualizer
	{
		#region implemented abstract members of PreviewVisualizer

		public override bool CanVisualize (ObjectValue val)
		{
			throw new InvalidOperationException ();//Should never be called since this is special PreviewVisualizer
		}

		public override Control GetVisualizerWidget (ObjectValue val)
		{
			string value = val.Value;
			Gdk.Color col = new Gdk.Color (85, 85, 85);

			if (!val.IsNull && (val.TypeName == "string" || val.TypeName == "char[]"))
				value = '"' + GetString (val) + '"';
			if (DebuggingService.HasInlineVisualizer (val))
				value = DebuggingService.GetInlineVisualizer (val).InlineVisualize (val);

			var label = new Gtk.Label (value);
			var font = label.Style.FontDescription.Copy ();

			if (font.SizeIsAbsolute) {
				font.AbsoluteSize = font.Size - 1;
			} else {
				font.Size -= (int)(Pango.Scale.PangoScale);
			}

			label.ModifyFont (font);
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
				var line15 = label.Layout.GetLine (15);
				if (line15 != null) {
					label.Text = value.Substring (0, line15.StartIndex).TrimEnd ('\r', '\n') + "\n…";
				}
			}

			label.Show ();

			return label;
		}

		#endregion

		string GetString (ObjectValue val)
		{
			var ops = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
			ops.AllowTargetInvoke = true;
			ops.ChunkRawStrings = true;
			if (val.TypeName == "string") {
				var rawString = val.GetRawValue (ops) as RawValueString;
				var length = rawString.Length;
				if (length > 0) {
					return rawString.Substring (0, Math.Min (length, 4096));
				} else {
					return "";
				}
			} else if (val.TypeName == "char[]") {
				var rawArray = val.GetRawValue (ops) as RawValueArray;
				var length = rawArray.Length;
				if (length > 0) {
					return new string (rawArray.GetValues (0, Math.Min (length, 4096)) as char[]);
				} else {
					return "";
				}

			} else {
				throw new InvalidOperationException ();
			}
		}
	}
}

