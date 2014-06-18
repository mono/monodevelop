//
// ColorInlineVisualizer.cs
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

namespace MonoDevelop.Debugger.InlineVisualizers
{
	public class ColorInlineVisualizer : InlineVisualizer
	{
		#region InlineVisualizer implementation

		public override bool CanInlineVisualize (ObjectValue val)
		{
			return DebuggingService.HasColorConverter (val);
		}

		public override string InlineVisualize (ObjectValue val)
		{
			var color = DebuggingService.GetColorConverter (val).GetColor (val);
			return "R=" + ((byte)(color.Red * 255.0)) + ", " +
			"G=" + ((byte)(color.Green * 255.0)) + ", " +
			"B=" + ((byte)(color.Blue * 255.0)) + ", " +
			"A=" + ((byte)(color.Alpha * 255.0));
		}

		#endregion
	}
}

