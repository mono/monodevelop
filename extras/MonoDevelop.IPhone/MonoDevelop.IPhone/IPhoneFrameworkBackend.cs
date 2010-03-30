// 
// IPhoneFrameworkBackend.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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


namespace MonoDevelop.IPhone
{
	public class IPhoneFrameworkBackend : MonoFrameworkBackend
	{
		const string SDK_ROOT = "/Developer/MonoTouch";
		
		string sdkDir;
		string sdkBin;
		
		public IPhoneFrameworkBackend ()
		{
			if (Directory.Exists (SDK_ROOT)) {
				try {
					sdkDir = SDK_ROOT + "/usr/lib/mono/2.1";
					sdkBin = SDK_ROOT + "/usr/bin";
					if (!File.Exists (Path.Combine (sdkDir, "mscorlib.dll"))) {
						sdkDir = null;
					    throw new Exception ("Missing mscorlib in iPhone SDK " + SDK_ROOT);
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Unexpected error finding iPhone SDK directory", ex);
				}
			}
		}
		
		public override IEnumerable<string> GetToolsPaths ()
		{
			yield return sdkBin;
			yield return sdkDir;
			foreach (string path in base.GetToolsPaths ())
				yield return path;
		}
		
		public override IEnumerable<string> GetFrameworkFolders ()
		{
			yield return sdkDir;
		}
		
		public override SystemPackageInfo GetFrameworkPackageInfo (string packageName)
		{
			SystemPackageInfo info = base.GetFrameworkPackageInfo ("mono-iphone");
			info.Name = "mono-iphone";
			return info;
		}
		
		public override bool IsInstalled {
			get { return sdkDir != null; }
		}
	}
	
	public static class IPhoneFramework
	{
		static bool? isInstalled = null;
		static bool? simOnly = null;
		static IPhoneSdkVersion[] installedSdkVersions;
		
		public static bool IsInstalled {
			get {
				if (!isInstalled.HasValue) {
					isInstalled = Directory.Exists ("/Developer/MonoTouch");
				}
				return isInstalled.Value;
			}
		}
		
		public static bool SimOnly {
			get {
				if (!simOnly.HasValue) {
					simOnly = !File.Exists ("/Developer/MonoTouch/usr/bin/arm-darwin-mono");
				}
				return simOnly.Value;
			}
		}
		
		public static MonoDevelop.Projects.BuildResult GetSimOnlyError ()
		{
			var res = new MonoDevelop.Projects.BuildResult ();
			res.AddError (GettextCatalog.GetString (
				"The evaluation version of MonoTouch does not support targeting the device. " + 
				"Please go to http://monotouch.net to purchase the full version."));
			return res;
		}
		
		public static bool SdkIsInstalled (string version)
		{
			return File.Exists ("/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS"
			                    + version + ".sdk/ResourceRules.plist");
		}
		
		public static bool SdkIsInstalled (IPhoneSdkVersion version)
		{
			return SdkIsInstalled (version.ToString ());
		}
		
		public static IPhoneSdkVersion GetClosestInstalledSdk (IPhoneSdkVersion v)
		{
			IPhoneSdkVersion? previous = null;
			foreach (var i in InstalledSdkVersions) {
				var cmp = v.CompareTo (i);
				if (cmp == 0) {
					return i;
				} else if (cmp > 0) {
					return previous ?? i;	
				}
				previous = i;
			}
			return IPhoneSdkVersion.Default;
		}
		
		public static IList<IPhoneSdkVersion> InstalledSdkVersions {
			get {
				EnsureSdkVersions ();
				return installedSdkVersions;
			}
		}
		
		static void EnsureSdkVersions ()
		{
			if (installedSdkVersions != null)
				return;
			
			const string sdkDir = "/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/";
			if (!Directory.Exists (sdkDir)) {
				installedSdkVersions = new IPhoneSdkVersion[0];
				return;
			}
			
			var sdks = new List<string> ();
			foreach (var dir in Directory.GetDirectories (sdkDir)) {
				string d = dir.Substring (sdkDir.Length);
				if (d.StartsWith ("iPhoneOS"))
					d = d.Substring ("iPhoneOS".Length);
				if (d.EndsWith (".sdk"))
					d = d.Substring (0, d.Length - ".sdk".Length);
				if (d.Length > 0)
					sdks.Add (d);
			}
			var vs = new List<IPhoneSdkVersion> ();
			foreach (var s in sdks) {
				try {
					vs.Add (IPhoneSdkVersion.Parse (s));
				} catch (Exception ex) {
					LoggingService.LogError ("Could not parse iPhone SDK version {0}:\n{1}", s, ex.ToString ());
				}
			}
			installedSdkVersions = vs.ToArray ();
			Array.Sort (installedSdkVersions);
		}
		
		public static void ShowSimOnlyDialog ()
		{
			if (!SimOnly)
				return;
			
			var dialog = new Dialog ();
			dialog.Title =  GettextCatalog.GetString ("Evaluation Version");
			
			dialog.VBox.PackStart (
			 	new Label ("<b><big>Feature Not Available In Evaluation Version</big></b>") {
					Xalign = 0.5f,
					UseMarkup = true
				}, true, false, 12);
			
			var align = new Gtk.Alignment (0.5f, 0.5f, 1.0f, 1.0f) { LeftPadding = 12, RightPadding = 12 };
			dialog.VBox.PackStart (align, true, false, 12);
			align.Add (new Label (
				"You should upgrade to the full version of MonoTouch to target and deploy\n" +
				" to the device, and to enable your applications to be distributed.") {
					Xalign = 0.5f,
					Justify = Justification.Center
				});
			
			align = new Gtk.Alignment (0.5f, 0.5f, 1.0f, 1.0f) { LeftPadding = 12, RightPadding = 12 };
			dialog.VBox.PackStart (align, true, false, 12);
			var buyButton = new Button (
				new Label (GettextCatalog.GetString ("<big>Buy MonoTouch</big>")) { UseMarkup = true } );
			buyButton.Clicked += delegate {
				System.Diagnostics.Process.Start ("http://monotouch.net");
				dialog.Respond (ResponseType.Accept);
			};
			align.Add (buyButton);
			
			dialog.AddButton (GettextCatalog.GetString ("Continue evaluation"), ResponseType.Close);
			dialog.ShowAll ();
			
			MessageService.ShowCustomDialog (dialog);
		}
	}
	
	public class MonoTouchInstalledCondition : ConditionType
	{
		public override bool Evaluate (NodeElement conditionNode)
		{
			return IPhoneFramework.IsInstalled;
		}
	}
	
	public struct IPhoneSdkVersion : IComparable<IPhoneSdkVersion>, IEquatable<IPhoneSdkVersion>
	{
		public static readonly IPhoneSdkVersion Default = GetDefaultSdkVersion ();
		
		static IPhoneSdkVersion GetDefaultSdkVersion ()
		{
			var v = IPhoneFramework.InstalledSdkVersions;
			return v.Count > 0? v[0] : new IPhoneSdkVersion (new int[] { 3, 0 });
		}
		
		int[] version;
		
		public IPhoneSdkVersion (int[] version)
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
		
		public int[] Version { get { return version ?? Default.version; } }
		
		public override string ToString ()
		{
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
			if (x.Length != y.Length)
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
	}
}
