//
// ConfigurationMergingTests.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Projects
{
	public class ConfigurationMergingTests: TestBase
	{
		[Test]
		public async Task TestConfigurationMerging()
		{
			string solFile = Util.GetSampleProject("test-configuration-merging", "TestConfigurationMerging.sln");
			Solution sol = await Services.ProjectService.ReadWorkspaceItem(Util.GetMonitor(), solFile) as Solution;
			Assert.IsNotNull(sol);
			Assert.AreEqual(1, sol.Items.Count);

			DotNetProject p = sol.Items[0] as DotNetProject;
			Assert.IsNotNull(p);

			// Debug config

			DotNetProjectConfiguration conf = p.Configurations["Debug"] as DotNetProjectConfiguration;
			Assert.IsNotNull(conf);
			Assert.AreEqual("Debug", conf.Name);
			Assert.AreEqual(string.Empty, conf.Platform);

			dynamic pars = conf.CompilationParameters;
			Assert.IsNotNull(pars);
			Assert.AreEqual(2, pars.WarningLevel);

			pars.WarningLevel = 4;

			// Release config

			conf = p.Configurations["Release"] as DotNetProjectConfiguration;
			Assert.IsNotNull(conf);
			Assert.AreEqual("Release", conf.Name);
			Assert.AreEqual(string.Empty, conf.Platform);

			pars = conf.CompilationParameters;
			Assert.IsNotNull(pars);
			Assert.AreEqual("ReleaseMod", Path.GetFileName(conf.OutputDirectory));
			Assert.AreEqual(3, pars.WarningLevel);

			pars.WarningLevel = 1;
			Assert.AreEqual(1, pars.WarningLevel);
			conf.DebugType = "full";
			conf.DebugSymbols = true;

			await sol.SaveAsync(Util.GetMonitor());
			Assert.AreEqual(1, pars.WarningLevel);

			string savedFile = Path.Combine(p.BaseDirectory, "TestConfigurationMergingSaved.csproj");
			Assert.AreEqual(File.ReadAllText(savedFile), File.ReadAllText(p.FileName));

			sol.Dispose();
		}

		[Test]
		public async Task TestConfigurationMergingDefaultValues()
		{
			string projectFile = Util.GetSampleProject("test-configuration-merging", "TestConfigurationMerging3.csproj");
			DotNetProject p = await Services.ProjectService.ReadSolutionItem(Util.GetMonitor(), projectFile) as DotNetProject;
			Assert.IsNotNull(p);

			DotNetProjectConfiguration conf = p.Configurations["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull(conf);
			dynamic cparams = conf.CompilationParameters;
			Assert.AreEqual(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Default, cparams.LangVersion);
			cparams.LangVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp5;
			Assert.IsTrue(cparams.UnsafeCode);
			cparams.UnsafeCode = false;

			await p.SaveAsync(Util.GetMonitor());

			Assert.AreEqual(Util.ToSystemEndings(File.ReadAllText(p.FileName + ".saved")), File.ReadAllText(p.FileName));

			p.Dispose();		}

		[Test]
		public async Task TestConfigurationMergingKeepOldConfig()
		{
			string projectFile = Util.GetSampleProject ("test-configuration-merging", "TestConfigurationMerging4.csproj");
			DotNetProject p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
			Assert.IsNotNull (p);

			DotNetProjectConfiguration conf = p.Configurations ["Debug|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsTrue (conf.DebugSymbols);
			dynamic cparams = conf.CompilationParameters;
			Assert.IsTrue (cparams.UnsafeCode);

			conf = p.Configurations ["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull (conf);
			Assert.IsFalse (conf.DebugSymbols);
			conf.DebugSymbols = true;
			cparams = conf.CompilationParameters;
			Assert.IsFalse (cparams.UnsafeCode);
			cparams.UnsafeCode = true;

			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ToSystemEndings (File.ReadAllText (p.FileName + ".saved")), File.ReadAllText (p.FileName));

			p.Dispose ();
		}

		[Test]
		public async Task TestConfigurationMergingChangeNoMergeToParent()
		{
			string projectFile = Util.GetSampleProject("test-configuration-merging", "TestConfigurationMerging5.csproj");
			DotNetProject p = await Services.ProjectService.ReadSolutionItem(Util.GetMonitor(), projectFile) as DotNetProject;
			Assert.IsNotNull(p);

			DotNetProjectConfiguration conf = p.Configurations["Debug|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull(conf);
			Assert.IsTrue(conf.SignAssembly);

			conf = p.Configurations["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull(conf);
			Assert.IsTrue(conf.SignAssembly);
			conf.SignAssembly = false;

			await p.SaveAsync(Util.GetMonitor());

			Assert.AreEqual(Util.ToSystemEndings(File.ReadAllText(p.FileName + ".saved")), File.ReadAllText(p.FileName));

			p.Dispose();
		}

		[Test]
		public async Task TestConfigurationMergingChangeMergeToParent()
		{
			string projectFile = Util.GetSampleProject("test-configuration-merging", "TestConfigurationMerging6.csproj");
			DotNetProject p = await Services.ProjectService.ReadSolutionItem(Util.GetMonitor(), projectFile) as DotNetProject;
			Assert.IsNotNull(p);

			DotNetProjectConfiguration conf = p.Configurations["Debug|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull(conf);
			Assert.IsTrue(conf.SignAssembly);
			conf.SignAssembly = false;

			conf = p.Configurations["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull(conf);
			Assert.IsTrue(conf.SignAssembly);
			conf.SignAssembly = false;

			await p.SaveAsync(Util.GetMonitor());

			Assert.AreEqual(Util.ToSystemEndings(File.ReadAllText(p.FileName + ".saved")), File.ReadAllText(p.FileName));

			p.Dispose();
		}

		[Test]
		public async Task TestConfigurationMergingChangeMergeToParent2()
		{
			string projectFile = Util.GetSampleProject("test-configuration-merging", "TestConfigurationMerging7.csproj");
			DotNetProject p = await Services.ProjectService.ReadSolutionItem(Util.GetMonitor(), projectFile) as DotNetProject;
			Assert.IsNotNull(p);

			DotNetProjectConfiguration conf = p.Configurations["Debug|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull(conf);
			Assert.IsTrue(conf.SignAssembly);
			conf.SignAssembly = true;

			conf = p.Configurations["Release|x86"] as DotNetProjectConfiguration;
			Assert.IsNotNull(conf);
			Assert.IsFalse(conf.SignAssembly);
			conf.SignAssembly = true;

			await p.SaveAsync(Util.GetMonitor());

			Assert.AreEqual(Util.ToSystemEndings(File.ReadAllText(p.FileName + ".saved")), File.ReadAllText(p.FileName));

			p.Dispose();
		}

		[Test]
		public async Task TestConfigurationMergingChangeMergeToParent3()
		{
			string projectFile = Util.GetSampleProject("test-configuration-merging", "TestConfigurationMerging8.csproj");
			DotNetProject p = await Services.ProjectService.ReadSolutionItem(Util.GetMonitor(), projectFile) as DotNetProject;
			Assert.IsNotNull(p);

			var refXml = File.ReadAllText(p.FileName);
			await p.SaveAsync(Util.GetMonitor());

			Assert.AreEqual(refXml, File.ReadAllText(p.FileName));

			p.Dispose();
		}
	}
}
