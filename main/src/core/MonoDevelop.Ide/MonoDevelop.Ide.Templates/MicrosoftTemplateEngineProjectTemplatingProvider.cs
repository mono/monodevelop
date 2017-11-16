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

namespace MonoDevelop.Ide.Templates
{
	class MicrosoftTemplateEngineProjectTemplatingProvider : IProjectTemplatingProvider
	{
		public bool CanProcessTemplate (SolutionTemplate template)
		{
			return template is MicrosoftTemplateEngineSolutionTemplate;
		}

		static EngineEnvironmentSettings environmentSettings = new EngineEnvironmentSettings (new MyTemplateEngineHost (), (env) => new SettingsLoader (env));
		static TemplateCreator templateCreator = new TemplateCreator (environmentSettings);

		static bool dontUpdateCache = true;

		static MicrosoftTemplateEngineProjectTemplatingProvider ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/Templates", OnExtensionChanged);
			dontUpdateCache = false;
			UpdateCache ();
		}

		static List<TemplateExtensionNode> TemplatesNodes = new List<TemplateExtensionNode> ();
		static List<MicrosoftTemplateEngineSolutionTemplate> templates = new List<MicrosoftTemplateEngineSolutionTemplate> ();

		static void UpdateCache ()
		{
			if (dontUpdateCache)//Avoid updating cache while scan paths are added during registration 
				return;

			// Prevent a TypeInitializationException in when calling SettingsLoader.Save when no templates
			// are available, which throws an exception, by returning here. This prevents the MonoDevelop.Ide addin
			// from loading. In practice this should not happen unless the .NET Core addin is disabled.
			if (!TemplatesNodes.Any ())
				return;

			var paths = new Paths (environmentSettings);

			//TODO: Uncomment this IF, but also add logic to invalidate/check if new templates were added from newly installed AddOns...
			//if (!paths.Exists (paths.User.BaseDir) || !paths.Exists (paths.User.FirstRunCookie)) {
			paths.DeleteDirectory (paths.User.BaseDir);//Delete cache
			var settingsLoader = (SettingsLoader)environmentSettings.SettingsLoader;
			foreach (var scanPath in TemplatesNodes.Select (t => t.ScanPath).Distinct ()) {
				settingsLoader.UserTemplateCache.Scan (scanPath);
			}
			settingsLoader.Save ();
			paths.WriteAllText (paths.User.FirstRunCookie, "");
			//}
			var templateInfos = settingsLoader.UserTemplateCache.List (false, t => new MatchInfo ()).ToDictionary (m => m.Info.Identity, m => m.Info);
			var newTemplates = new List<MicrosoftTemplateEngineSolutionTemplate> ();
			foreach (var template in TemplatesNodes) {
				ITemplateInfo templateInfo;
				if (!templateInfos.TryGetValue (template.TemplateId, out templateInfo)) {
					LoggingService.LogWarning ("Template {0} not found.", template.TemplateId);
					continue;
				}
				newTemplates.Add (new MicrosoftTemplateEngineSolutionTemplate (template, templateInfo));
			}
			templates = newTemplates;
		}

		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				var codon = (TemplateExtensionNode)args.ExtensionNode;
				try {
					TemplatesNodes.Add (codon);
				} catch (Exception e) {
					string extId = null, addinId = null;
					if (codon != null) {
						if (codon.HasId)
							extId = codon.Id;
						if (codon.Addin != null)
							addinId = codon.Addin.Id;
					}
					LoggingService.LogError ("Error loading template id {0} in addin {1}:\n{2}",
											 extId ?? "(null)", addinId ?? "(null)", e.ToString ());
				}
			} else {
				foreach (var pt in TemplatesNodes) {
					var codon = (TemplateExtensionNode)args.ExtensionNode;
					if (pt.Id == codon.Id) {
						TemplatesNodes.Remove (pt);
						break;
					}
				}
			}
			UpdateCache ();
		}

		public IEnumerable<SolutionTemplate> GetTemplates ()
		{
			return templates;
		}

		/// <summary>
		/// Used by unit tests to create a new solution template without having to use an addin.
		/// </summary>
		static internal SolutionTemplate CreateTemplate (string templateId, string scanPath)
		{
			var settingsLoader = (SettingsLoader)environmentSettings.SettingsLoader;
			settingsLoader.UserTemplateCache.Scan (scanPath);
			settingsLoader.Save ();

			var templateInfo = settingsLoader.UserTemplateCache.TemplateInfo
				.FirstOrDefault (t => t.Identity == templateId);

			return new MicrosoftTemplateEngineSolutionTemplate (templateId, templateId, null, templateInfo);
		}

		static MonoDevelop.Core.Instrumentation.Counter TemplateCounter = MonoDevelop.Core.Instrumentation.InstrumentationService.CreateCounter ("Template Instantiated", "Project Model", id: "Core.Template.Instantiated");

		public async Task<ProcessedTemplateResult> ProcessTemplate (SolutionTemplate template, NewProjectConfiguration config, SolutionFolder parentFolder)
		{
			var solutionTemplate = (MicrosoftTemplateEngineSolutionTemplate)template;
			var parameters = GetParameters (solutionTemplate, config);
			var templateInfo = solutionTemplate.templateInfo;
			var workspaceItems = new List<IWorkspaceFileObject> ();

			var filesBeforeCreation = Directory.GetFiles (config.ProjectLocation, "*", SearchOption.AllDirectories);

			var result = await templateCreator.InstantiateAsync (
				templateInfo,
				config.ProjectName,
				config.GetValidProjectName (),
				config.ProjectLocation,
				parameters,
				true,
				false,
				null
			);

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

			var metadata = new Dictionary<string, string> ();
			metadata ["Id"] = templateInfo.Identity;
			metadata ["Name"] = templateInfo.Name;
			metadata ["Language"] = template.Language;
			metadata ["Platform"] = string.Join(";", templateInfo.Classifications);
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
							await FormatFile (parentFolder?.Policies ?? p.Policies, file.FilePath);
						}
					}
				}
			}
			processResult.SetFilesToOpen (filesToOpen);
			return processResult;
		}

		string GetPath (ICreationPath path)
		{
			if (Path.DirectorySeparatorChar != '\\')
				return path.Path.Replace ('\\', Path.DirectorySeparatorChar);

			return path.Path;
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

		async Task FormatFile (PolicyContainer policies, FilePath file)
		{
			string mime = DesktopService.GetMimeTypeForUri (file);
			if (mime == null)
				return;

			var formatter = CodeFormatterService.GetFormatter (mime);
			if (formatter != null) {
				try {
					var content = await TextFileUtility.ReadAllTextAsync (file);
					var formatted = formatter.FormatText (policies, content.Text);
					if (formatted != null)
						TextFileUtility.WriteText (file, formatted, content.Encoding);
				} catch (Exception ex) {
					LoggingService.LogError ("File formatting failed", ex);
				}
			}
		}

		class MyTemplateEngineHost : DefaultTemplateEngineHost
		{
			public MyTemplateEngineHost () : base (BrandingService.ApplicationName, BuildInfo.CompatVersion, "en-US", new Dictionary<string, string> { { "dotnet-cli-version", "0" } }, new Dictionary<Guid, Func<Type>>
			   {
				{ new Guid("0C434DF7-E2CB-4DEE-B216-D7C58C8EB4B3"), () => typeof(RunnableProjectGenerator) },
				{ new Guid("3147965A-08E5-4523-B869-02C8E9A8AAA1"), () => typeof(BalancedNestingConfig) },
				{ new Guid("3E8BCBF0-D631-45BA-A12D-FBF1DE03AA38"), () => typeof(ConditionalConfig) },
				{ new Guid("A1E27A4B-9608-47F1-B3B8-F70DF62DC521"), () => typeof(FlagsConfig) },
				{ new Guid("3FAE1942-7257-4247-B44D-2DDE07CB4A4A"), () => typeof(IncludeConfig) },
				{ new Guid("3D33B3BF-F40E-43EB-A14D-F40516F880CD"), () => typeof(RegionConfig) },
				{ new Guid("62DB7F1F-A10E-46F0-953F-A28A03A81CD1"), () => typeof(ReplacementConfig) },
				{ new Guid("370996FE-2943-4AED-B2F6-EC03F0B75B4A"), () => typeof(ConstantMacro) },
				{ new Guid("BB625F71-6404-4550-98AF-B2E546F46C5F"), () => typeof(EvaluateMacro) },
				{ new Guid("10919008-4E13-4FA8-825C-3B4DA855578E"), () => typeof(GuidMacro) },
				{ new Guid("F2B423D7-3C23-4489-816A-41D8D2A98596"), () => typeof(NowMacro) },
				{ new Guid("011E8DC1-8544-4360-9B40-65FD916049B7"), () => typeof(RandomMacro) },
				{ new Guid("8A4D4937-E23F-426D-8398-3BDBD1873ADB"), () => typeof(RegexMacro) },
				{ new Guid("B57D64E0-9B4F-4ABE-9366-711170FD5294"), () => typeof(SwitchMacro) },
				{ new Guid("10919118-4E13-4FA9-825C-3B4DA855578E"), () => typeof(CaseChangeMacro) }
			}.ToList ())
			{
			}

			public override bool TryGetHostParamDefault (string paramName, out string value)
			{
				if (paramName == "HostIdentifier") {
					value = this.HostIdentifier;
					return true;
				}
				value = null;
				return false;
			}
		}
	}
}
