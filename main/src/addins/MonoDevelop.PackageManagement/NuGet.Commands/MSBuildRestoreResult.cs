// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using MonoDevelop.Core;

namespace NuGet.Commands
{
	/// <summary>
	/// TODO: Look at removing this class and instead just using the existing PackageReference support.
	/// </summary>
	class MSBuildRestoreResult
	{
		/// <summary>
		/// The macros that we may use in MSBuild to replace path roots.
		/// </summary>
		private static readonly string[] MacroCandidates = new[]
		{
			"UserProfile", // e.g. C:\users\myusername
		};

		/// <summary>
		/// Gets a boolean indicating if the necessary MSBuild file could be generated
		/// </summary>
		public bool Success { get; }

		public string ProjectName { get; }
		public string ProjectDirectory { get; }

		/// <summary>
		/// Gets the root of the repository containing packages with MSBuild files
		/// </summary>
		public string RepositoryRoot { get; }

		/// <summary>
		/// Gets a list of MSBuild props files provided by packages during this restore
		/// </summary>
		public IEnumerable<string> Props { get; }

		/// <summary>
		/// Gets a list of MSBuild targets files provided by packages during this restore
		/// </summary>
		public IEnumerable<string> Targets { get; }

		public MSBuildRestoreResult(string projectName, string projectDirectory, bool success)
		{
			Success = success;
			ProjectName = projectName;
			ProjectDirectory = projectDirectory;
			RepositoryRoot = string.Empty;
			Props = Enumerable.Empty<string>();
			Targets = Enumerable.Empty<string>();
		}

		public MSBuildRestoreResult(string projectName,
			string projectDirectory,
			string repositoryRoot,
			IEnumerable<string> props,
			IEnumerable<string> targets)
		{
			Success = true;
			ProjectName = projectName;
			ProjectDirectory = projectDirectory;
			RepositoryRoot = repositoryRoot;
			Props = props;
			Targets = targets;
		}

		public void Commit(NuGet.Common.ILogger log)
		{
			if (!Success)
			{
				var name = $"{ProjectName}.nuget.targets";
				var path = Path.Combine(ProjectDirectory, name);

				log.LogMinimal(GettextCatalog.GetString ("Generating MSBuild file {0}.", name));
				GenerateMSBuildErrorFile(path);
			}
			else
			{
				// Generate the files as needed
				var targetsName = $"{ProjectName}.nuget.targets";
				var propsName = $"{ProjectName}.nuget.props";
				var targetsPath = Path.Combine(ProjectDirectory, targetsName);
				var propsPath = Path.Combine(ProjectDirectory, propsName);

				if (Targets.Any())
				{
					log.LogMinimal(GettextCatalog.GetString ("Generating MSBuild file {0}.", targetsName));

					GenerateImportsFile(targetsPath, Targets);
				}
				else if (File.Exists(targetsPath))
				{
					File.Delete(targetsPath);
				}

				if (Props.Any())
				{
					log.LogMinimal(GettextCatalog.GetString ("Generating MSBuild file {0}.", propsName));

					GenerateImportsFile(propsPath, Props);
				}
				else if (File.Exists(propsPath))
				{
					File.Delete(propsPath);
				}
			}
		}

		private static string ReplacePathsWithMacros(string path)
		{
			foreach (var macroName in MacroCandidates)
			{
				string macroValue = Environment.GetEnvironmentVariable(macroName);
				if (!string.IsNullOrEmpty(macroValue) 
				    && path.StartsWith(macroValue, StringComparison.OrdinalIgnoreCase))
				{
					path = $"$({macroName})" + $"{path.Substring(macroValue.Length)}";
				}

				break;
			}

			return path;
		}

		private void GenerateMSBuildErrorFile(string path)
		{
			string warning = GettextCatalog.GetString ("Packages containing MSBuild targets and props files cannot be fully installed in projects targeting multiple frameworks. The MSBuild targets and props files have been ignored.");
			var ns = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");
			var doc = new XDocument(
				new XDeclaration("1.0", "utf-8", "no"),

				new XElement(ns + "Project",
					new XAttribute("ToolsVersion", "14.0"),

						new XElement(ns + "Target",
							new XAttribute("Name", "EmitMSBuildWarning"),
							new XAttribute("BeforeTargets", "Build"),

							new XElement(ns + "Warning",
								new XAttribute("Text", warning)))));

			using (var output = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
			{
				doc.Save(output);
			}
		}

		private void GenerateImportsFile(string path, IEnumerable<string> imports)
		{
			var ns = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");
			var doc = new XDocument(
				new XDeclaration("1.0", "utf-8", "no"),

				new XElement(ns + "Project",
					new XAttribute("ToolsVersion", "14.0"),

					new XElement(ns + "PropertyGroup",
						new XAttribute("Condition", "'$(NuGetPackageRoot)' == ''"),

						new XElement(ns + "NuGetPackageRoot", ReplacePathsWithMacros(RepositoryRoot))),
					new XElement(ns + "ImportGroup", imports.Select(i =>
						new XElement(ns + "Import",
							new XAttribute("Project", GetImportPath(i)),
							new XAttribute("Condition", $"Exists('{GetImportPath(i)}')"))))));

			using (var output = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
			{
				doc.Save(output);
			}
		}

		private string GetImportPath(string importPath)
		{
			var path = importPath;

			if (importPath.StartsWith(RepositoryRoot, StringComparison.Ordinal))
			{
				path = $"$(NuGetPackageRoot){importPath.Substring(RepositoryRoot.Length)}";
			}
			else
			{
				path = ReplacePathsWithMacros(importPath);
			}

			return path;
		}
	}
}