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
using MonoDevelop.Core.Serialization;

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
		[ItemProperty ("AssemblyName")]
		string assembly;
		
		ConfigurationParameters compilationParameters;
		
		string sourcePath;
		
		public DotNetProjectConfiguration ()
		{
		}

		public DotNetProjectConfiguration (string name): base (name)
		{
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
		
		public MonoDevelop.Core.TargetFramework TargetFramework {
			get {
				DotNetProject prj = ParentItem as DotNetProject;
				if (prj != null)
					return prj.TargetFramework;
				else
					return Services.ProjectService.DefaultTargetFramework;
			}
		}
		
		public MonoDevelop.Core.ClrVersion ClrVersion {
			get {
				return TargetFramework.ClrVersion;
			}
		}
		
		[ItemProperty ("CodeGeneration")]
		public ConfigurationParameters CompilationParameters {
			get { return compilationParameters; }
			set {
				compilationParameters = value; 
				if (compilationParameters != null)
					compilationParameters.ParentConfiguration = this;
			}
		}
		
		public ProjectParameters ProjectParameters {
			get {
				DotNetProject dnp = ParentItem as DotNetProject;
				if (dnp != null)
					return dnp.LanguageParameters;
				else
					return null;
			}
		}
		
		public string CompiledOutputName {
			get {
				string fullPath = Path.Combine (OutputDirectory, OutputAssembly);
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
		}
		
		public new DotNetProject ParentItem {
			get { return (DotNetProject) base.ParentItem; }
		}
	}
	
	public class UnknownCompilationParameters: ConfigurationParameters, IExtendedDataItem
	{
		Hashtable table = new Hashtable ();
		
		public IDictionary ExtendedProperties { 
			get { return table; }
		}
	}
	
	public class UnknownProjectParameters: ProjectParameters, IExtendedDataItem
	{
		Hashtable table = new Hashtable ();
		
		public IDictionary ExtendedProperties { 
			get { return table; }
		}
	}
}
