// 
// UserDataLocations.cs
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
using System.Runtime.InteropServices;

namespace MonoDevelop.Core
{
	public class UserDataLocations
	{
		const string APP_ID = "MonoDevelop";
		
		/// <summary>Location for cached data that can be regenerated.</summary>
		public FilePath Cache { get; private set; }
		
		/// <summary>Location for current preferences/settings.</summary>
		public FilePath Config { get; private set; }
		
		/// <summary>Preferences/settings specific to the local machine.</summary>
		public FilePath ConfigLocal { get; private set; }
		
		/// <summary>Root location for data files created or modifiable by the user, such as templates, snippets and color schemes.</summary>
		public FilePath Data { get; private set; }
		
		/// <summary>Location for log files.</summary>
		public FilePath Logs { get; private set; }
		
		/// <summary>Location for addins installed by the user.</summary>
		public FilePath Addins { get; private set; }
		
		//TODO: clear out temp files at startup
		/// <summary>Location for temporary files.</summary>
		public FilePath Temp { get; private set; }
		
		/// <summary>Gets a location by its ID.</summary>
		internal FilePath GetLocation (UserDataKind kind)
		{
			switch (kind) {
			case UserDataKind.Addins:
				return Addins;
			case UserDataKind.Cache:
				return Cache;
			case UserDataKind.Data:
				return Data;
			case UserDataKind.Logs:
				return Logs;
			case UserDataKind.Config:
				return Config;
			case UserDataKind.ConfigLocal:
				return ConfigLocal;
			case UserDataKind.Temp:
				return Temp;
			default:
				throw new ArgumentException ("Unknown UserDataLocation:" + kind.ToString ());
			}
		}
		
		/// <summary>
		/// Creates locations in a specific folder, for testing.
		/// </summary>
		internal static UserDataLocations ForTest (string version, FilePath profileLocation)
		{
			string appId = APP_ID + "-" + version;
			return new UserDataLocations () {
				Cache = profileLocation.Combine (appId, "Cache"),
				Data = profileLocation.Combine (appId, "Data"),
				ConfigLocal = profileLocation.Combine (appId, "Config"),
				Config = profileLocation.Combine (appId, "ConfigLocal"),
				Logs = profileLocation.Combine (appId, "Logs"),
				Addins = profileLocation.Combine (appId, "Addins"),
				Temp = profileLocation.Combine (appId, "Temp"),
			};
		}
		
		internal static UserDataLocations ForWindows (string version)
		{
			string appId = APP_ID + "-" + version;
			FilePath local = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
			FilePath roaming = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			//FilePath localLow = GetKnownFolderPath (new Guid ("A520A1A4-1780-4FF6-BD18-167343C5AF16"));
			
			local = local.Combine (appId);
			roaming = roaming.Combine (appId);
			
			return new UserDataLocations () {
				Data = roaming,
				Config = roaming.Combine ("Config"),
				ConfigLocal = local.Combine ("Config"),
				Addins = local.Combine ("Addins"),
				Logs = local.Combine ("Logs"),
				Cache = local.Combine ("Cache"),
				Temp = local.Combine ("Temp"),
			};
		}
		
		internal static UserDataLocations ForMac (string version)
		{
			string appId = APP_ID + "-" + version;
			FilePath home = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			FilePath library = home.Combine ("Library");
			
			FilePath data = library.Combine (appId);
			FilePath preferences = library.Combine ("Preferences", appId);
			FilePath cache = library.Combine ("Caches", appId);
			FilePath logs = library.Combine ("Logs", appId);
			FilePath appSupport = library.Combine ("Application Support", appId);
			
			return new UserDataLocations () {
				Cache = cache,
				Data = data,
				Config = preferences,
				ConfigLocal = preferences,
				Logs = logs,
				Addins = appSupport.Combine ("Addins"),
				Temp = cache.Combine ("Temp"),
			};
		}
		
		internal static UserDataLocations ForUnix (string version)
		{
			FilePath home = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			FilePath xdgDataHome = Environment.GetEnvironmentVariable ("XDG_DATA_HOME");
			if (xdgDataHome.IsNullOrEmpty)
				xdgDataHome = home.Combine (".local", "share");
			FilePath xdgConfigHome = Environment.GetEnvironmentVariable ("XDG_CONFIG_HOME");
			if (xdgConfigHome.IsNullOrEmpty)
				xdgConfigHome = home.Combine (".config");
			FilePath xdgCacheHome = Environment.GetEnvironmentVariable ("XDG_CACHE_HOME");
			if (xdgCacheHome.IsNullOrEmpty)
				xdgCacheHome = home.Combine (".cache");
			
			string appId = APP_ID + "-" + version;
			FilePath data = xdgDataHome.Combine (appId);
			FilePath config = xdgConfigHome.Combine (appId);
			FilePath cache = xdgCacheHome.Combine (appId);
			
			return new UserDataLocations () {
				Data = data,
				Addins = data.Combine ("Addins"),
				Config = config,
				ConfigLocal = config,
				Cache = cache,
				Temp = cache.Combine ("Temp"),
				Logs = cache.Combine ("Logs"),
			};
		}
		
		internal static UserDataLocations ForMD24 ()
		{
			FilePath appdata = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			var mdConfig = appdata.Combine ("MonoDevelop");
			return new UserDataLocations () {
				Data = mdConfig,
				Config = mdConfig,
				ConfigLocal = mdConfig,
				Addins = mdConfig.Combine ("addins"),
				Logs = mdConfig,
				Cache = mdConfig,
				//temp is not migratable
			};
		}
		/*
		static string GetKnownFolderPath (Guid knownFolderId)
		{
			var pathHandle = IntPtr.Zero;
			try {
				int hresult = SHGetKnownFolderPath (knownFolderId, 0, IntPtr.Zero, out pathHandle);
				if (hresult >= 0)
					return Marshal.PtrToStringAuto (pathHandle);
				throw Marshal.GetExceptionForHR (hresult);
			} finally {
				if (pathHandle != IntPtr.Zero)
					Marshal.FreeCoTaskMem (pathHandle);
			}
		}
		
		[DllImport ("shell32.dll")]
		static extern int SHGetKnownFolderPath ([MarshalAs (UnmanagedType.LPStruct)] Guid rfid,
			uint dwFlags, IntPtr hToken, out IntPtr pszPath);
		*/
    }
	
	enum UserDataKind
	{
		Cache,
		Config,
		ConfigLocal,
		Data,
		Logs,
		Addins,
		Temp,
	}
}
