//  ProjectConfiguration.cs
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Xml;

using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects
{
	/// <summary>
	/// External language bindings may choose to extend this class.
	/// It makes things a bit easier.
	/// </summary>
	public abstract class ProjectConfiguration : SolutionItemConfiguration
	{
		[ProjectPathItemProperty ("OutputPath")]
		string directory = "." + Path.DirectorySeparatorChar.ToString();
		
		[ProjectPathItemProperty ("ExecuteBeforeBuild", DefaultValue = "")]
		string executeBeforeBuild = String.Empty;
		
		[ProjectPathItemProperty ("ExecuteAfterBuild", DefaultValue = "")]
		string executeAfterBuild = String.Empty;
		
		[ItemProperty ("DebugSymbols", DefaultValue=false)]
		bool debugmode;
		
		[ItemProperty ("SignAssembly", DefaultValue = false)]
		bool signAssembly = false;
		
		[ProjectPathItemProperty ("AssemblyKeyFile")]
		string assemblyKeyFile = String.Empty;
		
		[ProjectPathItemProperty ("ExecuteScript", DefaultValue = "")]
		string executeScript = String.Empty;
		
		[ItemProperty ("RunWithWarnings", DefaultValue=true)]
		bool runWithWarnings = true;
		
		[ItemProperty ("Commandlineparameters", DefaultValue = "")]
		string commandLineParameters = String.Empty;
		
		[ItemProperty ("Externalconsole", DefaultValue=false)]
		bool externalConsole = false;

		[ItemProperty ("ConsolePause", DefaultValue=true)]
		bool pauseconsoleoutput = true;

		[ItemProperty ("EnvironmentVariables")]
		[ItemProperty ("Variable", Scope="item")]
		[ItemProperty ("name", Scope="key")]
		[ItemProperty ("value", Scope="value")]
		Dictionary<string,string> envVars;

		public ProjectConfiguration ()
		{
		}
		
		public ProjectConfiguration (string name): base (name)
		{
		}
		
		public virtual string OutputDirectory {
			get { return directory; }
			set { directory = value; }
		}
		
		public virtual string ExecuteScript {
			get { return executeScript; }
			set { executeScript = value; }
		}
		
		public virtual bool RunWithWarnings {
			get { return runWithWarnings; }
			set { runWithWarnings = value; }
		}
		
		public bool DebugMode {
			get { return debugmode; }
			set { debugmode = value; }
		}
		
		public string CommandLineParameters {
			get { return commandLineParameters; }
			set { commandLineParameters = value; }
		}
		
		public bool ExternalConsole {
			get { return externalConsole; }
			set { externalConsole = value; }
		}
		
		public bool PauseConsoleOutput {
			get { return pauseconsoleoutput; }
			set { pauseconsoleoutput = value; }
		}
		
		public bool SignAssembly {
			get { return signAssembly; }
			set { signAssembly = value; }
		}
		
		public string AssemblyKeyFile {
			get { return assemblyKeyFile; }
			set { assemblyKeyFile = value; }
		}
		
		public Dictionary<string,string> EnvironmentVariables {
			get {
				if (envVars == null)
					envVars = new Dictionary<string,string> ();
				return envVars; 
			}
		}
		
		
		public override void CopyFrom (ItemConfiguration configuration)
		{
			base.CopyFrom (configuration);
			ProjectConfiguration conf = (ProjectConfiguration) configuration;
			
			directory = conf.directory;
			executeScript = conf.executeScript;
			executeBeforeBuild = conf.executeBeforeBuild;
			executeAfterBuild = conf.executeAfterBuild;
			runWithWarnings = conf.runWithWarnings;
			debugmode = conf.debugmode;
			commandLineParameters = conf.commandLineParameters;
			externalConsole = conf.externalConsole;
			pauseconsoleoutput = conf.pauseconsoleoutput;
			signAssembly = conf.signAssembly;
			assemblyKeyFile = conf.assemblyKeyFile;
			if (conf.envVars != null)
				envVars = new Dictionary<string,string> (conf.envVars);
			else
				envVars = null;
		}
	}
}
