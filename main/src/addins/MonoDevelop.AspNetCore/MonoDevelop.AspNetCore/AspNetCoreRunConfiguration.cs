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
using MonoDevelop.Core.Serialization;
using MonoDevelop.DotNetCore;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore
{
	public class AspNetCoreRunConfiguration : AssemblyRunConfiguration
	{
		public bool IsDefault => Name == "Default";

		[ItemProperty (DefaultValue = null)]
		public PipeTransportSettings PipeTransport { get; set; }

		internal LaunchProfileData CurrentProfile { get; private set; }

		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration.CurrentProfile property")]
		public bool LaunchBrowser { get; set; } = true;
		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration.CurrentProfile property")]
		public string LaunchUrl { get; set; } = null;
		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration.CurrentProfile property")]
		public string ApplicationURL { get; set; } = "http://localhost:5000/";

		public bool IsDirty { get; set; } = true;

		internal event EventHandler SaveRequested;

		public AspNetCoreRunConfiguration (string name, LaunchProfileData profile)
			: base (name)
		{
			CurrentProfile = profile;

			InitializeLaunchSettings ();
		}

		public AspNetCoreRunConfiguration (string name)
			: base (name)
		{
		}

		internal void InitializeLaunchSettings ()
		{
			if (CurrentProfile.OtherSettings == null)
				CurrentProfile.OtherSettings = new Dictionary<string, object> (StringComparer.Ordinal);

			if (CurrentProfile.EnvironmentVariables == null)
				CurrentProfile.EnvironmentVariables = new Dictionary<string, string> (StringComparer.Ordinal);

			LoadEnvVariables ();
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

			// read from run config if CurrentProfile.* is empty, for backward compatibility
			if (!pset.HasProperty (nameof (EnvironmentVariables)) && EnvironmentVariables.Count == 0)
				EnvironmentVariables.Add ("ASPNETCORE_ENVIRONMENT", "Development");
#pragma warning disable CS0618 //disables warnings threw by obsolete methods used in nameof()
			if (CurrentProfile.LaunchBrowser == null)
				CurrentProfile.LaunchBrowser = pset.GetValue (nameof (LaunchBrowser), true);
			if (string.IsNullOrEmpty (CurrentProfile.TryGetApplicationUrl ())) {

				if (CurrentProfile.OtherSettings == null)
					CurrentProfile.OtherSettings = new Dictionary<string, object> (StringComparer.Ordinal);

				CurrentProfile.OtherSettings ["applicationUrl"] = pset.GetValue (nameof (ApplicationURL), "http://localhost:5000/"); 
			}
			if (string.IsNullOrEmpty (CurrentProfile.LaunchUrl))
				CurrentProfile.LaunchUrl = pset.GetValue (nameof (LaunchUrl), null);
#pragma warning restore CS0618
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

			OnSaveRequested ();

#pragma warning disable CS0618
			// persist values to Run Configuration for backward compatibility
			pset.SetValue (nameof (LaunchBrowser), CurrentProfile.LaunchBrowser, true);
			pset.SetValue (nameof (LaunchUrl), string.IsNullOrWhiteSpace (CurrentProfile.LaunchUrl) ? null : CurrentProfile.LaunchUrl, null);
			var appUrl = CurrentProfile.TryGetApplicationUrl ();
			if (!string.IsNullOrEmpty (appUrl))
				pset.SetValue (nameof (ApplicationURL), appUrl, "http://localhost:5000/");
#pragma warning restore CS0618

			IsDirty = false;
		}

		protected override void OnCopyFrom (ProjectRunConfiguration config, bool isRename)
		{
			base.OnCopyFrom (config, isRename);

			var other = (AspNetCoreRunConfiguration)config;

			CurrentProfile.LaunchBrowser = other.CurrentProfile.LaunchBrowser ?? true;
			CurrentProfile.LaunchUrl = other.CurrentProfile.LaunchUrl;
			var applicationUrl = other.CurrentProfile.TryGetApplicationUrl ();
			if (!string.IsNullOrEmpty (applicationUrl)) {

				if (CurrentProfile.OtherSettings == null)
					CurrentProfile.OtherSettings = new Dictionary<string, object> (StringComparer.Ordinal);

				CurrentProfile.OtherSettings ["applicationUrl"] = applicationUrl;
			}

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

		public void OnSaveRequested () => SaveRequested?.Invoke (this, EventArgs.Empty);

		internal void UpdateProfile (LaunchProfileData launchProfile)
		{
			CurrentProfile = launchProfile;
			if (CurrentProfile.EnvironmentVariables == null)
				CurrentProfile.EnvironmentVariables = new Dictionary<string, string> (StringComparer.Ordinal);
			LoadEnvVariables ();
		}
	}
}
