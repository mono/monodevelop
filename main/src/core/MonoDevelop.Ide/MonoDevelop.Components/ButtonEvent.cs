//
// ButtonEventArgs.cs
//
// Author:
//       therzok <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 therzok
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

namespace MonoDevelop.Components
{
	public class ButtonEvent : Xwt.ButtonEventArgs
	{
		public ButtonEvent ()
		{
		}

		Gdk.EventButton native;
		new bool Handled { get; set; }

		public static implicit operator ButtonEvent (Gdk.EventButton args)
		{
			int numPress;
			if (args.Type == Gdk.EventType.TwoButtonPress)
				numPress = 2;
			else if (args.Type == Gdk.EventType.ThreeButtonPress)
				numPress = 3;
			else
				numPress = 1;
			return new ButtonEvent {
				native = args,
				X = args.X,
				Y = args.Y,
				Button = (Xwt.PointerButton)args.Button,
				MultiplePress = numPress,
			};
		}

		public static implicit operator Gdk.EventButton (ButtonEvent args)
		{
			return args.native;
		}
	}
}

