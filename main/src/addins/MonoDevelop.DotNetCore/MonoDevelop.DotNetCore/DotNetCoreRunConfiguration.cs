//
// DotNetCoreRunConfiguration.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using System;
using System.Linq;

namespace MonoDevelop.DotNetCore
{
	public class DotNetCoreRunConfiguration : AssemblyRunConfiguration
	{
		bool webProject;

		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration.CurrentProfile property")]
		public bool LaunchBrowser { get; set; } = true;

		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration.CurrentProfile property")]
		public string LaunchUrl { get; set; } = null;

		[Obsolete ("Use MonoDevelop.AspNetCore.AspNetCoreRunConfiguration.CurrentProfile property")]
		public string ApplicationURL { get; set; } = "http://localhost:5000/";

		public DotNetCoreRunConfiguration (string name)
			: base (name)
		{
		}

		public DotNetCoreRunConfiguration (string name, bool isWeb)
			: base (name)
		{
			webProject = isWeb;
		}

		protected override void Read (IPropertySet pset)
		{
			base.Read (pset);
			ExternalConsole = pset.GetValue (nameof (ExternalConsole), !webProject);
		}

		protected override void Write (IPropertySet pset)
		{
			base.Write (pset);
			pset.SetValue (nameof (ExternalConsole), ExternalConsole, !webProject);
		}

		protected override void Initialize (Project project)
		{
			webProject = project.GetFlavor<DotNetCoreProjectExtension> ()?.IsWeb ?? false;
			base.Initialize (project);
			ExternalConsole = !webProject;
		}

		[ItemProperty (DefaultValue = null)]
		public PipeTransportSettings PipeTransport { get; set; }

		protected override void OnCopyFrom (ProjectRunConfiguration config, bool isRename)
		{
			base.OnCopyFrom (config, isRename);

			var other = (DotNetCoreRunConfiguration)config;

			if (other.PipeTransport == null)
				PipeTransport = null;
			else
				PipeTransport = new PipeTransportSettings (other.PipeTransport);
		}
	}

	public class PipeTransportSettings
	{
		public PipeTransportSettings ()
		{ }

		public PipeTransportSettings (PipeTransportSettings copy)
		{
			WorkingDirectory = copy.WorkingDirectory;
			Program = copy.Program;
			Arguments = copy.Arguments.ToArray ();//make copy of array
			DebuggerPath = copy.DebuggerPath;
			EnvironmentVariables = new EnvironmentVariableCollection (copy.EnvironmentVariables);
		}

		[ItemProperty (DefaultValue = null)]
		public string WorkingDirectory { get; set; }
		[ItemProperty (DefaultValue = null)]
		public string Program { get; set; }
		[ItemProperty (SkipEmpty = true)]
		public string [] Arguments { get; set; } = new string [0];
		[ItemProperty (DefaultValue = null)]
		public string DebuggerPath { get; set; }
		[ItemProperty (SkipEmpty = true, WrapObject = false)]
		public EnvironmentVariableCollection EnvironmentVariables { get; private set; } = new EnvironmentVariableCollection ();
	}
}
