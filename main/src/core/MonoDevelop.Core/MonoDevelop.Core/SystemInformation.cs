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
			var biFile = ((FilePath)Assembly.GetCallingAssembly ().Location).ParentDirectory.Combine ("buildinfo");
			if (File.Exists (biFile)) {
				foreach (var line in File.ReadAllLines (biFile)){
					if (!string.IsNullOrWhiteSpace (line))
						sb.AppendLine (line.Trim ());
				}
			} else {
				sb.AppendLine ("No build info");
			}

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