//
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
using System.Collections;
using System.IO;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.StringParsing;
using System.Collections.Generic;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	public enum CompileTarget {
		Exe,
		Library,
		WinExe, 
		Module
	};
	
	public class DotNetProjectConfiguration: ProjectConfiguration
	{
		string assembly;
		string sourcePath;
		DotNetCompilerParameters compilationParameters;

		public DotNetProjectConfiguration (string id): base (id)
		{
		}

		internal protected override void Read (IPropertySet pset)
		{
			base.Read (pset);

			assembly = pset.GetValue ("AssemblyName");
			signAssembly = pset.GetValue<bool> ("SignAssembly");
			delaySign = pset.GetValue<bool> ("DelaySign");
			assemblyKeyFile = pset.GetPathValue ("AssemblyOriginatorKeyFile", FilePath.Empty);
			if (string.IsNullOrEmpty (assemblyKeyFile))
				assemblyKeyFile = pset.GetPathValue ("AssemblyKeyFile", FilePath.Empty);
			if (compilationParameters != null)
				compilationParameters.Read (pset);
		}

		internal protected override void Write (IPropertySet pset)
		{
			base.Write (pset);
			pset.SetValue ("AssemblyName", assembly, mergeToMainGroup: true);
			pset.SetValue ("SignAssembly", signAssembly, defaultValue:false, mergeToMainGroup: true);
			pset.SetValue ("DelaySign", delaySign, defaultValue:false, mergeToMainGroup:true);
			pset.SetValue ("AssemblyOriginatorKeyFile", assemblyKeyFile, defaultValue:FilePath.Empty, mergeToMainGroup:true);
			if (compilationParameters != null)
				compilationParameters.Write (pset);
		}

		private bool signAssembly = false;
		public bool SignAssembly {
			get { return signAssembly; }
			set { signAssembly = value; }
		}
		
		private bool delaySign = false;
		public bool DelaySign {
			get { return delaySign; }
			set { delaySign = value; }
		}

		internal string OldAssemblyKeyFile {
			set { assemblyKeyFile = value; }
		}

		private FilePath assemblyKeyFile = FilePath.Empty;
		public FilePath AssemblyKeyFile {
			get { return assemblyKeyFile; }
			set { assemblyKeyFile = value; }
		}
		
		public virtual string OutputAssembly {
			get { return assembly; }
			set { assembly = value; }
		}
		
		public virtual CompileTarget CompileTarget {
			get {
				DotNetProject prj = ParentItem as DotNetProject;
				if (prj != null)
					return prj.CompileTarget;
				else
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
				DotNetProject prj = ParentItem as DotNetProject;
				if (prj != null)
					return prj.TargetFramework;
				else
					return Services.ProjectService.DefaultTargetFramework;
			}
		}
		
		public TargetRuntime TargetRuntime {
			get {
				DotNetProject prj = ParentItem as DotNetProject;
				if (prj != null)
					return prj.TargetRuntime;
				else
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
			get { return compilationParameters; }
			set {
				compilationParameters = value; 
				if (compilationParameters != null)
					compilationParameters.ParentConfiguration = this;
			}
		}
		
		public FilePath CompiledOutputName {
			get {
				FilePath fullPath = OutputDirectory.Combine (OutputAssembly);
				if (OutputAssembly.EndsWith (".dll") || OutputAssembly.EndsWith (".exe"))
					return fullPath;
				else
					return fullPath + (CompileTarget == CompileTarget.Library ? ".dll" : ".exe");
			}
		}
		
		protected override void OnCopyFrom (ItemConfiguration configuration, bool isRename)
		{
			base.OnCopyFrom (configuration, isRename);
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) configuration;
			
			assembly = conf.assembly;
			sourcePath = conf.sourcePath;
			bool notifyParentItem = ParentItem != null;
			if (ParentItem == null)
				SetParentItem (conf.ParentItem);
			CompilationParameters = conf.compilationParameters != null ? conf.compilationParameters.Clone () : null;
			if (notifyParentItem)
				ParentItem?.NotifyModified ("CompilerParameters");
			signAssembly = conf.signAssembly;
			delaySign = conf.delaySign;
			assemblyKeyFile = conf.assemblyKeyFile;
		}
		
		public new DotNetProject ParentItem {
			get { return (DotNetProject) base.ParentItem; }
		}

		public virtual IEnumerable<string> GetDefineSymbols ()
		{
			if (CompilationParameters != null)
				return CompilationParameters.GetDefineSymbols ();
			return new string[0];
		}
	}
	
	[Mono.Addins.Extension]
	class ProjectTagProvider: StringTagProvider<DotNetProjectConfiguration>, IStringTagProvider
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
