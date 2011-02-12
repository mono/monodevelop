// 
// IPhoneDebuggerSession.cs
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
using Mono.Debugger.Soft;
using Mono.Debugging;
using Mono.Debugging.Client;
using System.Threading;
using System.Diagnostics;
using MonoDevelop.Moonlight;
using System.IO;
using MonoDevelop.Core;
using System.Net.Sockets;
using System.Net;

namespace MonoDevelop.Debugger.Soft.Moonlight
{
	public class MoonlightSoftDebuggerSession : Mono.Debugging.Soft.SoftDebuggerSession
	{
		Process browser;
		
		const string DEFAULT_PROFILE="monodevelop-moonlight-debug";
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			var dsi = (MoonlightDebuggerStartInfo) startInfo;
			int assignedDebugPort;
			StartListening (dsi, out assignedDebugPort);
			StartBrowserProcess (dsi, assignedDebugPort);
		}

		void StartBrowserProcess (MoonlightDebuggerStartInfo dsi, int assignedDebugPort)
		{
			if (browser != null)
				throw new InvalidOperationException ("Browser already started");
			
			var firefoxProfile = PropertyService.Get<string> ("Moonlight.Debugger.FirefoxProfile", DEFAULT_PROFILE);
			CreateFirefoxProfileIfNecessary (firefoxProfile);
			
			var psi = new ProcessStartInfo ("firefox") {
				Arguments = string.Format (" -no-remote -P '{0}' '{1}'", firefoxProfile, dsi.Url),
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			var args = (Mono.Debugging.Soft.SoftDebuggerRemoteArgs) dsi.StartArgs;
			psi.EnvironmentVariables.Add ("MOON_SOFT_DEBUG",
				string.Format ("transport=dt_socket,address={0}:{1}", args.Address, assignedDebugPort));
			
			browser = Process.Start (psi);
			ConnectOutput (browser.StandardOutput, false);
			ConnectOutput (browser.StandardError, true);
			
			browser.EnableRaisingEvents = true;
			browser.Exited += delegate {
				EndSession ();
			};
		}
		
		protected override void EndSession ()
		{
			EndBrowserProcess ();
			base.EndSession ();
		}
		
		protected override void OnExit ()
		{
			EndBrowserProcess ();
			base.OnExit ();
		}
		
		void EndBrowserProcess ()
		{
			if (browser == null || browser.HasExited) {
				browser = null;
				return;
			}
			
			browser.Kill ();
			browser = null;
		}
		
		static void CreateFirefoxProfileIfNecessary (string profileName)
		{
			FilePath profilePath = FilePath.Null;
			try {
				profilePath = GetFirefoxProfilePath (profileName);
			} catch (Exception ex) {
				LoggingService.LogError ("Error reading Firefox profile list", ex);
			}
			if (profilePath.IsNullOrEmpty) {
				var psi = new ProcessStartInfo ("firefox") {
					Arguments = string.Format ("-no-remote -CreateProfile '{0}'", profileName),
				};
				using (var p = Process.Start (psi)) {
					p.WaitForExit (5000); //wait 5 seconds
					if (!p.HasExited) {
						p.Kill ();
						throw new UserException ("Failed to create Firefox profile. Firefox did not exit.");
					}
					if (p.ExitCode != 0)
						throw new UserException ("Failed to create Firefox profile. Firefox exit code: '" + p.ExitCode + "'");
				}
			}
		}
		
		static FilePath GetFirefoxProfilePath (string profileName)
		{
			FilePath profileDir = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			profileDir = profileDir.Combine (".mozilla", "firefox");
			FilePath iniFile = profileDir.Combine ("profiles.ini");
			
			if (!File.Exists (iniFile)) {
				LoggingService.LogWarning ("Firefox profile list '{0}' does not exist", iniFile);
				return FilePath.Null;
			}
			
			try {
				using (var file = File.OpenText (iniFile)) {
					string nameLine = "Name=" + profileName;
					while (!file.EndOfStream) {
						var line = file.ReadLine ();
						if (line != nameLine)
							continue;
						
						string relativeLine = null;
						string pathLine = null;
						do {
							line = file.ReadLine ();
							if (line.StartsWith ("[") || line.StartsWith ("Name="))
								break;
							if (line.StartsWith ("IsRelative="))
								relativeLine = line.Substring ("IsRelative=".Length);
							else if (line.StartsWith ("Path="))
								pathLine = line.Substring ("Path=".Length);
						} while (!file.EndOfStream && (relativeLine == null || pathLine == null));
						
						if (relativeLine == null || pathLine == null)
							throw new Exception ("Missing entries in Firefox profiles.ini");
						
						var dir = (relativeLine == "1")? profileDir.Combine (pathLine) : (FilePath)pathLine;
						
						if (!Directory.Exists (dir))
							throw new Exception ("Firefox profile is registered but directory '" + dir + "' is missing");
						
						return dir;
					}
					
					//no profile with this name exists
					return FilePath.Null;
				}
			} catch (IOException ex) {
				throw new Exception ("Error reading Firefox profiles.ini", ex);
			}
		}
	}
}
