// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
// https://github.com/dotnet/project-system/blob/master/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/ILaunchProfile.cs

using System.Collections.Immutable;

namespace MonoDevelop.AspNetCore
{
	public interface ILaunchProfile
	{
		string Name { get; }
		string CommandName { get; }
		string ExecutablePath { get; }
		string CommandLineArgs { get; }
		string WorkingDirectory { get; }
		bool LaunchBrowser { get; }
		string LaunchUrl { get; }
		ImmutableDictionary<string, string> EnvironmentVariables { get; }
		ImmutableDictionary<string, object> OtherSettings { get; }
	}
}
