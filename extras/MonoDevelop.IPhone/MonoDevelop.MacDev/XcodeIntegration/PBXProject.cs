// 
// PBXProject.cs
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
	class PBXProject : XcodeObject
	{
		XCConfigurationList configuration;
		PBXGroup group;
		public List<PBXNativeTarget> targets;

		public PBXProject (XCConfigurationList configuration, PBXGroup group)
		{
			this.configuration = configuration;
			this.group = group;
			this.targets = new List<PBXNativeTarget> ();
		}

		public void AddNativeTarget (PBXNativeTarget target)
		{
			this.targets.Add (target);
		}

		public override XcodeType Type {
			get {
				return XcodeType.PBXProject;
			}
		}

		public override string ToString ()
		{
			var sb = new StringBuilder ();

			sb.AppendFormat ("{0} = {{\n\t\t\tisa = {1};\n\t\t\ttargets = (\n", Token, Type);
			foreach (PBXNativeTarget target in targets) 
				sb.AppendFormat ("\t\t\t\t{0},\n", target.Token);
			sb.AppendFormat ("\t\t\t);\n\t\t\tbuildConfigurationList = {0};\n\t\t\tcompatibilityVersion = \"Xcode 3.1\";\n\t\t\thasScannedForEncodings = 1;\n\t\t\tproductRefGroup = {1};\n\t\t\tmainGroup = {1};\n\t\t\tprojectDirPath = \"\";\n\t\t\tprojectRoot = \"\";\n\t\t}};", configuration.Token, group.Token);
		
			return sb.ToString ();
		}
	}
}
