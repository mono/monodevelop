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
using Microsoft.TemplateEngine.Abstractions.Mount;
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

		static List<MicrosoftTemplateEngineSolutionTemplate> projectTemplates = new List<MicrosoftTemplateEngineSolutionTemplate> ();
		static List<MicrosoftTemplateEngineItemTemplate> itemTemplates = new List<MicrosoftTemplateEngineItemTemplate> ();

		static void UpdateCache ()
		{
			if (dontUpdateCache)//Avoid updating cache while scan paths are added during registration 
				return;

			// Prevent a TypeInitializationException in when calling SettingsLoader.Save when no templates
			// are available, which throws an exception, by returning here. This prevents the MonoDevelop.Ide addin
			// from loading. In practice this should not happen unless the .NET Core addin is disabled.
			var projectTemplateNodes = AddinManager.GetExtensionNodes<TemplateExtensionNode> ("/MonoDevelop/Ide/Templates");
			var itemTemplateNodes = AddinManager.GetExtensionNodes<ItemTemplateExtensionNode> ("/MonoDevelop/Ide/ItemTemplates");
			if (!projectTemplateNodes.Any () && !itemTemplateNodes.Any ())
				return;

			var paths = new Paths (environmentSettings);

			//TODO: Uncomment this IF, but also add logic to invalidate/check if new templates were added from newly installed AddOns...
			//if (!paths.Exists (paths.User.BaseDir) || !paths.Exists (paths.User.FirstRunCookie)) {
			paths.DeleteDirectory (paths.User.BaseDir);//Delete cache
			var settingsLoader = (SettingsLoader)environmentSettings.SettingsLoader;

			foreach (var path in projectTemplateNodes.Select (t => t.ScanPath).Distinct ()) {
				string scanPath = StringParserService.Parse (path);
				if (!string.IsNullOrEmpty (scanPath)) {
					settingsLoader.UserTemplateCache.Scan (scanPath);
				}
			}

			foreach (var path in itemTemplateNodes.Select (t => t.ScanPath).Distinct ()) {
				string scanPath = StringParserService.Parse (path);
				if (!string.IsNullOrEmpty (scanPath)) {
					settingsLoader.UserTemplateCache.Scan (scanPath);
				}
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
			UpdateCache ();
		}

		static void OnItemTemplateExtensionChanged (object sender, ExtensionNodeEventArgs args)
		{
			UpdateCache ();
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
			return NormalizePath (path.Path);
		}

		static string NormalizePath (string path)
		{
			if (Path.DirectorySeparatorChar != '\\')
				return path.Replace ('\\', Path.DirectorySeparatorChar);

			return path;
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
			List<TemplateParameter> priorityParameters = null;
			var parameters = new List<string> ();
			var cacheParameters = templateInfo.CacheParameters.Where (m => !string.IsNullOrEmpty (m.Value.DefaultValue));

			if (!cacheParameters.Any ())
				return defaultParameters;

			if (!string.IsNullOrEmpty (defaultParameters)) {
				priorityParameters = TemplateParameter.CreateParameters (defaultParameters).ToList ();
				defaultParameters += ",";
			}

			foreach (var p in cacheParameters) {
				if (priorityParameters == null || !priorityParameters.Exists (t => t.Name == p.Key))
					parameters.Add ($"{p.Key}={p.Value.DefaultValue}");
			}

			return defaultParameters += string.Join (",", parameters);
		}

		public static string GetLanguage (ITemplateInfo templateInfo)
		{
			ICacheTag languageTag;
			if (templateInfo.Tags.TryGetValue ("language", out languageTag)) {
				return languageTag.DefaultValue;
			}

			return string.Empty;
		}

		/// <summary>
		/// Use '${TemplateConfigDirectory}/template.json' to get the template.json file
		/// without having to specify the full path.
		/// </summary>
		public static Stream GetStream (ITemplateInfo template, string path)
		{
			path = NormalizePath (template, path);

			var settingsLoader = (SettingsLoader)environmentSettings.SettingsLoader;

			IMountPoint mountPoint;
			IFile file;
			if (settingsLoader.TryGetFileFromIdAndPath (template.ConfigMountPointId, path, out file, out mountPoint)) {
				return file.OpenRead ();
			}

			return null;
		}

		public static Xwt.Drawing.Image GetImage (ITemplateInfo template, string path)
		{
			var settingsLoader = (SettingsLoader) environmentSettings.SettingsLoader;
			var loader = new MicrosoftTemplateEngineImageLoader (settingsLoader, template);

			path = NormalizePath (template, path);

			return Xwt.Drawing.Image.FromCustomLoader (loader, path);
		}

		static string NormalizePath (ITemplateInfo template, string path)
		{
			path = NormalizePath (path);

			var tags = new string[,] {
				{"TemplateConfigDirectory", Path.GetDirectoryName (template.ConfigPlace) }
			};

			return StringParserService.Parse (path, tags);
		}

		class MyTemplateEngineHost : DefaultTemplateEngineHost
		{
			static readonly AssemblyComponentCatalog builtIns = new AssemblyComponentCatalog (new[] {
				typeof (RunnableProjectGenerator).Assembly,
			});

			public MyTemplateEngineHost () : base (BrandingService.ApplicationName, BuildInfo.CompatVersion, "en-US", new Dictionary<string, string> { { "dotnet-cli-version", "0" } }, builtIns)
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
