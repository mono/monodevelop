// 
// SupportedFramework.cs
//  
// Author: Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using System.IO;
using System.Xml;

namespace MonoDevelop.Core.Assemblies
{
	public class SupportedFramework
	{
		public static readonly Version NoMaximumVersion = new Version (int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);
		public static readonly Version NoMinumumVersion = new Version (0, 0, 0, 0);

		public SupportedFramework (TargetFramework target, string identifier, string display, string profile, Version minVersion, string minDisplayVersion)
		{
			MinimumVersionDisplayName = minDisplayVersion;
			MinimumVersion = minVersion;
			MaximumVersion = NoMaximumVersion;
			DisplayName = display;
			Identifier = identifier;
			Profile = profile;
			
			TargetFramework = target;
		}

		internal SupportedFramework (TargetFramework target)
		{
			MinimumVersionDisplayName = string.Empty;
			MinimumVersion = NoMinumumVersion;
			MaximumVersion = NoMaximumVersion;
			DisplayName = string.Empty;
			Identifier = string.Empty;
			Profile = string.Empty;
			
			TargetFramework = target;
		}
		
		public string DisplayName {
			get; internal set;
		}
		
		public string Identifier {
			get; internal set;
		}
		
		public string Profile {
			get; internal set;
		}
		
		public string MinimumVersionDisplayName {
			get; internal set;
		}
		
		public Version MinimumVersion {
			get; internal set;
		}
		
		public Version MaximumVersion {
			get; internal set;
		}
		
		public TargetFramework TargetFramework {
			get; private set;
		}
		
		static Version ParseVersion (string version, Version wildcard)
		{
			if (version == "*")
				return wildcard;
			
			return Version.Parse (version);
		}
		
		internal static SupportedFramework Load (TargetFramework target, FilePath path)
		{
			SupportedFramework fx = new SupportedFramework (target);

			fx.DisplayName = path.FileNameWithoutExtension;
			
			using (var reader = XmlReader.Create (path)) {
				if (!reader.ReadToDescendant ("Framework"))
					throw new Exception ("Missing Framework element");
				
				if (!reader.HasAttributes)
					throw new Exception ("Framework element does not contain any attributes");
				
				while (reader.MoveToNextAttribute ()) {
					switch (reader.Name) {
					case "MaximumVersion":
						fx.MaximumVersion = ParseVersion (reader.Value, NoMaximumVersion);
						break;
					case "MinimumVersion":
						fx.MinimumVersion = ParseVersion (reader.Value, NoMinumumVersion);
						break;
					case "Profile":
						fx.Profile = reader.Value;
						break;
					case "Identifier":
						fx.Identifier = reader.Value;
						break;
					case "MinimumVersionDisplayName":
						fx.MinimumVersionDisplayName = reader.Value;
						break;
					case "DisplayName":
						fx.DisplayName = reader.Value;
						break;
					}
				}
			}

			if (string.IsNullOrEmpty (fx.Identifier))
				throw new Exception ("Framework element did not specify an Identifier attribute");
			
			return fx;
		}
	}
}
