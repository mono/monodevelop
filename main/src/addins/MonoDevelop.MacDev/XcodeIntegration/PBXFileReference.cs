// 
// PBXFileReference.cs
//  
// Authors:
//       Geoff Norton <gnorton@novell.com>
//       Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2011 Novell, Inc.
// Copyright (c) 2011 Xamarin Inc.
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
using System.Collections.Generic;

namespace MonoDevelop.MacDev.XcodeIntegration
{
	class PBXFileReference : XcodeObject
	{
		public string SourceTree { get; private set; }
		public string Path { get; private set; }
		
		public override string Name {
			get {
				string dir = System.IO.Path.GetDirectoryName (Path);
				
				if (dir.EndsWith (".lproj")) {
					int dot = dir.LastIndexOf ('.');
					return dir.Substring (0, dot);
				}
				
				return System.IO.Path.GetFileName (Path);
			}
		}

		public PBXFileReference (string path, string sourceTree)
		{
			SourceTree = sourceTree;
			Path = path;
		}

		public override XcodeType Type {
			get {
				return XcodeType.PBXFileReference;
			}
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ("");
			int dot = Path.LastIndexOf ('.');

			sb.AppendFormat ("{0} /* {1} */ = {{isa = {2}; ", Token, Name, Type);

			if (dot > 0) {
				switch (Path.Substring (dot)) {
				case ".framework": sb.AppendFormat ("lastKnownFileType = wrapper.framework; name = {0}; ", Name); break;
				case ".app": sb.Append ("explicitFileType = wrapper.application; includeInIndex = 0; "); break;
				case ".storyboard": sb.Append ("lastKnownFileType = file.storyboard; "); break;
				case ".strings": sb.Append ("lastKnownFileType = text.plist.xml; "); break;
				case ".plist": sb.Append ("lastKnownFileType = text.plist.xml; "); break;
				case ".m": sb.Append ("lastKnownFileType = sourcecode.c.objc; "); break;
				case ".h": sb.Append ("lastKnownFileType = sourcecode.c.h; "); break;
				case ".zip": sb.Append ("lastKnownFileType = archive.zip; "); break;
				case ".gz": sb.Append ("lastKnownFileType = archive.gzip; "); break;
				case ".jpeg": sb.Append ("lastKnownFileType = image.jpeg; "); break;
				case ".jpg": sb.Append ("lastKnownFileType = image.jpeg; "); break;
				case ".png": sb.Append ("lastKnownFileType = image.png; "); break;
				case ".pdf": sb.Append ("lastKnownFileType = image.pdf; "); break;
				}
			}

			sb.AppendFormat ("name = \"{0}\"; path = \"{1}\"; sourceTree = {2}; }};", Name, Path, SourceTree);

			return sb.ToString ();
		}
	}
}
