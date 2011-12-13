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
		
		protected abstract void AppendOperatingSystem (StringBuilder sb);
		
		public override string ToString ()
		{
			var sb = new StringBuilder ();
			
			foreach (var info in AddinManager.GetExtensionObjects<ISystemInformationProvider> ("/MonoDevelop/Core/SystemInformation", false)) {
				try {
					sb.AppendLine (info.Description);
				} catch (Exception ex) {
					LoggingService.LogError ("Error getting about information: ", ex);
				}
			}
			
			var biFile = ((FilePath)Assembly.GetCallingAssembly ().Location).ParentDirectory.Combine ("buildinfo");
			if (File.Exists (biFile)) {
				sb.AppendLine ("Build information:");
				foreach (var line in File.ReadAllLines (biFile)){
					if (!string.IsNullOrWhiteSpace (line)) {
						sb.Append ("\t");
						sb.AppendLine (line.Trim ());
					}
				}
			} else {
				sb.AppendLine ("No build info");
			}
			
			sb.AppendLine ("Operating System:");
			AppendOperatingSystem (sb);
			
			int nameLength = 0;
			int versionLength = 0;
			sb.AppendLine ("Loaded assemblies:");

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies ()) {
				try {
					if (assembly.IsDynamic)
						continue;
					var assemblyName = assembly.GetName ();
					nameLength = Math.Max (nameLength, assemblyName.Name.Length);
					versionLength = Math.Max (versionLength, assemblyName.Version.ToString ().Length);
				} catch {
				}
			}
			nameLength++;
			versionLength++;
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies ()) {
				try {
					if (assembly.IsDynamic)
						continue;
					var assemblyName = assembly.GetName ();
					sb.AppendLine (assemblyName.Name.PadRight (nameLength) + assemblyName.Version.ToString ().PadRight (versionLength) + System.IO.Path.GetFullPath (assembly.Location));
				} catch {
				}
			}
			
			return sb.ToString ();
		}
	
		public static string ToText ()
		{
			return Instance.ToString ();
		}
	}
}

