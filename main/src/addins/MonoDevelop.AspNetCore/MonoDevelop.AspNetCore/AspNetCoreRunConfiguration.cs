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
using System.IO;
using System.Linq;
using MonoDevelop.Core.Serialization;
using MonoDevelop.DotNetCore;
using MonoDevelop.Projects;
using Newtonsoft.Json.Linq;

namespace MonoDevelop.AspNetCore
{
	public class AspNetCoreRunConfiguration : AssemblyRunConfiguration
	{
		public AspNetCoreRunConfiguration (string name)
			: base (name)
		{
		}

		public bool LaunchBrowser { get; set; } = true;

		public string LaunchUrl { get; set; } = null;

		public string ApplicationURL { get; set; } = "http://localhost:5000/";

		[ItemProperty (DefaultValue = null)]
		public PipeTransportSettings PipeTransport { get; set; }

		protected override void Initialize (Project project)
		{
			base.Initialize (project);
			ExternalConsole = false;
			// Pick up/import default values from "launchSettings.json"
			var launchSettingsJsonPath = Path.Combine (project.BaseDirectory, "Properties", "launchSettings.json");
			var launchSettingsJson = File.Exists (launchSettingsJsonPath) ? JObject.Parse (File.ReadAllText (launchSettingsJsonPath)) : null;
			var settings = (launchSettingsJson?.GetValue ("profiles") as JObject)?.GetValue (project.Name) as JObject;

			LaunchBrowser = settings?.GetValue ("launchBrowser")?.Value<bool?> () ?? true;
			LaunchUrl = settings?.GetValue ("launchUrl")?.Value<string> () ?? null;
			foreach (var pair in (settings?.GetValue ("environmentVariables") as JObject)?.Properties () ?? Enumerable.Empty<JProperty> ()) {
				if (!EnvironmentVariables.ContainsKey (pair.Name))
					EnvironmentVariables.Add (pair.Name, pair.Value.Value<string> ());
			}
			ApplicationURL = GetApplicationUrl (settings, EnvironmentVariables);
		}

		static string GetApplicationUrl (JObject settings, IDictionary<string, string> environmentVariables)
		{
			var applicationUrl = settings?.GetValue ("applicationUrl")?.Value<string> ();
			if (applicationUrl != null)
				return applicationUrl;

			if (environmentVariables.TryGetValue ("ASPNETCORE_URLS", out string applicationUrls)) {
				applicationUrl = applicationUrls.Split (';').FirstOrDefault ();
				if (applicationUrl != null)
					return applicationUrl;
			}

			return "http://localhost:5000";
		}

		protected override void Read (IPropertySet pset)
		{
			base.Read (pset);
			ExternalConsole = pset.GetValue (nameof (ExternalConsole), false);
			if (!pset.HasProperty (nameof (EnvironmentVariables)))
				EnvironmentVariables.Add ("ASPNETCORE_ENVIRONMENT", "Development");
			LaunchBrowser = pset.GetValue (nameof (LaunchBrowser), true);
			ApplicationURL = pset.GetValue (nameof (ApplicationURL), "http://localhost:5000/");
			LaunchUrl = pset.GetValue (nameof (LaunchUrl), null);
		}

		protected override void Write (IPropertySet pset)
		{
			base.Write (pset);
			pset.SetValue (nameof (ExternalConsole), ExternalConsole, false);
			pset.SetValue (nameof (LaunchBrowser), LaunchBrowser, true);
			pset.SetValue (nameof (LaunchUrl), string.IsNullOrWhiteSpace (LaunchUrl) ? null : LaunchUrl, null);
			pset.SetValue (nameof (ApplicationURL), ApplicationURL, "http://localhost:5000/");
			if (EnvironmentVariables.Count == 1 && EnvironmentVariables.ContainsKey ("ASPNETCORE_ENVIRONMENT") && EnvironmentVariables ["ASPNETCORE_ENVIRONMENT"] == "Development")
				pset.RemoveProperty (nameof (EnvironmentVariables));
		}

		protected override void OnCopyFrom (ProjectRunConfiguration config, bool isRename)
		{
			base.OnCopyFrom (config, isRename);

			var other = (AspNetCoreRunConfiguration)config;

			LaunchBrowser = other.LaunchBrowser;
			LaunchUrl = other.LaunchUrl;
			ApplicationURL = other.ApplicationURL;
			if (other.PipeTransport == null)
				PipeTransport = null;
			else
				PipeTransport = new PipeTransportSettings (other.PipeTransport);
		}
	}
}
