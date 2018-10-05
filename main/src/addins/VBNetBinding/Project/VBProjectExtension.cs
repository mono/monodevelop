//
// VBProjectExtension.cs
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
using MonoDevelop.Core.Serialization;
using System.Diagnostics;
using MonoDevelop.Core;
using System.Collections.Generic;

namespace MonoDevelop.VBNetBinding
{
	class VBProject: DotNetProject
	{
		[ItemProperty ("OptionInfer", DefaultValue="Off")]
		string optionInfer = "Off";

		[ItemProperty ("OptionExplicit", DefaultValue="On")]
		string optionExplicit = "On";

		[ItemProperty ("OptionCompare", DefaultValue="Binary")]
		string optionCompare = "Binary";

		[ItemProperty ("OptionStrict", DefaultValue="Off")]
		string optionStrict = "Off";

		[ItemProperty ("MyType", DefaultValue="")]
		string myType = string.Empty;

		[ItemProperty ("StartupObject", DefaultValue="")]
		string startupObject = string.Empty;

		[ProjectPathItemProperty ("ApplicationIcon", DefaultValue="")]
		string applicationIcon = string.Empty;

		[ItemProperty ("CodePage", DefaultValue="")]
		string codePage = string.Empty;

		public bool OptionInfer {
			get { return optionInfer == "On"; }
			set { optionInfer = value ? "On" : "Off"; }
		}

		public bool OptionExplicit {
			get { return optionExplicit == "On"; }
			set { optionExplicit = value ? "On" : "Off"; }
		}

		public bool BinaryOptionCompare {
			get { return optionCompare == "Binary"; }
			set { optionCompare = value ? "Binary" : "Text"; }
		}

		public bool OptionStrict {
			get { return optionStrict == "On"; }
			set { optionStrict = value ? "On" : "Off"; }
		}

		public string MyType {
			get { return myType; }
			set { myType = value ?? string.Empty; }
		}

		public string StartupObject {
			get { return startupObject; }
			set { startupObject = value ?? string.Empty; }
		}

		public string ApplicationIcon {
			get { return applicationIcon; }
			set { applicationIcon = value ?? string.Empty; }
		}

		public string CodePage {
			get { return codePage; }
			set { codePage = value ?? string.Empty; }
		}
		protected override void OnGetDefaultImports (List<string> imports)
		{
			base.OnGetDefaultImports (imports);
			imports.Add ("$(MSBuildBinPath)\\Microsoft.VisualBasic.targets");
		}

		protected override void OnInitialize ()
		{
			base.OnInitialize ();
			DefaultNamespaceIsImplicit = true;
			SupportsRoslyn = true;
			RoslynLanguageName = Microsoft.CodeAnalysis.LanguageNames.VisualBasic;

			StockIcon = "md-project";
		}

		[Obsolete]
		protected override BuildResult OnCompileSources (ProjectItemCollection items, DotNetProjectConfiguration configuration, ConfigurationSelector configSelector, MonoDevelop.Core.ProgressMonitor monitor)
		{
			return VBBindingCompilerServices.InternalCompile (items, configuration, configSelector, monitor);
		}

		protected override DotNetCompilerParameters OnCreateCompilationParameters (DotNetProjectConfiguration config, ConfigurationKind kind)
		{
			var pars = new VBCompilerParameters ();
			if (kind == ConfigurationKind.Debug)
				pars.AddDefineSymbol ("DEBUG");
			else if (kind == ConfigurationKind.Release)
				pars.Optimize = true;
			return pars;
		}

		protected override ClrVersion[] OnGetSupportedClrVersions ()
		{
			return new ClrVersion[] { ClrVersion.Net_2_0, ClrVersion.Net_4_0, ClrVersion.Net_4_5 };
		}

		protected override string OnGetDefaultResourceId (ProjectFile projectFile)
		{
			return VBNetResourceIdBuilder.GetDefaultResourceId (projectFile) ?? base.OnGetDefaultResourceId (projectFile);
		}

		protected override ProjectItem OnCreateProjectItem (IMSBuildItemEvaluated item)
		{
			if (item.Name == "Import")
				return new Import ();
			return base.OnCreateProjectItem (item);
		}
	}
}

