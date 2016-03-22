//
// MimeMapLoader.cs
//
// Author:
//   Geoff Norton  <gnorton@novell.com>
//   Michael Hutchinson <m.j.hutchinson@gmail.com
//   Matt Ward <matt.ward@xamarin.com>
//
// Copyright (C) 2007-2011 Novell, Inc (http://www.novell.com)
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Text.RegularExpressions;

namespace MonoDevelop.MacIntegration
{
	class MimeMapLoader
	{
		static char [] splitChars = new char [] { ' ' };
		Dictionary<string, string> map;

		public MimeMapLoader (Dictionary<string, string> map)
		{
			this.map = map;
		}

		public void LoadMimeMap (string fileName)
		{
			using (var file = File.OpenRead (fileName)) {
				using (var reader = new StreamReader (file)) {
					LoadMimeMap (reader);
				}
			}
		}

		public void LoadMimeMap (TextReader reader)
		{
			var mime = new Regex ("([a-zA-Z]+/[a-zA-z0-9+-_.]+)\t+([a-zA-Z0-9 ]+)", RegexOptions.Compiled);
			string line;
			while ((line = reader.ReadLine ()) != null) {
				Match m = mime.Match (line);
				if (m.Success) {
					string extensions = m.Groups [2].Captures [0].Value;
					foreach (string extension in extensions.Split (splitChars, StringSplitOptions.RemoveEmptyEntries)) {
						map ["." + extension] = m.Groups [1].Captures [0].Value;
					}
				}
			}
		}
	}
}

