// ProductInformationProvider.cs
//
// Author:
//       jason <jaimison@microsoft.com>
//
// Copyright (c) 2018 
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
using MonoDevelop.Core;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MonoDevelop.Ide
{
	class RuntimeVersionInfo : ProductInformationProvider
	{
		public override string Title => "Mono Framework MDK";

		public override string Version => GetMonoVersionNumber ();

		public override string ApplicationId => "964ebddd-1ffe-47e7-8128-5ce17ffffb05";

		protected override FilePath UpdateInfoFile => MonoUpdateInfoFile;

		public override string Description {
			get {
				var sb = new System.Text.StringBuilder ();
				sb.AppendLine ("Runtime:");
				sb.Append ("\t");
				sb.Append (GetRuntimeInfo ());
				sb.AppendLine ();
				if (Platform.IsMac && IsMono ()) {
					var pkgVer = GetMonoUpdateInfo ();
					if (!string.IsNullOrEmpty (pkgVer)) {
						sb.Append ("\tPackage version: ");
						sb.Append (pkgVer);
					}
				}
				return sb.ToString ();
			}
		}

		const string MonoUpdateInfoFile = "/Library/Frameworks/Mono.framework/Versions/Current/updateinfo";
		static string GetMonoUpdateInfo ()
		{
			try {
				FilePath mscorlib = typeof (object).Assembly.Location;
				if (!System.IO.File.Exists (MonoUpdateInfoFile))
					return null;
				var s = System.IO.File.ReadAllText (MonoUpdateInfoFile).Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				return s [s.Length - 1].Trim ();
			} catch {
			}
			return null;
		}

		static string GetMonoDisplayName ()
		{
			var t = Type.GetType ("Mono.Runtime");
			if (t == null)
				return "unknown";
			var mi = t.GetMethod ("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
			if (mi == null) {
				LoggingService.LogError ("No Mono.Runtime.GetDisplayName method found.");
				return "error";
			}
			return (string)mi.Invoke (null, null);
		}

		static string GetMonoVersionNumber ()
		{
			var monoDisplayName = GetMonoDisplayName ();
			// Convert from "5.14.0.177 (2018 - 04 / f3a2216b65a Fri Aug  3 09:28:16 EDT 2018)"
			// to "5.4.0.177"
			return Regex.Match (monoDisplayName, @"^[\d\.]+", RegexOptions.Compiled).Value;
		}

		static bool IsMono ()
		{
			return Type.GetType ("Mono.Runtime") != null;
		}

		public static string GetRuntimeInfo ()
		{
			string val;
			if (IsMono ()) {
				val = "Mono " + GetMonoDisplayName ();
			} else {
				val = "Microsoft .NET " + Environment.Version;
			}

			if (IntPtr.Size == 8)
				val += (" (64-bit)");

			return val;
		}
	}
}

