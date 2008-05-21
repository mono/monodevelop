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
using MonoDevelop.Projects.Serialization;

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
		
		MonoDevelop.Core.ClrVersion clrVersion = MonoDevelop.Core.ClrVersion.Net_2_0;
		
		[ItemProperty ("CodeGeneration", FallbackType=typeof(UnknownCompilationParameters))]
		ICloneable compilationParameters;
		
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
		
		public MonoDevelop.Core.ClrVersion ClrVersion {
			get { return (clrVersion == MonoDevelop.Core.ClrVersion.Default) ? MonoDevelop.Core.ClrVersion.Net_1_1 : clrVersion; }
			set { clrVersion = value; }
		}
		
		public ICloneable CompilationParameters {
			get { return compilationParameters; }
			set { compilationParameters = value; }
		}
		
		public string CompiledOutputName {
			get {
				string ext;
				if (OutputAssembly.EndsWith (".dll") || OutputAssembly.EndsWith (".exe"))
					ext = "";
				else
					ext = CompileTarget == CompileTarget.Library ? ".dll" : ".exe";
				return Path.Combine (OutputDirectory, OutputAssembly) + ext;
			}
		}
		
		public string SourceDirectory {
			get { return sourcePath; }
			set { sourcePath = value; }
		}
		
		public override void CopyFrom (ItemConfiguration configuration)
		{
			base.CopyFrom (configuration);
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) configuration;
			
			assembly = conf.assembly;
			sourcePath = conf.sourcePath;
			clrVersion = conf.clrVersion;
			compilationParameters = conf.compilationParameters != null ? (ICloneable)conf.compilationParameters.Clone () : null;
		}
	}
	
	public class UnknownCompilationParameters: ICloneable, IExtendedDataItem
	{
		Hashtable table = new Hashtable ();
		
		public IDictionary ExtendedProperties { 
			get { return table; }
		}
		
		public object Clone ()
		{
			return MemberwiseClone (); 
		}
	}
}
