//
// ColorsConverters.cs
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
using Mono.Debugging.Client;
using Xwt.Drawing;

namespace MonoDevelop.Debugger.Converters
{
	class ColorsConverters : DebugValueConverter<Color>
	{
		#region ColorConverter implementation

		public override bool CanGetValue (ObjectValue val)
		{
			return val.TypeName != null && (
			    val.TypeName == "System.Drawing.Color" ||
			    val.TypeName == "Gdk.Color" ||
			    val.TypeName.EndsWith ("UIKit.UIColor") ||
			    val.TypeName.EndsWith ("CoreGraphics.CGColor"));
		}

		public override Color GetValue (ObjectValue val)
		{
			var ops = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
			ops.AllowTargetInvoke = true;
			if (val.TypeName == "System.Drawing.Color") {
				return Color.FromBytes (
					(byte)val.GetChild ("R", ops).GetRawValue (ops),
					(byte)val.GetChild ("G", ops).GetRawValue (ops),
					(byte)val.GetChild ("B", ops).GetRawValue (ops),
					(byte)val.GetChild ("A", ops).GetRawValue (ops));
			} else if (val.TypeName == "Gdk.Color") {
				return new Color (
					((ushort)val.GetChild ("Red", ops).GetRawValue (ops) / 65535.0),
					((ushort)val.GetChild ("Green", ops).GetRawValue (ops) / 65535.0),
					((ushort)val.GetChild ("Blue", ops).GetRawValue (ops) / 65535.0));
			} else if (val.TypeName.EndsWith ("UIKit.UIColor")) {
				var arrayObject = (RawValueArray)val.GetChild ("CGColor", ops).GetChild ("Components").GetRawValue (ops);
				var components = (float[])(arrayObject).GetValues (0, arrayObject.Length);
				if (components.Length == 4)
					return new Color (components [0], components [1], components [2], components [3]);
				else
					return new Color (components [0], components [0], components [0], components [1]);
			} else if (val.TypeName.EndsWith ("CoreGraphics.CGColor")) {
				var arrayObject = (RawValueArray)val.GetChild ("Components").GetRawValue (ops);
				var components = (float[])(arrayObject).GetValues (0, arrayObject.Length);
				if (components.Length == 4)
					return new Color (components [0], components [1], components [2], components [3]);
				else
					return new Color (components [0], components [0], components [0], components [1]);
			}
			throw new NotSupportedException ();
		}

		#endregion
	}
}

