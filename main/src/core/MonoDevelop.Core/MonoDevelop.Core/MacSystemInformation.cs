// 
// MacSystemInformation.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright (c) 2011, Xamarin Inc
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
using System.Text;

namespace MonoDevelop.Core
{
	public class MacSystemInformation : UnixSystemInformation
	{
		public static readonly Version MountainLion = new Version (10, 8);
		public static readonly Version Lion = new Version (10, 7);
		public static readonly Version SnowLeopard = new Version (10, 6);
		static Version version;

		[System.Runtime.InteropServices.DllImport ("/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon")]
		static extern int Gestalt (int selector, out int result);
		
		//TODO: there are other gestalt selectors that return info we might want to display
		//mac API for obtaining info about the system
		static int Gestalt (string selector)
		{
			System.Diagnostics.Debug.Assert (selector != null && selector.Length == 4);
			int cc = selector[3] | (selector[2] << 8) | (selector[1] << 16) | (selector[0] << 24);
			int result;
			int ret = Gestalt (cc, out result);
			if (ret != 0)
				throw new Exception (string.Format ("Error reading gestalt for selector '{0}': {1}", selector, ret));
			return result;
		}

		static MacSystemInformation ()
		{
			version = new Version (Gestalt ("sys1"), Gestalt ("sys2"), Gestalt ("sys3"));
		}

		public static Version OsVersion {
			get { return version; }
		}
		
		internal override void AppendOperatingSystem (StringBuilder sb)
		{
			sb.AppendFormat ("Mac OS X {0}", version);
			sb.AppendLine ();
			
			base.AppendOperatingSystem (sb);
		}
	}
}
