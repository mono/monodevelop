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
using MonoDevelop.Core.Serialization;

namespace CSharpBinding
{
	public enum LangVersion {
		Default = 0,
		ISO_1   = 1,
		ISO_2   = 2
	}
	
	/// <summary>
	/// This class handles project specific compiler parameters
	/// </summary>
	public class CSharpCompilerParameters: ConfigurationParameters
	{
		// Configuration parameters
		
		[ItemProperty ("WarningLevel")]
		int  warninglevel = 4;
		
		[ItemProperty ("NoWarn", DefaultValue = "")]
		string noWarnings = String.Empty;
		
		[ItemProperty ("Optimize")]
		bool optimize;
		
		[ItemProperty ("AllowUnsafeBlocks", DefaultValue = false)]
		bool unsafecode = false;
		
		[ItemProperty ("CheckForOverflowUnderflow", DefaultValue = false)]
		bool generateOverflowChecks;
		
		[ItemProperty ("DefineConstants", DefaultValue = "")]
		string definesymbols = String.Empty;
		
		[ItemProperty ("GenerateDocumentation", DefaultValue = false)]
		bool generateXmlDocumentation = false;
		
		[ItemProperty ("additionalargs", DefaultValue = "")]
		string additionalArgs = string.Empty;
		
		[ItemProperty ("LangVersion", DefaultValue = "Default")]
		string langVersion = "Default";
		
		[ItemProperty ("NoStdLib", DefaultValue = false)]
		bool noStdLib;
		
		[ItemProperty ("TreatWarningsAsErrors", DefaultValue = false)]
		bool treatWarningsAsErrors;

		[ItemProperty("PlatformTarget", DefaultValue="")]
		string platformTarget = "";
		
		
		#region Members required for backwards compatibility. Not used for anything else.
		
		[ItemProperty ("StartupObject", DefaultValue = null)]
		internal string mainclass;
		
		[ProjectPathItemProperty ("ApplicationIcon", DefaultValue = null)]
		internal string win32Icon;

		[ProjectPathItemProperty ("Win32Resource", DefaultValue = null)]
		internal string win32Resource;
	
		[ItemProperty ("CodePage", DefaultValue = null)]
		internal string codePage;
		
		#endregion
		
		
		protected override void OnEndLoad ()
		{
			base.OnEndLoad ();
			
			// Backwards compatibility. Move parameters to the project parameters object
			if (ParentConfiguration != null && ParentConfiguration.ProjectParameters != null) {
				CSharpProjectParameters cparams = (CSharpProjectParameters) ParentConfiguration.ProjectParameters;
				if (win32Icon != null) {
					cparams.Win32Icon = win32Icon;
					win32Icon = null;
				}
				if (win32Resource != null) {
					cparams.Win32Resource = win32Resource;
					win32Resource = null;
				}
				if (mainclass != null) {
					cparams.MainClass = mainclass;
					mainclass = null;
				}
				if (!string.IsNullOrEmpty (codePage)) {
					cparams.CodePage = int.Parse (codePage);
					codePage = null;
				}
			}
		}
	
		public string AdditionalArguments {
			get { return additionalArgs; }
			set { additionalArgs = value ?? string.Empty; }
		}
		
		public LangVersion LangVersion {
			get {
				string val = langVersion.ToString ().Replace ('-','_'); 
				return (LangVersion) Enum.Parse (typeof(LangVersion), val); 
			}
			set {
				langVersion = value.ToString ().Replace ('_','-'); 
			}
		}

#region Code Generation
		
		public string DefineSymbols {
			get {
				return definesymbols;
			}
			set {
				definesymbols = value ?? string.Empty;
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
		
		public bool UnsafeCode {
			get {
				return unsafecode;
			}
			set {
				unsafecode = value;
			}
		}
		
		public bool GenerateOverflowChecks {
			get {
				return generateOverflowChecks;
			}
			set {
				generateOverflowChecks = value;
			}
		}
		
		public bool GenerateXmlDocumentation {
			get {
				return generateXmlDocumentation;
			}
			set {
				generateXmlDocumentation = value;
			}
		}
		
		public string PlatformTarget {
			get {
				return platformTarget;
			}
			set {
				platformTarget = value ?? string.Empty;
			}
		}
		
#endregion

#region Errors and Warnings 
		public int WarningLevel {
			get {
				return warninglevel;
			}
			set {
				warninglevel = value;
			}
		}
		
		public string NoWarnings {
			get {
				return noWarnings;
			}
			set {
				noWarnings = value;
			}
		}

		public bool NoStdLib {
			get {
				return noStdLib;
			}
			set {
				noStdLib = value;
			}
		}

		public bool TreatWarningsAsErrors {
			get {
				return treatWarningsAsErrors;
			}
			set {
				treatWarningsAsErrors = value;
			}
		}
#endregion
	}
}
