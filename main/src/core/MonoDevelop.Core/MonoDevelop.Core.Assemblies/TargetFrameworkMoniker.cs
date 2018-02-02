// 
// TargetFrameworkMoniker.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
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

using System;
using System.Collections.Generic;
using MonoDevelop.Core.Serialization;
using System.Reflection;
using Mono.Addins;
using MonoDevelop.Core.AddIns;
using Mono.PkgConfig;
namespace MonoDevelop.Core.Assemblies
{
	/// <summary>
	/// Unique identifier for a target framework.
	/// </summary>
	[Serializable]
	public class TargetFrameworkMoniker : IEquatable<TargetFrameworkMoniker>
	{
		string identifier, version, profile;
		
		TargetFrameworkMoniker ()
		{
		}
		
		public TargetFrameworkMoniker (string version) : this (ID_NET_FRAMEWORK, version, null)
		{
		}
		
		public TargetFrameworkMoniker (string identifier, string version) : this (identifier, version, null)
		{
		}
		
		public TargetFrameworkMoniker (string identifier, string version, string profile)
		{
			if (version == null || version.Length == 0 || (version.Length == 1 && version[0] == 'v'))
				throw new ArgumentException ("A version must be provided", "version");
			
			if (string.IsNullOrEmpty (identifier))
				throw new ArgumentException ("An identifier must be provided", "identifier");
			
			if (version[0] == 'v')
				version = version.Substring (1);
			
			if (profile != null & profile == "")
				profile = null;
			
			this.identifier = identifier;
			this.version = version;
			this.profile = profile;
		}
		
		/// <summary>
		/// The root identifier of the framework, e.g. ".NETFramework" or "Silverlight"
		/// </summary>
		public string Identifier { get { return identifier; } }
		
		/// <summary>
		/// The version of the framework.
		/// </summary>
		public string Version { get { return version; } }
		
		/// <summary>
		/// Optional. A named subset of a particular framework version, e.g. "Client".
		/// </summary>
		public string Profile { get { return profile; } }

		public static TargetFrameworkMoniker Parse (string value)
		{
			TargetFrameworkMoniker moniker;
			if (!TryParse (value, out moniker)) {
				throw new FormatException (string.Format ("Invalid framework moniker '{0}'", value));
			}
			return moniker;
		}

		public static bool TryParse (string value, out TargetFrameworkMoniker moniker)
		{
			moniker = new TargetFrameworkMoniker ();
			if (moniker.ParseInternal (value)) {
				return true;
			}
			moniker = null;
			return false;
		}
		
		bool ParseInternal (string value)
		{
			profile = null;
			
			int i = value.IndexOf (',');
			
			//HACK: this isn't strictly valid but it makes back-compat a lot easier
			if (i < 1) {
				if (value == "SL2.0") {
					identifier = ID_SILVERLIGHT;
					version = "2.0";
					return true;
				}
				if (value == "SL3.0") {
					identifier = ID_SILVERLIGHT;
					version = "3.0";
					return true;
				}
				if (value == "IPhone") {
					identifier = ID_MONOTOUCH;
					version = "1.0";
					return true;
				}
				if (value [0] == 'v') {
					value = value.Substring (1);
				}
				identifier = ID_NET_FRAMEWORK;
				version = value;
			} else {
				identifier = value.Substring (0, i);

				if (value.IndexOf (",Version=v", i, ",Version=v".Length, StringComparison.Ordinal) != i) {
					return false;
				}
				i += ",Version=v".Length;

				int i2 = value.IndexOf (',', i);
				if (i2 < 0) {
					version = value.Substring (i);
				} else {
					version = value.Substring (i, i2 - i);
					profile = value.Substring (i2 + ",Profile=".Length);
				}
			}

			Version v;
			return System.Version.TryParse (version, out v);
		}
		
		internal string ToLegacyIdString ()
		{
			if (identifier == ID_SILVERLIGHT && (version == "2.0" || version == "3.0"))
				return "SL" + version;
			else if (identifier == ID_MONOTOUCH && version == "1.0")
				return "IPhone";
			else if (identifier == ID_NET_FRAMEWORK)
				return version;
			else
				return ToString ();
		}
		
		public override string ToString ()
		{
			string val = identifier + ",Version=v" + version;
			if (profile != null)
				val = val + ",Profile=" + profile;
			return val;
		}

		string cachedAssemblyDirectoryName;
		public string GetAssemblyDirectoryName ()
		{
			// PERF: This is queried a lot, so cache it.
			if (cachedAssemblyDirectoryName == null) {
				if (profile != null)
					cachedAssemblyDirectoryName = System.IO.Path.Combine (identifier, "v" + version, "Profile", profile);
				cachedAssemblyDirectoryName = System.IO.Path.Combine (identifier, "v" + version);
			}
			return cachedAssemblyDirectoryName;
		}
		
		public bool Equals (TargetFrameworkMoniker other)
		{
			return other != null && identifier == other.identifier && version == other.version && profile == other.profile;
		}
		
		public override bool Equals (object obj)
		{
			return Equals (obj as TargetFrameworkMoniker);
		}
		
		public override int GetHashCode ()
		{
			int ret = 0;
			if (identifier != null)
				ret ^= identifier.GetHashCode ();
			if (version != null)
				ret ^= version.GetHashCode ();
			if (profile != null)
				ret ^= profile.GetHashCode ();
			return ret;
		}
		
		public static bool operator == (TargetFrameworkMoniker a, TargetFrameworkMoniker b)
		{
			if (((object)a) == null)
				return ((object)b) == null;
			return a.Equals (b);
		}
		
		public static bool operator != (TargetFrameworkMoniker a, TargetFrameworkMoniker b)
		{
			if (((object)a) == null)
				return ((object)b) != null;
			return !a.Equals (b);
		}
		
		public static TargetFrameworkMoniker Default {
			get { return NET_1_1; }
		}
		
		public static TargetFrameworkMoniker NET_1_1 {
			get { return new TargetFrameworkMoniker ("1.1"); }
		}
		
		public static TargetFrameworkMoniker NET_2_0 {
			get { return new TargetFrameworkMoniker ("2.0"); }
		}
		
		public static TargetFrameworkMoniker NET_3_0 {
			get { return new TargetFrameworkMoniker ("3.0"); }
		}
		
		public static TargetFrameworkMoniker NET_3_5 {
			get { return new TargetFrameworkMoniker ("3.5"); }
		}
		
		public static TargetFrameworkMoniker NET_4_0 {
			get { return new TargetFrameworkMoniker ("4.0"); }
		}

		public static TargetFrameworkMoniker NET_4_5 {
			get { return new TargetFrameworkMoniker ("4.5"); }
		}

		public static TargetFrameworkMoniker NET_4_6 {
			get { return new TargetFrameworkMoniker ("4.6"); }
		}

		public static TargetFrameworkMoniker NET_4_6_1 {
			get { return new TargetFrameworkMoniker ("4.6.1"); }
		}

		public static TargetFrameworkMoniker NET_4_6_2 {
			get { return new TargetFrameworkMoniker ("4.6.2"); }
		}

		public static TargetFrameworkMoniker NET_4_7 {
			get { return new TargetFrameworkMoniker ("4.7"); }
		}

		public static TargetFrameworkMoniker NET_4_7_1 {
			get { return new TargetFrameworkMoniker ("4.7.1"); }
		}

		public static TargetFrameworkMoniker PORTABLE_4_0 {
			get { return new TargetFrameworkMoniker (ID_PORTABLE, "4.0", "Profile1"); }
		}
		
		public static TargetFrameworkMoniker SL_2_0 {
			get { return new TargetFrameworkMoniker (ID_SILVERLIGHT, "2.0"); }
		}
		
		public static TargetFrameworkMoniker SL_3_0 {
			get { return new TargetFrameworkMoniker (ID_SILVERLIGHT, "3.0"); }
		}
		
		public static TargetFrameworkMoniker SL_4_0 {
			get { return new TargetFrameworkMoniker (ID_SILVERLIGHT, "4.0"); }
		}
		
		public static TargetFrameworkMoniker MONOTOUCH_1_0 {
			get { return new TargetFrameworkMoniker (ID_MONOTOUCH, "1.0"); }
		}
		
		public static TargetFrameworkMoniker UNKNOWN {
			get { return new TargetFrameworkMoniker ("Unknown", "0.0"); }
		}
		
		public const string ID_NET_FRAMEWORK = ".NETFramework";
		public const string ID_SILVERLIGHT = "Silverlight";
		public const string ID_PORTABLE = ".NETPortable";
		public const string ID_MONOTOUCH = "MonoTouch";
		public const string ID_MONODROID = "MonoAndroid";
	}
	
	class TargetFrameworkMonikerDataType: DataType
	{
		public TargetFrameworkMonikerDataType (Type dataType) : base (dataType)
		{
		}
		
		public override bool IsSimpleType { get { return true; } }
		public override bool CanCreateInstance { get { return true; } }
		public override bool CanReuseInstance { get { return false; } }
		
		internal protected override DataNode OnSerialize (SerializationContext serCtx, object mapData, object value)
		{
			return new DataValue (Name, ((TargetFrameworkMoniker)value).ToString ());
		}
		
		internal protected override object OnDeserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			return TargetFrameworkMoniker.Parse (((DataValue)data).Value);
		}
	}
}
