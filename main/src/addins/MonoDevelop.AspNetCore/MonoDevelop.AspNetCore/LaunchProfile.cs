
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
// https://github.com/dotnet/project-system/blob/master/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/LaunchProfile.cs

using System;
using System.Collections.Immutable;

namespace MonoDevelop.AspNetCore
{
	internal class LaunchProfile : ILaunchProfile
	{
		public LaunchProfile ()
		{
		}

		public LaunchProfile (LaunchProfileData data)
		{
			Name = data.Name;
			ExecutablePath = data.ExecutablePath;
			CommandName = data.CommandName;
			CommandLineArgs = data.CommandLineArgs;
			WorkingDirectory = data.WorkingDirectory;
			LaunchBrowser = data.LaunchBrowser ?? false;
			LaunchUrl = data.LaunchUrl;
			EnvironmentVariables = data.EnvironmentVariables?.ToImmutableDictionary ();
			OtherSettings = data.OtherSettings?.ToImmutableDictionary ();
			DoNotPersist = data.InMemoryProfile;
		}


		/// <summary>
		/// Useful to create a mutable version from an existing immutable profile
		/// </summary>
		public LaunchProfile (ILaunchProfile existingProfile)
		{
			Name = existingProfile.Name;
			ExecutablePath = existingProfile.ExecutablePath;
			CommandName = existingProfile.CommandName;
			CommandLineArgs = existingProfile.CommandLineArgs;
			WorkingDirectory = existingProfile.WorkingDirectory;
			LaunchBrowser = existingProfile.LaunchBrowser;
			LaunchUrl = existingProfile.LaunchUrl;
			EnvironmentVariables = existingProfile.EnvironmentVariables;
			OtherSettings = existingProfile.OtherSettings;
			//DoNotPersist = existingProfile.IsInMemoryObject ();
		}

		public string Name { get; set; }
		public string CommandName { get; set; }
		public string ExecutablePath { get; set; }
		public string CommandLineArgs { get; set; }
		public string WorkingDirectory { get; set; }
		public bool LaunchBrowser { get; set; }
		public string LaunchUrl { get; set; }
		public bool DoNotPersist { get; set; }

		public ImmutableDictionary<string, string> EnvironmentVariables { get; set; }
		public ImmutableDictionary<string, object> OtherSettings { get; set; }

		/// <summary>
		/// Compares two profile names. Using this function ensures case comparison consistency
		/// </summary>
		public static bool IsSameProfileName (string name1, string name2)
		{
			return string.Equals (name1, name2, StringComparison.Ordinal);
		}
	}
}
