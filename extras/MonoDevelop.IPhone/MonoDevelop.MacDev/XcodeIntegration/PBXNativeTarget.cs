// 
// PBXNativeTarget.cs
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
		}

		public override XcodeType Type {
			get {
				return XcodeType.PBXNativeTarget;
			}
		}

		public void AddBuildPhase (XcodeObject phase)
		{
			this.buildphases.Add (phase);
		}

		public override string ToString ()
		{
			var sb = new StringBuilder ();

			sb.AppendFormat ("{0} = {{\n\t\t\tisa = {1};\n\t\t\tbuildConfigurationList = {2};\n\t\t\tbuildPhases = (\n", Token, Type, configuration.Token);
			foreach (XcodeObject xco in buildphases)
				sb.AppendFormat ("\t\t\t\t{0},\n", xco.Token);
			sb.AppendFormat ("\t\t\t);\n\t\t\tbuildRules = ();\n\t\t\tdependencies = ();\n\t\t\tname = {0};\n\t\t\tproductName = {0};\n\t\t\tproductReference = {1};\n\t\t\tproductType = \"com.apple.product-type.application\";\n\t\t}};", name, target.Token);

			return sb.ToString ();
		}
	}
}
