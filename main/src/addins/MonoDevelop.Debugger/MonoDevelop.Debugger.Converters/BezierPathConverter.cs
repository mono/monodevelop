//
// BezierPathConverter.cs
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
using System.Collections.Generic;

namespace MonoDevelop.Debugger.Converters
{
	public enum BezierPathElementType
	{
		MoveTo,
		LineTo,
		CurveTo,
		ClosePath
	}

	public struct BezierPathElement
	{
		public BezierPathElementType ElementType;
		public Xwt.Point Point1;
		public Xwt.Point Point2;
		public Xwt.Point Point3;
	}

	class BezierPathConverter : DebugValueConverter<List<BezierPathElement>>
	{
		#region implemented abstract members of DebugValueConverter

		public override bool CanGetValue (ObjectValue val)
		{
			return val.TypeName.EndsWith ("AppKit.NSBezierPath") ||
			val.TypeName.EndsWith ("UIKit.UIBezierPath");
		}

		enum NSBezierPathElement
		{
			MoveTo,
			LineTo,
			CurveTo,
			ClosePath
		}

		public override List<BezierPathElement> GetValue (ObjectValue val)
		{
			var ops = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
			ops.AllowTargetInvoke = true;
			var elements = new List<BezierPathElement> ();
			var rawVal = val.GetRawValue (ops) as RawValue;
			int elementCount = (int)rawVal.GetMemberValue ("ElementCount");
			for (int i = 0; i < elementCount; i++) {
				object[] outArgs;
				var elementType = rawVal.CallMethod ("ElementAt", out outArgs, i, new System.Drawing.PointF[0]);
				if (outArgs == null) {
					//If outArgs are null we are probably on old mono runtime which
					//doesn't support out arguments
					return null;
				}
				var rawArray = outArgs [1] as RawValueArray;
				var array = rawArray.GetValues (0, rawArray.Length);
				switch ((NSBezierPathElement)(long)elementType) {
				case NSBezierPathElement.MoveTo:
					elements.Add (new BezierPathElement () {
						ElementType = BezierPathElementType.MoveTo,
						Point1 = new Xwt.Point (
							(float)(array.GetValue (0) as RawValue).GetMemberValue ("X"),
							(float)(array.GetValue (0) as RawValue).GetMemberValue ("Y"))
					});
					break;
				case NSBezierPathElement.ClosePath:
					elements.Add (new BezierPathElement () {
						ElementType = BezierPathElementType.ClosePath
					});
					break;
				case NSBezierPathElement.LineTo:
					elements.Add (new BezierPathElement () {
						ElementType = BezierPathElementType.LineTo,
						Point1 = new Xwt.Point (
							(float)(array.GetValue (0) as RawValue).GetMemberValue ("X"),
							(float)(array.GetValue (0) as RawValue).GetMemberValue ("Y"))
					});
					break;
				case NSBezierPathElement.CurveTo:
					elements.Add (new BezierPathElement () {
						ElementType = BezierPathElementType.CurveTo,
						Point1 = new Xwt.Point ((float)(array.GetValue (0) as RawValue).GetMemberValue ("X"),
							(float)(array.GetValue (0) as RawValue).GetMemberValue ("Y")),
						Point2 = new Xwt.Point ((float)(array.GetValue (1) as RawValue).GetMemberValue ("X"),
							(float)(array.GetValue (1) as RawValue).GetMemberValue ("Y")),
						Point3 = new Xwt.Point ((float)(array.GetValue (2) as RawValue).GetMemberValue ("X"),
							(float)(array.GetValue (2) as RawValue).GetMemberValue ("Y"))
					});
					break;
				}
			}
			return elements;
		}

		#endregion
	}
}

