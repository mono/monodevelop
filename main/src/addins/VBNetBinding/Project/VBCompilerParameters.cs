//  VBCompilerParameters.cs
//
//  This file was derived from a file from #Develop, and relicensed
//  by Markus Palme to MIT/X11
//
//  Authors:
//    Markus Palme <MarkusPalme@gmx.de>
//    Rolf Bjarne Kvinge <RKvinge@novell.com>
//
//  Copyright (C) 2008 Novell, Inc. (http://www.novell.com)
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

using System;
using System.ComponentModel;

using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.VBNetBinding
{
	public class VBCompilerParameters: ConfigurationParameters
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
		
		[ItemProperty ("DocumentationFile", DefaultValue="")]
		string documentationFile = string.Empty;
		
		[ItemProperty ("NoWarn", DefaultValue="")]
		string noWarnings = string.Empty;
		
		[ItemProperty ("WarningsAsErrors", DefaultValue="")]
		string warningsAsErrors = string.Empty;
		
		[ItemProperty ("TreatWarningsAsErrors", DefaultValue=false)]
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
		
		public override void AddDefineSymbol (string symbol)
		{
			DefineConstants += symbol + ";";
		}

		public override bool HasDefineSymbol (string symbol)
		{
			return DefineConstants.Contains (symbol);
		}

		public override void RemoveDefineSymbol (string symbol)
		{
			DefineConstants = DefineConstants.Replace (symbol + ";", "");
		}
		
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
	}
}
