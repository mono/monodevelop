//
// ValaCompilationParameters.cs: Project compilation parameters
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
using System.Xml;
using System.Diagnostics;
using System.Collections;

using Mono.Addins;

using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

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
