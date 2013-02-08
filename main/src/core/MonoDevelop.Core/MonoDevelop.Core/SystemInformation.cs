// 
// SystemInformation.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright (c) 2011, Xamarin Inc
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
using System.Text;
using System.IO;
using System.Reflection;
using Mono.Addins;
using System.Collections.Generic;

namespace MonoDevelop.Core
{
	public abstract class SystemInformation
	{
		static SystemInformation Instance {
			get; set;
		}
		
		public static string InstallationUuid {
			get {
				return PropertyService.Get<string> ("MonoDevelop.Core.InstallUuid", Guid.NewGuid ().ToString ());
			}
		}
		
		public static string SessionUuid {
			get; private set;
		}

		internal SystemInformation ()
		{
		}
		
		static SystemInformation ()
		{
			if (Platform.IsMac)
				Instance = new MacSystemInformation ();
			else if (Platform.IsWindows)
				Instance = new WindowsSystemInformation ();
			else
				Instance = new LinuxSystemInformation ();
			
			SessionUuid = DateTime.UtcNow.Ticks.ToString ();
		}
		
		internal abstract void AppendOperatingSystem (StringBuilder sb);
		
		IEnumerable<ISystemInformationProvider> InternalGetDescription ()
		{
			foreach (var info in AddinManager.GetExtensionObjects<ISystemInformationProvider> ("/MonoDevelop/Core/SystemInformation", false)) {
				yield return info;
			}

			var sb = new StringBuilder ();
			// First append the MonoDevelop build information
			var biFile = ((FilePath)Assembly.GetEntryAssembly ().Location).ParentDirectory.Combine ("buildinfo");
			if (File.Exists (biFile)) {
				var lines = File.ReadAllLines (biFile)
					.Select (l => l.Trim ())
					.Where (l => !string.IsNullOrEmpty (l))
					.ToArray ();
				if (lines.Length > 0) {
					foreach (var line in lines) {
						sb.AppendLine (line);
					}
				}
			}

			// Then append the Xamarin Addins information if it exists
			biFile = ((FilePath) Assembly.GetEntryAssembly ().Location).ParentDirectory.Combine ("buildinfo_xamarin");
			if (File.Exists (biFile)) {
				var lines = File.ReadAllLines (biFile)
					.Select (l => l.Trim ())
					.Where (l => !string.IsNullOrEmpty (l))
					.ToArray ();
				if (lines.Length > 0) {
					sb.Append ("Xamarin addins: ");
					foreach (var line in lines)
						sb.AppendLine (line);
				}
			}

			if (sb.Length == 0)
				sb.AppendLine ("Build information unavailable");

			yield return new SystemInformationSection () {
				Title = "Build Information",
				Description = sb.ToString ()
			};

			sb.Clear ();
			AppendOperatingSystem (sb);

			yield return new SystemInformationSection () {
				Title = "Operating System",
				Description = sb.ToString ()
			};
		}

		public static string GetReleaseId ()
		{
			var biFile = ((FilePath)Assembly.GetEntryAssembly ().Location).ParentDirectory.Combine ("buildinfo");
			if (File.Exists (biFile)) {
				var line = File.ReadAllLines (biFile).Select (l => l.Split (':')).Where (a => a.Length > 1 && a [0].Trim () == "Release ID").FirstOrDefault ();
				if (line != null)
					return line [1].Trim ();
			}
			return null;
		}

		public static IEnumerable<ISystemInformationProvider> GetDescription ()
		{
			return Instance.InternalGetDescription ();
		}

		public static string GetTextDescription ()
		{
			StringBuilder sb = new StringBuilder ();
			foreach (var info in GetDescription ()) {
				sb.Append ("=== ").Append (info.Title.Trim ()).Append (" ===\n\n");
				sb.Append (info.Description.Trim ());
				sb.Append ("\n\n");
			}
			return sb.ToString ();
		}
	}

	class SystemInformationSection: ISystemInformationProvider
	{
		public string Title { get; set; }
		public string Description { get; set; }
	}
}
