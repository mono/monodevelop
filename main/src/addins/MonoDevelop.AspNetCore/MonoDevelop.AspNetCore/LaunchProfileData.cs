//
// AspNetCoreRunConfiguration.cs
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
// https://github.com/dotnet/project-system/blob/master/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/LaunchProfileData.cs

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonoDevelop.AspNetCore
{
	[JsonObject (MemberSerialization.OptIn)]
	internal class LaunchProfileData
	{
		// Well known properties
		private const string Prop_commandName = "commandName";
		private const string Prop_executablePath = "executablePath";
		private const string Prop_commandLineArgs = "commandLineArgs";
		private const string Prop_workingDirectory = "workingDirectory";
		private const string Prop_launchBrowser = "launchBrowser";
		private const string Prop_launchUrl = "launchUrl";
		private const string Prop_environmentVariables = "environmentVariables";

		private static readonly HashSet<string> s_knownProfileProperties = new HashSet<string> (StringComparer.Ordinal)
		{
			{Prop_commandName},
			{Prop_executablePath},
			{Prop_commandLineArgs},
			{Prop_workingDirectory},
			{Prop_launchBrowser},
			{Prop_launchUrl},
			{Prop_environmentVariables},
		};

		public static bool IsKnownProfileProperty (string propertyName)
		{
			return s_knownProfileProperties.Contains (propertyName);
		}

		// We don't serialize the name as it the dictionary index
		public string Name { get; set; }

		// Or serialize the InMemoryProfile state
		public bool InMemoryProfile { get; set; }

		[JsonProperty (PropertyName = Prop_commandName)]
		public string CommandName { get; set; }

		[JsonProperty (PropertyName = Prop_executablePath)]
		public string ExecutablePath { get; set; }

		[JsonProperty (PropertyName = Prop_commandLineArgs)]
		public string CommandLineArgs { get; set; }

		[JsonProperty (PropertyName = Prop_workingDirectory)]
		public string WorkingDirectory { get; set; }

		[JsonProperty (PropertyName = Prop_launchBrowser)]
		public bool? LaunchBrowser { get; set; }

		[JsonProperty (PropertyName = Prop_launchUrl)]
		public string LaunchUrl { get; set; }

		[JsonProperty (PropertyName = Prop_environmentVariables)]
		public IDictionary<string, string> EnvironmentVariables { get; set; }

		public IDictionary<string, object> OtherSettings { get; set; }

		/// <summary>
		/// To handle custom settings, we serialize using LaunchProfileData first and then walk the settings
		/// to pick up other settings. Currently limited to boolean, integer, string and dictionary of string
		/// </summary>
		public static Dictionary<string, LaunchProfileData> DeserializeProfiles (JObject profilesObject)
		{
			var profiles = new Dictionary<string, LaunchProfileData> (StringComparer.Ordinal);

			if (profilesObject == null) {
				return profiles;
			}

			// We walk the profilesObject and serialize each subobject component as either a string, or a dictionary<string,string>
			foreach (var profile in profilesObject) {

				var jToken = profile.Value;
				var key = profile.Key;
				// Name of profile is the key, value is it's contents. We have specific serializing of the data based on the 
				// JToken type
				LaunchProfileData profileData = JsonConvert.DeserializeObject<LaunchProfileData> (jToken.ToString ());

				// Now pick up any custom properties. Handle string, int, boolean
				var customSettings = new Dictionary<string, object> (StringComparer.Ordinal);
				foreach (JToken data in jToken.Children ()) {
					if (!(data is JProperty dataProperty)) {
						continue;
					}
					if (!IsKnownProfileProperty (dataProperty.Name)) {
						try {
							switch (dataProperty.Value.Type) {
							case JTokenType.Boolean: {
									bool value = bool.Parse (dataProperty.Value.ToString ());
									customSettings.Add (dataProperty.Name, value);
									break;
								}
							case JTokenType.Integer: {
									int value = int.Parse (dataProperty.Value.ToString ());
									customSettings.Add (dataProperty.Name, value);
									break;
								}
							case JTokenType.Object: {
									Dictionary<string, string> value = JsonConvert.DeserializeObject<Dictionary<string, string>> (dataProperty.Value.ToString ());
									customSettings.Add (dataProperty.Name, value);
									break;
								}
							case JTokenType.String: {
									customSettings.Add (dataProperty.Name, dataProperty.Value.ToString ());
									break;
								}
							default: {
									break;
								}
							}
						} catch (Exception e) {
							// TODO: should have message indicating the setting is being ignored. Fix as part of issue
							//       https://github.com/dotnet/roslyn-project-system/issues/424
						}
					}
				}

				// Only add custom settings if we actually picked some up
				if (customSettings.Count > 0) {
					profileData.OtherSettings = customSettings;
				}

				profiles.Add (key, profileData);
			}

			return profiles;
		}

		/// <summary>
		/// Helper to convert an ILaunchProfile back to its serializable form. Basically, it
		/// converts it to a dictionary of settings. This preserves custom values
		/// </summary>
		internal static Dictionary<string, object> ToSerializableForm (ILaunchProfile profile)
		{
			var data = new Dictionary<string, object> (StringComparer.Ordinal);

			// Don't write out empty elements
			if (!string.IsNullOrEmpty (profile.CommandName)) {
				data.Add (Prop_commandName, profile.CommandName);
			}

			if (!string.IsNullOrEmpty (profile.ExecutablePath)) {
				data.Add (Prop_executablePath, profile.ExecutablePath);
			}

			if (!string.IsNullOrEmpty (profile.CommandLineArgs)) {
				data.Add (Prop_commandLineArgs, profile.CommandLineArgs);
			}

			if (!string.IsNullOrEmpty (profile.WorkingDirectory)) {
				data.Add (Prop_workingDirectory, profile.WorkingDirectory);
			}

			if (profile.LaunchBrowser) {
				data.Add (Prop_launchBrowser, profile.LaunchBrowser);
			}

			if (!string.IsNullOrEmpty (profile.LaunchUrl)) {
				data.Add (Prop_launchUrl, profile.LaunchUrl);
			}

			if (profile.EnvironmentVariables != null) {
				data.Add (Prop_environmentVariables, profile.EnvironmentVariables);
			}

			if (profile.OtherSettings != null) {
				foreach (var prof in profile.OtherSettings) {
					data.Add (prof.Key, prof.Value);
				}
			}

			return data;
		}

		/// <summary>
		/// Helper to convert an ILaunchProfile back to its serializable form. It does some
		/// fixup. Like setting empty values to null.
		/// </summary>
		internal static LaunchProfileData FromILaunchProfile (ILaunchProfile profile)
		{
			return new LaunchProfileData () {
				Name = profile.Name,
				ExecutablePath = profile.ExecutablePath,
				CommandName = profile.CommandName,
				CommandLineArgs = profile.CommandLineArgs,
				WorkingDirectory = profile.WorkingDirectory,
				LaunchBrowser = profile.LaunchBrowser,
				LaunchUrl = profile.LaunchUrl,
				EnvironmentVariables = profile.EnvironmentVariables,
				OtherSettings = profile.OtherSettings,
				//InMemoryProfile = profile.IsInMemoryObject ()
			};
		}
	}
}
