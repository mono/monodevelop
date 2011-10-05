// 
// XCConfigurationList.cs
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
	class XCConfigurationList : XcodeObject
	{
		List<XCBuildConfiguration> configurations;

		public XCConfigurationList ()
		{
			configurations = new List<XCBuildConfiguration> ();
		}

		public void AddBuildConfiguration (XCBuildConfiguration configuration)
		{
			if (DefaultConfiguration == null)
				DefaultConfiguration = configuration;
			
			configurations.Add (configuration);
		}

		public XCBuildConfiguration DefaultConfiguration {
			get; set;
		}

		public override string Name {
			get {
				if (Target != null)
					return string.Format ("Build configuration list for {0} \"{1}\"", Target.Type, Target.Name);
				else
					return "Build configuration list";
			}
		}

		public XcodeObject Target {
			get; set;
		}

		public override XcodeType Type {
			get {
				return XcodeType.XCConfigurationList;
			}
		}

		public override string ToString ()
		{
			var sb = new StringBuilder ();

			sb.AppendFormat ("{0} /* {1} */ = {{\n", Token, Name);
			sb.AppendFormat ("\t\t\tisa = {0};\n", Type);
			sb.AppendFormat ("\t\t\tbuildConfigurations = (\n");
			foreach (XCBuildConfiguration config in configurations) 
				sb.AppendFormat ("\t\t\t\t{0} /* {1} */,\n", config.Token, config.Name);
			sb.AppendFormat ("\t\t\t);\n");
			sb.AppendFormat ("\t\t\tdefaultConfigurationIsVisible = 0;\n");
			if (DefaultConfiguration != null)
				sb.AppendFormat ("\t\t\tdefaultConfigurationName = {0};\n", DefaultConfiguration.Name);
			sb.AppendFormat ("\t\t}};");

			return sb.ToString ();
		}
	}
}
