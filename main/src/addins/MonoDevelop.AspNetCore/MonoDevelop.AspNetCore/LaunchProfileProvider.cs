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
using System.IO;
using System.Linq;
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
		readonly object fileLocker = new object ();
		public IDictionary<string, JToken> GlobalSettings { get; private set; }
		internal JObject ProfilesObject { get; private set; }
		public ConcurrentDictionary<string, LaunchProfileData> Profiles { get; set; }
		internal string LaunchSettingsJsonPath => Path.Combine (baseDirectory, "Properties", "launchSettings.json");
		const string DefaultGlobalSettings = @"{
    						""windowsAuthentication"": false,
    						""anonymousAuthentication"": true
  							}";

		public LaunchProfileData DefaultProfile {
			get {
				if (!Profiles.ContainsKey (defaultNamespace)) {
					var defaultProfile = CreateDefaultProfile ();
					Profiles [defaultNamespace] = defaultProfile;
					return defaultProfile;
				}
				return Profiles [defaultNamespace];
			}
		}

		public LaunchProfileProvider (string baseDirectory, string defaultNamespace)
		{
			this.baseDirectory = baseDirectory;
			this.defaultNamespace = defaultNamespace;
			GlobalSettings = new Dictionary<string, JToken> ();
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

			var newProfile = CreateProfile (name);
			Profiles [name] = newProfile;
			return newProfile;
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
			defaultProfile.OtherSettings.Add ("applicationUrl", "https://localhost:5001;http://localhost:5000");

			return defaultProfile;
		}

		void CreateAndAddDefaultLaunchSettings ()
		{
			GlobalSettings.Add ("iisSettings", JToken.Parse (DefaultGlobalSettings));
			var profiles = new Dictionary<string, LaunchProfileData> {
				{ defaultNamespace, CreateDefaultProfile () }
			};
			Profiles = new ConcurrentDictionary<string, LaunchProfileData> (profiles);
			SaveLaunchSettings ();
		}

		/// <summary>
		/// Updates Project.RunConfigurations
		/// </summary>
		/// <param name="project"></param>
		internal void SyncRunConfigurations (DotNetProject project)
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
					project.RunConfigurations [index] = runConfig;
				}
			}

			var itemsRemoved = new RunConfigurationCollection ();

			foreach (var config in project.RunConfigurations) {
				var key = config.Name;

				if (config.Name == "Default") {
					key = project.DefaultNamespace;
				}

				if (Profiles.TryGetValue (key, out var _))
					continue;

				itemsRemoved.Add (config);
			}

			project.RunConfigurations.RemoveRange (itemsRemoved);
		}
	}
}
