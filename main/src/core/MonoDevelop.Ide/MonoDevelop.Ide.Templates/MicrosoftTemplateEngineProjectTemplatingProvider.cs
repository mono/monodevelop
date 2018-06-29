//
// MicrosoftTemplateEngineProjectTemplatingProvider.cs
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
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Utils;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects;
using System.Linq;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using MonoDevelop.Core;
using Microsoft.TemplateEngine.Edge;
using System.IO;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Config;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros;
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Ide.Codons;
using Microsoft.TemplateEngine.Abstractions;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Core.Text;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.Ide.Templates
{
	class MicrosoftTemplateEngineProjectTemplatingProvider : IProjectTemplatingProvider
	{
		public bool CanProcessTemplate (SolutionTemplate template)
		{
			return template is MicrosoftTemplateEngineSolutionTemplate;
		}

		public IEnumerable<SolutionTemplate> GetTemplates ()
		{
			return MicrosoftTemplateEngine.GetProjectTemplates ();
		}

		/// <summary>
		/// Used by unit tests to create a new solution template without having to use an addin.
		/// </summary>
		static internal SolutionTemplate CreateTemplate (string templateId, string scanPath)
		{
			return MicrosoftTemplateEngine.CreateProjectTemplate (templateId, scanPath);
		}

		static Counter<TemplateMetadata> TemplateCounter = InstrumentationService.CreateCounter<TemplateMetadata> ("Template Instantiated", "Project Model", id: "Core.Template.Instantiated");

		public async Task<ProcessedTemplateResult> ProcessTemplate (SolutionTemplate template, NewProjectConfiguration config, SolutionFolder parentFolder)
		{
			var solutionTemplate = (MicrosoftTemplateEngineSolutionTemplate)template;
			var parameters = GetParameters (solutionTemplate, config);
			var templateInfo = solutionTemplate.templateInfo;
			var workspaceItems = new List<IWorkspaceFileObject> ();

			var filesBeforeCreation = Directory.GetFiles (config.ProjectLocation, "*", SearchOption.AllDirectories);

			var result = await MicrosoftTemplateEngine.InstantiateAsync (templateInfo, config, parameters);

			if (result.Status != CreationResultStatus.Success) {
				string message = string.Format ("Could not create template. Id='{0}' {1} {2}", template.Id, result.Status, result.Message);
				throw new InvalidOperationException (message);
			}

			var filesToOpen = new List<string> ();
			foreach (var postAction in result.ResultInfo.PostActions) {
				switch (postAction.ActionId.ToString ().ToUpper ()) {
				case "84C0DA21-51C8-4541-9940-6CA19AF04EE6":
					if (postAction.Args.TryGetValue ("files", out var files))
						foreach (var fi in files.Split (';'))
							if (int.TryParse (fi.Trim (), out var i))
								filesToOpen.Add (Path.Combine (config.ProjectLocation, GetPath (result.ResultInfo.PrimaryOutputs [i])));
					break;
				case "D396686C-DE0E-4DE6-906D-291CD29FC5DE":
					//TODO: Load project files
					break;
				}
			}

			//TODO: Once templates support "D396686C-DE0E-4DE6-906D-291CD29FC5DE" use that to load projects
			foreach (var path in result.ResultInfo.PrimaryOutputs) {
				var fullPath = Path.Combine (config.ProjectLocation, GetPath (path));
				if (Services.ProjectService.IsSolutionItemFile (fullPath))
					workspaceItems.Add (await MonoDevelop.Projects.Services.ProjectService.ReadSolutionItem (new Core.ProgressMonitor (), fullPath));
			}

			var metadata = new TemplateMetadata {
				Id = templateInfo.Identity,
				Name = templateInfo.Name,
				Language = template.Language,
				Platform = string.Join(";", templateInfo.Classifications)
			};
			TemplateCounter.Inc (1, null, metadata);

			MicrosoftTemplateEngineProcessedTemplateResult processResult;

			if (parentFolder == null) {
				var solution = new Solution ();
				solution.SetLocation (config.SolutionLocation, config.SolutionName);
				foreach (var item in workspaceItems.Cast<SolutionFolderItem> ()) {
					IConfigurationTarget configurationTarget = item as IConfigurationTarget;
					if (configurationTarget != null) {
						foreach (ItemConfiguration configuration in configurationTarget.Configurations) {
							bool flag = false;
							foreach (SolutionConfiguration solutionCollection in solution.Configurations) {
								if (solutionCollection.Id == configuration.Id)
									flag = true;
							}
							if (!flag)
								solution.AddConfiguration (configuration.Id, true);
						}
					}
					solution.RootFolder.AddItem (item);
				}
				processResult = new MicrosoftTemplateEngineProcessedTemplateResult (new [] { solution }, solution.FileName, config.ProjectLocation);
			} else {
				processResult = new MicrosoftTemplateEngineProcessedTemplateResult (workspaceItems.ToArray (), parentFolder.ParentSolution.FileName, config.ProjectLocation);
			}

			// Format all source files generated during the project creation
			foreach (var p in workspaceItems.OfType<Project> ()) {
				foreach (var file in p.Files) {
					if (!filesBeforeCreation.Contains ((string)file.FilePath, FilePath.PathComparer)) { //Format only newly created files
						if (solutionTemplate.ShouldFormatFile (file.FilePath)) {
							await MicrosoftTemplateEngine.FormatFile (parentFolder?.Policies ?? p.Policies, file.FilePath);
						}
					}
				}
			}
			processResult.SetFilesToOpen (filesToOpen);
			return processResult;
		}

		static string GetPath (ICreationPath path)
		{
			return MicrosoftTemplateEngine.GetPath (path);
		}

		Dictionary<string, string> GetParameters (MicrosoftTemplateEngineSolutionTemplate template, NewProjectConfiguration config)
		{
			var parameters = new Dictionary<string, string> ();
			if (!string.IsNullOrEmpty (template.DefaultParameters)) {
				foreach (TemplateParameter parameter in GetValidParameters (template.DefaultParameters)) {
					parameters [parameter.Name] = parameter.Value;
				}
			}

			// If the template has no wizard then no extra parameters will be set.
			if (template.HasWizard) {
				var model = (IStringTagModel)config.Parameters;
				foreach (ITemplateParameter parameter in template.templateInfo.Parameters) {
					string parameterValue = (string)model.GetValue (parameter.Name);
					if (parameterValue != null)
						parameters [parameter.Name] = parameterValue;
				}
			}

			return parameters;
		}

		static IEnumerable<TemplateParameter> GetValidParameters (string parameters)
		{
			return TemplateParameter.CreateParameters (parameters)
				.Where (parameter => parameter.IsValid);
		}
	}

	class TemplateMetadata : CounterMetadata
	{
		public TemplateMetadata ()
		{
		}

		public string Id {
			get => GetProperty<string> ();
			set => SetProperty (value);
		}

		public string Name {
			get => GetProperty<string> ();
			set => SetProperty (value);
		}

		public string Language {
			get => GetProperty<string> ();
			set => SetProperty (value);
		}

		public string Platform {
			get => GetProperty<string> ();
			set => SetProperty (value);
		}
	}
}
