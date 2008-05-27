//
// ValaCompilationParameters.cs: Project compilation parameters
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
using System.Xml;
using System.Diagnostics;
using System.Collections;

using Mono.Addins;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.ValaBinding
{
	public enum WarningLevel {
		None,
		Normal,
		All
	}
	
	public class ValaCompilationParameters : ICloneable
	{		
		[ItemProperty ("WarningLevel")]
		private WarningLevel warning_level = WarningLevel.Normal;
		
		[ItemProperty ("WarningsAsErrors")]
		private bool warnings_as_errors = false;
		
		[ItemProperty ("OptimizationLevel")]
		private int optimization = 0;
		
		[ItemProperty ("ExtraCompilerArguments")]
		private string extra_compiler_args = string.Empty;
		
		[ItemProperty ("DefineSymbols")]
		private string define_symbols = string.Empty;
		
		public object Clone ()
		{
			return MemberwiseClone ();
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
		
		public string DefineSymbols {
			get { return define_symbols; }
			set { define_symbols = value; }
		}
	}
}
