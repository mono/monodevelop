//
// DotNetCoreProjectTemplateWizardTests.cs
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

using System.Reflection;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.DotNetCore.Templating;
using MonoDevelop.Ide.Templates;
using NUnit.Framework;

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	class DotNetCoreProjectTemplateWizardTests : DotNetCoreVersionsRestorerTestBase
	{
		DotNetCoreProjectTemplateWizard wizard;

		void CreateWizard ()
		{
			wizard = new DotNetCoreProjectTemplateWizard ();
			AddSupportedParameters (null);
		}

		bool WizardHasParameter (string name)
		{
			var model = wizard.Parameters as IStringTagModel;
			return model.GetValue (name) != null;
		}

		void AddSupportedParameters (string parameters)
		{
			var template = new SolutionTemplate ("id", "name", null);
			template.SupportedParameters = parameters;

			var flags = BindingFlags.Instance | BindingFlags.NonPublic;
			var method = wizard.GetType ().GetMethod ("UpdateParameters", flags);
			method.Invoke (wizard, new object [] { template });
		}

		[Test]
		public void NetStandard_NetCore20Installed ()
		{
			CreateWizard ();
			AddSupportedParameters ("NetStandard");
			DotNetCoreRuntimesInstalled ("2.0.1");

			int pages = wizard.TotalPages;

			Assert.AreEqual (1, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsFalse (WizardHasParameter ("UseNetStandard1x"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore20"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore1x"));
			Assert.IsFalse (WizardHasParameter ("framework"));
			Assert.AreEqual (".NETStandard,Version=v2.0", wizard.TargetFrameworks [0].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.6", wizard.TargetFrameworks [1].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.5", wizard.TargetFrameworks [2].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.4", wizard.TargetFrameworks [3].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.3", wizard.TargetFrameworks [4].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.2", wizard.TargetFrameworks [5].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.1", wizard.TargetFrameworks [6].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.0", wizard.TargetFrameworks [7].Id.ToString ());
		}

		/// <summary>
		/// F# project templates do not support .NET Standard below 1.6.
		/// </summary>
		[Test]
		public void NetStandard_FSharp_NetCore20Installed ()
		{
			CreateWizard ();
			AddSupportedParameters ("NetStandard;FSharpNetStandard");
			DotNetCoreRuntimesInstalled ("2.0.1");

			int pages = wizard.TotalPages;

			Assert.AreEqual (1, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsFalse (WizardHasParameter ("UseNetStandard1x"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore20"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore1x"));
			Assert.IsFalse (WizardHasParameter ("framework"));
			Assert.AreEqual (".NETStandard,Version=v2.0", wizard.TargetFrameworks [0].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.6", wizard.TargetFrameworks [1].Id.ToString ());
			Assert.AreEqual (2, wizard.TargetFrameworks.Count);
		}

		[Test]
		public void NetStandard_NoRuntimesInstalled ()
		{
			CreateWizard ();
			AddSupportedParameters ("NetStandard");
			DotNetCoreRuntimesInstalled (new string[0]);

			int pages = wizard.TotalPages;

			Assert.AreEqual (1, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsFalse (WizardHasParameter ("UseNetStandard1x"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore20"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore1x"));
			Assert.IsFalse (WizardHasParameter ("framework"));
			Assert.AreEqual (".NETStandard,Version=v1.6", wizard.TargetFrameworks [0].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.5", wizard.TargetFrameworks [1].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.4", wizard.TargetFrameworks [2].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.3", wizard.TargetFrameworks [3].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.2", wizard.TargetFrameworks [4].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.1", wizard.TargetFrameworks [5].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.0", wizard.TargetFrameworks [6].Id.ToString ());
		}

		/// <summary>
		/// F# project templates do not support .NET Standard below 1.6.
		/// </summary>
		[Test]
		public void NetStandard_FSharp_NoRuntimesInstalled ()
		{
			CreateWizard ();
			AddSupportedParameters ("NetStandard;FSharpNetStandard");
			DotNetCoreRuntimesInstalled (new string[0]);

			int pages = wizard.TotalPages;

			Assert.AreEqual (0, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsTrue (wizard.Parameters.GetBoolValue ("UseNetStandard1x"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore20"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore1x"));
			Assert.IsFalse (WizardHasParameter ("framework"));
			Assert.AreEqual (".NETStandard,Version=v1.6", wizard.TargetFrameworks [0].Id.ToString ());
			Assert.AreEqual (1, wizard.TargetFrameworks.Count);
		}

		[Test]
		public void NetCoreApp_NetCore20Installed ()
		{
			CreateWizard ();
			DotNetCoreRuntimesInstalled ("2.0.1");

			int pages = wizard.TotalPages;

			Assert.AreEqual (0, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsFalse (WizardHasParameter ("UseNetStandard1x"));
			Assert.IsTrue (wizard.Parameters.GetBoolValue ("UseNetCore20"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore1x"));
			Assert.IsFalse (WizardHasParameter ("framework"));
			Assert.AreEqual (".NETCoreApp,Version=v2.0", wizard.TargetFrameworks [0].Id.ToString ());
			Assert.AreEqual (1, wizard.TargetFrameworks.Count);
		}

		[Test]
		public void NetCoreApp_NetCore11Installed ()
		{
			CreateWizard ();
			DotNetCoreRuntimesInstalled ("1.1.0");

			int pages = wizard.TotalPages;

			Assert.AreEqual (0, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsFalse (WizardHasParameter ("UseNetStandard1x"));
			Assert.IsTrue (wizard.Parameters.GetBoolValue ("UseNetCore1x"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore20"));
			Assert.IsFalse (WizardHasParameter ("framework"));
			Assert.AreEqual (".NETCoreApp,Version=v1.1", wizard.TargetFrameworks [0].Id.ToString ());
			Assert.AreEqual (1, wizard.TargetFrameworks.Count);
		}

		[Test]
		public void NetCoreApp_NetCore20AndNetCore1xInstalled ()
		{
			CreateWizard ();
			DotNetCoreRuntimesInstalled ("2.0.1", "1.1.0", "1.0.2");

			int pages = wizard.TotalPages;

			Assert.AreEqual (1, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsFalse (WizardHasParameter ("UseNetStandard1x"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore20"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore1x"));
			Assert.IsFalse (WizardHasParameter ("framework"));
			Assert.AreEqual (".NETCoreApp,Version=v2.0", wizard.TargetFrameworks [0].Id.ToString ());
			Assert.AreEqual (".NETCoreApp,Version=v1.1", wizard.TargetFrameworks [1].Id.ToString ());
			Assert.AreEqual (".NETCoreApp,Version=v1.0", wizard.TargetFrameworks [2].Id.ToString ());
			Assert.AreEqual (3, wizard.TargetFrameworks.Count);
		}

		/// <summary>
		/// Slightly contrived test - since the project template should not be available
		/// if .NET Core 1.1 runtime is installed but .NET Core 2.0 is not.
		/// </summary>
		[TestCase ("FSharpNetCoreLibrary")]
		[TestCase ("RazorPages")]
		[TestCase ("FSharpWebApi")]
		public void NetCoreApp_NetCore11Installed_TemplateDoesNotSupportNetCore11 (string supportedParameters)
		{
			CreateWizard ();
			AddSupportedParameters (supportedParameters);
			DotNetCoreRuntimesInstalled ("1.1.2");

			int pages = wizard.TotalPages;

			Assert.AreEqual (0, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsFalse (WizardHasParameter ("UseNetStandard1x"));
			Assert.IsTrue (wizard.Parameters.GetBoolValue ("UseNetCore20"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore1x"));
			Assert.IsFalse (WizardHasParameter ("framework"));
			Assert.AreEqual (0, wizard.TargetFrameworks.Count);
		}

		[TestCase ("FSharpNetCoreLibrary")]
		[TestCase ("RazorPages")]
		[TestCase ("FSharpWebApi")]
		public void NetCoreApp_NetCore20AndNetCore11Installed_TemplateDoesNotSupportNetCore11 (string supportedParameters)
		{
			CreateWizard ();
			AddSupportedParameters (supportedParameters);
			DotNetCoreRuntimesInstalled ("2.0.5", "1.1.2");

			int pages = wizard.TotalPages;

			Assert.AreEqual (0, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsFalse (WizardHasParameter ("UseNetStandard1x"));
			Assert.IsTrue (wizard.Parameters.GetBoolValue ("UseNetCore20"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore1x"));
			Assert.IsFalse (WizardHasParameter ("framework"));
			Assert.AreEqual (".NETCoreApp,Version=v2.0", wizard.TargetFrameworks [0].Id.ToString ());
			Assert.AreEqual (1, wizard.TargetFrameworks.Count);
		}

		[Test]
		public void NetCoreLibrary_NetCore20Installed ()
		{
			CreateWizard ();
			AddSupportedParameters ("NetCoreLibrary");
			DotNetCoreRuntimesInstalled ("2.0.1");

			int pages = wizard.TotalPages;

			Assert.AreEqual (0, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsFalse (WizardHasParameter ("UseNetStandard1x"));
			Assert.IsTrue (wizard.Parameters.GetBoolValue ("UseNetCore20"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore1x"));
			Assert.AreEqual ("netcoreapp2.0", wizard.Parameters ["framework"]);
			Assert.AreEqual (".NETCoreApp,Version=v2.0", wizard.TargetFrameworks [0].Id.ToString ());
			Assert.AreEqual (1, wizard.TargetFrameworks.Count);
		}

		[Test]
		public void NetCoreLibrary_NoRuntimesInstalled ()
		{
			CreateWizard ();
			AddSupportedParameters ("NetCoreLibrary");
			DotNetCoreRuntimesInstalled (new string[0]);

			int pages = wizard.TotalPages;

			Assert.AreEqual (0, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsFalse (WizardHasParameter ("UseNetStandard1x"));
			Assert.IsTrue (wizard.Parameters.GetBoolValue ("UseNetCore1x"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore20"));
			Assert.AreEqual ("netcoreapp1.1", wizard.Parameters ["framework"]);
			Assert.AreEqual (0, wizard.TargetFrameworks.Count);
		}

		[Test]
		public void NetCoreApp_NetCore21Installed ()
		{
			CreateWizard ();
			DotNetCoreRuntimesInstalled ("2.1.1");

			int pages = wizard.TotalPages;

			Assert.AreEqual (0, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsFalse (WizardHasParameter ("UseNetStandard1x"));
			Assert.IsTrue (wizard.Parameters.GetBoolValue ("UseNetCore21"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore20"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore1x"));
			Assert.IsFalse (WizardHasParameter ("framework"));
			Assert.AreEqual (".NETCoreApp,Version=v2.1", wizard.TargetFrameworks [0].Id.ToString ());
			Assert.AreEqual (1, wizard.TargetFrameworks.Count);
		}

		[Test]
		public void NetCoreLibrary_NetCore21Installed ()
		{
			CreateWizard ();
			AddSupportedParameters ("NetCoreLibrary");
			DotNetCoreRuntimesInstalled ("2.1.2");

			int pages = wizard.TotalPages;

			Assert.AreEqual (0, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsFalse (WizardHasParameter ("UseNetStandard1x"));
			Assert.IsTrue (wizard.Parameters.GetBoolValue ("UseNetCore21"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore1x"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore20"));
			Assert.AreEqual ("netcoreapp2.1", wizard.Parameters ["framework"]);
			Assert.AreEqual (".NETCoreApp,Version=v2.1", wizard.TargetFrameworks [0].Id.ToString ());
			Assert.AreEqual (1, wizard.TargetFrameworks.Count);
		}

		[TestCase ("FSharpNetCoreLibrary")]
		[TestCase ("RazorPages")]
		[TestCase ("FSharpWebApi")]
		public void NetCoreApp_NetCore21AndNetCore11Installed_TemplateDoesNotSupportNetCore11 (string supportedParameters)
		{
			CreateWizard ();
			AddSupportedParameters (supportedParameters);
			DotNetCoreRuntimesInstalled ("2.1.5", "1.1.2");

			int pages = wizard.TotalPages;

			Assert.AreEqual (0, pages);
			Assert.IsFalse (WizardHasParameter ("UseNetStandard20"));
			Assert.IsFalse (WizardHasParameter ("UseNetStandard1x"));
			Assert.IsTrue (wizard.Parameters.GetBoolValue ("UseNetCore21"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore20"));
			Assert.IsFalse (WizardHasParameter ("UseNetCore1x"));
			Assert.IsFalse (WizardHasParameter ("framework"));
			Assert.AreEqual (".NETCoreApp,Version=v2.1", wizard.TargetFrameworks [0].Id.ToString ());
			Assert.AreEqual (1, wizard.TargetFrameworks.Count);
		}
	}
}
