//
// DotNetCoreRunConfiguration.cs
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
using System.Net;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

namespace MonoDevelop.DotNetCore
{
	public class DotNetCoreRunConfiguration : AssemblyRunConfiguration
	{
		bool webProject;

		public DotNetCoreRunConfiguration (string name)
			: base (name)
		{
		}

		public DotNetCoreRunConfiguration (string name, bool isWeb)
			: base (name)
		{
			webProject = isWeb;
		}

		protected override void Read (IPropertySet pset)
		{
			base.Read (pset);
			ExternalConsole = pset.GetValue (nameof (ExternalConsole), !webProject);
			if (!webProject)
				return;
			if (!pset.HasProperty (nameof (EnvironmentVariables)))
				EnvironmentVariables.Add ("ASPNETCORE_ENVIRONMENT", "Development");
#pragma warning disable CS0618 // Type or member is obsolete
			LaunchBrowser = pset.GetValue (nameof (LaunchBrowser), true);
			ApplicationURL = pset.GetValue (nameof (ApplicationURL), "http://localhost:5000/");
			LaunchUrl = pset.GetValue (nameof (LaunchUrl), null);
#pragma warning restore CS0618 // Type or member is obsolete
		}

		protected override void Write (IPropertySet pset)
		{
			base.Write (pset);
			pset.SetValue (nameof (ExternalConsole), ExternalConsole, !webProject);
			if (!webProject)
				return;
#pragma warning disable CS0618 // Type or member is obsolete
			pset.SetValue (nameof (LaunchBrowser), LaunchBrowser, true);
			pset.SetValue (nameof (LaunchUrl), string.IsNullOrWhiteSpace (LaunchUrl) ? null : LaunchUrl, null);
			pset.SetValue (nameof (ApplicationURL), ApplicationURL, "http://localhost:5000/");
#pragma warning restore CS0618 // Type or member is obsolete
			if (EnvironmentVariables.Count == 1 && EnvironmentVariables.ContainsKey ("ASPNETCORE_ENVIRONMENT") && EnvironmentVariables ["ASPNETCORE_ENVIRONMENT"] == "Development")
				pset.RemoveProperty (nameof (EnvironmentVariables));
		}

		protected override void Initialize (Project project)
		{
			webProject = project.GetFlavor<DotNetCoreProjectExtension> ()?.IsWeb ?? false;
			base.Initialize (project);
			ExternalConsole = !webProject;
			if (!webProject)
				return;
			// Pick up/import default values from "launchSettings.json"
			var launchSettingsJsonPath = Path.Combine (project.BaseDirectory, "Properties", "launchSettings.json");
			var launchSettingsJson = File.Exists (launchSettingsJsonPath) ? JObject.Parse (File.ReadAllText (launchSettingsJsonPath)) : null;
			var settings = (launchSettingsJson?.GetValue ("profiles") as JObject)?.GetValue (project.Name) as JObject;

#pragma warning disable CS0618 // Type or member is obsolete
			LaunchBrowser = settings?.GetValue ("launchBrowser")?.Value<bool?> () ?? true;
			LaunchUrl = settings?.GetValue ("launchUrl")?.Value<string> () ?? null;
			foreach (var pair in (settings?.GetValue ("environmentVariables") as JObject)?.Properties () ?? Enumerable.Empty<JProperty> ()) {
				if (!EnvironmentVariables.ContainsKey (pair.Name))
					EnvironmentVariables.Add (pair.Name, pair.Value.Value<string> ());
			}
			ApplicationURL = settings?.GetValue ("applicationUrl")?.Value<string> () ?? "http://localhost:5000/";
#pragma warning restore CS0618 // Type or member is obsolete
		}

		[Obsolete("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration class")]
		public bool LaunchBrowser { get; set; } = true;

		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration class")]
		public string LaunchUrl { get; set; } = null;

		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration class")]
		public string ApplicationURL { get; set; } = "http://localhost:5000/";

		[ItemProperty (DefaultValue = null)]
		public PipeTransportSettings PipeTransport { get; set; }

		protected override void OnCopyFrom (ProjectRunConfiguration config, bool isRename)
		{
			base.OnCopyFrom (config, isRename);

			var other = (DotNetCoreRunConfiguration)config;

#pragma warning disable CS0618 // Type or member is obsolete
			LaunchBrowser = other.LaunchBrowser;
			LaunchUrl = other.LaunchUrl;
			ApplicationURL = other.ApplicationURL;
#pragma warning restore CS0618 // Type or member is obsolete
			if (other.PipeTransport == null)
				PipeTransport = null;
			else
				PipeTransport = new PipeTransportSettings (other.PipeTransport);
		}
	}

	public class PipeTransportSettings
	{
		public PipeTransportSettings ()
		{ }

		public PipeTransportSettings (PipeTransportSettings copy)
		{
			WorkingDirectory = copy.WorkingDirectory;
			Program = copy.Program;
			Arguments = copy.Arguments.ToArray ();//make copy of array
			DebuggerPath = copy.DebuggerPath;
			EnvironmentVariables = new EnvironmentVariableCollection (copy.EnvironmentVariables);
		}

		[ItemProperty (DefaultValue = null)]
		public string WorkingDirectory { get; set; }
		[ItemProperty (DefaultValue = null)]
		public string Program { get; set; }
		[ItemProperty (SkipEmpty = true)]
		public string [] Arguments { get; set; } = new string [0];
		[ItemProperty (DefaultValue = null)]
		public string DebuggerPath { get; set; }
		[ItemProperty (SkipEmpty = true, WrapObject = false)]
		public EnvironmentVariableCollection EnvironmentVariables { get; private set; } = new EnvironmentVariableCollection ();
	}
}
