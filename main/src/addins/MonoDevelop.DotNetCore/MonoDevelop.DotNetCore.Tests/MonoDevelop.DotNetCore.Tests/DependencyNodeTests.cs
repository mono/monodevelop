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
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using NuGet.Packaging.Core;
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
		FrameworkReferencesNode frameworksFolderNode;
		TaskCompletionSource<bool> packageDependenciesChanged;
		TaskCompletionSource<bool> frameworkReferencesChanged;
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

		async Task CreateDependenciesNode (IUpdatedNuGetPackagesInWorkspace updatedNuGetPackages = null)
		{
			dependenciesNodeBuilder = new TestableDependenciesNodeBuilder ();
			dependenciesNode = new DependenciesNode (project, updatedNuGetPackages ?? PackageManagementServices.UpdatedPackagesInWorkspace);
			dependenciesNode.PackageDependencyCache.PackageDependenciesChanged += PackageDependenciesChanged;
			dependenciesNode.FrameworkReferencesCache.FrameworkReferencesChanged += FrameworkReferencesChanged;
			packageDependenciesChanged = new TaskCompletionSource<bool> ();
			frameworkReferencesChanged = new TaskCompletionSource<bool> ();

			dependenciesNode.PackageDependencyCache.Refresh ();
			dependenciesNode.FrameworkReferencesCache.Refresh ();

			await WaitForPackageDependenciesChanged ();

			dependenciesNodeBuilder.BuildChildNodes (null, dependenciesNode);
			nugetFolderNode = dependenciesNodeBuilder.PackageDependencies;
			sdkFolderNode = dependenciesNodeBuilder.SdkDependencies;
			frameworksFolderNode = dependenciesNodeBuilder.FrameworkReferences;
		}

		void PackageDependenciesChanged (object sender, EventArgs e)
		{
			packageDependenciesChanged.TrySetResult (true);
		}

		void FrameworkReferencesChanged (object sender, EventArgs e)
		{
			frameworkReferencesChanged.TrySetResult (true);
		}

		async Task WaitForPackageDependenciesChanged (int millisecondsTimeout = 60000)
		{
			var timeoutTask = Task.Delay (millisecondsTimeout);
			var result = await Task.WhenAny (timeoutTask, packageDependenciesChanged.Task);
			if (result == timeoutTask)
				Assert.Fail ("Timed out waiting for package dependencies to be updated.");
		}

		async Task WaitForFrameworkReferencesChanged (int millisecondsTimeout = 60000)
		{
			var timeoutTask = Task.Delay (millisecondsTimeout);
			var result = await Task.WhenAny (timeoutTask, frameworkReferencesChanged.Task);
			if (result == timeoutTask)
				Assert.Fail ("Timed out waiting for framework references to be updated.");
		}

		static SdkDependenciesNode GetSdkFolder (TargetFrameworkNode frameworkNode)
		{
			var nodeBuilder = new TestableTargetFrameworkNodeBuilder ();
			nodeBuilder.BuildChildNodes (null, frameworkNode);
			return nodeBuilder.SdkDependencies;
		}

		List<PackageDependencyNode> GetSdkFolderChildDependencies ()
		{
			return GetSdkFolderChildDependencies (sdkFolderNode);
		}

		static List<PackageDependencyNode> GetSdkFolderChildDependencies (SdkDependenciesNode node)
		{
			var nodeBuilder = new TestableSdkDependenciesNodeBuilder ();
			nodeBuilder.BuildChildNodes (null, node);
			return nodeBuilder.ChildNodesAsPackageDependencyNodes ().ToList ();
		}

		static PackageDependenciesNode GetNuGetFolder (TargetFrameworkNode frameworkNode)
		{
			var nodeBuilder = new TestableTargetFrameworkNodeBuilder ();
			nodeBuilder.BuildChildNodes (null, frameworkNode);
			return nodeBuilder.PackageDependencies;
		}

		List<PackageDependencyNode> GetNuGetFolderChildDependencies ()
		{
			return GetNuGetFolderChildDependencies (nugetFolderNode);
		}

		static List<PackageDependencyNode> GetNuGetFolderChildDependencies (PackageDependenciesNode node)
		{
			var nodeBuilder = new TestablePackageDependenciesNodeBuilder ();
			nodeBuilder.BuildChildNodes (null, node);
			return nodeBuilder.ChildNodesAsPackageDependencyNodes ().ToList ();
		}

		List<TargetFrameworkNode> GetTargetFrameworkChildDependencies ()
		{
			return dependenciesNodeBuilder.TargetFrameworks;
		}

		List<FrameworkReferenceNode> GetFrameworksFolderChildDependencies ()
		{
			return GetFrameworksFolderChildDependencies (frameworksFolderNode);
		}

		static List<FrameworkReferenceNode> GetFrameworksFolderChildDependencies (FrameworkReferencesNode node)
		{
			var nodeBuilder = new TestableFrameworkReferencesNodeBuilder ();
			nodeBuilder.BuildChildNodes (null, node);
			return nodeBuilder.ChildNodesAsFrameworkReferenceNode ().ToList ();
		}

		static FrameworkReferencesNode GetFrameworkReferencesFolder (TargetFrameworkNode node)
		{
			var nodeBuilder = new TestableTargetFrameworkNodeBuilder ();
			nodeBuilder.BuildChildNodes (null, node);
			return nodeBuilder.FrameworkReferences;
		}

		static bool IsDotNetCoreSdk30OrLaterInstalled ()
		{
			return DotNetCoreSdk.Versions.Any (version => version.Major == 3);
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
			Assert.AreEqual ("Test.Xam.IndirectNuGetWarning", activeDirectoryNode.Name);
			Assert.AreEqual ("Test.Xam.IndirectNuGetWarning", activeDirectoryNode.GetLabel ());
			Assert.AreEqual ("(0.1.0)", activeDirectoryNode.GetSecondaryLabel ());
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
			string diagnosticMessage = "Test.Xam.IndirectNuGetWarning 0.1.0 depends on System.Net.Http (>= 4.0.1) but System.Net.Http 4.0.1 was not found. An approximate best match of System.Net.Http 4.1.0 was resolved.";
			Assert.AreEqual (systemNetHttpNode.GetStatusMessage (), diagnosticMessage);

			// Diagnostic child node should be added to the systemNetHttpNode.
			var diagnosticNode = systemNetHttpNode.GetDependencyNodes ().FirstOrDefault (node => node.Name == "NU1603");
			Assert.AreEqual ("NU1603", diagnosticNode.Name);
			Assert.IsFalse (diagnosticNode.CanBeRemoved);
			Assert.AreEqual (TaskSeverity.Warning, diagnosticNode.GetStatusSeverity ());
			Assert.AreEqual (diagnosticNode.GetStatusMessage (), diagnosticMessage);
		}

		[Test]
		public async Task MultiTarget_NetStandardAndNetCoreApp_NewtonsoftJsonNuGetPackageReference ()
		{
			FilePath projectFileName = Util.GetSampleProject ("multi-target", "multi-target3.csproj");
			Restore (projectFileName);
			project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName);
			await CreateDependenciesNode ();

			var frameworkNodes = GetTargetFrameworkChildDependencies ();
			var netstandardFrameworkNode = frameworkNodes.FirstOrDefault (node => node.Name == ".NETStandard");
			var netframeworkNode = frameworkNodes.FirstOrDefault (node => node.Name == ".NETFramework");

			Assert.IsNotNull (netstandardFrameworkNode);
			Assert.IsNotNull (netframeworkNode);
			Assert.AreEqual ("(1.0.0.0)", netstandardFrameworkNode.GetSecondaryLabel ());
			Assert.AreEqual ("(4.7.2.0)", netframeworkNode.GetSecondaryLabel ());
			Assert.AreEqual (2, frameworkNodes.Count);

			// .NET Standard child nodes
			var childSdkFolderNode = GetSdkFolder (netstandardFrameworkNode);
			var sdkNode = GetSdkFolderChildDependencies (childSdkFolderNode).Single ();
			Assert.AreEqual ("NETStandard.Library", sdkNode.Name);
			Assert.AreEqual ("NETStandard.Library", sdkNode.GetLabel ());
			Assert.IsTrue (sdkNode.IsReadOnly);
			Assert.IsFalse (sdkNode.CanBeRemoved);
			Assert.IsTrue (sdkNode.IsTopLevel);

			var childNodes = sdkNode.GetDependencyNodes ().ToList ();
			var systemThreadingNode = childNodes.FirstOrDefault (node => node.Name == "System.Threading");
			Assert.IsFalse (systemThreadingNode.CanBeRemoved);
			Assert.IsFalse (systemThreadingNode.IsTopLevel);

			var childNuGetFolderNode = GetNuGetFolder (netstandardFrameworkNode);
			var newtonsoftNode = GetNuGetFolderChildDependencies (childNuGetFolderNode).Single ();
			Assert.AreEqual ("Newtonsoft.Json", newtonsoftNode.Name);
			Assert.AreEqual ("Newtonsoft.Json", newtonsoftNode.GetLabel ());
			Assert.AreEqual ("(10.0.1)", newtonsoftNode.GetSecondaryLabel ());
			Assert.IsTrue (newtonsoftNode.CanBeRemoved);
			Assert.IsFalse (newtonsoftNode.IsReadOnly);
			Assert.IsTrue (newtonsoftNode.IsTopLevel);
			Assert.IsTrue (newtonsoftNode.IsReleaseVersion ());
			Assert.IsTrue (newtonsoftNode.HasDependencies ());

			childNodes = newtonsoftNode.GetDependencyNodes ().ToList ();
			var microsoftCSharpNode = childNodes.FirstOrDefault (node => node.Name == "Microsoft.CSharp");
			Assert.IsFalse (microsoftCSharpNode.CanBeRemoved);
			Assert.IsFalse (microsoftCSharpNode.IsTopLevel);

			// .NET Framework child nodes.
			childSdkFolderNode = GetSdkFolder (netframeworkNode);
			Assert.IsNull (childSdkFolderNode); // Should not exist.

			childNuGetFolderNode = GetNuGetFolder (netframeworkNode);
			newtonsoftNode = GetNuGetFolderChildDependencies (childNuGetFolderNode).Single ();
			Assert.AreEqual ("Newtonsoft.Json", newtonsoftNode.Name);
			Assert.AreEqual ("Newtonsoft.Json", newtonsoftNode.GetLabel ());
			Assert.AreEqual ("(10.0.3)", newtonsoftNode.GetSecondaryLabel ());
			Assert.IsTrue (newtonsoftNode.CanBeRemoved);
			Assert.IsFalse (newtonsoftNode.IsReadOnly);
			Assert.IsTrue (newtonsoftNode.IsTopLevel);
			Assert.IsTrue (newtonsoftNode.IsReleaseVersion ());
		}

		[Test]
		public async Task NetStandard21Library_NewtonsoftJsonNuGetPackageReference ()
		{
			if (!IsDotNetCoreSdk30OrLaterInstalled ()) {
				Assert.Ignore (".NET Core 3 SDK is not installed.");
			}

			FilePath projectFileName = Util.GetSampleProject ("DotNetCoreDependenciesFolder", "NetStandard21JsonNet.csproj");
			Restore (projectFileName);
			project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName);
			await CreateDependenciesNode ();

			// Should be no sdk folder node.
			Assert.IsNull (sdkFolderNode);

			var frameworkNode = GetFrameworksFolderChildDependencies ().Single ();
			Assert.AreEqual ("NETStandard.Library", frameworkNode.Name);
			Assert.AreEqual ("NETStandard.Library", frameworkNode.GetLabel ());

			var defaultNode = frameworksFolderNode.GetDefaultNodes ().Single ();
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
		}

		[Test]
		public async Task NetCore30Library_NewtonsoftJsonNuGetPackageReference ()
		{
			if (!IsDotNetCoreSdk30OrLaterInstalled ()) {
				Assert.Ignore (".NET Core 3 SDK is not installed.");
			}

			FilePath projectFileName = Util.GetSampleProject ("DotNetCoreDependenciesFolder", "NetCore30JsonNet.csproj");
			Restore (projectFileName);
			project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName);
			await CreateDependenciesNode ();

			// Should be no sdk folder node.
			Assert.IsNull (sdkFolderNode);

			var frameworkNode = GetFrameworksFolderChildDependencies ().Single ();
			Assert.AreEqual ("Microsoft.NETCore.App", frameworkNode.Name);
			Assert.AreEqual ("Microsoft.NETCore.App", frameworkNode.GetLabel ());

			var defaultNode = frameworksFolderNode.GetDefaultNodes ().Single ();
			Assert.AreEqual ("Microsoft.NETCore.App", defaultNode.Name);
			Assert.AreEqual ("Microsoft.NETCore.App", defaultNode.GetLabel ());

			var newtonsoftNode = GetNuGetFolderChildDependencies ().Single ();
			Assert.AreEqual ("Newtonsoft.Json", newtonsoftNode.Name);
			Assert.AreEqual ("Newtonsoft.Json", newtonsoftNode.GetLabel ());
			Assert.AreEqual ("(10.0.3)", newtonsoftNode.GetSecondaryLabel ());
			Assert.IsTrue (newtonsoftNode.CanBeRemoved);
			Assert.IsFalse (newtonsoftNode.IsReadOnly);
			Assert.IsTrue (newtonsoftNode.IsTopLevel);
			Assert.IsTrue (newtonsoftNode.IsReleaseVersion ());
			Assert.IsTrue (newtonsoftNode.HasDependencies ());
		}

		[Test]
		public async Task NetStandard21Library_NewtonsoftJsonNuGetPackageReferenceHasUpdates ()
		{
			if (!IsDotNetCoreSdk30OrLaterInstalled ()) {
				Assert.Ignore (".NET Core 3 SDK is not installed.");
			}

			FilePath projectFileName = Util.GetSampleProject ("DotNetCoreDependenciesFolder", "NetStandard21JsonNet.csproj");
			Restore (projectFileName);
			project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName);

			var updatedPackages = new FakeUpdatedPackagesInWorkspace ();
			updatedPackages.AddUpdatedPackages (new PackageIdentity ("Newtonsoft.Json", NuGetVersion.Parse ("11.0.1")));
			await CreateDependenciesNode (updatedPackages);

			// Should be no sdk folder node.
			Assert.IsNull (sdkFolderNode);

			Assert.AreEqual ("(1 update)", dependenciesNode.GetSecondaryLabel ());

			var newtonsoftNode = GetNuGetFolderChildDependencies ().Single ();
			Assert.AreEqual ("Newtonsoft.Json", newtonsoftNode.GetLabel ());
			Assert.AreEqual ("(10.0.3)", newtonsoftNode.GetSecondaryLabel ());
			Assert.AreEqual ("md-package-update", newtonsoftNode.GetStatusIconId ().ToString ());

			Assert.AreEqual ("(1 update)", nugetFolderNode.GetSecondaryLabel ());

			var frameworkNode = GetFrameworksFolderChildDependencies ().Single ();
			Assert.AreEqual ("NETStandard.Library", frameworkNode.GetLabel ());

			// No updates label.
			Assert.AreEqual (string.Empty, frameworksFolderNode.GetSecondaryLabel ());
		}

		[Test]
		public async Task MultiTarget_NetStandard21_NetStandard20_NetCoreApp30_NetCoreApp21 ()
		{
			if (!IsDotNetCoreSdk30OrLaterInstalled ()) {
				Assert.Ignore (".NET Core 3 SDK is not installed.");
			}

			FilePath projectFileName = Util.GetSampleProject ("DotNetCoreDependenciesFolder", "NetStandard21NetStandard20NetCore30NetCore21.csproj");
			Restore (projectFileName);
			project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName);
			await CreateDependenciesNode ();
			await WaitForFrameworkReferencesChanged ();

			var frameworkNodes = GetTargetFrameworkChildDependencies ();
			var netstandard21FrameworkNode = frameworkNodes.Single (node => node.Name == ".NETStandard" && node.GetSecondaryLabel () == "(2.1.0.0)");
			var netstandard20FrameworkNode = frameworkNodes.Single (node => node.Name == ".NETStandard" && node.GetSecondaryLabel () == "(2.0.0.0)");
			var netCoreApp30FrameworkNode = frameworkNodes.Single (node => node.Name == ".NETCoreApp" && node.GetSecondaryLabel () == "(3.0.0.0)");
			var netCoreApp21FrameworkNode = frameworkNodes.Single (node => node.Name == ".NETCoreApp" && node.GetSecondaryLabel () == "(2.1.0.0)");

			// .NET Standard 2.1 nodes
			var frameworksFolder = GetFrameworkReferencesFolder (netstandard21FrameworkNode);
			var frameworkNode = GetFrameworksFolderChildDependencies (frameworksFolder).Single ();
			Assert.AreEqual ("NETStandard.Library", frameworkNode.Name);
			Assert.AreEqual ("NETStandard.Library", frameworkNode.GetLabel ());
			Assert.IsTrue (
				frameworkNode.GetSecondaryLabel ().StartsWith ("(2.1."),
				"Unexpected secondary label '{0}'", frameworkNode.GetSecondaryLabel ());

			var defaultNode = frameworksFolder.GetDefaultNodes ().Single ();
			Assert.AreEqual ("NETStandard.Library", defaultNode.Name);
			Assert.AreEqual ("NETStandard.Library", defaultNode.GetLabel ());

			// .NET Standard 2.0 nodes.
			var sdkFolder = GetSdkFolder (netstandard20FrameworkNode);
			var packageDependency = GetSdkFolderChildDependencies (sdkFolder).Single ();
			Assert.AreEqual ("NETStandard.Library", packageDependency.Name);
			Assert.AreEqual ("NETStandard.Library", packageDependency.GetLabel ());
			Assert.IsTrue (
				packageDependency.GetSecondaryLabel ().StartsWith ("(2.0."),
				"Unexpected secondary label '{0}'", packageDependency.GetSecondaryLabel ());

			// .NET Core 3.0 nodes.
			frameworksFolder = GetFrameworkReferencesFolder (netCoreApp30FrameworkNode);
			frameworkNode = GetFrameworksFolderChildDependencies (frameworksFolder).Single ();
			Assert.AreEqual ("Microsoft.NETCore.App", frameworkNode.Name);
			Assert.AreEqual ("Microsoft.NETCore.App", frameworkNode.GetLabel ());
			Assert.IsTrue (
				frameworkNode.GetSecondaryLabel ().StartsWith ("(3.0."),
				"Unexpected secondary label '{0}'", frameworkNode.GetSecondaryLabel ());

			defaultNode = frameworksFolder.GetDefaultNodes ().Single ();
			Assert.AreEqual ("Microsoft.NETCore.App", defaultNode.Name);
			Assert.AreEqual ("Microsoft.NETCore.App", defaultNode.GetLabel ());

			// .NET Core 2.1 nodes.
			// Note that for some reason in a multi-target project a .NET Core 2.1 project also gets
			// a .NET Standard dependency if .net standard is one of the frameworks. With a non-multi-target
			// project this does not happen.
			sdkFolder = GetSdkFolder (netCoreApp21FrameworkNode);
			var packageDependencies = GetSdkFolderChildDependencies (sdkFolder);
			packageDependency = packageDependencies.Single (p => p.Name == "Microsoft.NETCore.App");
			Assert.AreEqual ("Microsoft.NETCore.App", packageDependency.GetLabel ());
			Assert.IsTrue (
				packageDependency.GetSecondaryLabel ().StartsWith ("(2.1."),
				"Unexpected secondary label '{0}'", packageDependency.GetSecondaryLabel ());
		}
	}
}
