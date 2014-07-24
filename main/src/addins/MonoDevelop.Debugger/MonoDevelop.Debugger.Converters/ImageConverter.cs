//
// ImageConverter.cs
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
using Xwt.Drawing;
using Mono.Debugging.Client;
using System.IO;

namespace MonoDevelop.Debugger.Converters
{
	class ImageConverter : DebugValueConverter<Image>
	{
		#region implemented abstract members of DebugValueConverter

		public override bool CanGetValue (ObjectValue val)
		{
			return val.TypeName != null && (
			    val.TypeName == "Android.Graphics.Bitmap" ||
			    val.TypeName == "Gdk.Pixbuf" ||
			    val.TypeName.EndsWith ("UIKit.UIImage") ||
			    val.TypeName.EndsWith ("CoreGraphics.CGImage"));
		}

		public override Image GetValue (ObjectValue val)
		{
			var ops = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
			ops.AllowTargetInvoke = true;
			var rawVal = (RawValue)val.GetRawValue (ops);
			if (val.TypeName == "Gdk.Pixbuf") {
				var arrayObject = (RawValueArray)rawVal.CallMethod ("SaveToBuffer", "png");
				var bytes = (byte[])(arrayObject).GetValues (0, arrayObject.Length);
				var ms = new MemoryStream (bytes, false);
				return Image.FromStream (ms);
			} else if (val.TypeName == "Android.Graphics.Bitmap") {
				var memoryStream = DebuggingService.CurrentFrame.GetExpressionValue ("new System.IO.MemoryStream()", true).GetRawValue (ops);
				var pngEnum = DebuggingService.CurrentFrame.GetExpressionValue ("Android.Graphics.Bitmap.CompressFormat.Png", true).GetRawValue (ops);
				((RawValue)val.GetRawValue (ops)).CallMethod ("Compress", pngEnum, 0, memoryStream);
				var arrayObject = (RawValueArray)((RawValue)memoryStream).CallMethod ("ToArray");
				var bytes = (byte[])(arrayObject).GetValues (0, arrayObject.Length);
				var ms = new MemoryStream (bytes, false);
				return Image.FromStream (ms);
			} else if (val.TypeName.EndsWith ("CoreGraphics.CGImage")) {
				rawVal = (RawValue)DebuggingService.CurrentFrame.GetExpressionValue ("MonoTouch.UIKit.UIImage.FromImage(" + val.Name + ")", true).GetRawValue (ops);
				RawValue nsData = (RawValue)rawVal.CallMethod ("AsPNG");
				var arrayObject = (RawValueArray)nsData.CallMethod ("ToArray");
				var bytes = (byte[])(arrayObject).GetValues (0, arrayObject.Length);
				var ms = new MemoryStream (bytes, false);
				return Image.FromStream (ms);
			} else if (val.TypeName.EndsWith ("UIKit.UIImage")) {
				RawValue nsData = (RawValue)rawVal.CallMethod ("AsPNG");
				var arrayObject = (RawValueArray)nsData.CallMethod ("ToArray");
				var bytes = (byte[])(arrayObject).GetValues (0, arrayObject.Length);
				var ms = new MemoryStream (bytes, false);
				return Image.FromStream (ms);
			} else {
				throw new NotSupportedException ();
			}
		}

		#endregion
	}
}

