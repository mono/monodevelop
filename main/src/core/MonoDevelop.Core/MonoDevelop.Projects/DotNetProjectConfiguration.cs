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
using MonoDevelop.Projects.Formats.MSBuild;

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

		public DotNetProjectConfiguration ()
		{
		}

		public DotNetProjectConfiguration (string name): base (name)
		{
		}

		internal protected override void Read (IMSBuildPropertySet pset, MSBuildFileFormat format)
		{
			base.Read (pset, format);

			assembly = pset.GetValue ("AssemblyName");
			signAssembly = pset.GetValue<bool> ("SignAssembly");
			delaySign = pset.GetValue<bool> ("DelaySign");
			assemblyKeyFile = pset.GetPathValue ("AssemblyOriginatorKeyFile");
			if (string.IsNullOrEmpty (assemblyKeyFile))
				assemblyKeyFile = pset.GetPathValue ("AssemblyKeyFile");
			if (compilationParameters != null)
				((IMSBuildDataObject)compilationParameters).Read (pset, format);
		}

		internal protected override void Write (IMSBuildPropertySet pset, MSBuildFileFormat format)
		{
			base.Write (pset, format);
			pset.SetValue ("AssemblyName", assembly, mergeToMainGroup: true);
			pset.SetValue ("SignAssembly", signAssembly, defaultValue:false, mergeToMainGroup: true);
			pset.SetValue ("DelaySign", delaySign, defaultValue:false, mergeToMainGroup:true);
			pset.SetValue ("AssemblyOriginatorKeyFile", assemblyKeyFile, mergeToMainGroup:true);
			if (compilationParameters != null)
				((IMSBuildDataObject)compilationParameters).Write (pset, format);
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
				return TargetFramework.ClrVersion;
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
		
		public override void CopyFrom (ItemConfiguration configuration)
		{
			base.CopyFrom (configuration);
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) configuration;
			
			assembly = conf.assembly;
			sourcePath = conf.sourcePath;
			if (ParentItem == null)
				SetParentItem (conf.ParentItem);
			CompilationParameters = conf.compilationParameters != null ? conf.compilationParameters.Clone () : null;
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
	
	public class UnknownCompilationParameters: DotNetCompilerParameters, IExtendedDataItem
	{
		readonly Hashtable table = new Hashtable ();
		
		public IDictionary ExtendedProperties { 
			get { return table; }
		}
	}
	
	public class UnknownProjectParameters: ProjectParameters, IExtendedDataItem
	{
		readonly Hashtable table = new Hashtable ();
		
		public IDictionary ExtendedProperties { 
			get { return table; }
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
				case "PROJECTCONFIG": return conf.Name + "." + conf.Platform;
				case "PROJECTCONFIGNAME": return conf.Name;
				case "PROJECTCONFIGPLAT": return conf.Platform;
			}
			throw new NotSupportedException ();
		}
	}
}
