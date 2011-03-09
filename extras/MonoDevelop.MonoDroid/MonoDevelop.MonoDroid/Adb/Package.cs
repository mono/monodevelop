// 
// Package.cs
//  
// Author:
//       Jonathan Pobst (monkey@jpobst.com)
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

// COPIED FROM ANDROIDVS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MonoDevelop.MonoDroid
{
	public class InstalledPackage
	{
		private int version = -1;

		public string Name { get; set; }
		public string ApkFile { get; set; }

		public InstalledPackage ()
		{
		}

		public InstalledPackage (string value)
		{
			if (value.StartsWith ("package:"))
				value = value.Substring ("package:".Length);

			var pieces = value.Split ('=');

			ApkFile = pieces[0];
			Name = pieces[1];
		}

		public InstalledPackage (string name, string apkfile)
		{
			Name = name;
			ApkFile = apkfile;
		}

		public InstalledPackage (string name, string apkfile, int version)
		{
			Name = name;
			ApkFile = apkfile;
			this.version = version;
		}

		public string ApkFileWithoutVersion {
			get { return Path.GetFileNameWithoutExtension (ApkFile).Split ('-')[0]; }
		}

		public int Version {
			get {
				if (version != -1)
					return version;

				return int.MaxValue;
			}
		}

		public override string ToString ()
		{
			return string.Format ("{0} [{1}]", ApkFileWithoutVersion, Version);
		}
	}
}
