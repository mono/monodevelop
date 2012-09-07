// 
// SystemAssembly.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Reflection;

namespace MonoDevelop.Core.Assemblies
{
	public class SystemAssembly
	{
		AssemblyName aname;

		public string FullName { get; internal set; }
		public string Location { get; private set; }

		public AssemblyName AssemblyName {
			get {
				if (aname == null)
					aname = AssemblyContext.ParseAssemblyName (FullName);
				return aname;
			}
		}
		
		public SystemPackage Package { get; internal set; }
		
		internal SystemAssembly NextSameName;
		internal SystemAssembly NextSamePackage;
		
		public SystemAssembly (string file, string name)
		{
			FullName = name;
			Location = file;
		}
		
		internal static SystemAssembly FromFile (string file)
		{
			return new SystemAssembly (file, SystemAssemblyService.GetAssemblyName (file));
		}
		
		internal static SystemAssembly FromFile (string file, AssemblyInfo ainfo)
		{
			if (ainfo == null || ainfo.Version == null)
				return FromFile (file);
			string token = (string.IsNullOrEmpty (ainfo.PublicKeyToken) || ainfo.PublicKeyToken == "null")?
				String.Empty : ", PublicKeyToken=" + ainfo.PublicKeyToken;
			string fn = ainfo.Name + ", Version=" + ainfo.Version +", Culture=neutral" + token;
			return new SystemAssembly (file, fn);
		}
		
		public string Name {
			get {
				int i = FullName.IndexOf (',');
				if (i != -1)
					return FullName.Substring (0, i).Trim ();
				else
					return FullName;
			}
		}
		
		public string Version {
			get {
				int i = FullName.IndexOf ("Version=");
				if (i == -1)
					return string.Empty;
				i += 8;
				int j = FullName.IndexOf (',', i);
				if (j == -1)
					j = FullName.Length;
				return FullName.Substring (i, j - i);
			}
		}
		
		internal IEnumerable<SystemAssembly> AllSameName ()
		{
			SystemAssembly asm = this;
			do {
				yield return asm;
				asm = asm.NextSameName;
			} while (asm != null);
		}
	}
}
