// 
// PBXNativeTarget.cs
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
	class PBXNativeTarget : XcodeObject
	{
		XCConfigurationList configuration;
		List<XcodeObject> buildphases;
		string name;
		PBXFileReference target;

		public PBXNativeTarget (string name, XCConfigurationList configuration, PBXFileReference target)
		{
			this.name = name;
			this.configuration = configuration;
			this.target = target;
			this.buildphases = new List<XcodeObject> ();

			configuration.Target = this;
		}

		public override string Name {
			get { return name; }
		}

		public override XcodeType Type {
			get {
				return XcodeType.PBXNativeTarget;
			}
		}

		public void AddBuildPhase (XcodeObject phase)
		{
			buildphases.Add (phase);
		}

		public override string ToString ()
		{
			var sb = new StringBuilder ();

			sb.AppendFormat ("{0} /* {1} */ = {{\n", Token, name);
			sb.AppendFormat ("\t\t\tisa = {0};\n", Type);
			sb.AppendFormat ("\t\t\tbuildConfigurationList = {0} /* {1} */;\n", configuration.Token, configuration.Name);
			sb.AppendFormat ("\t\t\tbuildPhases = (\n");
			foreach (XcodeObject xco in buildphases)
				sb.AppendFormat ("\t\t\t\t{0} /* {1} */,\n", xco.Token, xco.Name);
			sb.AppendFormat ("\t\t\t);\n");
			sb.AppendFormat ("\t\t\tbuildRules = (\n");
			sb.AppendFormat ("\t\t\t);\n");
			sb.AppendFormat ("\t\t\tdependencies = (\n");
			sb.AppendFormat ("\t\t\t);\n");
			sb.AppendFormat ("\t\t\tname = \"{0}\";\n", name);
			sb.AppendFormat ("\t\t\tproductName = \"{0}\";\n", name);
			sb.AppendFormat ("\t\t\tproductReference = {0} /* {1} */;\n", target.Token, target.Name);
			sb.AppendFormat ("\t\t\tproductType = \"com.apple.product-type.application\";\n");
			sb.AppendFormat ("\t\t}};");

			return sb.ToString ();
		}
	}
}
