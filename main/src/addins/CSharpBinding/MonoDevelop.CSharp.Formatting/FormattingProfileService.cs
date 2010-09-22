// 
// FormattingProfileService.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.Formatting
{
	public static class FormattingProfileService
	{
		static List<CSharpFormattingPolicy> profiles = new List<CSharpFormattingPolicy> ();
		
		
		static string ProfilePath {
			get {
				return Path.Combine (PropertyService.ConfigPath, Path.Combine ("format", "csharp"));
			}
		}
		
		static void LoadBuiltInProfiles ()
		{
			var asm = typeof (FormattingProfileService).Assembly;
			foreach (string str in asm.GetManifestResourceNames ()) {
				if (str.EndsWith ("FormattingProfiles.xml")) {
					var p = CSharpFormattingPolicy.Load (asm.GetManifestResourceStream (str));
					p.IsBuiltIn = true;
					profiles.Add (p);
				}
			}
		}
		
		static void LoadCustomProfiles ()
		{
			if (Directory.Exists (ProfilePath)) {
				foreach (string file in Directory.GetFiles (ProfilePath, "*.xml")) {
					profiles.Add (CSharpFormattingPolicy.Load (file));
				}
			}
		}
		
		static FormattingProfileService ()
		{
			LoadBuiltInProfiles ();
			LoadCustomProfiles ();
		}
		
		public static List<CSharpFormattingPolicy> Profiles {
			get {
				return profiles;
			}
		}
		
		public static CSharpFormattingPolicy GetProfile (string name)
		{
			return profiles.FirstOrDefault (p => p.Name == name);
		}
		
		public static void AddProfile (CSharpFormattingPolicy profile)
		{
			profiles.Add (profile);
			if (!Directory.Exists (ProfilePath))
				Directory.CreateDirectory (ProfilePath);
			string fileName = Path.Combine (ProfilePath, profile.Name + ".xml");
			profile.Save (fileName);
		}
		
		public static void Remove (CSharpFormattingPolicy profile)
		{
			if (!profiles.Contains (profile))
				return;
			profiles.Remove (profile);
			try {
				string fileName = Path.Combine (ProfilePath, profile.Name + ".xml");
				if (File.Exists (fileName)) 
					File.Delete (fileName);
			} catch (Exception e) {
				
			}
		}
	}
}
