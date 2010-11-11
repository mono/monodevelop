// 
// IPhoneFrameworkBackend.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Ide;
using Gtk;
using MonoDevelop.Core.Serialization;
namespace MonoDevelop.IPhone
{
	public struct IPhoneSdkVersion : IComparable<IPhoneSdkVersion>, IEquatable<IPhoneSdkVersion>
	{
		public static IPhoneSdkVersion GetDefault ()
		{
			var v = IPhoneFramework.InstalledSdkVersions;
			return v.Count > 0? v[v.Count - 1] : UseDefault;
		}
		
		int[] version;
		
		public IPhoneSdkVersion (params int[] version)
		{
			if (version == null)
				throw new ArgumentNullException ();
			this.version = version;
		}
		
		public static IPhoneSdkVersion Parse (string s)
		{
			var vstr = s.Split ('.');
			var vint = new int[vstr.Length];
			for (int j = 0; j < vstr.Length; j++)
				vint[j] = int.Parse (vstr[j]);
			return new IPhoneSdkVersion (vint);
		}
		
		public int[] Version { get { return version; } }
		
		public override string ToString ()
		{
			if (IsUseDefault)
				return "";
			string[] v = new string [version.Length];
			for (int i = 0; i < v.Length; i++)
				v[i] = version[i].ToString ();
			return string.Join (".", v);
		}
		
		public int CompareTo (IPhoneSdkVersion other)
		{
			var x = this.Version;
			var y = other.Version;
			if (ReferenceEquals (x, y))
				return 0;
			
			if (x == null)
				return -1;
			if (y == null)
				return 1;
			
			for (int i = 0; i < Math.Min (x.Length,y.Length); i++) {
				int res = x[i] - y[i];
				if (res != 0)
					return res;
			}
			return x.Length - y.Length;
		}
		
		public bool Equals (IPhoneSdkVersion other)
		{
			var x = this.Version;
			var y = other.Version;
			if (ReferenceEquals (x, y))
				return true;
			if (x == null || y == null || x.Length != y.Length)
				return false;
			for (int i = 0; i < x.Length; i++)
				if (x[i] != y[i])
					return false;
			return true;
		}
		
		public override int GetHashCode ()
		{
			unchecked {
				var x = this.Version;
				int acc = 0;
				for (int i = 0; i < x.Length; i++)
					acc ^= x[i] << i;
				return acc;
			}
		}
		
		public bool IsUseDefault {
			get {
				return version == null || version.Length == 0;
			}
		}
		
		public IPhoneSdkVersion ResolveIfDefault ()
		{
			if (IsUseDefault)
				return GetDefault ();
			else
				return this;
		}
		
		public static readonly IPhoneSdkVersion UseDefault = new IPhoneSdkVersion (new int[0]);
		
		public static readonly IPhoneSdkVersion V3_0 = new IPhoneSdkVersion (3, 0);
		public static readonly IPhoneSdkVersion V3_2 = new IPhoneSdkVersion (3, 2);
		public static readonly IPhoneSdkVersion V4_0 = new IPhoneSdkVersion (4, 0);
		public static readonly IPhoneSdkVersion V4_1 = new IPhoneSdkVersion (4, 1);
		public static readonly IPhoneSdkVersion V4_2 = new IPhoneSdkVersion (4, 2);
	}
}
