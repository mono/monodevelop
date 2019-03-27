//
// LaunchSettingsTests.cs
//
// Author:
//       josemiguel <jostor@microsoft.com>
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
using System.Collections.Generic;
using System.Threading.Tasks;
using IdeUnitTests;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MonoDevelop.AspNetCore.Tests
{
	[TestFixture]
	public class LaunchSettingsTests
	{
		#region launchSettings.json sample
		const string LaunchSettings = @"{
									  ""iisSettings"": {
									    ""windowsAuthentication"": false,
									    ""anonymousAuthentication"": true,
									    ""iisExpress"": {
									      ""applicationUrl"": ""http://localhost:54339/"",
									      ""sslPort"": 0
									    }
									  },
									  ""profiles"": {
									    ""IIS Express"": {
									      ""commandName"": ""IISExpress"",
									      ""launchBrowser"": true,
									      ""environmentVariables"": {
									        ""ASPNETCORE_My_Environment"": ""1"",
									        ""ASPNETCORE_DETAILEDERRORS"": ""1"",
									        ""ASPNETCORE_ENVIRONMENT"": ""Staging""
									      }
									    },
									    ""EnvironmentsSample"": {
									      ""commandName"": ""Project"",
									      ""launchBrowser"": true,
									      ""environmentVariables"": {
									        ""ASPNETCORE_ENVIRONMENT"": ""Staging""
									      },
									      ""applicationUrl"": ""http://localhost:54340/""
									    },
									    ""Kestrel Staging"": {
									      ""commandName"": ""Project"",
									      ""launchBrowser"": true,
									      ""environmentVariables"": {
									        ""ASPNETCORE_My_Environment"": ""1"",
									        ""ASPNETCORE_DETAILEDERRORS"": ""1"",
									        ""ASPNETCORE_ENVIRONMENT"": ""Staging""
									      },
									      ""applicationUrl"": ""http://localhost:51997/""
									    }
									  }
									}";
		#endregion

		[TestFixtureSetUp]
		public void SetUp ()
		{
			string runTests = Environment.GetEnvironmentVariable ("DOTNETCORE_IGNORE_PROJECT_TEMPLATE_TESTS");
			DesktopService.Initialize ();
			if (!IdeApp.IsInitialized)
				IdeApp.Initialize (new ProgressMonitor ());
		}

		[Test]
		public async Task TestLoad ()
		{
			var solution = await CreateFromTemplateAndBuild ("NetCore2x", "Microsoft.Web.Empty.CSharp", "UseNetCore22=true");
			var project = (DotNetProject)solution.StartupItem;

			var launchProfileProvider = new LaunchProfileProvider (project);
			launchProfileProvider.LoadLaunchSettings ();

			Assert.That (launchProfileProvider.ProfilesObject, Is.Not.Null);
			Assert.That (launchProfileProvider.GlobalSettings, Is.Not.Empty);

			var profiles = LaunchProfileData.DeserializeProfiles (launchProfileProvider.ProfilesObject);

			Assert.That (profiles, Has.Count.EqualTo (2));

			profiles.Add ("Test", new LaunchProfileData ());

			launchProfileProvider.SaveLaunchSettings (profiles.ToSerializableForm ());

			launchProfileProvider = new LaunchProfileProvider (project);
			launchProfileProvider.LoadLaunchSettings ();

			profiles = LaunchProfileData.DeserializeProfiles (launchProfileProvider.ProfilesObject);

			Assert.That (profiles, Has.Count.EqualTo (3));
		}

		[Test]
		public async Task RefreshLaunchSettings_returns_expected_Profile ()
		{
			var solution = await CreateFromTemplateAndBuild ("NetCore2x", "Microsoft.Web.Mvc.CSharp", "UseNetCore22=true");
			var project = (DotNetProject)solution.StartupItem;

			var launchProfileProvider = new LaunchProfileProvider (project);
			launchProfileProvider.LoadLaunchSettings ();

			Assert.That (launchProfileProvider.ProfilesObject, Is.Not.Null);
			Assert.That (launchProfileProvider.GlobalSettings, Is.Not.Empty);

			//modifiying launchSettings.json externally
			System.IO.File.WriteAllText (launchProfileProvider.launchSettingsJsonPath, LaunchSettings);
			var config = project.GetDefaultRunConfiguration() as AspNetCoreRunConfiguration;
			config.RefreshLaunchSettings (project.DefaultNamespace);

			Assert.That (config, Is.Not.Null, "GetDefaultRunConfiguration cast to AspNetCoreRunConfiguration is null");
			Assert.That (config.ActiveProfile, Is.EqualTo ("EnvironmentsSample"));
			Assert.That (config.CurrentProfile, Is.Not.Null);
			Assert.That (config.CurrentProfile.TryGetOtherSettings<string> ("applicationUrl"), Is.EqualTo ("http://localhost:54340/"));
		}

		[Test]
		public void ToSerializableForm_Returns_ExpectedValues ()
		{
			var launchSettingsJson = JObject.Parse (LaunchSettings);
			var profiles = launchSettingsJson?.GetValue ("profiles") as JObject;
			var profilesData = LaunchProfileData.DeserializeProfiles (profiles);
			var result = profilesData.ToSerializableForm ();

			Assert.That (result, Is.Not.Null, "ToSerializableForm returned null; expected not null");
			Assert.That (result ["IIS Express"], Has.Count.EqualTo (3), "IIS Express profile expects 3 elements");
			Assert.That (result ["EnvironmentsSample"], Has.Count.EqualTo (4), "EnvironmentsSample profile expects 4 elements");
			Assert.True (result ["EnvironmentsSample"].ContainsKey ("applicationUrl"), "EnvironmentsSample does not contains applicationUrl");
			Assert.That (result ["EnvironmentsSample"] ["applicationUrl"], Is.EqualTo ("http://localhost:54340/"), "EnvironmentsSample applicationUrl is incorrect");
			Assert.That (result ["Kestrel Staging"], Has.Count.EqualTo (4), "Kestrel Staging profile expects 4 elements");
			Assert.True (result ["Kestrel Staging"].ContainsKey ("environmentVariables"), "Kestrel Staging profile does not contains environmentVariables");
			Assert.That (result ["Kestrel Staging"] ["environmentVariables"], Has.Count.EqualTo (3), "Kestrel Staging profile, environmentVariables count is incorrect");
		}

		[Test]
		public void TryGetOtherSettings_Returns_ExpectedValue ()
		{
			var launchSettingsJson = JObject.Parse (LaunchSettings);
			var profiles = launchSettingsJson?.GetValue ("profiles") as JObject;
			var profilesData = LaunchProfileData.DeserializeProfiles (profiles);

			var appUrl = profilesData ["EnvironmentsSample"].TryGetOtherSettings<string> ("applicationUrl");

			Assert.That (appUrl, Is.EqualTo ("http://localhost:54340/"));
		}

		static async Task<Solution> CreateFromTemplateAndBuild (string basename, string templateId, string parameters)
		{
			using (var ptt = new ProjectTemplateTest (basename, templateId)) {

				foreach (var templateParameter in TemplateParameter.CreateParameters (parameters)) {
					ptt.Config.Parameters [templateParameter.Name] = templateParameter.Value;
				}

				var template = await ptt.CreateAndBuild ();

				return ptt.Solution;
			}
		}
	}

	class TemplateParameter
	{
		public TemplateParameter (string parameter)
		{
			Parse (parameter);
		}

		public string Name { get; private set; }
		public string Value { get; private set; }
		public bool IsValid { get; private set; }

		void Parse (string parameter)
		{
			int index = parameter.IndexOf ('=');
			if (index <= 0) {
				IsValid = false;
				Name = String.Empty;
				Value = String.Empty;
				return;
			}

			IsValid = true;
			Name = parameter.Substring (0, index).Trim ();
			Value = parameter.Substring (index + 1).Trim ();
		}

		public static IEnumerable<TemplateParameter> CreateParameters (string condition)
		{
			string [] parts = condition.Split (new [] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string part in parts) {
				var parameter = new TemplateParameter (part);
				if (!parameter.IsValid) {
					LoggingService.LogWarning ("Invalid template condition '{0}'", condition);
				}
				yield return parameter;
			}
		}
	}
}
