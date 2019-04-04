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
		internal LaunchProfileProvider launchProfileProvider;
		readonly string projectName = string.Empty;

		[ItemProperty (DefaultValue = null)]
		public PipeTransportSettings PipeTransport { get; set; }
		internal Dictionary<string, LaunchProfileData> Profiles { get; set; }
		internal string ActiveProfile { get; set; }
		internal LaunchProfileData CurrentProfile {
			get {
				if ((string.IsNullOrEmpty (ActiveProfile) && !SetActiveProfile ()) || !Profiles.ContainsKey (ActiveProfile)) {
					return CreateAndAddDefaultProfile ();
				}

				return Profiles [ActiveProfile];
			}
		}

		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration.CurrentProfile property")]
		public bool LaunchBrowser { get; set; } = true;
		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration.CurrentProfile property")]
		public string LaunchUrl { get; set; } = null;
		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration.CurrentProfile property")]
		public string ApplicationURL { get; set; } = "http://localhost:5000/";

		public AspNetCoreRunConfiguration (string name, DotNetProject project)
			: base (name)
		{
			Initialize ();

			projectName = project.DefaultNamespace;

			launchProfileProvider = new LaunchProfileProvider (project);
			launchProfileProvider.LoadLaunchSettings ();

			InitializeLaunchSettings ();
		}

		public AspNetCoreRunConfiguration (string name)
			: base (name)
		{
			Initialize ();
		}

		void Initialize ()
		{
			Profiles = new Dictionary<string, LaunchProfileData> ();
			ActiveProfile = string.Empty;
		}

		bool SetActiveProfile ()
		{
			//we assume that the project.Name is the default profile
			ActiveProfile = Profiles.FirstOrDefault (x => x.Key == projectName).Key;

			if (string.IsNullOrEmpty (ActiveProfile)) //otherwise the first "Project" one 
				ActiveProfile = Profiles.FirstOrDefault (p => p.Value.CommandName == "Project").Key;

			//if it does not exist, we create a new one
			if (string.IsNullOrEmpty (ActiveProfile)) {
				return false;
			}

			return true;
		}

		LaunchProfileData CreateAndAddDefaultProfile ()
		{
			var newProfile = launchProfileProvider.CreateDefaultProfile ();
			Profiles.Add (newProfile.Name, newProfile);
			ActiveProfile = newProfile.Name;

			return Profiles [ActiveProfile];
		}

		internal void InitializeLaunchSettings ()
		{
			if (launchProfileProvider == null)
				return;

			Profiles = LaunchProfileData.DeserializeProfiles (launchProfileProvider.ProfilesObject);

			ActiveProfile = string.Empty;

			if (CurrentProfile.OtherSettings == null)
				CurrentProfile.OtherSettings = new Dictionary<string, object> (StringComparer.Ordinal);

			if (CurrentProfile.EnvironmentVariables == null)
				CurrentProfile.EnvironmentVariables = new Dictionary<string, string> (StringComparer.Ordinal);

			LoadEnvVariables ();
		}

		internal void RefreshLaunchSettings ()
		{
			if (launchProfileProvider == null)
				return;

			launchProfileProvider.LoadLaunchSettings ();
			InitializeLaunchSettings ();
		}

		public string GetApplicationUrl ()
		{
			var applicationUrl = CurrentProfile.TryGetApplicationUrl ();
			if (applicationUrl != null)
				return applicationUrl;

			return "http://localhost:5000";
		}

		protected override void Read (IPropertySet pset)
		{
			base.Read (pset);
			ExternalConsole = pset.GetValue (nameof (ExternalConsole), false);
		}

		void LoadEnvVariables ()
		{
			EnvironmentVariables.Clear ();
			foreach (var pair in CurrentProfile.EnvironmentVariables) {
					EnvironmentVariables [pair.Key] = pair.Value;
			}
		}

		protected override void Write (IPropertySet pset)
		{
			base.Write (pset); 

			pset.SetValue (nameof (ExternalConsole), ExternalConsole, false);
			if (EnvironmentVariables.Count == 1 && EnvironmentVariables.ContainsKey ("ASPNETCORE_ENVIRONMENT") && EnvironmentVariables ["ASPNETCORE_ENVIRONMENT"] == "Development")
				pset.RemoveProperty (nameof (EnvironmentVariables));

			if (CurrentProfile == null) {
				return;
			}

			CurrentProfile.EnvironmentVariables.Clear ();
			foreach (var pair in EnvironmentVariables) {
					CurrentProfile.EnvironmentVariables [pair.Key] = pair.Value;
			}

			launchProfileProvider?.SaveLaunchSettings (Profiles.ToSerializableForm ());
		}

		protected override void OnCopyFrom (ProjectRunConfiguration config, bool isRename)
		{
			base.OnCopyFrom (config, isRename);

			var other = (AspNetCoreRunConfiguration)config;

			CurrentProfile.LaunchBrowser = other.CurrentProfile.LaunchBrowser;
			CurrentProfile.LaunchUrl = other.CurrentProfile.LaunchUrl;
			var applicationUrl = other.CurrentProfile.TryGetApplicationUrl ();
			if (!string.IsNullOrEmpty (applicationUrl))
				CurrentProfile.OtherSettings ["applicationUrl"] = applicationUrl;

			if (other.PipeTransport == null)
				PipeTransport = null;
			else
				PipeTransport = new PipeTransportSettings (other.PipeTransport);
		}

		internal bool UsingHttps ()
		{
			var applicationUrl = CurrentProfile.TryGetApplicationUrl ();

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
