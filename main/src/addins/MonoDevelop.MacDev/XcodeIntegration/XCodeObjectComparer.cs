// 
// XCodeObjectComparer.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright (c) 2011 Xamarin 2011
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
using System.Collections.Generic;

namespace MonoDevelop.MacDev.XcodeIntegration {
	public enum XcodeObjectSortDirection {
		Descending = -1,
		None       = 0,
		Ascending  = 1,
	};
	
	public class XcodeObjectComparer : IComparer<XcodeObject> {
		int sign;
		
		public XcodeObjectComparer (XcodeObjectSortDirection direction = XcodeObjectSortDirection.Ascending)
		{
			sign = (int) direction;
		}
		
		public int Compare (XcodeObject x, XcodeObject y)
		{
			if (x == null)
				return y == null ? 0 : -1;
			if (y == null)
				return 1;
			if (x.GetType () != y.GetType ())
				return x.GetType ().Name.CompareTo (y.GetType ().Name) * sign;
			
			if (x is PBXFileReference) {
				var left = (PBXFileReference) x;
				var right = (PBXFileReference) y;
				return left.Path.CompareTo (right.Path) * sign;
			}
			
			if (x is PBXGroup) {
				var left = (PBXGroup) x;
				var right = (PBXGroup) y;
				return left.Name.CompareTo (right.Name) * sign;
			}
			
			return 0;
		}
	}
}

