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
using MonoDevelop.Projects.Serialization;

namespace CBinding
{
	public enum CompileTarget {
		Bin,
		StaticLibrary,
		SharedLibrary
	};
	
	public class CProjectConfiguration : AbstractProjectConfiguration
	{
		[ItemProperty("Output/output")]
		string output = string.Empty;
		
		[ItemProperty("Build/target")]
		CBinding.CompileTarget target = CBinding.CompileTarget.Bin;
		
		[ItemProperty ("Includes")]
		[ItemProperty ("Include", Scope = 1, ValueType = typeof(string))]
    	private ArrayList includes = new ArrayList ();
		
		[ItemProperty ("LibPaths")]
		[ItemProperty ("LibPath", Scope = 1, ValueType = typeof(string))]
    	private ArrayList libpaths = new ArrayList ();
		
		[ItemProperty ("Libs")]
		[ItemProperty ("Lib", Scope = 1, ValueType = typeof(string))]
    	private ArrayList libs = new ArrayList ();
		
		[ItemProperty ("CodeGeneration", FallbackType = typeof(UnknownCompilationParameters))]
		ICloneable compilationParameters;
		
		[ProjectPathItemProperty ("SourceDirectory")]
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

		public ICloneable CompilationParameters {
			get { return compilationParameters; }
			set { compilationParameters = value; }
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
		
		public override void CopyFrom (IConfiguration configuration)
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
			
			if (conf.CompilationParameters == null) {
				compilationParameters = null;
			} else {
				compilationParameters = (ICloneable)compilationParameters.Clone ();
			}
		}
	}
	
	public class UnknownCompilationParameters : ICloneable, IExtendedDataItem
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
