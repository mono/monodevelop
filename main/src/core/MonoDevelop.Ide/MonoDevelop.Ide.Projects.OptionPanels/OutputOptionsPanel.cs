// OutputOptionsPanel.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using IO = System.IO;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal class OutputOptionsPanel : MultiConfigItemOptionsPanel
	{
		OutputOptionsPanelWidget widget;
		
		public OutputOptionsPanel ()
		{
			AllowMixedConfigurations = true;
		}
		
		public override bool IsVisible ()
		{
			return ConfiguredProject is DotNetProject;
		}

		public override Control CreatePanelWidget()
		{
			return (widget = new OutputOptionsPanelWidget ());
		}
		
		public override bool ValidateChanges ()
		{
			return widget.ValidateChanges ();
		}
		
		public override void LoadConfigData ()
		{
			widget.Load (ConfiguredProject, CurrentConfigurations);
		}

		protected override bool ConfigurationsAreEqual (IEnumerable<ItemConfiguration> configs)
		{
			return !string.IsNullOrEmpty (configs.CompareTemplates ());
		}
		
		public override void ApplyChanges()
		{
			widget.Store ();
		}
	}

	partial class OutputOptionsPanelWidget : Gtk.Bin 
	{
		ItemConfiguration[] configurations;

		public OutputOptionsPanelWidget ()
		{
			Build ();

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			label91.Accessible.Role = Atk.Role.Filler;
			assemblyNameEntry.SetCommonAccessibilityAttributes ("OutputOptionsPanel.AssemblyEntry",
			                                                    GettextCatalog.GetString ("Assembly Name"),
			                                                    GettextCatalog.GetString ("Enter the name of the output assembly"));
			assemblyNameEntry.SetAccessibilityLabelRelationship (label98);
			outputPathEntry.EntryAccessible.SetCommonAttributes ("OutputOptionsPanel.OutputEntry",
				                                                 GettextCatalog.GetString ("Output Path"),
				                                                 GettextCatalog.GetString ("Enter the output path"));
			outputPathEntry.EntryAccessible.SetTitleUIElement (label99.Accessible);
			label99.Accessible.SetTitleFor (outputPathEntry.EntryAccessible);
		}
		
		public void Load (Project project, ItemConfiguration[] configs)
		{	
			configurations = configs;

			var outDirTemplate = configs.CompareTemplates ();
			assemblyNameEntry.Text = configs.GetAssemblyName ();
			
			outputPathEntry.DefaultPath = project.BaseDirectory;

			if (configs.Length == 1 && configs [0] is DotNetProjectConfiguration dotNetConfig) {
				outDirTemplate = dotNetConfig.ParseOutDirectoryTemplate (outDirTemplate);
			} 
			outputPathEntry.Path = outDirTemplate;
		}

		public bool ValidateChanges ()
		{
			if (configurations == null)
				return true;
			
			foreach (DotNetProjectConfiguration conf in configurations) {
				if (assemblyNameEntry.Text.Length == 0 && conf.OutputAssembly.Length == 0) {
					MessageService.ShowError (GettextCatalog.GetString ("Invalid assembly name specified"));
					return false;
				}
				string dir = outputPathEntry.Path;
				dir = dir.Replace ("$(Configuration)", conf.Name);
				dir = dir.Replace ("$(Platform)", conf.Platform);
				dir = dir.Replace ("@(TargetFramework)", conf.TargetFrameworkShortName);
				if (!string.IsNullOrEmpty (dir) && !FileService.IsValidPath (dir)) {
					MessageService.ShowError (GettextCatalog.GetString ("Invalid output directory: {0}", dir));
					return false;
				}
			}
			
			return true;
		}
		
		public void Store ()
		{	
			if (configurations == null)
				return;
			
			foreach (DotNetProjectConfiguration conf in configurations) {
				if (assemblyNameEntry.Text.Length > 0)
					conf.OutputAssembly = assemblyNameEntry.Text;

				var dir = conf.ResolveOutDirectoryTemplate (outputPathEntry.Path);

				if (!string.IsNullOrEmpty (dir)) {
					conf.OutputDirectory = dir; 
				}
			}
		}
	}

	internal static class DotNetProjectConfigurationExtensions
	{
		/// <summary>
		/// Given a DotNetProjectConfiguration returns final output directory with no msbuild variables
		/// </summary>
		/// <param name="conf"></param>
		/// <param name="dir"></param>
		/// <returns></returns>
		internal static string ParseOutDirectoryTemplate (this DotNetProjectConfiguration conf, string dir)
		{
			dir = dir.Replace ("$(Configuration)", conf.Name);
			dir = dir.Replace ("$(Platform)", conf.Platform);
			dir = dir.Replace ("$(TargetFramework)", conf.TargetFrameworkShortName);
			return dir;
		}

		/// <summary>
		/// Given a DotNetProjectConfiguration returns temporary output directory for OutputOptionsPanelWidget
		/// </summary>
		/// <param name="conf"></param>
		/// <param name="dir"></param>
		/// <returns></returns>
		internal static string ResolveOutDirectoryTemplate (this DotNetProjectConfiguration conf, string dir)
		{
			dir = dir.Replace ("$(Configuration)", conf.Name);
			dir = dir.Replace ("$(Platform)", conf.Platform);

			var outputDirTemp = dir.Replace ("$(TargetFramework)", conf.TargetFrameworkShortName);
			// check if the outputDirectory has been modified
			if (conf.OutputDirectory.FullPath.ToString ().IndexOf (outputDirTemp, StringComparison.InvariantCulture) != 0
				|| conf.AppendTargetFrameworkToOutputPath) { 
				// if outputDirectory does not contain the targetFramework.Id, AppendTargetFrameworkToOutputPath is false for that config
				conf.AppendTargetFrameworkToOutputPath = dir.Contains (conf.TargetFrameworkShortName) || dir.Contains ("$(TargetFramework)");
				// if so, we have to remove $(TargetFramework) since msbuild will add it due to AppendTargetFrameworkToOutputPath == true 
				dir = dir.Replace ("$(TargetFramework)", string.Empty);
				// in case we are in a specific configuration i.e. Debug, Release, there will be no msbuild variable $(TargetFramework)
				// but TargetFrameworkId values, therefore we have to apply the same logic checking the end of the dir template
				if (dir.TrimEnd (IO.Path.DirectorySeparatorChar).EndsWith (conf.TargetFrameworkShortName, StringComparison.InvariantCulture)) {
					dir = dir.Replace (conf.TargetFrameworkShortName, string.Empty);
				}
			} else {
				// no changes, leaving it as it is
				dir = outputDirTemp;
			}

			return dir.TrimEnd (IO.Path.DirectorySeparatorChar);
		}

		/// <summary>
		/// Returns common name of AssemblyName for all configs; otherwise returns string.Empty
		/// </summary>
		/// <param name="configs"></param>
		/// <returns></returns>
		internal static string GetAssemblyName (this IEnumerable<ItemConfiguration> configs)
		{
			var outAsm = string.Empty;

			foreach (DotNetProjectConfiguration conf in configs) {
				if (string.IsNullOrEmpty (outAsm)) {
					outAsm = conf.OutputAssembly;
					continue;
				}

				if (outAsm != conf.OutputAssembly) {
					outAsm = "";
					break;
				}
			}

			return outAsm;
		}

		/// <summary>
		/// If exists returns template pattern for all configs
		/// </summary>
		/// <param name="configs"></param>
		/// <returns>String.Empty if there is no common pattern otherwise the template pattern</returns>
		internal static string CompareTemplates (this IEnumerable<ItemConfiguration> configs)
		{
			var baseTemplate = string.Empty;
			foreach (DotNetProjectConfiguration config in configs) {
				var template = GetTemplate (config);
				if (string.IsNullOrEmpty (baseTemplate)) {
					baseTemplate = template;
					continue;
				}
				if (template != baseTemplate) {
					return string.Empty;
				}
			}

			return baseTemplate;
		}

		/// <summary>
		/// Given a DotNetProjectConfiguration returns output directory template with msbuild variables
		/// </summary>
		/// <param name="conf"></param>
		/// <returns></returns>
		internal static string GetTemplate (this DotNetProjectConfiguration conf)
		{
			var dirTemplate = conf.OutputDirectory.ToString ();

			if (conf.AppendTargetFrameworkToOutputPath) {
				//default output directory contains TargetFrameworkId but if the output directory
				//has been previously modified then it does not, however we have to append it to dir template since will be part of
				//the output part due to AppendTargetFrameworkToOutputPath == true
				if (!dirTemplate.Contains (conf.TargetFrameworkShortName)) {
					dirTemplate = IO.Path.Combine (dirTemplate, conf.TargetFrameworkShortName);
				}

				dirTemplate = dirTemplate.Replace ($"{IO.Path.DirectorySeparatorChar}{conf.TargetFrameworkShortName}", $"{IO.Path.DirectorySeparatorChar}$(TargetFramework)");
			}

			dirTemplate = dirTemplate.Replace ($"{IO.Path.DirectorySeparatorChar}{conf.Name}", $"{IO.Path.DirectorySeparatorChar}$(Configuration)");
			if (conf.Platform.Length > 0)
				dirTemplate = dirTemplate.Replace ($"{IO.Path.DirectorySeparatorChar}{conf.Platform}", $"{IO.Path.DirectorySeparatorChar}$(Platform)");

			return dirTemplate;
		}
	}
}
