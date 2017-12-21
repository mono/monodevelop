//
// CSharpProjectExtension.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;
using System.Collections.Generic;
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.CSharp.Project
{
	class CSharpProject: DotNetProject, ICSharpProject
	{
		[ItemProperty ("StartupObject", DefaultValue = "")]
		string mainclass = string.Empty;

		[ProjectPathItemProperty ("ApplicationIcon", DefaultValue = "")]
		string win32Icon = String.Empty;

		[ProjectPathItemProperty ("Win32Resource", DefaultValue = "")]
		string win32Resource = String.Empty;

		[ItemProperty ("CodePage", DefaultValue = 0)]
		int codePage;

		// Keep the platforms combo of CodeGenerationPanelWidget in sync with this list
		public static IList<string> SupportedPlatforms = new string[] { "anycpu", "x86", "x64", "itanium" };

		public CSharpProject ()
		{
			Initialize (this);
		}

		protected override void OnInitialize ()
		{
			base.OnInitialize ();
			SupportsRoslyn = true;
			StockIcon = "md-csharp-project";
		}

		protected override void OnGetDefaultImports (List<string> imports)
		{
			base.OnGetDefaultImports (imports);
			imports.Add ("$(MSBuildBinPath)\\Microsoft.CSharp.targets");
		}

		public string MainClass {
			get {
				return mainclass;
			}
			set {
				mainclass = value ?? string.Empty;
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

		public string Win32Icon {
			get {
				return win32Icon;
			}
			set {
				win32Icon = value ?? string.Empty;
			}
		}

		public string Win32Resource {
			get {
				return win32Resource;
			}
			set {
				win32Resource = value ?? string.Empty;
			}
		}

		protected override void OnReadConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IPropertySet pset)
		{
			base.OnReadConfiguration (monitor, config, pset);

			// Backwards compatibility. Move parameters to the project parameters object

			var prop = pset.GetProperty ("ApplicationIcon");
			if (prop != null)
				win32Icon = prop.GetPathValue ();

			prop = pset.GetProperty ("Win32Resource");
			if (prop != null)
				win32Resource = prop.GetPathValue ();

			prop = pset.GetProperty ("StartupObject");
			if (prop != null)
				mainclass = prop.Value;

			prop = pset.GetProperty ("CodePage");
			if (prop != null)
				codePage = int.Parse (prop.Value);
		}

		protected override BuildResult OnCompileSources (ProjectItemCollection items, DotNetProjectConfiguration configuration, ConfigurationSelector configSelector, ProgressMonitor monitor)
		{
			return CSharpBindingCompilerManager.Compile (items, configuration, configSelector, monitor);
		}

		protected override DotNetCompilerParameters OnCreateCompilationParameters (DotNetProjectConfiguration config, ConfigurationKind kind)
		{
			var pars = new CSharpCompilerParameters ();
			if (SupportedPlatforms.Contains (config.Platform))
				pars.PlatformTarget = config.Platform;
			
			if (kind == ConfigurationKind.Debug)
				pars.AddDefineSymbol ("DEBUG");
			else if (kind == ConfigurationKind.Release)
				pars.Optimize = true;
			return pars;
		}

		protected override ClrVersion[] OnGetSupportedClrVersions ()
		{
			return new ClrVersion[] { 
				ClrVersion.Net_1_1, 
				ClrVersion.Net_2_0, 
				ClrVersion.Clr_2_1,
				ClrVersion.Net_4_0,
				ClrVersion.Net_4_5
			};
		}

		protected override string OnGetDefaultResourceId (ProjectFile projectFile)
		{
			return CSharpResourceIdBuilder.GetDefaultResourceId (projectFile) ?? base.OnGetDefaultResourceId (projectFile);
		}
	}

	public interface ICSharpProject
	{
		string MainClass { get; set; }

		int CodePage { get; set; }

		string Win32Icon { get; set; }

		string Win32Resource { get; set; }
	}

	internal static class Counters
	{
		public static Counter ResolveTime = InstrumentationService.CreateCounter ("Resolve Time", "Timing");
	}
}

