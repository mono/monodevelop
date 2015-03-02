//
// ValaProjectConfiguration.cs: Configuration for Vala projects
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
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

namespace MonoDevelop.ValaBinding
{
	public enum CompileTarget {
		Bin,
		StaticLibrary,
		SharedLibrary
	};
	
	public class ValaProjectConfiguration : ProjectConfiguration
	{
		[ItemProperty("Output/output")]
		string output = string.Empty;
		
		[ItemProperty("Build/target")]
		ValaBinding.CompileTarget target = ValaBinding.CompileTarget.Bin;
		
		[ItemProperty ("Includes")]
		[ProjectPathItemProperty ("Include", Scope = "*", ValueType = typeof(string))]
		private ArrayList includes = new ArrayList ();
		
//		[ItemProperty ("LibPaths")]
//		[ProjectPathItemProperty ("LibPath", Scope = 1, ValueType = typeof(string))]
//		private ArrayList libpaths = new ArrayList ();
//		
		[ItemProperty ("Libs")]
		[ItemProperty ("Lib", Scope = "*", ValueType = typeof(string))]
		private ArrayList libs = new ArrayList ();
		
		[ItemProperty ("CodeGeneration",
					  FallbackType = typeof(UnknownCompilationParameters))]
		ICloneable compilationParameters;
		
		[ProjectPathItemProperty ("SourceDirectory")]
		private string source_directory_path;
		
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

        public string CompiledOutputName
        {
            get
            {
                string suffix = string.Empty;
                string prefix = string.Empty;

                switch (target)
                {
                    case CompileTarget.Bin:
                        if (MonoDevelop.Core.Platform.IsWindows)
                        {
                            if (!Output.EndsWith(".exe"))
                                suffix = ".exe";
                        }
                        break;
                    
                    case CompileTarget.StaticLibrary:
                        if (!Output.StartsWith("lib"))
                            prefix = "lib";
                        if (!Output.EndsWith(".a"))
                            suffix = ".a";
                        break;
                    
                    case CompileTarget.SharedLibrary:
                        if (!Output.StartsWith("lib"))
                            prefix = "lib";
                        if (MonoDevelop.Core.Platform.IsWindows)
                        {
                            if (!Output.EndsWith(".dll"))
                                suffix = ".dll";
                        }
                        else
                        {
                            if (!Output.EndsWith(".so"))
                            {
                                suffix = ".so";
                            }
                        }

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
		
		public ArrayList Libs {
			get { return libs; }
			set { libs = value; }
		}
		
		public override void CopyFrom (ItemConfiguration configuration)
		{
			base.CopyFrom (configuration);
			ValaProjectConfiguration conf = (ValaProjectConfiguration)configuration;
			
			output = conf.output;
			target = conf.target;
			includes = conf.includes;
			libs = conf.libs;
			source_directory_path = conf.source_directory_path;
			
			if (conf.CompilationParameters == null) {
				compilationParameters = null;
			} else {
				compilationParameters = (ICloneable)conf.compilationParameters.Clone ();
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
