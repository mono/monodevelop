//  JavaCompilerParameters.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Xml;
using System.Diagnostics;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

namespace JavaBinding
{
	/// <summary>
	/// This class handles project specific compiler parameters
	/// </summary>
	public class JavaCompilerParameters: ICloneable
	{
		[ItemProperty("Deprecation", DefaultValue=true)]
		bool deprecation = true;
		
		[ItemProperty("Optimize", DefaultValue=true)]
		bool optimize = true;
		
		[ItemProperty("MainClass", DefaultValue=null)]
		string  mainclass = null;
		
		[ItemProperty("DefineSymbols", DefaultValue="")]
		string definesymbols = String.Empty;
		
		[ItemProperty("ClassPath", DefaultValue="")]
		string classpath = String.Empty;
		
		[ItemProperty ("Compiler", DefaultValue=JavaCompiler.Gcj)]
		JavaCompiler compiler = JavaCompiler.Gcj;

		[ItemProperty("CompilerPath", DefaultValue="gcj")]
		string compilerpath = "gcj";		
		
		[ItemProperty("GenWarnings", DefaultValue=false)]
		bool genwarnings = false;
		
		public object Clone ()
		{
			return MemberwiseClone ();
		}
		
		public bool GenWarnings {
			get {
				return genwarnings;
			}
			set {
				genwarnings = value;
			}
		}
		
		public string ClassPath {
			get {
				return classpath;
			}
			set {
				classpath = value;
			}
		}

		public JavaCompiler Compiler {
			get {
				return compiler;
			}
			set {
				compiler = value;
			}
		}
		
		public string CompilerPath {
			get {
				return compilerpath;
			}
			set {
				compilerpath = value;
			}
		}
		
		public bool Deprecation {
			get {
				return deprecation;
			}
			set {
				deprecation = value;
			}
		}
		
		public bool Optimize {
			get {
				return optimize;
			}
			set {
				optimize = value;
			}
		}
		
		public string MainClass {
			get {
				return mainclass;
			}
			set {
				mainclass = value;
			}
		}
		
		public string DefineSymbols {
			get {
				return definesymbols;
			}
			set {
				definesymbols = value;
			}
		}
	}
}
