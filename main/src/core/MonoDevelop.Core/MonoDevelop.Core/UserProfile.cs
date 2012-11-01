// 
// UserProfile.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.Core
{
	public class UserProfile
	{
		const string PROFILE_ENV_VAR = "MONODEVELOP_PROFILE";
		
		//These are the known profile versions that can be migrated.
		//MUST BE SORTED, low to high.
		//The last is the current profile version.
		//Should be increased only for major MD versions. Cannot contain '+' or '-'.
		internal static string[] ProfileVersions = new[] {
			"2.4",
			"2.6",
			"2.7",
			"2.8",
			"3.0",
			"4.0"
		};
		
		static UserProfile ()
		{
			Current = GetProfile (ProfileVersions[ProfileVersions.Length-1]);
		}
		
		public static UserProfile Current { get; private set; }
		
		/// <summary>Location for cached data that can be regenerated.</summary>
		public FilePath CacheDir { get; private set; }
		
		/// <summary>Location for current preferences/settings.</summary>
		public FilePath ConfigDir { get; private set; }
		
		/// <summary>Preferences/settings specific to the local machine.</summary>
		public FilePath LocalConfigDir { get; private set; }
		
		/// <summary>User-visible root location for user-created data files such as templates, snippets and color schemes.</summary>
		public FilePath UserDataRoot { get; private set; }
		
		/// <summary>Location for log files.</summary>
		public FilePath LogDir { get; private set; }
		
		/// <summary>Location for files installed from external sources.</summary>
		public FilePath LocalInstallDir { get; private set; }
		
		//TODO: clear out temp files at startup
		/// <summary>Location for temporary files.</summary>
		public FilePath TempDir { get; private set; }
		
		/// <summary>Gets a location by its ID.</summary>
		internal FilePath GetLocation (UserDataKind kind)
		{
			switch (kind) {
			case UserDataKind.LocalInstall:
				return LocalInstallDir;
			case UserDataKind.Cache:
				return CacheDir;
			case UserDataKind.UserData:
				return UserDataRoot;
			case UserDataKind.Logs:
				return LogDir;
			case UserDataKind.Config:
				return ConfigDir;
			case UserDataKind.LocalConfig:
				return LocalConfigDir;
			case UserDataKind.Temp:
				return TempDir;
			default:
				throw new ArgumentException ("Unknown UserDataLocation:" + kind.ToString ());
			}
		}
		
		internal static UserProfile GetProfile (string profileVersion)
		{
			FilePath testProfileRoot = Environment.GetEnvironmentVariable (PROFILE_ENV_VAR);
			if (!testProfileRoot.IsNullOrEmpty)
				return UserProfile.ForTest (profileVersion, testProfileRoot);
			
			if (Platform.IsWindows)
				return UserProfile.ForWindows (profileVersion);
			else if (Platform.IsMac)
				return UserProfile.ForMac (profileVersion);
			else
				return UserProfile.ForUnix (profileVersion);
		}

		static string GetAppId (string version)
		{
			return BrandingService.ProfileDirectoryName + "-" + version;;
		}
		
		/// <summary>
		/// Creates locations in a specific folder, for testing.
		/// </summary>
		internal static UserProfile ForTest (string version, FilePath profileLocation)
		{
			string appId = GetAppId (version);
			return new UserProfile () {
				CacheDir = profileLocation.Combine (appId, "Cache"),
				UserDataRoot = profileLocation.Combine (appId, "UserData"),
				ConfigDir = profileLocation.Combine (appId, "Config"),
				LocalConfigDir = profileLocation.Combine (appId, "LocalConfig"),
				LogDir = profileLocation.Combine (appId, "Logs"),
				LocalInstallDir = profileLocation.Combine (appId, "LocalInstall"),
				TempDir = profileLocation.Combine (appId, "Temp"),
			};
		}
		
		internal static UserProfile ForWindows (string version)
		{
			string appId = GetAppId (version);
			FilePath local = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
			FilePath roaming = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			//FilePath localLow = GetKnownFolderPath (new Guid ("A520A1A4-1780-4FF6-BD18-167343C5AF16"));
			
			local = local.Combine (appId);
			roaming = roaming.Combine (appId);
			
			return new UserProfile () {
				UserDataRoot = roaming,
				ConfigDir = roaming.Combine ("Config"),
				LocalConfigDir = local.Combine ("Config"),
				LocalInstallDir = local.Combine ("LocalInstall"),
				LogDir = local.Combine ("Logs"),
				CacheDir = local.Combine ("Cache"),
				TempDir = local.Combine ("Temp"),
			};
		}
		
		internal static UserProfile ForMac (string version)
		{
			string appId = GetAppId (version);
			FilePath home = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			FilePath library = home.Combine ("Library");
			
			FilePath data = library.Combine (appId);
			FilePath preferences = library.Combine ("Preferences", appId);
			FilePath cache = library.Combine ("Caches", appId);
			FilePath logs = library.Combine ("Logs", appId);
			FilePath appSupport = library.Combine ("Application Support", appId);
			
			return new UserProfile () {
				CacheDir = cache,
				UserDataRoot = data,
				ConfigDir = preferences,
				LocalConfigDir = preferences,
				LogDir = logs,
				LocalInstallDir = appSupport.Combine ("LocalInstall"),
				TempDir = cache.Combine ("Temp"),
			};
		}
		
		internal static UserProfile ForUnix (string version)
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
			
			string appId = GetAppId (version);
			FilePath data = xdgDataHome.Combine (appId);
			FilePath config = xdgConfigHome.Combine (appId);
			FilePath cache = xdgCacheHome.Combine (appId);
			
			return new UserProfile () {
				UserDataRoot = data,
				LocalInstallDir = data.Combine ("LocalInstall"),
				ConfigDir = config,
				LocalConfigDir = config,
				CacheDir = cache,
				TempDir = cache.Combine ("Temp"),
				LogDir = cache.Combine ("Logs"),
			};
		}
		
		internal static UserProfile ForMD24 ()
		{
			FilePath appdata = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			var mdConfig = appdata.Combine ("MonoDevelop");
			return new UserProfile () {
				UserDataRoot = mdConfig,
				ConfigDir = mdConfig,
				LocalConfigDir = mdConfig,
				LocalInstallDir = mdConfig.Combine ("addins"),
				LogDir = mdConfig,
				CacheDir = mdConfig,
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
		LocalConfig,
		UserData,
		Logs,
		LocalInstall,
		Temp,
	}
}
