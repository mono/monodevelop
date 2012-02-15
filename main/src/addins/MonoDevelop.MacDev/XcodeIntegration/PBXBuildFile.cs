// 
// PBXBuildFile.cs
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
using System.Collections.Generic;

namespace MonoDevelop.MacDev.XcodeIntegration
{
	class PBXBuildFile : XcodeObject
	{
		public PBXBuildFile (PBXFileReference fileRef)
		{
			FileReference = fileRef;
		}
		
		public PBXBuildFile (PBXVariantGroup fileRef)
		{
			FileReference = fileRef;
		}

		public XcodeObject BuildPhase {
			get; set;
		}
		
		public XcodeObject FileReference {
			get; private set;
		}

		public override string Name {
			get { return FileReference.Name; }
		}

		public override XcodeType Type {
			get {
				return XcodeType.PBXBuildFile;
			}
		}

		public override string ToString ()
		{
			if (BuildPhase != null)
				return string.Format ("{0} /* {1} in {2} */ = {{isa = {3}; fileRef = {4} /* {1} */; }};", Token, FileReference.Name, BuildPhase.Name, Type, FileReference.Token);
			else
				return string.Format ("{0} /* {1} */ = {{isa = {2}; fileRef = {3} /* {1} */; }};", Token, FileReference.Name, Type, FileReference.Token);
		}
	}
}
