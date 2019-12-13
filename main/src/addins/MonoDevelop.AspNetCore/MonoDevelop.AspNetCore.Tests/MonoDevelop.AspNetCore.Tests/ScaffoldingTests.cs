//
// ScaffoldingTests.cs
//
// Author:
//       jasonimison <jaimison@microsoft.com>
//
// Copyright (c) 2019 ${CopyrightHolder}
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
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.WebTools.Scaffolding.Core.Config;
using MonoDevelop.AspNetCore.Scaffolding;
using MonoDevelop.Projects;
using NUnit.Framework;

namespace MonoDevelop.AspNetCore.Tests
{
	[TestFixture]
	class ScaffoldingTests
	{
		[Test]
		public void RazorPageScaffolder ()
		{
			var args = new ScaffolderArgs ();
			args.ParentFolder = "/MyProject/Pages";
			using var project = CreateProject ();
			args.Project = project;
			var scaffolder = new RazorPageScaffolder (args);
			args.Scaffolder = scaffolder;
			scaffolder.GetField ("Name of the Razor Page:").SelectedValue = "PageName";
			var wizard = CreateWizard (args, project);
			var commandLineArgs = wizard.GetArguments (args);
			commandLineArgs = Regex.Replace (commandLineArgs, @"\s+", " ");
			Assert.AreEqual (@"aspnet-codegenerator --project ""ProjectName.csproj"" razorpage PageName Empty --referenceScriptLibraries --no-build -outDir ""/MyProject/Pages"" --namespaceName ProjectName", commandLineArgs);
		}

		[Test]
		public void RazorPageScaffolderWithoutLayoutPage ()
		{
			var args = new ScaffolderArgs ();
			args.ParentFolder = "/MyProject/Pages";
			using var project = CreateProject ();
			args.Project = project;
			var scaffolder = new RazorPageScaffolder (args);
			args.Scaffolder = scaffolder;
			scaffolder.GetField ("Name of the Razor Page:").SelectedValue = "PageName";
			(scaffolder.GetField ("Use a layout page") as BoolField).Selected = false;
			var wizard = CreateWizard (args, project);
			var commandLineArgs = wizard.GetArguments (args);
			commandLineArgs = Regex.Replace (commandLineArgs, @"\s+", " ");
			Assert.AreEqual (@"aspnet-codegenerator --project ""ProjectName.csproj"" razorpage PageName Empty --referenceScriptLibraries --useDefaultLayout --no-build -outDir ""/MyProject/Pages"" --namespaceName ProjectName", commandLineArgs);
		}

		[Test]
		public void RazorPageScaffolderWithoutReferenceScriptLibraries ()
		{
			var args = new ScaffolderArgs ();
			args.ParentFolder = "/MyProject/Pages";
			using var project = CreateProject ();
			args.Project = project;
			var scaffolder = new RazorPageScaffolder (args);
			args.Scaffolder = scaffolder;
			scaffolder.GetField ("Name of the Razor Page:").SelectedValue = "PageName";
			var wizard = CreateWizard (args, project);
			(scaffolder.GetField ("Reference script libraries") as BoolField).Selected = false;
			var commandLineArgs = wizard.GetArguments (args);
			commandLineArgs = Regex.Replace (commandLineArgs, @"\s+", " ");
			Assert.AreEqual (@"aspnet-codegenerator --project ""ProjectName.csproj"" razorpage PageName Empty --no-build -outDir ""/MyProject/Pages"" --namespaceName ProjectName", commandLineArgs);
		}

		[Test]
		public void RazorPageEntityFrameworkScaffolder ()
		{
			var args = new ScaffolderArgs ();
			args.ParentFolder = "/MyProject/Pages";
			using var project = CreateProject ();
			args.Project = project;
			var scaffolder = new RazorPageEntityFrameworkScaffolder (args);
			args.Scaffolder = scaffolder;
			scaffolder.GetField ("Name of the Razor Page:").SelectedValue = "PageName";
			scaffolder.GetField ("Model class to use:").SelectedValue = "ModelClass";
			scaffolder.GetField ("DbContext class to use:").SelectedValue = "DataContext";
			var wizard = CreateWizard (args, project);
			var commandLineArgs = wizard.GetArguments (args);
			commandLineArgs = Regex.Replace (commandLineArgs, @"\s+", " ");
			Assert.AreEqual (@"aspnet-codegenerator --project ""ProjectName.csproj"" razorpage PageName --model ModelClass --dataContext DataContext --referenceScriptLibraries --no-build -outDir ""/MyProject/Pages"" --namespaceName ProjectName", commandLineArgs);
		}

		[Test]
		public void RazorPageEntityFrameworkCrudScaffolder ()
		{
			var args = new ScaffolderArgs ();
			args.ParentFolder = "/MyProject/Pages";
			using var project = CreateProject ();
			args.Project = project;
			var scaffolder = new RazorPageEntityFrameworkCrudScaffolder (args);
			args.Scaffolder = scaffolder;
			// no name field
			scaffolder.GetField ("Model class to use:").SelectedValue = "ModelClass";
			scaffolder.GetField ("DbContext class to use:").SelectedValue = "DataContext";
			var wizard = CreateWizard (args, project);
			var commandLineArgs = wizard.GetArguments (args);
			commandLineArgs = Regex.Replace (commandLineArgs, @"\s+", " ");
			Assert.AreEqual (@"aspnet-codegenerator --project ""ProjectName.csproj"" razorpage --model ModelClass --dataContext DataContext --referenceScriptLibraries --no-build -outDir ""/MyProject/Pages"" --namespaceName ProjectName", commandLineArgs);
		}

		[Test]
		public void EmptyMvcControllerScaffolder ()
		{
			var args = new ScaffolderArgs ();
			args.ParentFolder = "/MyProject/Controllers";
			using var project = CreateProject ();
			args.Project = project;
			var scaffolder = new EmptyMvcControllerScaffolder (args);
			args.Scaffolder = scaffolder;
			scaffolder.GetField ("Controller name:").SelectedValue = "ControllerName";
			var wizard = CreateWizard (args, project);
			var commandLineArgs = wizard.GetArguments (args);
			commandLineArgs = Regex.Replace (commandLineArgs, @"\s+", " ");
			Assert.AreEqual (@"aspnet-codegenerator --project ""ProjectName.csproj"" controller -name ControllerName --no-build -outDir ""/MyProject/Controllers"" --controllerNamespace ProjectName", commandLineArgs);
		}

		[Test]
		public void EmptyApiControllerScaffolder ()
		{
			var args = new ScaffolderArgs ();
			args.ParentFolder = "/MyProject/Controllers";
			using var project = CreateProject ();
			args.Project = project;
			var scaffolder = new EmptyApiControllerScaffolder (args);
			args.Scaffolder = scaffolder;
			var wizard = CreateWizard (args, project);
			var commandLineArgs = wizard.GetArguments (args);
			commandLineArgs = Regex.Replace (commandLineArgs, @"\s+", " ").TrimEnd ();
			Assert.AreEqual (@"aspnet-codegenerator --project ""ProjectName.csproj"" controller -name --no-build -outDir ""/MyProject/Controllers"" --controllerNamespace ProjectName --restWithNoViews", commandLineArgs);
		}

		[Test]
		public void MvcControllerWithActionsScaffolder ()
		{
			var args = new ScaffolderArgs ();
			args.ParentFolder = "/MyProject/Controllers";
			using var project = CreateProject ();
			args.Project = project;
			var scaffolder = new MvcControllerWithActionsScaffolder (args);
			args.Scaffolder = scaffolder;
			var wizard = CreateWizard (args, project);
			var commandLineArgs = wizard.GetArguments (args);
			commandLineArgs = Regex.Replace (commandLineArgs, @"\s+", " ").TrimEnd ();
			Assert.AreEqual (@"aspnet-codegenerator --project ""ProjectName.csproj"" controller -name --no-build -outDir ""/MyProject/Controllers"" --controllerNamespace ProjectName --readWriteActions", commandLineArgs);
		}

		[Test]
		public void ApiControllerEntityFrameworkScaffolder ()
		{
			var args = new ScaffolderArgs ();
			args.ParentFolder = "/MyProject/Controllers";
			using var project = CreateProject ();
			args.Project = project;
			var scaffolder = new ApiControllerEntityFrameworkScaffolder (args);
			args.Scaffolder = scaffolder;
			scaffolder.GetField ("Model class to use:").SelectedValue = "ModelClass";
			scaffolder.GetField ("DbContext class to use:").SelectedValue = "DataContext";
			var wizard = CreateWizard (args, project);
			var commandLineArgs = wizard.GetArguments (args);
			commandLineArgs = Regex.Replace (commandLineArgs, @"\s+", " ").TrimEnd ();
			Assert.AreEqual (@"aspnet-codegenerator --project ""ProjectName.csproj"" controller --model ModelClass --dataContext DataContext -name --no-build -outDir ""/MyProject/Controllers"" --controllerNamespace ProjectName", commandLineArgs);
		}

		[Test]
		public void ApiControllerWithActionsScaffolder ()
		{
			var args = new ScaffolderArgs ();
			args.ParentFolder = "/MyProject/Controllers";
			using var project = CreateProject ();
			args.Project = project;
			var scaffolder = new ApiControllerWithActionsScaffolder (args);
			args.Scaffolder = scaffolder;
			var wizard = CreateWizard (args, project);
			var commandLineArgs = wizard.GetArguments (args);
			commandLineArgs = Regex.Replace (commandLineArgs, @"\s+", " ").TrimEnd ();
			Assert.AreEqual (@"aspnet-codegenerator --project ""ProjectName.csproj"" controller -name --no-build -outDir ""/MyProject/Controllers"" --controllerNamespace ProjectName --restWithNoViews --readWriteActions", commandLineArgs);
		}

		[Test]
		[Ignore]
		public async void CanDeserializeConfig ()
		{
			var config = await ScaffoldingConfig.LoadFromJsonAsync ();
			Assert.IsTrue (config.NetStandard20Packages.Any ());
		}

		DotNetProject CreateProject ()
		{
			var info = new ProjectCreateInformation {
				ProjectBasePath = "/MyProject",
				ProjectName = "ProjectName"
			};

			var doc = new XmlDocument ();
			var projectOptions = doc.CreateElement ("Options");
			projectOptions.SetAttribute ("language", "C#");

			return (DotNetProject)Services.ProjectService.CreateProject ("C#", info, projectOptions);
		}

		static ScaffolderWizard CreateWizard (ScaffolderArgs args, DotNetProject project)
		{
			var selectPage = new ScaffolderTemplateSelectPage (args);
			return new ScaffolderWizard (project, args.ParentFolder, selectPage, args);
		}
	}

	static class ScaffolderFieldExtension
	{
		public static ScaffolderField GetField (this ScaffolderBase scaffolder, string displayName)
		{
			var field = scaffolder.Fields.FirstOrDefault (f => f.DisplayName == displayName);
			if (field != null)
				return field;

			var optionList = scaffolder.Fields.OfType<BoolFieldList> ().First ();

			return optionList.Options.FirstOrDefault (f => f.DisplayName == displayName);
		}
	}


}
