//  CSharpCompilerParameters.cs
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
using System.ComponentModel;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

namespace CSharpBinding
{
	public enum CsharpCompiler {
		Csc,
		Mcs
	};
	
	/// <summary>
	/// This class handles project specific compiler parameters
	/// </summary>
	public class CSharpCompilerParameters: ICloneable
	{
		// Configuration parameters
		
		[ItemProperty ("compiler")]
		CsharpCompiler csharpCompiler = CsharpCompiler.Mcs;
		
		[ItemProperty ("warninglevel")]
		int  warninglevel = 4;
		
		[ItemProperty ("nowarn", DefaultValue = "")]
		string noWarnings = String.Empty;
		
		[ItemProperty ("optimize")]
		bool optimize = true;
		
		[ItemProperty ("unsafecodeallowed")]
		bool unsafecode = false;
		
		[ItemProperty ("generateoverflowchecks")]
		bool generateOverflowChecks = true;
		
		[ItemProperty ("mainclass")]
		string mainclass = null;
		
		[ItemProperty ("definesymbols", DefaultValue = "")]
		string definesymbols = String.Empty;
		
		[ItemProperty ("generatexmldocumentation")]
		bool generateXmlDocumentation = false;
		
		[ProjectPathItemProperty ("win32Icon", DefaultValue = "")]
		string win32Icon = String.Empty;

		[ProjectPathItemProperty ("win32Resource", DefaultValue = "")]
		string win32Resource = String.Empty;
	
		[ItemProperty ("codepage", DefaultValue = 0)]
		int codePage;
	
		public object Clone ()
		{
			return MemberwiseClone ();
		}
		
		[Browsable(false)]
		public CsharpCompiler CsharpCompiler {
			get {
				return csharpCompiler;
			}
			set {
				csharpCompiler = value;
			}
		}
		
		public int CodePage {
			get {
				return codePage;
			}
			set {
				codePage = value;
			}
		}
		
		[Browsable(false)]
		public string Win32Icon {
			get {
				return win32Icon;
			}
			set {
				win32Icon = value;
			}
		}
		
		[Browsable(false)]
		public string Win32Resource {
			get {
				return win32Resource;
			}
			set {
				win32Resource = value;
			}
		}

#region Code Generation
		[DefaultValue("")]
		public string MainClass {
			get {
				return mainclass;
			}
			set {
				mainclass = value;
			}
		}
		
		[DefaultValue("")]
		public string DefineSymbols {
			get {
				return definesymbols;
			}
			set {
				definesymbols = value;
			}
		}
		
		[DefaultValue(true)]
		public bool Optimize {
			get {
				return optimize;
			}
			set {
				optimize = value;
			}
		}
		
		[DefaultValue(false)]
		public bool UnsafeCode {
			get {
				return unsafecode;
			}
			set {
				unsafecode = value;
			}
		}
		
		[DefaultValue(true)]
		public bool GenerateOverflowChecks {
			get {
				return generateOverflowChecks;
			}
			set {
				generateOverflowChecks = value;
			}
		}
		
		[DefaultValue(false)]
		public bool GenerateXmlDocumentation {
			get {
				return generateXmlDocumentation;
			}
			set {
				generateXmlDocumentation = value;
			}
		}
		
#endregion

#region Errors and Warnings 
		[DefaultValue(4)]
		public int WarningLevel {
			get {
				return warninglevel;
			}
			set {
				warninglevel = value;
			}
		}
		
		[DefaultValue("")]
		public string NoWarnings {
			get {
				return noWarnings;
			}
			set {
				noWarnings = value;
			}
		}
#endregion
	}
}
