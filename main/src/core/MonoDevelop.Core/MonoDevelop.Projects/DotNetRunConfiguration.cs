//
// DotNetRunConfiguration.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Core.Serialization;
using System.Linq;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Projects
{
	public class DotNetRunConfiguration: ProjectRunConfiguration
	{
		MonoExecutionParameters monoParameters = new MonoExecutionParameters ();

		public DotNetRunConfiguration (string name): base (name)
		{
		}

		[ItemProperty (DefaultValue = "")]
		public string StartArguments { get; set; } = "";

		[ItemProperty (DefaultValue = "")]
		public FilePath StartWorkingDirectory { get; set; } = "";

		[ItemProperty (DefaultValue = "")]
		public string StartAction { get; set; } = StartActions.Project;

		[ItemProperty (DefaultValue = "")]
		public FilePath StartProgram { get; set; } = "";

		[ItemProperty ("ConsolePause", DefaultValue = true)]
		public bool PauseConsoleOutput { get; set; } = true;

		[ItemProperty (DefaultValue = false)]
		public bool ExternalConsole { get; set; } = false;

		[ItemProperty (SkipEmpty = true, WrapObject = false)]
		public EnvironmentVariableCollection EnvironmentVariables { get; private set; } = new EnvironmentVariableCollection ();

		public class StartActions
		{
			public const string Project = "Project";
			public const string Program = "Program";
		}

		public bool IsEmpty {
			get { return string.IsNullOrEmpty (StartArguments) && string.IsNullOrEmpty (StartWorkingDirectory) && StartAction == StartActions.Project && EnvironmentVariables.Count == 0; }
		}

		internal protected override void Read (IPropertySet pset)
		{
			base.Read (pset);
			pset.ReadObjectProperties (monoParameters, monoParameters.GetType (), false);
		}

		internal protected override void Write (IPropertySet pset)
		{
			pset.WriteObjectProperties (monoParameters, monoParameters.GetType (), false);
			base.Write (pset);
		}

		public MonoExecutionParameters MonoParameters {
			get { return monoParameters; }
		}

		protected override void OnCopyFrom (ProjectRunConfiguration config, bool isRename)
		{
			base.OnCopyFrom (config, isRename);

			var other = (DotNetRunConfiguration)config;

			StartArguments = other.StartArguments;
			StartWorkingDirectory = other.StartWorkingDirectory;
			StartProgram = other.StartProgram;
			StartAction = other.StartAction;
			EnvironmentVariables = new EnvironmentVariableCollection (other.EnvironmentVariables);
			monoParameters = other.monoParameters.Clone ();
			ExternalConsole = other.ExternalConsole;
			PauseConsoleOutput = other.PauseConsoleOutput;
		}
	}
}

