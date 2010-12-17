// 
// AvdWatcher.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core;
using System.IO;
namespace MonoDevelop.MonoDroid
{
	public class AndroidVirtualDevice
	{
		public AndroidVirtualDevice (string name, FilePath path, string target)
		{
			this.Name = name;
			this.Path = path;
			this.Target = target;
		}
		
		public string Name { get; private set; }
		public FilePath Path { get; private set; }
		public string Target { get; private set; }
		
		public Dictionary<string,string> ReadConfig ()
		{
			return ReadIni (this.Path.Combine ("config.ini"));
		}
		
		public static AndroidVirtualDevice Load (FilePath avdIni)
		{
			var ini = ReadIni (avdIni);
			return new AndroidVirtualDevice (avdIni.FileNameWithoutExtension, ini["path"], ini["target"]);
		}
		
		static Dictionary<string,string> ReadIni (string filename)
		{
			var dict = new Dictionary<string,string> ();
			var lines = File.ReadAllLines (filename);
			foreach (var l in lines) {
				var i = l.IndexOf ('=');
				if (i <= 0)
					continue;
				string key = l.Substring (0, i);
				string val = null;
				if (i + 1 < l.Length)
					val = l.Substring (i + 1); 
				dict.Add (key, val);
			}
			return dict;
		}
	}
}
