// 
// LinuxSystemInformation.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright (c) 2011, Xamarin Inc
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
using System.IO;
using System.Text;

namespace MonoDevelop.Core
{
	class LinuxSystemInformation : UnixSystemInformation
	{
		internal override void AppendOperatingSystem (StringBuilder sb)
		{
			string OSName = "", OSVersion = "";
			try {
				foreach (var line in File.ReadAllLines ("/etc/os-release")) {
					var parsedline = Parse (line);
					if (parsedline.Key.Equals ("NAME")) {
						OSName = parsedline.Value;
					}
					if (parsedline.Key.Equals ("VERSION")) {
						OSVersion = parsedline.Value;
					}
				}
			} catch {
				OSName = "Linux";
				OSVersion = "Unknown";
			}
			if (string.IsNullOrWhiteSpace (OSName) || string.IsNullOrWhiteSpace (OSVersion)) {
				OSName = "Linux";
				OSVersion = "Unknown";
			}
			sb.AppendLine ("\t" + OSName + " " + OSVersion);
			base.AppendOperatingSystem (sb);
		}

		KeyValuePair<string,string> Parse (string inputstring)
		{
			string [] parsed = inputstring.Split ('=');
			if (parsed.Length != 2) {
				return new KeyValuePair<string, string> ();
			}
			return new KeyValuePair<string, string> (parsed [0], parsed [1].Trim ('"'));
		}
	}
}

