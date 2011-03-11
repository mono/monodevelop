// 
// XCBuildConfiguration.cs
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
	class XCBuildConfiguration : XcodeObject
	{
		Dictionary<string, string> settings;
		string name;

		public XCBuildConfiguration (string name)
		{
			this.name = name;
			this.settings = new Dictionary<string, string> ();
		}

		public void AddSetting (string key, string val)
		{
			this.settings.Add (key, val);
		}

		public override XcodeType Type {
			get {
				return XcodeType.XCBuildConfiguration;
			}
		}

		public override string ToString ()
		{
			var sb = new StringBuilder ();

			sb.AppendFormat ("{0} = {{\n\t\t\tisa = {1};\n\t\t\tbuildSettings = {{\n", Token, Type);
			foreach (KeyValuePair <string,string> kvp in settings) 
				sb.AppendFormat ("\t\t\t\t{0} = {1};\n", kvp.Key, kvp.Value);
			sb.AppendFormat ("\t\t\t}};\n\t\t\tname = {0};\n\t\t}};", name);
		
			return sb.ToString ();
		}
	}
}
