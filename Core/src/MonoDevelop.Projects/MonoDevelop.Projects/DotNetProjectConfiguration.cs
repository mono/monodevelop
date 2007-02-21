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
	public enum NetRuntime {
		Mono,
		MonoInterpreter,
		MsNet
	};
	
	public enum CompileTarget {
		Exe,
		Library,
		WinExe, 
		Module
	};
	
	public class DotNetProjectConfiguration: AbstractProjectConfiguration
	{
		[ItemProperty ("Output/assembly")]
		string assembly = "a";
		
		[ItemProperty ("Execution/runtime")]
		NetRuntime netRuntime = NetRuntime.MsNet;
		
		[ItemProperty ("Execution/clr-version")]
		MonoDevelop.Core.ClrVersion clrVersion = MonoDevelop.Core.ClrVersion.Net_1_1;
		
		[ItemProperty ("Build/target")]
		CompileTarget compiletarget = CompileTarget.Exe;
		
		[ItemProperty ("CodeGeneration", FallbackType=typeof(UnknownCompilationParameters))]
		ICloneable compilationParameters;
		
		string sourcePath;

		public virtual string OutputAssembly {
			get { return assembly; }
			set { assembly = value; }
		}
		
		public NetRuntime NetRuntime {
			get { return netRuntime; }
			set { netRuntime = value; }
		}

		public virtual CompileTarget CompileTarget {
			get { return compiletarget; }
			set { compiletarget = value; }
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
		
		public override void CopyFrom (IConfiguration configuration)
		{
			base.CopyFrom (configuration);
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) configuration;
			
			assembly = conf.assembly;
			netRuntime = conf.netRuntime;
			compiletarget = conf.compiletarget;
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
