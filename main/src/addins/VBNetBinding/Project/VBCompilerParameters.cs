//  VBCompilerParameters.cs
//
//  This file was derived from a file from #Develop. 
//
//  Authors:
//    Markus Palme <MarkusPalme@gmx.de>
//    Rolf Bjarne Kvinge <RKvinge@novell.com>
//
//  Copyright (C) 2001-2007 Markus Palme <MarkusPalme@gmx.de>
//  Copyright (C) 2008 Novell, Inc. (http://www.novell.com)
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
using System.ComponentModel;

using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.VBNetBinding {
	public class VBCompilerParameters: ICloneable
	{
		//
		// Project level properties:
		// - StartupObject <string>
		// - RootNamespace <string>
		// - FileAlignment <int>
		// - MyType <string> WindowsForms | Windows | Console | WebControl
		// - TargetFrameworkversion <string> v2.0 | v3.0 | v3.5
		// - OptionExplicit <string> On | Off
		// - OptionCompare <string> Binary | Text
		// - OptionStrict <string> On | Off
		// - OptionInfer <string> On | Off
		// - ApplicationIcon <string>
		//
		// Configuration level properties:
		// - DebugSymbols <bool>
		// - DefineDebug <bool>
		// - DefineTrace <bool>
		// - DebugType <string> full | pdbonly | none
		// - DocumentationFile <string>
		// - NoWarn <string> comma separated list of numbers
		// - WarningsAsErrors <string> comma separated list of numbers
		// - TreatWarningsAsErrors <bool>
		// - WarningLevel <int> if 0, disable warnings.
		// - Optimize <bool>
		// - DefineConstants <string>
		// - RemoveIntegerChecks <bool>
		//

		[ItemProperty ("DefineDebug")]
		bool defineDebug = false;

		[ItemProperty ("DefineTrace")]
		bool defineTrace = false;
		
		[ItemProperty ("DebugType", DefaultValue="full")]
		string debugType = "full";
		
		[ItemProperty ("DocumentationFile")]
		string documentationFile = string.Empty;
		
		[ItemProperty ("NoWarn")]
		string noWarnings = string.Empty;
		
		[ItemProperty ("WarningsAsErrors")]
		string warningsAsErrors = string.Empty;
		
		[ItemProperty ("TreatWarningsAsErrors")]
		bool treatWarningsAsErrors = false;

		[ItemProperty ("WarningLevel", DefaultValue=1)]
		int warningLevel = 1; // 0: disable warnings, 1: enable warnings
		
		[ItemProperty ("Optimize", DefaultValue=false)]
		bool optimize = false;

		[ItemProperty ("DefineConstants", DefaultValue="")]
		string definesymbols = String.Empty;
		
		[ItemProperty ("RemoveIntegerChecks", DefaultValue=false)]
		bool generateOverflowChecks = false;

		// MD-only properties
		
		[ItemProperty ("AdditionalParameters")]
		string additionalParameters = String.Empty;
		
		public bool DefineDebug {
			get { return defineDebug; }
			set { defineDebug = value; }
		}

		public bool DefineTrace {
			get { return defineTrace; }
			set { defineTrace = value; }
		}

		public string DebugType {
			get { return debugType; }
			set { debugType = value; }
		}

		public string DocumentationFile {
			get { return documentationFile; }
			set { documentationFile = value; }
		}

		public string NoWarn {
			get { return noWarnings; }
			set { noWarnings = value; }
		}

		public string WarningsAsErrors {
			get { return warningsAsErrors; }
			set { warningsAsErrors = value; }
		}

		public bool TreatWarningsAsErrors {
			get { return treatWarningsAsErrors; }
			set { treatWarningsAsErrors = value; }
		}

		public bool Optimize {
			get { return optimize; }
			set { optimize = value; }
		}

		public string DefineConstants {
			get { return definesymbols; }
			set { definesymbols = value; }
		}

		public bool RemoveIntegerChecks {
			get { return generateOverflowChecks; }
			set { generateOverflowChecks = value; }
		}

		public bool WarningsDisabled {
			get { return warningLevel == 0; }
			set { warningLevel = value ? 0 : 1; }
		}
		
		// MD-only properties
		
		public string AdditionalParameters {
			get { return additionalParameters; }
			set { additionalParameters = value; }
		}

		public object Clone ()
		{
			return MemberwiseClone ();
		}
	}
}
