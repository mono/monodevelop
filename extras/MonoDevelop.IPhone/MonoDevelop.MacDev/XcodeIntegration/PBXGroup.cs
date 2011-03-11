// 
// PBXGroup.cs
//  
// Author:
//       Geoff Norton <gnorton@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
	class PBXGroup : XcodeObject
	{
		string name;
		List<PBXFileReference> children;

		public PBXGroup (string name)
		{
			this.name = name;
			this.children = new List<PBXFileReference> ();
		}

		public void AddChild (PBXFileReference file)
		{
			this.children.Add (file);
		}

		public override XcodeType Type {
			get {
				return XcodeType.PBXGroup;
			}
		}

		public override string ToString ()
		{
			var sb = new StringBuilder ();

			sb.AppendFormat ("{0} = {{\n\t\t\tisa = {1};\n\t\t\tchildren = (\n", Token, Type);
			foreach (PBXFileReference file in children) 
				sb.AppendFormat ("\t\t\t\t{0},\n", file.Token);
			sb.AppendFormat ("\t\t\t);\n\t\t\tname = {0};\n\t\t\tsourceTree = \"<group>\";\n\t\t}};", name);
		
			return sb.ToString ();
		}
	}
}
