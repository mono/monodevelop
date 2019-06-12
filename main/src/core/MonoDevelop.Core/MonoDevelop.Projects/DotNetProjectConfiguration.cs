﻿//
// DotNetProjectConfiguration.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.StringParsing;
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	public enum CompileTarget
	{
		Exe,
		Library,
		WinExe,
		Module
	};

	public class DotNetProjectConfiguration : ProjectConfiguration
	{
		bool appendTargetFrameworkToOutputPath;
		DotNetCompilerParameters compilationParameters;

		public DotNetProjectConfiguration (string id) : base (id)
		{
		}

		internal protected override void Read (IPropertySet pset)
		{
			base.Read (pset);

			OutputAssembly = pset.GetValue ("AssemblyName");
			appendTargetFrameworkToOutputPath = pset.GetValue<bool> ("AppendTargetFrameworkToOutputPath");
			if (appendTargetFrameworkToOutputPath)
				// if appendTargetFrameworkToOutputPath is true (default value) then "OutputPath" property
				// will append TargetFramework.Id to default output directory (i.e. bin/($Configuration)/netcoreapp30)
				// and OptionsDialog - output directory hides TargtetFramework.ID, otherwise TargetFramework will be appended again
				OutputDirectory = OutputDirectory.ParentDirectory; 	
			SignAssembly = pset.GetValue<bool> ("SignAssembly");
			DelaySign = pset.GetValue<bool> ("DelaySign");
			PublicSign = pset.GetValue<bool> (nameof (PublicSign));
			AssemblyKeyFile = pset.GetPathValue ("AssemblyOriginatorKeyFile", FilePath.Empty);
			if (string.IsNullOrEmpty (AssemblyKeyFile))
				AssemblyKeyFile = pset.GetPathValue ("AssemblyKeyFile", FilePath.Empty);
			if (compilationParameters != null)
				compilationParameters.Read (pset);
		}

		internal protected override void Write (IPropertySet pset)
		{
			base.Write (pset);
			pset.SetValue ("AssemblyName", OutputAssembly, mergeToMainGroup: true);
			pset.SetValue ("SignAssembly", SignAssembly, defaultValue: false, mergeToMainGroup: true);
			pset.SetValue ("DelaySign", DelaySign, defaultValue: false, mergeToMainGroup: true);
			pset.SetValue (nameof (PublicSign), PublicSign, defaultValue: false, mergeToMainGroup: true);
			pset.SetValue ("AssemblyOriginatorKeyFile", AssemblyKeyFile, defaultValue: FilePath.Empty, mergeToMainGroup: true);
			if (compilationParameters != null)
				compilationParameters.Write (pset);
		}

		public bool SignAssembly { get; set; } = false;
		public bool DelaySign { get; set; } = false;
		public bool PublicSign { get; set; }

		internal string OldAssemblyKeyFile {
			set { AssemblyKeyFile = value; }
		}

		public FilePath AssemblyKeyFile { get; set; } = FilePath.Empty;

		public virtual string OutputAssembly { get; set; }

		public virtual CompileTarget CompileTarget {
			get {
				if (ParentItem is DotNetProject prj)
					return prj.CompileTarget;

				return CompileTarget.Library;
			}
		}

		public override SolutionItemConfiguration FindBestMatch (SolutionItemConfigurationCollection configurations)
		{
			// Get all configurations with the same value for the 'DEBUG' symbol
			var isDebug = compilationParameters.GetDefineSymbols ().Contains ("DEBUG");
			var matches = configurations.OfType<DotNetProjectConfiguration> ().Where (c =>
				c.CompilationParameters.GetDefineSymbols ().Contains ("DEBUG") == isDebug
			).ToArray ();

			// If the base method can't find a direct match then try to match based on finding a configuration
			// with a matching value for the 'DEBUG' symbol and some other heuristics
			return base.FindBestMatch (configurations)
				?? matches.FirstOrDefault (c => Platform == c.Platform)
				?? matches.FirstOrDefault (c => c.Platform == "");
		}

		public TargetFramework TargetFramework {
			get {
				if (ParentItem is DotNetProject prj)
					return prj.TargetFramework;

				return Services.ProjectService.DefaultTargetFramework;
			}
		}

		public TargetRuntime TargetRuntime {
			get {
				if (ParentItem is DotNetProject prj)
					return prj.TargetRuntime;

				return Runtime.SystemAssemblyService.DefaultRuntime;
			}
		}

		public MonoDevelop.Core.ClrVersion ClrVersion {
			get {
#pragma warning disable CS0618 // Type or member is obsolete
				return TargetFramework.ClrVersion;
#pragma warning restore CS0618 // Type or member is obsolete
			}
		}

		public DotNetCompilerParameters CompilationParameters {
			get => compilationParameters;
			set {
				compilationParameters = value;
				if (compilationParameters != null)
					compilationParameters.ParentConfiguration = this;
			}
		}

		public FilePath CompiledOutputName {
			get {
				if (OutputAssembly == null)
					return FilePath.Empty;
				FilePath fullPath = OutputDirectory.Combine (OutputAssembly);
				if (OutputAssembly.EndsWith (".dll") || OutputAssembly.EndsWith (".exe"))
					return fullPath;

				return fullPath + (CompileTarget == CompileTarget.Library ? ".dll" : ".exe");
			}
		}

		protected override void OnCopyFrom (ItemConfiguration configuration, bool isRename)
		{
			base.OnCopyFrom (configuration, isRename);
			var conf = (DotNetProjectConfiguration)configuration;

			OutputAssembly = conf.OutputAssembly;
			bool notifyParentItem = ParentItem != null;
			if (ParentItem == null)
				SetParentItem (conf.ParentItem);
			CompilationParameters = conf.compilationParameters?.Clone ();
			if (notifyParentItem)
				ParentItem?.NotifyModified ("CompilerParameters");
			SignAssembly = conf.SignAssembly;
			DelaySign = conf.DelaySign;
			AssemblyKeyFile = conf.AssemblyKeyFile;
		}

		public new DotNetProject ParentItem => (DotNetProject)base.ParentItem;

		public virtual IEnumerable<string> GetDefineSymbols ()
		{
			if (CompilationParameters != null)
				return CompilationParameters.GetDefineSymbols ();
			return new string [0];
		}
	}

	[Mono.Addins.Extension]
	class ProjectTagProvider : StringTagProvider<DotNetProjectConfiguration>, IStringTagProvider
	{
		public override IEnumerable<StringTagDescription> GetTags ()
		{
			yield return new StringTagDescription ("ProjectConfig", GettextCatalog.GetString ("Project Configuration"));
			yield return new StringTagDescription ("ProjectConfigName", GettextCatalog.GetString ("Project Configuration Name"));
			yield return new StringTagDescription ("ProjectConfigPlat", GettextCatalog.GetString ("Project Configuration Platform"));
			yield return new StringTagDescription ("TargetFile", GettextCatalog.GetString ("Target File"));
			yield return new StringTagDescription ("TargetPath", GettextCatalog.GetString ("Target Path"));
			yield return new StringTagDescription ("TargetName", GettextCatalog.GetString ("Target Name"));
			yield return new StringTagDescription ("TargetDir", GettextCatalog.GetString ("Target Directory"));
			yield return new StringTagDescription ("TargetExt", GettextCatalog.GetString ("Target Extension"));
		}

		public override object GetTagValue (DotNetProjectConfiguration conf, string tag)
		{
			switch (tag) {
			case "TARGETPATH":
			case "TARGETFILE": return conf.CompiledOutputName;
			case "TARGETNAME": return conf.CompiledOutputName.FileName;
			case "TARGETDIR": return conf.CompiledOutputName.ParentDirectory;
			case "TARGETEXT": return conf.CompiledOutputName.Extension;
			case "PROJECTCONFIG": return string.IsNullOrEmpty (conf.Platform) ? conf.Name : conf.Name + "." + conf.Platform;
			case "PROJECTCONFIGNAME": return conf.Name;
			case "PROJECTCONFIGPLAT": return conf.Platform;
			}
			throw new NotSupportedException ();
		}
	}
}
