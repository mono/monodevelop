//
// FeatureSwitchServiceTests.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2018 Microsoft, Inc (http://microsoft.com)
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
using System.Linq;
using MonoDevelop.Core.FeatureConfiguration;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class FeatureSwitchServiceTests : TestBase
	{
		[Test]
		public void IgnoresUnknownFeatures ()
		{
			Assert.IsNull (FeatureSwitchService.IsFeatureEnabled ("FakeFeature"));
		}

		[Test]
		public void CanEnableWithEnvVar ()
		{
			Environment.SetEnvironmentVariable ("MD_FEATURES_ENABLED", "MonoDevelop.Core.FeatureSwitchTests");
			Assert.True (FeatureSwitchService.IsFeatureEnabled ("MonoDevelop.Core.FeatureSwitchTests") ?? false);
		}

		[Test]
		public void CanEnableMultipleWithEnvVar ()
		{
			Environment.SetEnvironmentVariable ("MD_FEATURES_ENABLED", "Feature1;Feature2;Feature3;Feature4");
			Assert.True (FeatureSwitchService.IsFeatureEnabled ("Feature1") ?? false);
			Assert.True (FeatureSwitchService.IsFeatureEnabled ("Feature2") ?? false);
			Assert.True (FeatureSwitchService.IsFeatureEnabled ("Feature3") ?? false);
			Assert.True (FeatureSwitchService.IsFeatureEnabled ("Feature4") ?? false);
		}

		[Test]
		public void CanDisableWithEnvVar ()
		{
			Environment.SetEnvironmentVariable ("MD_FEATURES_DISABLED", "MonoDevelop.Core.FeatureSwitchTests");
			Assert.False (FeatureSwitchService.IsFeatureEnabled ("MonoDevelop.Core.FeatureSwitchTests") ?? true);
		}

		[Test]
		public void CanDisableMultipleWithEnvVar ()
		{
			Environment.SetEnvironmentVariable ("MD_FEATURES_DISABLED", "Feature1;Feature2;Feature3;Feature4");
			Assert.False (FeatureSwitchService.IsFeatureEnabled ("Feature1") ?? true);
			Assert.False (FeatureSwitchService.IsFeatureEnabled ("Feature2") ?? true);
			Assert.False (FeatureSwitchService.IsFeatureEnabled ("Feature3") ?? true);
			Assert.False (FeatureSwitchService.IsFeatureEnabled ("Feature4") ?? true);
		}

		[Test]
		public void CanChangeWhileRunning ()
		{
			string featureName = nameof (CanChangeWhileRunning);

			Assert.IsNull (FeatureSwitchService.IsFeatureEnabled (featureName));
			FeatureSwitchService.SetFeatureSwitchValue (featureName, true);
			Assert.True (FeatureSwitchService.IsFeatureEnabled (featureName) ?? false);

			FeatureSwitchService.SetFeatureSwitchValue (featureName, false);
			Assert.False (FeatureSwitchService.IsFeatureEnabled (featureName) ?? true);
		}

		[Test]
		public void CanRegisterAndUnregisterFeatureSwitches ()
		{
			string featureName = nameof (CanRegisterAndUnregisterFeatureSwitches);

			for (int i = 1; i <= 10; i++) {
				FeatureSwitchService.RegisterFeatureSwitch ($"{featureName}{i}", "Test feature", i % 2 == 0);

				var switches = FeatureSwitchService.DescribeFeatures ()
					.Where (x => x.Name.StartsWith (featureName, StringComparison.OrdinalIgnoreCase))
					.ToList ();

				foreach (var feature in switches) {
					Assert.That (FeatureSwitchService.IsFeatureEnabled (feature.Name).GetValueOrDefault (), Is.EqualTo (feature.DefaultValue));
					Assert.That (switches.Count (x => x.Name == feature.Name), Is.EqualTo (1));

					// Check we can bypass the controller with the environment variables
					if (feature.DefaultValue) {
						Environment.SetEnvironmentVariable ("MD_FEATURES_DISABLED", feature.Name);
						Assert.False (FeatureSwitchService.IsFeatureEnabled (feature.Name).GetValueOrDefault (true));
					} else {
						Environment.SetEnvironmentVariable ("MD_FEATURES_ENABLED", feature.Name);
						Assert.True (FeatureSwitchService.IsFeatureEnabled (feature.Name).GetValueOrDefault (false));
					}

					Environment.SetEnvironmentVariable ("MD_FEATURES_ENABLED", null);
					Environment.SetEnvironmentVariable ("MD_FEATURES_DISABLED", null);
				}

				FeatureSwitchService.UnregisterFeatureSwitch ($"{featureName}{i}");
			}
		}
	}
}
