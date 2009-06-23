// 
// Switch.cs
//  
// Author:
//       Jérémie "Garuma" Laval <jeremie.laval@gmail.com>
// 
// Copyright (c) 2009 Jérémie "Garuma" Laval
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Deployment;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Autotools
{
	[Serializable]
	public sealed class Switch
	{
		[ItemProperty]
		string switchName;
		[ItemProperty]
		string define;
		[ItemProperty]
		string helpStr;
		
		public string Define {
			get {
				return define;
			}
		}
		
		public string HelpStr {
			get {
				return helpStr;
			}
		}
		
		public string SwitchName {
			get {
				return switchName;
			}
		}
		
		public Switch (string switchName,
		               string define,
		               string helpStr)
		{
			this.switchName = EspaceSwitchName (switchName);
			this.define = EscapeSwitchDefine (define);
			this.helpStr = helpStr;
		}
		
		public Switch ()
		{	
		}
		
		public static string EspaceSwitchName (string name)
		{
			StringBuilder sb = new StringBuilder (name.Length);
			foreach (char c in name) {
				if (char.IsLetter (c))
					sb.Append (char.ToLowerInvariant (c));
				else if (char.IsSeparator (c) || char.IsPunctuation (c))
					sb.Append ('-');
			}
			
			return sb.ToString ();
		}
		
		public static string EscapeSwitchDefine (string def)
		{
			StringBuilder sb = new StringBuilder (def.Length);
			foreach (char c in def) {
				if (char.IsLetter (c))
					sb.Append (char.ToUpperInvariant (c));
				else if (char.IsSeparator (c) || char.IsPunctuation (c))
					sb.Append ('_');
			}
			
			return sb.ToString ();
		}
	}
}
