//
// DependencyNodeTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.DotNetCore.NodeBuilders;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Projects;
using NuGet.Versioning;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	class DependencyNodeTests : DotNetCoreTestBase
	{
		DotNetProject project;
		DependenciesNode dependenciesNode;
		SdkDependenciesNode sdkFolderNode;
		PackageDependenciesNode nugetFolderNode;
		TaskCompletionSource<bool> packageDependenciesChanged;
		TestableDependenciesNodeBuilder dependenciesNodeBuilder;
		// Ensure NuGet.Versioning assembly is loaded by the tests otherwise they fail
		// when run from the command line with mdtool.
		NuGetVersion nuGetVersion = new NuGetVersion ("1.0"); 

		[TearDown]
		public override void TearDown ()
		{
			project?.Dispose ();
			project = null;

			base.TearDown ();
		}

		void Restore (FilePath fileName)
		{
			CreateNuGetConfigFile (fileName.ParentDirectory);

			var process = Process.Start ("msbuild", $"/t:Restore /p:RestoreDisableParallel=true \"{fileName}\"");
			Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");
			Assert.AreEqual (0, process.ExitCode);
		}

		async Task CreateDependenciesNode ()
		{
			dependenciesNodeBuilder = new TestableDependenciesNodeBuilder ();
			dependenciesNode = new DependenciesNode (project);
			dependenciesNode.PackageDependencyCache.PackageDependenciesChanged += PackageDependenciesChanged;
			packageDependenciesChanged = new TaskCompletionSource<bool> ();

			dependenciesNode.PackageDependencyCache.Refresh ();

			await WaitForPackageDependenciesChanged ();

			dependenciesNodeBuilder.BuildChildNodes (null, dependenciesNode);
			nugetFolderNode = dependenciesNodeBuilder.PackageDependencies;
			sdkFolderNode = dependenciesNodeBuilder.SdkDependencies;
		}

		void PackageDependenciesChanged (object sender, EventArgs e)
		{
			packageDependenciesChanged.TrySetResult (true);
		}

		async Task WaitForPackageDependenciesChanged (int millisecondsTimeout = 60000)
		{
			var timeoutTask = Task.Delay (millisecondsTimeout);
			var result = await Task.WhenAny (timeoutTask, packageDependenciesChanged.Task);
			if (result == timeoutTask)
				Assert.Fail ("Timed out waiting for package dependencies to be updated.");
		}

		List<PackageDependencyNode> GetSdkFolderChildDependencies ()
		{
			var nodeBuilder = new TestableSdkDependenciesNodeBuilder ();
			nodeBuilder.BuildChildNodes (null, sdkFolderNode);
			return nodeBuilder.ChildNodesAsPackageDependencyNodes ().ToList ();
		}

		List<PackageDependencyNode> GetNuGetFolderChildDependencies ()
		{
			var nodeBuilder = new TestablePackageDependenciesNodeBuilder ();
			nodeBuilder.BuildChildNodes (null, nugetFolderNode);
			return nodeBuilder.ChildNodesAsPackageDependencyNodes ().ToList ();
		}

		[Test]
		public async Task NetStandardLibrary_NewtonsoftJsonNuGetPackageReference ()
		{
			FilePath projectFileName = Util.GetSampleProject ("DotNetCoreDependenciesFolder", "NetStandardJsonNet.csproj");
			Restore (projectFileName);
			project = (DotNetProject) await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName);
			await CreateDependenciesNode ();

			var sdkNode = GetSdkFolderChildDependencies ().Single ();
			Assert.AreEqual ("NETStandard.Library", sdkNode.Name);
			Assert.AreEqual ("NETStandard.Library", sdkNode.GetLabel ());
			Assert.IsTrue (sdkNode.IsReadOnly);
			Assert.IsFalse (sdkNode.CanBeRemoved);
			Assert.IsTrue (sdkNode.IsTopLevel);

			var childNodes = sdkNode.GetDependencyNodes ().ToList ();
			var systemThreadingNode = childNodes.FirstOrDefault (node => node.Name == "System.Threading");
			Assert.IsFalse (systemThreadingNode.CanBeRemoved);
			Assert.IsFalse (systemThreadingNode.IsTopLevel);

			var defaultNode = sdkFolderNode.GetDefaultNodes ().Single ();
			Assert.AreEqual ("NETStandard.Library", defaultNode.Name);
			Assert.AreEqual ("NETStandard.Library", defaultNode.GetLabel ());

			var newtonsoftNode = GetNuGetFolderChildDependencies ().Single ();
			Assert.AreEqual ("Newtonsoft.Json", newtonsoftNode.Name);
			Assert.AreEqual ("Newtonsoft.Json", newtonsoftNode.GetLabel ());
			Assert.AreEqual ("(10.0.3)", newtonsoftNode.GetSecondaryLabel ());
			Assert.IsTrue (newtonsoftNode.CanBeRemoved);
			Assert.IsFalse (newtonsoftNode.IsReadOnly);
			Assert.IsTrue (newtonsoftNode.IsTopLevel);
			Assert.IsTrue (newtonsoftNode.IsReleaseVersion ());
			Assert.IsTrue (newtonsoftNode.HasDependencies ());

			childNodes = newtonsoftNode.GetDependencyNodes ().ToList ();
			var microsoftCSharpNode = childNodes.FirstOrDefault (node => node.Name == "Microsoft.CSharp");
			Assert.IsFalse (microsoftCSharpNode.CanBeRemoved);
			Assert.IsFalse (microsoftCSharpNode.IsTopLevel);

			var packageReferenceNode = nugetFolderNode.GetProjectPackageReferencesAsDependencyNodes ().Single ();
			Assert.AreEqual ("Newtonsoft.Json", packageReferenceNode.Name);
			Assert.AreEqual ("Newtonsoft.Json", packageReferenceNode.GetLabel ());
			Assert.AreEqual ("(10.0.3)", packageReferenceNode.GetSecondaryLabel ());
			Assert.IsTrue (packageReferenceNode.CanBeRemoved);
			Assert.IsFalse (packageReferenceNode.IsReadOnly);
			Assert.IsTrue (packageReferenceNode.IsTopLevel);
			Assert.IsTrue (packageReferenceNode.IsReleaseVersion ());
			Assert.IsFalse (packageReferenceNode.HasDependencies ());

			// Check default sdk if project converted from .NET Standard to .NET Core 1.1.
			var moniker = TargetFrameworkMoniker.Parse (".NETCoreApp,Version=v1.1");
			project.TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (moniker);

			defaultNode = sdkFolderNode.GetDefaultNodes ().Single ();
			Assert.AreEqual ("Microsoft.NETCore.App", defaultNode.Name);
			Assert.AreEqual ("Microsoft.NETCore.App", defaultNode.GetLabel ());
		}

		[Test]
		public async Task NetStandardLibrary_OneNuGetDiagnosticWarningsForSystemComponentModelEventBasedAsync ()
		{
			FilePath projectFileName = Util.GetSampleProject ("DotNetCoreDependenciesFolder", "NetStandardOneNuGetWarning.csproj");
			Restore (projectFileName);
			project = (DotNetProject) await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName);
			await CreateDependenciesNode ();

			var sdkNode = GetSdkFolderChildDependencies ().Single ();
			Assert.AreEqual ("NETStandard.Library", sdkNode.Name);
			Assert.AreEqual ("NETStandard.Library", sdkNode.GetLabel ());
			Assert.IsTrue (sdkNode.IsReadOnly);
			Assert.IsFalse (sdkNode.CanBeRemoved);
			Assert.IsTrue (sdkNode.IsTopLevel);

			var componentModelNode = GetNuGetFolderChildDependencies ().Single ();
			Assert.AreEqual ("System.ComponentModel.EventBasedAsync", componentModelNode.Name);
			Assert.AreEqual ("System.ComponentModel.EventBasedAsync", componentModelNode.GetLabel ());
			Assert.AreEqual ("(4.0.10)", componentModelNode.GetSecondaryLabel ());
			Assert.IsTrue (componentModelNode.CanBeRemoved);
			Assert.IsFalse (componentModelNode.IsReadOnly);
			Assert.IsTrue (componentModelNode.IsTopLevel);
			Assert.IsTrue (componentModelNode.HasDependencies ());
			Assert.AreEqual (TaskSeverity.Warning, componentModelNode.GetStatusSeverity ());
			string diagnosticMessage = "Package 'System.ComponentModel.EventBasedAsync 4.0.10' was restored using '.NETFramework,Version=v4.6' instead of the project target framework '.NETStandard,Version=v1.6'. This package may not be fully compatible with your project.";
			Assert.AreEqual (componentModelNode.GetStatusMessage (), diagnosticMessage);

			// Diagnostic child node should be added to the componentModelNode.
			var diagnosticNode = componentModelNode.GetDependencyNodes ().Single ();
			Assert.AreEqual ("NU1701", diagnosticNode.Name);
			Assert.IsFalse (diagnosticNode.CanBeRemoved);
			Assert.AreEqual (TaskSeverity.Warning, diagnosticNode.GetStatusSeverity ());
			Assert.AreEqual (diagnosticNode.GetStatusMessage (), diagnosticMessage);
		}

		[Test]
		public async Task NetStandardLibrary_TwoNuGetDiagnosticWarningsForSystemComponentModelEventBasedAsync ()
		{
			FilePath projectFileName = Util.GetSampleProject ("DotNetCoreDependenciesFolder", "NetStandardTwoNuGetWarnings.csproj");
			Restore (projectFileName);
			project = (DotNetProject) await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName);
			await CreateDependenciesNode ();

			var sdkNode = GetSdkFolderChildDependencies ().Single ();
			Assert.AreEqual ("NETStandard.Library", sdkNode.Name);
			Assert.AreEqual ("NETStandard.Library", sdkNode.GetLabel ());
			Assert.IsTrue (sdkNode.IsReadOnly);
			Assert.IsFalse (sdkNode.CanBeRemoved);
			Assert.IsTrue (sdkNode.IsTopLevel);

			var componentModelNode = GetNuGetFolderChildDependencies ().Single ();
			Assert.AreEqual ("System.ComponentModel.EventBasedAsync", componentModelNode.Name);
			Assert.AreEqual ("System.ComponentModel.EventBasedAsync", componentModelNode.GetLabel ());
			Assert.AreEqual ("(4.0.10)", componentModelNode.GetSecondaryLabel ());
			Assert.IsTrue (componentModelNode.CanBeRemoved);
			Assert.IsFalse (componentModelNode.IsReadOnly);
			Assert.IsTrue (componentModelNode.IsTopLevel);
			Assert.IsTrue (componentModelNode.HasDependencies ());
			Assert.AreEqual (TaskSeverity.Warning, componentModelNode.GetStatusSeverity ());
			Assert.AreEqual (componentModelNode.GetStatusMessage (), GettextCatalog.GetString ("Package restored with warnings. Expand the package to see the warnings."));

			// Diagnostic child nodes should be added to the componentModelNode.
			var childNodes = componentModelNode.GetDependencyNodes ().ToList ();
			Assert.AreEqual (2, childNodes.Count);

			var diagnosticNode1 = childNodes.FirstOrDefault (node => node.Name == "NU1603");
			Assert.IsFalse (diagnosticNode1.CanBeRemoved);
			Assert.AreEqual (TaskSeverity.Warning, diagnosticNode1.GetStatusSeverity ());
			Assert.AreEqual (diagnosticNode1.GetStatusMessage (), "NetStandardTwoNuGetWarnings depends on System.ComponentModel.EventBasedAsync (>= 4.0.1) but System.ComponentModel.EventBasedAsync 4.0.1 was not found. An approximate best match of System.ComponentModel.EventBasedAsync 4.0.10 was resolved.");

			var diagnosticNode2 = childNodes.FirstOrDefault (node => node.Name == "NU1701");
			Assert.IsFalse (diagnosticNode2.CanBeRemoved);
			Assert.AreEqual (TaskSeverity.Warning, diagnosticNode2.GetStatusSeverity ());
			Assert.AreEqual (diagnosticNode2.GetStatusMessage (), "Package 'System.ComponentModel.EventBasedAsync 4.0.10' was restored using '.NETFramework,Version=v4.6' instead of the project target framework '.NETStandard,Version=v1.6'. This package may not be fully compatible with your project.");
		}

		/// <summary>
		/// Diagnostic is for a child dependency and not the top level NuGet package referenced by the project.
		/// </summary>
		[Test]
		public async Task NetStandardLibrary_OneIndirectNuGetDiagnosticWarningsForSystemNetHttp ()
		{
			FilePath projectFileName = Util.GetSampleProject ("DotNetCoreDependenciesFolder", "NetStandardOneIndirectNuGetWarning.csproj");
			Restore (projectFileName);
			project = (DotNetProject) await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName);
			await CreateDependenciesNode ();

			var sdkNode = GetSdkFolderChildDependencies ().Single ();
			Assert.AreEqual ("NETStandard.Library", sdkNode.Name);
			Assert.AreEqual ("NETStandard.Library", sdkNode.GetLabel ());
			Assert.IsTrue (sdkNode.IsReadOnly);
			Assert.IsFalse (sdkNode.CanBeRemoved);
			Assert.IsTrue (sdkNode.IsTopLevel);

			var activeDirectoryNode = GetNuGetFolderChildDependencies ().Single ();
			Assert.AreEqual ("Microsoft.IdentityModel.Clients.ActiveDirectory", activeDirectoryNode.Name);
			Assert.AreEqual ("Microsoft.IdentityModel.Clients.ActiveDirectory", activeDirectoryNode.GetLabel ());
			Assert.AreEqual ("(3.13.5)", activeDirectoryNode.GetSecondaryLabel ());
			Assert.IsTrue (activeDirectoryNode.CanBeRemoved);
			Assert.IsFalse (activeDirectoryNode.IsReadOnly);
			Assert.IsTrue (activeDirectoryNode.IsTopLevel);
			Assert.IsTrue (activeDirectoryNode.HasDependencies ());
			Assert.AreEqual (TaskSeverity.Warning, activeDirectoryNode.GetStatusSeverity ());
			Assert.AreEqual (activeDirectoryNode.GetStatusMessage (), GettextCatalog.GetString ("Package restored with warnings. Expand the package to see the warnings."));

			var systemNetHttpNode = activeDirectoryNode.GetDependencyNodes ().FirstOrDefault (node => node.Name == "System.Net.Http");
			Assert.IsFalse (systemNetHttpNode.CanBeRemoved);
			Assert.IsFalse (systemNetHttpNode.IsTopLevel);
			Assert.AreEqual (TaskSeverity.Warning, systemNetHttpNode.GetStatusSeverity ());
			string diagnosticMessage = "Microsoft.IdentityModel.Clients.ActiveDirectory 3.13.5 depends on System.Net.Http (>= 4.0.1) but System.Net.Http 4.0.1 was not found. An approximate best match of System.Net.Http 4.1.0 was resolved.";
			Assert.AreEqual (systemNetHttpNode.GetStatusMessage (), diagnosticMessage);

			// Diagnostic child node should be added to the systemNetHttpNode.
			var diagnosticNode = systemNetHttpNode.GetDependencyNodes ().FirstOrDefault (node => node.Name == "NU1603");
			Assert.AreEqual ("NU1603", diagnosticNode.Name);
			Assert.IsFalse (diagnosticNode.CanBeRemoved);
			Assert.AreEqual (TaskSeverity.Warning, diagnosticNode.GetStatusSeverity ());
			Assert.AreEqual (diagnosticNode.GetStatusMessage (), diagnosticMessage);
		}
	}
}
