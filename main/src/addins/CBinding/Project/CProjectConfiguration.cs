//
// CProjectConfiguration.cs: Configuration for C/C++ projects
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
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

using Mono.Addins;

using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace CBinding
{
	public enum CompileTarget {
		Bin,
		StaticLibrary,
		SharedLibrary
	};
	
	public enum WarningLevel {
		None,
		Normal,
		All
	}
	
	public class CProjectConfiguration : ProjectConfiguration
	{
		[ItemProperty("OutputName")]
		string output = string.Empty;
		
		[ItemProperty("CompileTarget")]
		CBinding.CompileTarget target = CBinding.CompileTarget.Bin;
		
		[ItemProperty ("Includes")]
		[ItemProperty ("Include", Scope = "*", ValueType = typeof(string))]
    	private ArrayList includes = new ArrayList ();
		
		[ItemProperty ("LibPaths")]
		[ItemProperty ("LibPath", Scope = "*", ValueType = typeof(string))]
    	private ArrayList libpaths = new ArrayList ();
		
		[ItemProperty ("Libs")]
		[ItemProperty ("Lib", Scope = "*", ValueType = typeof(string))]
    	private ArrayList libs = new ArrayList ();
		
		[ItemProperty ("WarningLevel", DefaultValue=WarningLevel.Normal)]
		private WarningLevel warning_level = WarningLevel.Normal;
		
		[ItemProperty ("WarningsAsErrors", DefaultValue=false)]
		private bool warnings_as_errors = false;
		
		[ItemProperty ("OptimizationLevel", DefaultValue=0)]
		private int optimization = 0;
		
		[ItemProperty ("ExtraCompilerArguments", DefaultValue="")]
		private string extra_compiler_args = string.Empty;
		
		[ItemProperty ("ExtraLinkerArguments", DefaultValue="")]
		private string extra_linker_args = string.Empty;
		
		[ItemProperty ("DefineSymbols", DefaultValue="")]
		private string define_symbols = string.Empty;
		
		[ProjectPathItemProperty ("SourceDirectory", DefaultValue=null)]
		private string source_directory_path;
		
		[ItemProperty ("UseCcache", DefaultValue=false)]
		private bool use_ccache = false;
		
		[ItemProperty ("PrecompileHeaders", DefaultValue=true)]
		private bool precompileHeaders = true;
		
		public string Output {
			get { return output; }
			set { output = value; }
		}
		
		public CompileTarget CompileTarget {
			get { return target; }
			set { target = value; }
		}

		// TODO: This should be revised to use the naming conventions depending on OS
		public string CompiledOutputName {
			get {
				string suffix = string.Empty;
				string prefix = string.Empty;
				
				switch (target)
				{
				case CompileTarget.Bin:
					break;
				case CompileTarget.StaticLibrary:
					if (!Output.StartsWith ("lib"))
						prefix = "lib";
					if (!Output.EndsWith (".a"))
						suffix = ".a";
					break;
				case CompileTarget.SharedLibrary:
					if (!Output.StartsWith ("lib"))
						prefix = "lib";
					if (!Output.EndsWith (".so"))
						suffix = ".so";
					break;
				}
				
				return string.Format("{0}{1}{2}", prefix, Output, suffix);
			}
		}
		
		public string SourceDirectory {
			get { return source_directory_path; }
			set { source_directory_path = value; }
		}
		
		public ArrayList Includes {
			get { return includes; }
			set { includes = value; }
		}
		
		public ArrayList LibPaths {
			get { return libpaths; }
			set { libpaths = value; }
		}
		
		public ArrayList Libs {
			get { return libs; }
			set { libs = value; }
		}
		
		public bool UseCcache {
			get { return use_ccache; }
			set { use_ccache = value; }
		}
		
		public bool PrecompileHeaders {
			get { return precompileHeaders; }
			set { precompileHeaders = value; }
		}
		
		public WarningLevel WarningLevel {
			get { return warning_level; }
			set { warning_level = value; }
		}
		
		public bool WarningsAsErrors {
			get { return warnings_as_errors; }
			set { warnings_as_errors = value; }
		}
		
		public int OptimizationLevel {
			get { return optimization; }
			set {
				if (value >= 0 && value <= 3)
					optimization = value;
				else
					optimization = 0;
			}
		}
		
		public string ExtraCompilerArguments {
			get { return extra_compiler_args; }
			set { extra_compiler_args = value; }
		}
		
		public string ExtraLinkerArguments {
			get { return extra_linker_args; }
			set { extra_linker_args = value; }
		}
		
		public string DefineSymbols {
			get { return define_symbols; }
			set { define_symbols = value; }
		}
		
		public override void CopyFrom (ItemConfiguration configuration)
		{
			base.CopyFrom (configuration);
			CProjectConfiguration conf = (CProjectConfiguration)configuration;
			
			output = conf.output;
			target = conf.target;
			includes = conf.includes;
			libpaths = conf.libpaths;
			libs = conf.libs;
			source_directory_path = conf.source_directory_path;
			use_ccache = conf.use_ccache;
			
			warning_level = conf.warning_level;
			warnings_as_errors = conf.warnings_as_errors;
			optimization = conf.optimization;
			extra_compiler_args = conf.extra_compiler_args;
			extra_linker_args = conf.extra_linker_args;
			define_symbols = conf.define_symbols;
		}
	}
}
