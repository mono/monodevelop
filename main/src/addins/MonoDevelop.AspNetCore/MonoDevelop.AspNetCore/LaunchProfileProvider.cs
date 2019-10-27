//
// LaunchProfileProvider.cs
//
// Author:
//       José Miguel Torres <jostor@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonoDevelop.AspNetCore
{
	internal class LaunchProfileProvider
	{
		readonly string baseDirectory;
		readonly string defaultNamespace;
		readonly DotNetProject project;
		readonly object fileLocker = new object ();
		public IDictionary<string, JToken> GlobalSettings { get; private set; }
		internal JObject ProfilesObject { get; private set; }
		public ConcurrentDictionary<string, LaunchProfileData> Profiles { get; private set; }
		internal string LaunchSettingsJsonPath => Path.Combine (baseDirectory, "Properties", "launchSettings.json");
		const string DefaultGlobalSettings = @"{
    						""windowsAuthentication"": false,
    						""anonymousAuthentication"": true
  							}";

		const string defaultHttpUrl = "http://localhost:5000";
		const string defaultHttpsUrl = "https://localhost:5001";

		public LaunchProfileData DefaultProfile {
			get {
				if (!Profiles.TryGetValue (defaultNamespace, out var defaultProfile)) {
					defaultProfile = CreateDefaultProfile ();
					Profiles [defaultNamespace] = defaultProfile;
				}
				return defaultProfile;
			}
		}

		public LaunchProfileProvider (DotNetProject project)
		{
			this.project = project;
			this.baseDirectory = project.BaseDirectory;
			this.defaultNamespace = project.DefaultNamespace;
			GlobalSettings = new Dictionary<string, JToken> ();
			Profiles = new ConcurrentDictionary<string, LaunchProfileData> ();
		}

		static bool ApplicationUrlIsDefault (string applicationUrl)
		{
			return applicationUrl != null && applicationUrl.Contains (defaultHttpUrl) || applicationUrl.Contains (defaultHttpsUrl);
		}

		static bool TryAllocatePort (int testPort)
		{
			Socket testSocket = null;

			try {
				if (Socket.OSSupportsIPv4) {
					testSocket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				} else if (Socket.OSSupportsIPv6) {
					testSocket = new Socket (AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
				}
			} catch {
				testSocket?.Dispose ();
				return false;
			}

			if (testSocket != null) {
				var endPoint = new IPEndPoint (testSocket.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, testPort);

				try {
					testSocket.Bind (endPoint);
					return true;
				} catch {
					testSocket?.Dispose ();
					return false;
				}
			}

			return false;
		}

		public void LoadLaunchSettings ()
		{
			if (!File.Exists (LaunchSettingsJsonPath)) {
				CreateAndAddDefaultLaunchSettings ();
				return;
			}

			var launchSettingsJson = TryParse ();
			GlobalSettings.Clear ();

			foreach (var token in launchSettingsJson) {
				if (token.Key == "profiles") {
					ProfilesObject = token.Value as JObject;
					continue;
				}
				GlobalSettings.Add (token.Key, token.Value);
			}

			Profiles = new ConcurrentDictionary<string, LaunchProfileData> (LaunchProfileData.DeserializeProfiles (ProfilesObject));
		}

		/// <summary>
		/// Gets the next free port taking into account which ports are in use by other projects
		/// </summary>
		/// <returns>The next free port.</returns>
		int GetNextFreePort ()
		{
			var projects = Enumerable.Empty<Project> ();

			if (project.ParentSolution != null)
				projects = project.ParentSolution.GetAllProjects ();

			var runConfigurations = projects.SelectMany (p => p.RunConfigurations).OfType<AspNetCoreRunConfiguration> ();
			var applicationUrls =
				runConfigurations.Select (r => r.CurrentProfile.TryGetApplicationUrl ())
				.Where (a => a != null)
				.SelectMany (u => u.Split (';'));

			var portsInUse = applicationUrls.Select (url => new Uri (url).Port);
			var validPortRange = Enumerable.Range (5000, 100);
			int port = validPortRange.Except (portsInUse).First (TryAllocatePort);
			return port;
		}

		bool ShouldGenerateNewPort (string applicationUrl)
		{
			if (project.ParentSolution != null && ApplicationUrlIsDefault (applicationUrl)) {
				var allProjects = project.ParentSolution.GetAllProjects ().ToArray();
				if (allProjects.Length == 1) {
					return false;
				}
				if (allProjects.Length > 1 && allProjects [0] == project) {
					// If we have more than one project and we are currently on the first
					// then we don't do anything.
	 				// If we are on the 2nd (or higher) project and that has default ports, 
	 				// then we need to change them if necessary. 
	 				// If the 2nd project uses default ports but no other does, then GetNextFreePort will start
					// numbering from 5000 so it will result in no changes being made.
					return false;
				}
				return true;
			}
			return false;
		}

		internal void FixPortNumbers ()
		{
			if (Profiles.ContainsKey (defaultNamespace)) {
				var applicationUrl = DefaultProfile.OtherSettings ["applicationUrl"] as string;

				if (ShouldGenerateNewPort(applicationUrl)) {
					applicationUrl = applicationUrl.Replace (defaultHttpUrl, "http://localhost:" + GetNextFreePort ());
					applicationUrl = applicationUrl.Replace (defaultHttpsUrl, "https://localhost:" + GetNextFreePort ());
					DefaultProfile.OtherSettings ["applicationUrl"] = applicationUrl;
					SaveLaunchSettings ();
				}
			}
		}

		JObject TryParse ()
		{
			try {
				return JObject.Parse (File.ReadAllText (LaunchSettingsJsonPath));
			} catch {
				return new JObject ();
			}
		}

		public void SaveLaunchSettings ()
		{
			var doc = new JObject ();
			var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

			var profilesData = Profiles.ToSerializableForm ();

			ProfilesObject = JObject.Parse (JsonConvert.SerializeObject (profilesData, Formatting.Indented, settings));

			foreach (var set in GlobalSettings) {
				doc.Add (set.Key, set.Value);
			}

			doc.Add ("profiles", ProfilesObject);

			try {
				string jsonDocString = doc.ToString (Formatting.Indented);
				string propertiesDirectory = Path.GetDirectoryName (LaunchSettingsJsonPath);
				Directory.CreateDirectory (propertiesDirectory);

				lock (fileLocker) {
					File.WriteAllText (LaunchSettingsJsonPath, jsonDocString);
				}
			} catch (IOException ioe) {
				string message = GettextCatalog.GetString ("Failed to write {0}", LaunchSettingsJsonPath);
				ReportError (message, ioe);
			} catch (UnauthorizedAccessException uae) {
				string message = GettextCatalog.GetString ("Failed to write {0}. Unable to access file or access is denied", LaunchSettingsJsonPath);
				ReportError (message, uae);
			}
		}

		void ReportError (string message, Exception ex)
		{
			LoggingService.LogError (message, ex);
			MessageService.ShowError (message);
		}

		public LaunchProfileData AddNewProfile (string name)
		{
			if (Profiles == null)
				Profiles = new ConcurrentDictionary<string, LaunchProfileData> ();

			return Profiles [name] = CreateProfile (name);
		}

		public LaunchProfileData CreateDefaultProfile () => CreateProfile (defaultNamespace);

		LaunchProfileData CreateProfile (string name)
		{
			var defaultProfile = new LaunchProfileData {
				Name = name,
				CommandName = "Project",
				LaunchBrowser = true,
				EnvironmentVariables = new Dictionary<string, string> (StringComparer.Ordinal),
				OtherSettings = new Dictionary<string, object> (StringComparer.Ordinal)
			};

			defaultProfile.EnvironmentVariables.Add ("ASPNETCORE_ENVIRONMENT", "Development");
			var anyConfigurationUsesHttps = false;
			foreach (var runConfiguration in project.RunConfigurations) {
				if (AspNetCoreCertificateManager.UsingHttps (runConfiguration)) {
					anyConfigurationUsesHttps = true;
					break;
				}
			}

			string applicationUrl;

			var httpPort = GetNextFreePort ();
			var httpsPort = GetNextFreePort ();
			if (anyConfigurationUsesHttps)
				applicationUrl = $"https://localhost:{httpsPort};http://localhost:{httpPort}";
			else
				applicationUrl = $"http://localhost:{httpPort}";

			defaultProfile.OtherSettings.Add ("applicationUrl", applicationUrl);

			return defaultProfile;
		}

		void CreateAndAddDefaultLaunchSettings ()
		{
			GlobalSettings.Add ("iisSettings", JToken.Parse (DefaultGlobalSettings));
			Profiles = new ConcurrentDictionary<string, LaunchProfileData> ();
			Profiles.TryAdd (defaultNamespace, CreateDefaultProfile ());
			SaveLaunchSettings ();
		}

		/// <summary>
		/// Updates Project.RunConfigurations
		/// </summary>
		internal void SyncRunConfigurations ()
		{
			foreach (var profile in this.Profiles) {

				if (profile.Value.CommandName != "Project")
					continue;

				var key = string.Empty;

				if (profile.Key == project.DefaultNamespace) {
					key = "Default";
				} else {
					key = profile.Key;
				}

				var runConfig = project.RunConfigurations.FirstOrDefault (x => x.Name == key);
				if (runConfig == null) {
					var projectRunConfiguration = new AspNetCoreRunConfiguration (key, profile.Value) {
						StartAction = "Project",
						StoreInUserFile = false
					};
					project.RunConfigurations.Add (projectRunConfiguration);
				} else if (runConfig is AspNetCoreRunConfiguration aspNetCoreRunConfiguration) {
					var index = project.RunConfigurations.IndexOf (runConfig);
					aspNetCoreRunConfiguration.UpdateProfile (profile.Value);
					if (index >= 0) {
						project.RunConfigurations [index] = runConfig;
					}

					Debug.Assert (index >= 0, "Didn't find expected run configuration");
				}
			}

			var itemsRemoved = new RunConfigurationCollection ();

			foreach (var config in project.RunConfigurations) {
				var key = config.Name;

				if (config.Name == "Default") {
					key = project.DefaultNamespace;
				}

				if (Profiles.TryGetValue (key, out _))
					continue;

				itemsRemoved.Add (config);
			}

			project.RunConfigurations.RemoveRange (itemsRemoved);
		}
	}
}
