//
// FileUtil.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using System.IO;

namespace MonoDevelop.RazorGenerator
{
	static class FileUtil
	{
		//from MonoDevelop.Core.FileService, copied here so RazorGenerator can be used w/o MD dependency
		public unsafe static string AbsoluteToRelativePath (string baseDirectoryPath, string absPath)
		{
			if (!Path.IsPathRooted (absPath) || string.IsNullOrEmpty (baseDirectoryPath))
				return absPath;

			absPath = GetFullPath (absPath);
			baseDirectoryPath = GetFullPath (baseDirectoryPath).TrimEnd (Path.DirectorySeparatorChar);

			fixed (char* bPtr = baseDirectoryPath, aPtr = absPath) {
				var bEnd = bPtr + baseDirectoryPath.Length;
				var aEnd = aPtr + absPath.Length;
				char* lastStartA = aEnd;
				char* lastStartB = bEnd;

				int indx = 0;
				// search common base path
				var a = aPtr;
				var b = bPtr;
				while (a < aEnd) {
					if (*a != *b)
						break;
					if (IsSeparator (*a)) {
						indx++;
						lastStartA = a + 1;
						lastStartB = b; 
					}
					a++;
					b++;
					if (b >= bEnd) {
						if (a >= aEnd || IsSeparator (*a)) {
							indx++;
							lastStartA = a + 1;
							lastStartB = b;
						}
						break;
					}
				}
				if (indx == 0) 
					return absPath;

				if (lastStartA >= aEnd)
					return ".";

				// handle case a: some/path b: some/path/deeper...
				if (a >= aEnd) {
					if (IsSeparator (*b)) {
						lastStartA = a + 1;
						lastStartB = b;
					}
				}

				// look how many levels to go up into the base path
				int goUpCount = 0;
				while (lastStartB < bEnd) {
					if (IsSeparator (*lastStartB))
						goUpCount++;
					lastStartB++;
				}
				var size = goUpCount * 2 + goUpCount + aEnd - lastStartA;
				var result = new char [size];
				fixed (char* rPtr = result) {
					// go paths up
					var r = rPtr;
					for (int i = 0; i < goUpCount; i++) {
						*(r++) = '.';
						*(r++) = '.';
						*(r++) = Path.DirectorySeparatorChar;
					}
					// copy the remaining absulute path
					while (lastStartA < aEnd)
						*(r++) = *(lastStartA++);
				}
				return new string (result);
			}
		}

		static bool IsSeparator (char ch)
		{
			return ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar || ch == Path.VolumeSeparatorChar;
		}

		static string GetFullPath (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");
			if (!isWindows || path.IndexOf ('*') == -1)
				return Path.GetFullPath (path);
			else {
				// On Windows, GetFullPath doesn't work if the path contains wildcards.
				path = path.Replace ("*", wildcardMarker);
				path = Path.GetFullPath (path);
				return path.Replace (wildcardMarker, "*");
			}
		}

		static readonly string wildcardMarker = "_" + Guid.NewGuid () + "_";
		static readonly bool isWindows = Path.DirectorySeparatorChar == '\\';
	}
}

