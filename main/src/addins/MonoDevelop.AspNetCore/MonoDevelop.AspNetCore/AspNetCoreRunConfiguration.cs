//
// AspNetCoreRunConfiguration.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using MonoDevelop.Core.Serialization;
using MonoDevelop.DotNetCore;
using MonoDevelop.Projects;
using Newtonsoft.Json.Linq;

namespace MonoDevelop.AspNetCore
{
	public class AspNetCoreRunConfiguration : AssemblyRunConfiguration
	{
		readonly IDictionary<string, JToken> globalSettings = new Dictionary<string, JToken> ();
		LaunchProfileProvider launchProfileProvider;

		[ItemProperty (DefaultValue = null)]
		public PipeTransportSettings PipeTransport { get; set; }
		internal Dictionary<string, LaunchProfileData> Profiles { get; set; }
		internal string ActiveProfile { get; set; }
		internal LaunchProfileData CurrentProfile => Profiles [ActiveProfile];

		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration.CurrentProfile property")]
		public bool LaunchBrowser { get; set; } = true;
		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration.CurrentProfile property")]
		public string LaunchUrl { get; set; } = null;
		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration.CurrentProfile property")]
		public string ApplicationURL { get; set; } = "http://localhost:5000/";

		public AspNetCoreRunConfiguration (string name)
			: base (name)
		{
		}

		public void LoadLaunchSettings (DotNetProject project)
		{
			launchProfileProvider = new LaunchProfileProvider (project);
			launchProfileProvider.LoadLaunchSettings ();

			InitializeLaunchSettings (project.DefaultNamespace);
		}

		void InitializeLaunchSettings (string name)
		{
			Profiles = LaunchProfileData.DeserializeProfiles (launchProfileProvider.ProfilesObject);

			//we assume that the project.Name is the default profile
			ActiveProfile = Profiles.FirstOrDefault (x => x.Key == name).Key;

			if (ActiveProfile == null) //otherwise the first "Project" one 
				ActiveProfile = Profiles.FirstOrDefault (p => p.Value.CommandName == "Project").Key;

			//if it does not exist, we create a new one
			if (string.IsNullOrEmpty (ActiveProfile)) {
				var newProfile = launchProfileProvider.CreateDefaultProfile ();
				Profiles.Add (name, newProfile);
				ActiveProfile = newProfile.Name;
			}

			if (CurrentProfile.OtherSettings == null)
				CurrentProfile.OtherSettings = new Dictionary<string, object> (StringComparer.Ordinal);

			if (CurrentProfile.EnvironmentVariables == null)
				CurrentProfile.EnvironmentVariables = new Dictionary<string, string> (StringComparer.Ordinal);

			EnvironmentVariables.Clear ();
			LoadEnvVariables ();
		}

		internal void RefreshLaunchSettings (string name)
		{
			if (launchProfileProvider == null)
				return;

			launchProfileProvider.LoadLaunchSettings ();
			InitializeLaunchSettings (name);
		}

		public string GetApplicationUrl ()
		{
			var applicationUrl = CurrentProfile.TryGetOtherSettings<string> ("applicationUrl");
			if (applicationUrl != null)
				return applicationUrl;

			return "http://localhost:5000";
		}

		protected override void Read (IPropertySet pset)
		{
			base.Read (pset);

			LoadEnvVariables ();

			ExternalConsole = pset.GetValue (nameof (ExternalConsole), false);
		}

		void LoadEnvVariables ()
		{
			foreach (var pair in Profiles [ActiveProfile].EnvironmentVariables) {
				if (!EnvironmentVariables.ContainsKey (pair.Key))
					EnvironmentVariables.Add (pair.Key, pair.Value);
				else
					EnvironmentVariables [pair.Key] = pair.Value;
			}
		}

		protected override void Write (IPropertySet pset)
		{
			base.Write (pset);

			pset.SetValue (nameof (ExternalConsole), ExternalConsole, false);
			if (EnvironmentVariables.Count == 1 && EnvironmentVariables.ContainsKey ("ASPNETCORE_ENVIRONMENT") && EnvironmentVariables ["ASPNETCORE_ENVIRONMENT"] == "Development")
				pset.RemoveProperty (nameof (EnvironmentVariables));

			if (ActiveProfile == null) {
				return;
			}

			Profiles [ActiveProfile].EnvironmentVariables.Clear ();
			foreach (var pair in EnvironmentVariables) {
					Profiles [ActiveProfile].EnvironmentVariables [pair.Key] = pair.Value;
			}

			launchProfileProvider.SaveLaunchSettings (Profiles.ToSerializableForm ());
		}

		protected override void OnCopyFrom (ProjectRunConfiguration config, bool isRename)
		{
			base.OnCopyFrom (config, isRename);

			var other = (AspNetCoreRunConfiguration)config;

			CurrentProfile.LaunchBrowser = other.CurrentProfile.LaunchBrowser;
			CurrentProfile.LaunchUrl = other.CurrentProfile.LaunchUrl;
			var applicationUrl = other.CurrentProfile.TryGetOtherSettings<string> ("applicationUrl");
			if (!string.IsNullOrEmpty ("applicationUrl"))
				CurrentProfile.OtherSettings ["applicationUrl"] = applicationUrl;

			if (other.PipeTransport == null)
				PipeTransport = null;
			else
				PipeTransport = new PipeTransportSettings (other.PipeTransport);
		}

		internal bool UsingHttps ()
		{
			var applicationUrl = CurrentProfile.TryGetOtherSettings<string> ("applicationUrl");

			if (!string.IsNullOrEmpty (applicationUrl)) {
				return applicationUrl.IndexOf ("https://", StringComparison.OrdinalIgnoreCase) >= 0;
			}

			var environmentVariables = (IDictionary<string, string>)EnvironmentVariables;

			if (environmentVariables.TryGetValue ("ASPNETCORE_URLS", out string applicationUrls)) {
				if (applicationUrls != null) {
					return applicationUrls.IndexOf ("https://", StringComparison.OrdinalIgnoreCase) >= 0;
				}
			}

			return false;
		}
	}
}
