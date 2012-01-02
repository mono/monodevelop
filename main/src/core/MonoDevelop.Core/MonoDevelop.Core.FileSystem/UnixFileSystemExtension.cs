// UnixFileSystemExtension.cs
//
// Author:
//   Alan McGovern <alan@xamarin.com>
//
// Copyright (c) 2011 Xamarin, Inc (http://www.xamarin.com)
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
//
//

using System;
using System.Runtime.InteropServices;

namespace MonoDevelop.Core.FileSystem
{
	class UnixFileSystemExtension : DefaultFileSystemExtension
	{
		const int PATHMAX = 4096 + 1;
		
		[DllImport ("libc")]
		static extern IntPtr realpath (string path, IntPtr buffer);
		
		public override FilePath ResolveFullPath (FilePath path)
		{
			IntPtr buffer = IntPtr.Zero;
			try {
				buffer = Marshal.AllocHGlobal (PATHMAX);
				var result = realpath (path, buffer);
				return result == IntPtr.Zero ? "" : Marshal.PtrToStringAuto (buffer);
			} finally {
				if (buffer != IntPtr.Zero)
					Marshal.FreeHGlobal (buffer);
			}
		}
	}
}

