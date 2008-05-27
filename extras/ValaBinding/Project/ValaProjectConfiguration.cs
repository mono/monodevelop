//
// ValaProjectConfiguration.cs: Configuration for Vala projects
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU Lesser General Public License as published by
//    the Free Software Foundation, either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//


using System;
using System.Collections;

using Mono.Addins;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

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
		[ProjectPathItemProperty ("Include", Scope = 1, ValueType = typeof(string))]
		private ArrayList includes = new ArrayList ();
		
//		[ItemProperty ("LibPaths")]
//		[ProjectPathItemProperty ("LibPath", Scope = 1, ValueType = typeof(string))]
//		private ArrayList libpaths = new ArrayList ();
//		
		[ItemProperty ("Libs")]
		[ItemProperty ("Lib", Scope = 1, ValueType = typeof(string))]
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
