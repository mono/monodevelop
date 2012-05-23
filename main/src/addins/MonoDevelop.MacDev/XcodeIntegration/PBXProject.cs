// 
// PBXProject.cs
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
	class PBXProject : XcodeObject
	{
		XCConfigurationList configuration;
		PBXGroup productGroup, mainGroup;
		List<PBXNativeTarget> targets;
		string name;

		public PBXProject (string name, XCConfigurationList configuration, PBXGroup mainGroup, PBXGroup productGroup)
		{
			this.KnownRegions = new HashSet<string> ();
			this.targets = new List<PBXNativeTarget> ();
			this.configuration = configuration;
			this.productGroup = productGroup;
			this.mainGroup = mainGroup;
			this.name = name;

			configuration.Target = this;
			
			KnownRegions.Add ("en");
		}

		public void AddNativeTarget (PBXNativeTarget target)
		{
			targets.Add (target);
		}
		
		public HashSet<string> KnownRegions {
			get; private set;
		}

		public override string Name {
			get { return name; }
		}

		public override XcodeType Type {
			get {
				return XcodeType.PBXProject;
			}
		}

		public override string ToString ()
		{
			var sb = new StringBuilder ();
			sb.AppendFormat ("{0} /* Project object */ = {{\n", Token);
			sb.AppendFormat ("\t\t\tisa = {0};\n", Type);
			sb.AppendFormat ("\t\t\tattributes = {{ }};\n");
			sb.AppendFormat ("\t\t\tbuildConfigurationList = {0} /* {1} */;\n",
					 configuration.Token, configuration.Name);
			sb.AppendFormat ("\t\t\tcompatibilityVersion = \"Xcode 3.2\";\n");
			sb.AppendFormat ("\t\t\thasScannedForEncodings = 0;\n");
			sb.AppendFormat ("\t\t\tknownRegions = (\n");
			foreach (var lang in KnownRegions)
				sb.AppendFormat ("\t\t\t\t{0},\n", lang);
			sb.AppendFormat ("\t\t\t);\n");
			sb.AppendFormat ("\t\t\tmainGroup = {0};\n", mainGroup.Token);
			sb.AppendFormat ("\t\t\tproductRefGroup = {0} /* {1} */;\n", productGroup.Token, productGroup.Name);
			sb.AppendFormat ("\t\t\tprojectDirPath = \"\";\n");
			sb.AppendFormat ("\t\t\tprojectRoot = \"\";\n");
			sb.AppendFormat ("\t\t\ttargets = (\n");
			foreach (PBXNativeTarget target in targets) 
				sb.AppendFormat ("\t\t\t\t{0} /* {1} */,\n", target.Token, target.Name);
			sb.AppendFormat ("\t\t\t);\n");
			sb.AppendFormat ("\t\t}};");
			return sb.ToString ();
		}
	}
}
