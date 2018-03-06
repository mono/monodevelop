//
// MicrosoftTemplateEngineItemTemplatingProvider.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Template;
using Mono.Addins;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Projects;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Templates
{
	class MicrosoftTemplateEngineItemTemplatingProvider
	{
		public IEnumerable<ItemTemplate> GetTemplates ()
		{
			return MicrosoftTemplateEngine.GetItemTemplates ();
		}

		public async Task ProcessTemplate (ItemTemplate template, Project project, NewItemConfiguration config)
		{
			var itemTemplate = (MicrosoftTemplateEngineItemTemplate)template;
			var parameters = GetParameters (project, itemTemplate, config);
			var result = await MicrosoftTemplateEngine.InstantiateAsync (itemTemplate.TemplateInfo, config, parameters);

			if (result.Status != CreationResultStatus.Success) {
				string message = string.Format ("Could not create template. Id='{0}' {1} {2}", template.Id, result.Status, result.Message);
				throw new InvalidOperationException (message);
			}

			foreach (var path in result.ResultInfo.PrimaryOutputs) {
				string fullPath = Path.Combine (config.Directory, GetPath (path));

				await MicrosoftTemplateEngine.FormatFile (project?.Policies, fullPath);

				if (project != null) {
					AddFileToProject (project, fullPath);
				}

				IdeApp.Workbench.OpenDocument (fullPath, project).Ignore ();

				if (project != null) {
					await InstallNuGetPackages (project, result.ResultInfo);
				}
			}
		}

		static Dictionary<string, string> GetParameters (Project project, MicrosoftTemplateEngineItemTemplate template, NewItemConfiguration config)
		{
			var parameters = new Dictionary<string, string> ();

			var model = (IStringTagModel)config;
			foreach (ITemplateParameter parameter in template.TemplateInfo.Parameters) {
				string parameterValue = (string)model.GetValue (parameter.Name);
				if (parameterValue != null)
					parameters [parameter.Name] = parameterValue;
			}

			var dotNetProject = project as DotNetProject;
			if (dotNetProject != null) {
				string fileName = GetFullPathIncludingFileExtension (template, config);
				parameters ["namespace"] = dotNetProject.GetDefaultNamespace (fileName);
			}

			return parameters;
		}

		static string GetFullPathIncludingFileExtension (MicrosoftTemplateEngineItemTemplate template, NewItemConfiguration config)
		{
			string fileName = config.Name;
			if (StringComparer.OrdinalIgnoreCase.Equals (template.Language, "C#")) {
				fileName = Path.ChangeExtension (config.Name, ".cs");
			}
			return Path.Combine (config.Directory, fileName);
		}

		void AddFileToProject (Project project, string fileName)
		{
			string buildAction = project.GetDefaultBuildAction (fileName);
			ProjectFile projectFile = project.AddFile (fileName, buildAction);
		}

		static string GetPath (ICreationPath path)
		{
			return MicrosoftTemplateEngine.GetPath (path);
		}

		async Task InstallNuGetPackages (Project project, ICreationResult result)
		{
			var packageReferences = GetPackageReferences (result).ToList ();

			if (!packageReferences.Any ())
				return;

			foreach (ItemTemplatePackageInstaller installer in AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/ItemTemplatePackageInstallers")) {
				await installer.Run (project, packageReferences);
			}
		}

		IEnumerable<TemplatePackageReference> GetPackageReferences (ICreationResult result)
		{
			foreach (var postAction in result.PostActions) {
				var packageReference = CreatePackageReference (postAction);
				if (packageReference != null) {
					yield return packageReference;
				}
			}
		}

		TemplatePackageReference CreatePackageReference (IPostAction action)
		{
			if (!IsInstallPackagePostAction (action))
				return null;

			if (!action.Args.TryGetValue ("reference", out string packageId))
				return null;

			if (!action.Args.TryGetValue ("version", out string packageVersion))
				return null;

			return new TemplatePackageReference (packageId, packageVersion);
		}

		static readonly Guid addReferencePostActionId = new Guid ("B17581D1-C5C9-4489-8F0A-004BE667B814");

		static bool IsInstallPackagePostAction (IPostAction action)
		{
			return action.ActionId == addReferencePostActionId &&
				action.Args != null &&
				action.Args.TryGetValue ("referenceType", out string referenceType) &&
				StringComparer.OrdinalIgnoreCase.Equals (referenceType, "package");
		}
	}
}
