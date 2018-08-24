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
using System.Xml;
using System.Collections.Generic;

namespace MonoDevelop.Core.Assemblies
{
	public class SupportedFramework
	{
		public static readonly Version NoMaximumVersion = new Version (int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);
		public static readonly Version NoMinimumVersion = new Version (0, 0, 0, 0);

		[Obsolete("This was misspelled, use NoMinimumVersion")]
		public static readonly Version NoMinumumVersion = NoMinimumVersion;

		[Obsolete("Use the overload without a TargetFramework parameter")]
		public SupportedFramework (TargetFramework target, string identifier, string display, string profile, Version minVersion, string minDisplayVersion)
			: this (identifier, display, profile, minVersion, minDisplayVersion)
		{
		}

		public SupportedFramework (string identifier, string display, string profile, Version minVersion, string minDisplayVersion)
		{
			MinimumVersionDisplayName = minDisplayVersion;
			MinimumVersion = minVersion;
			MaximumVersion = NoMaximumVersion;
			DisplayName = display;
			Identifier = identifier;
			Profile = profile;
		}

		internal SupportedFramework ()
		{
			MinimumVersionDisplayName = string.Empty;
			MinimumVersion = NoMinimumVersion;
			MaximumVersion = NoMaximumVersion;
			DisplayName = string.Empty;
			Identifier = string.Empty;
			Profile = string.Empty;
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

		public string MonoSpecificVersion {
			get; internal set;
		}

		public string MonoSpecificVersionDisplayName {
			get; internal set;
		}

		//this referred to the "parent" rather than framework decsribed by this instance
		[Obsolete ("This property was misleading and is no longer supported")]
		public TargetFramework TargetFramework => TargetFramework.Default;
		
		static Version ParseVersion (string version, Version wildcard)
		{
			if (version == "*")
				return wildcard;
			
			return Version.Parse (version);
		}
		
		internal static SupportedFramework Load (FilePath path)
		{
			using (var reader = XmlReader.Create (path)) {
				if (!reader.ReadToDescendant ("Framework"))
					throw new Exception ("Missing Framework element");
				var fx = LoadFromAttributes (reader);
				if (string.IsNullOrEmpty (fx.DisplayName)) {
					fx.DisplayName = path.FileNameWithoutExtension;
				}
				return fx;
			}
			
		}
		internal static SupportedFramework LoadFromAttributes (XmlReader reader)
		{
			var fx = new SupportedFramework ();
			if (!reader.HasAttributes)
				throw new Exception ("Framework element does not contain any attributes");

			while (reader.MoveToNextAttribute ()) {
				switch (reader.Name) {
				case "MaximumVersion":
					fx.MaximumVersion = ParseVersion (reader.Value, NoMaximumVersion);
					break;
				case "MinimumVersion":
					fx.MinimumVersion = ParseVersion (reader.Value, NoMinimumVersion);
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
				case "MonoSpecificVersion":
					fx.MonoSpecificVersion = reader.Value;
					break;
				case "MonoSpecificVersionDisplayName":
					fx.MonoSpecificVersionDisplayName = reader.Value;
					break;
				}
			}

			if (string.IsNullOrEmpty (fx.Identifier))
				throw new Exception ("Framework element did not specify an Identifier attribute");

			return fx;
		}

		internal void SaveAsElement (XmlWriter writer)
		{
			writer.WriteStartElement ("SupportedFramework");
			if (MaximumVersion != NoMaximumVersion) {
				writer.WriteAttributeString ("MaximumVersion", MaximumVersion.ToString ());
			}
			if (MinimumVersion != NoMinimumVersion) {
				writer.WriteAttributeString ("MinimumVersion", MinimumVersion.ToString ());
			}
			WriteNonEmptyAttribute ("Profile", Profile);
			WriteNonEmptyAttribute ("Identifier", Identifier);
			WriteNonEmptyAttribute ("DisplayName", DisplayName);
			WriteNonEmptyAttribute ("MinimumVersionDisplayName", MinimumVersionDisplayName);
			WriteNonEmptyAttribute ("MonoSpecificVersion", MonoSpecificVersion);
			WriteNonEmptyAttribute ("MonoSpecificVersionDisplayName", MonoSpecificVersionDisplayName);

			writer.WriteEndElement ();

			void WriteNonEmptyAttribute (string name, string val)
			{
				if (!string.IsNullOrEmpty (val)) {
					writer.WriteAttributeString (name, val);
				}
			}
		}

		public override int GetHashCode ()
		{
			return DisplayName != null ? DisplayName.GetHashCode () : 0;
		}

		public override bool Equals (object obj)
		{
			var other = obj as SupportedFramework;
			if (other == null)
				return false;

			if (!string.Equals (DisplayName, other.DisplayName))
				return false;
			if (!string.Equals (Identifier, other.Identifier))
				return false;
			if (!string.Equals (Profile, other.Profile))
				return false;
			if (!string.Equals (MonoSpecificVersion, other.MonoSpecificVersion))
				return false;
			if (!string.Equals (MonoSpecificVersionDisplayName, other.MonoSpecificVersionDisplayName))
				return false;
			if (!string.Equals (MinimumVersionDisplayName, other.MinimumVersionDisplayName))
				return false;
			if (!MinimumVersion.Equals (other.MinimumVersion))
				return false;
			if (!MaximumVersion.Equals (other.MaximumVersion))
				return false;
			return true;
		}

		public static IEqualityComparer<SupportedFramework> EqualityComparer = new _Comparer ();

		class _Comparer : IEqualityComparer<SupportedFramework> {
			public bool Equals (SupportedFramework x, SupportedFramework y)
			{
				return x.Equals (y);
			}
			public int GetHashCode (SupportedFramework obj)
			{
				return obj.GetHashCode ();
			}
		}
	}
}
