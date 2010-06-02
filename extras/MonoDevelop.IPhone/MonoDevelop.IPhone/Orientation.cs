// 
// Orientation.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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

namespace MonoDevelop.IPhone
{
	[Flags]	
	public enum Orientation {
		None  = 0,
		Up    = 1 << 0, // UIInterfaceOrientationPortrait
		Down  = 1 << 1, // UIInterfaceOrientationPortraitUpsideDown
		Left  = 1 << 2, // UIInterfaceOrientationLandscapeLeft
		Right = 1 << 3,  // UIInterfaceOrientationLandscapeRight
		Portrait = Up | Down,
		Landscape = Right | Left,
		Both = Portrait | Landscape,
	}
	
	public static class OrientationUtil
	{
		//ignores invalid values
		public static Orientation Parse (PropertyList.PlistArray arr)
		{
			var o = Orientation.None;
			if (arr == null)
				return o;
			foreach (PropertyList.PlistString s in arr) {
				switch (s.Value) {
				case "UIInterfaceOrientationPortrait":
					o |= Orientation.Down;
					break;
				case "UIInterfaceOrientationPortraitUpsideDown":
					o |= Orientation.Up;
					break;
				case "UIInterfaceOrientationLandscapeLeft":
					o |= Orientation.Left;
					break;
				case "UIInterfaceOrientationLandscapeRight":
					o |= Orientation.Right;
					break;
				}
			}
			return o;
		}
		
		public static PropertyList.PlistArray ToPlist (Orientation o)
		{
			var arr = new PropertyList.PlistArray ();
			if ((o & Orientation.Up) != 0)
				arr.Add ("UIInterfaceOrientationPortrait");
			if ((o & Orientation.Down) != 0)
				arr.Add ("UIInterfaceOrientationPortraitUpsideDown");
			if ((o & Orientation.Left) != 0)
				arr.Add ("UIInterfaceOrientationLandscapeLeft");
			if ((o & Orientation.Right) != 0)
				arr.Add ("UIInterfaceOrientationLandscapeRight");
			return arr.Count == 0? null : arr;
		}
		
		public const string KEY = "UISupportedInterfaceOrientations";
		public const string KEY_IPAD = "UISupportedInterfaceOrientations~ipad";
		
		public static bool IsValidPair (Orientation val)
		{
			return val == Orientation.Both || val == Orientation.Landscape || val == Orientation.Portrait;
		}
	}
}

