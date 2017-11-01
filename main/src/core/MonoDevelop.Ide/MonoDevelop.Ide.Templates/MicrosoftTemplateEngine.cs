//
// MicrosoftTemplateEngine.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Config;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros;
using Microsoft.TemplateEngine.Utils;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Ide.Templates
{
	class MicrosoftTemplateEngine
	{
		static EngineEnvironmentSettings environmentSettings = new EngineEnvironmentSettings (new MyTemplateEngineHost (), (env) => new SettingsLoader (env));
		static TemplateCreator templateCreator = new TemplateCreator (environmentSettings);

		static bool dontUpdateCache = true;

		static MicrosoftTemplateEngine ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/Templates", OnProjectTemplateExtensionChanged);
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/ItemTemplates", OnItemTemplateExtensionChanged);
			dontUpdateCache = false;
			UpdateCache ();
		}

		static List<TemplateExtensionNode> projectTemplateNodes = new List<TemplateExtensionNode> ();
		static List<MicrosoftTemplateEngineSolutionTemplate> projectTemplates = new List<MicrosoftTemplateEngineSolutionTemplate> ();
		static List<ItemTemplateExtensionNode> itemTemplateNodes = new List<ItemTemplateExtensionNode> ();
		static List<MicrosoftTemplateEngineItemTemplate> itemTemplates = new List<MicrosoftTemplateEngineItemTemplate> ();

		static void UpdateCache ()
		{
			if (dontUpdateCache)//Avoid updating cache while scan paths are added during registration 
				return;

			// Prevent a TypeInitializationException in when calling SettingsLoader.Save when no templates
			// are available, which throws an exception, by returning here. This prevents the MonoDevelop.Ide addin
			// from loading. In practice this should not happen unless the .NET Core addin is disabled.
			if (!projectTemplateNodes.Any () && !itemTemplateNodes.Any ())
				return;

			var paths = new Paths (environmentSettings);

			//TODO: Uncomment this IF, but also add logic to invalidate/check if new templates were added from newly installed AddOns...
			//if (!paths.Exists (paths.User.BaseDir) || !paths.Exists (paths.User.FirstRunCookie)) {
			paths.DeleteDirectory (paths.User.BaseDir);//Delete cache
			var settingsLoader = (SettingsLoader)environmentSettings.SettingsLoader;

			foreach (var scanPath in projectTemplateNodes.Select (t => t.ScanPath).Distinct ()) {
				settingsLoader.UserTemplateCache.Scan (scanPath);
			}

			foreach (var scanPath in itemTemplateNodes.Select (t => t.ScanPath).Distinct ()) {
				settingsLoader.UserTemplateCache.Scan (scanPath);
			}

			settingsLoader.Save ();
			paths.WriteAllText (paths.User.FirstRunCookie, "");
			//}
			var templateInfos = settingsLoader.UserTemplateCache.List (false, t => new MatchInfo ()).ToDictionary (m => m.Info.Identity, m => m.Info);
			var newProjectTemplates = new List<MicrosoftTemplateEngineSolutionTemplate> ();
			foreach (var template in projectTemplateNodes) {
				ITemplateInfo templateInfo;
				if (!templateInfos.TryGetValue (template.TemplateId, out templateInfo)) {
					LoggingService.LogWarning ("Template {0} not found.", template.TemplateId);
					continue;
				}
				newProjectTemplates.Add (new MicrosoftTemplateEngineSolutionTemplate (template, templateInfo));
			}
			projectTemplates = newProjectTemplates;

			var newItemTemplates = new List<MicrosoftTemplateEngineItemTemplate> ();
			foreach (var template in itemTemplateNodes) {
				ITemplateInfo templateInfo;
				if (!templateInfos.TryGetValue (template.TemplateId, out templateInfo)) {
					LoggingService.LogWarning ("Template {0} not found.", template.TemplateId);
					continue;
				}
				newItemTemplates.Add (new MicrosoftTemplateEngineItemTemplate (template, templateInfo));
			}
			itemTemplates = newItemTemplates;
		}

		static void OnProjectTemplateExtensionChanged (object sender, ExtensionNodeEventArgs args)
		{
			var node = (TemplateExtensionNode)args.ExtensionNode;

			OnExtensionChanged (projectTemplateNodes, node, args.Change);
		}

		static void OnItemTemplateExtensionChanged (object sender, ExtensionNodeEventArgs args)
		{
			var node = (ItemTemplateExtensionNode)args.ExtensionNode;

			OnExtensionChanged (itemTemplateNodes, node, args.Change);
		}

		static void OnExtensionChanged<T> (List<T> extensionNodes, T node, ExtensionChange change)
			where T : ExtensionNode
		{
			if (change == ExtensionChange.Add) {
				try {
					extensionNodes.Add (node);
				} catch (Exception ex) {
					LogExtensionChangedError (ex, node);
				}
			} else {
				foreach (var existingNode in extensionNodes) {
					if (existingNode.Id == node.Id) {
						extensionNodes.Remove (existingNode);
						break;
					}
				}
			}

			UpdateCache ();
		}

		static void LogExtensionChangedError (Exception ex, ExtensionNode node)
		{
			string extId = null;
			string addinId = null;

			if (node != null) {
				if (node.HasId)
					extId = node.Id;
				if (node.Addin != null)
					addinId = node.Addin.Id;
			}

			LoggingService.LogError ("Error loading template id {0} in addin {1}:\n{2}",
				extId ?? "(null)", addinId ?? "(null)", ex.ToString ());
		}

		public static IEnumerable<SolutionTemplate> GetProjectTemplates ()
		{
			return projectTemplates;
		}

		public static IEnumerable<ItemTemplate> GetItemTemplates ()
		{
			return itemTemplates;
		}

		/// <summary>
		/// Used by unit tests to create a new solution template without having to use an addin.
		/// </summary>
		static internal SolutionTemplate CreateProjectTemplate (string templateId, string scanPath)
		{
			var settingsLoader = (SettingsLoader)environmentSettings.SettingsLoader;
			settingsLoader.UserTemplateCache.Scan (scanPath);
			settingsLoader.Save ();

			var templateInfo = settingsLoader.UserTemplateCache.TemplateInfo
				.FirstOrDefault (t => t.Identity == templateId);

			return new MicrosoftTemplateEngineSolutionTemplate (templateId, templateId, null, templateInfo);
		}

		public static Task<TemplateCreationResult> InstantiateAsync (
			ITemplateInfo templateInfo,
			NewProjectConfiguration config,
			IReadOnlyDictionary<string, string> parameters)
		{
			return templateCreator.InstantiateAsync (
				templateInfo,
				config.ProjectName,
				config.GetValidProjectName (),
				config.ProjectLocation,
				parameters,
				true,
				false,
				null
			);
		}

		public static Task<TemplateCreationResult> InstantiateAsync (
			ITemplateInfo templateInfo,
			NewItemConfiguration config,
			IReadOnlyDictionary<string, string> parameters)
		{
			return templateCreator.InstantiateAsync (
				templateInfo,
				config.NameWithoutExtension,
				config.NameWithoutExtension,
				config.Directory,
				parameters,
				true,
				false,
				null
			);
		}

		public static string GetPath (ICreationPath path)
		{
			if (Path.DirectorySeparatorChar != '\\')
				return path.Path.Replace ('\\', Path.DirectorySeparatorChar);

			return path.Path;
		}

		public static async Task FormatFile (PolicyContainer policies, FilePath file)
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

		public static string MergeDefaultParameters (string defaultParameters, ITemplateInfo templateInfo)
		{
			var cacheParameters = templateInfo.CacheParameters.Where (m => !string.IsNullOrEmpty (m.Value.DefaultValue));
			return MergeParameters (defaultParameters, cacheParameters, true);
		}

		public static string GetSupportedParameters (ITemplateInfo templateInfo)
		{
			return MergeParameters (null, templateInfo.CacheParameters, false);
		}

		static string MergeParameters (
			string parameterString,
			IEnumerable<KeyValuePair<string, ICacheParameter>> cacheParameters,
			bool includeDefaultValue)
		{
			List<TemplateParameter> priorityParameters = null;
			var parameters = new List<string> ();

			if (!cacheParameters.Any ())
				return parameterString;

			if (!string.IsNullOrEmpty (parameterString)) {
				priorityParameters = TemplateParameter.CreateParameters (parameterString).ToList ();
				parameterString += ",";
			}

			foreach (var p in cacheParameters) {
				if (priorityParameters == null || !priorityParameters.Exists (t => t.Name == p.Key)) {
					if (includeDefaultValue) {
						parameters.Add ($"{p.Key}={p.Value.DefaultValue}");
					} else {
						parameters.Add ($"{p.Key}");
					}
				}
			}

			return parameterString += string.Join (",", parameters);
		}

		public static string GetLanguage (ITemplateInfo templateInfo)
		{
			ICacheTag languageTag;
			if (templateInfo.Tags.TryGetValue ("language", out languageTag)) {
				return languageTag.DefaultValue;
			}

			return string.Empty;
		}

		class MyTemplateEngineHost : DefaultTemplateEngineHost
		{
			public MyTemplateEngineHost ()
				: base (BrandingService.ApplicationName, BuildInfo.CompatVersion, "en-US", new Dictionary<string, string> { { "dotnet-cli-version", "0" } }, new Dictionary<Guid, Func<Type>>
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
