//
// RectanglesConverters.cs
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
using Xwt;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Converters
{
	class RectanglesConverters : DebugValueConverter<Rectangle>
	{
		#region implemented abstract members of DebugValueConverter

		public override bool CanGetValue (ObjectValue val)
		{
			return val.TypeName == "System.Drawing.Rectangle" ||
			val.TypeName == "Gdk.Rectangle" ||
			val.TypeName == "Xamarin.Forms.Rectangle" ||
			val.TypeName == "System.Drawing.RectangleF" ||
			val.TypeName == "Android.Graphics.Rect" ||
			val.TypeName == "Android.Graphics.RectF";
		}

		public override Rectangle GetValue (ObjectValue val)
		{
			var ops = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
			ops.AllowTargetInvoke = true;
			var rectangle = new Rectangle ();
			if (val.TypeName == "System.Drawing.RectangleF") {
				rectangle.X = (float)val.GetChild ("X", ops).GetRawValue (ops);
				rectangle.Y = (float)val.GetChild ("Y", ops).GetRawValue (ops);
				rectangle.Width = (float)val.GetChild ("Width", ops).GetRawValue (ops);
				rectangle.Height = (float)val.GetChild ("Height", ops).GetRawValue (ops);
			}
			if (val.TypeName == "Android.Graphics.Rect") {
				rectangle.X = (int)val.GetChild ("Left", ops).GetRawValue (ops);
				rectangle.Y = (int)val.GetChild ("Top", ops).GetRawValue (ops);
				rectangle.Right = (int)val.GetChild ("Right", ops).GetRawValue (ops);
				rectangle.Bottom = (int)val.GetChild ("Bottom", ops).GetRawValue (ops);
			} else if (val.TypeName == "Android.Graphics.RectF") {
				rectangle.X = (float)val.GetChild ("Left", ops).GetRawValue (ops);
				rectangle.Y = (float)val.GetChild ("Top", ops).GetRawValue (ops);
				rectangle.Right = (float)val.GetChild ("Right", ops).GetRawValue (ops);
				rectangle.Bottom = (float)val.GetChild ("Bottom", ops).GetRawValue (ops);
			} else {
				rectangle.X = (int)val.GetChild ("X", ops).GetRawValue (ops);
				rectangle.Y = (int)val.GetChild ("Y", ops).GetRawValue (ops);
				rectangle.Width = (int)val.GetChild ("Width", ops).GetRawValue (ops);
				rectangle.Height = (int)val.GetChild ("Height", ops).GetRawValue (ops);
			}
			return rectangle;
		}

		#endregion
	}
}

