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
using MonoDevelop.Core.FeatureConfiguration;
using NUnit.Framework;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class FeatureSwitchServiceTests
	{
		[TestCase ("7.6", true)]
		[TestCase (BuildInfo.FullVersion, true)]
		[TestCase ("100.1", false)]
		public void CanCheckForVersionCondition (string version, bool expected)
		{
			var cond = new VersionGreaterThanFeatureSwitchCondition (version);
			Assert.That (cond.Evaluate (), Is.EqualTo (expected));
		}

		[TestCase ("FeatureSwitchServiceTests", null)]
		[TestCase ("FeatureSwitchServiceTests", "value")]
		public void CanCheckForEnvironmentVar (string variable, string value)
		{
			var cond = new EnvVarExistsFeatureSwitchCondition (variable, value);

			// Check it returns false without the env var defined
			Assert.False (cond.Evaluate ());

			// Check the var exists and value is correct
			Environment.SetEnvironmentVariable (variable, value ?? "True");
			Assert.True (cond.Evaluate ());

			// Check the value is compared
			if (!string.IsNullOrEmpty (value)) {
				Environment.SetEnvironmentVariable (variable, value.Insert (0, "insert"));
				Assert.False (cond.Evaluate ());
			}
		}

		[Test]
		public void CanAggregateConditions ()
		{
			Environment.SetEnvironmentVariable ("FeatureSwitchServiceTests", "1");

			// Check when all conditions are met
			var cond = new AggregatedFeatureSwitchCondition (true,
				new VersionGreaterThanFeatureSwitchCondition (BuildInfo.FullVersion),
				new EnvVarExistsFeatureSwitchCondition ("FeatureSwitchServiceTests", null)
			);
			Assert.True (cond.Evaluate ());

			// Check when one condition is not met
			Environment.SetEnvironmentVariable ("FeatureSwitchServiceTests", null);
			Assert.False (cond.Evaluate ());

			// Check when any condition is met
			cond = new AggregatedFeatureSwitchCondition (false,
				new VersionGreaterThanFeatureSwitchCondition (BuildInfo.FullVersion),
				new EnvVarExistsFeatureSwitchCondition ("FeatureSwitchServiceTests", null)
			);
			Assert.True (cond.Evaluate ());
		}

		[TestCase (BuildInfo.FullVersion, true)]
		[TestCase ("100.1", false)]
		public void CanRegisterSwitches (string version, bool expected)
		{
			var feature = FeatureSwitchService.RegisterFeature (Guid.NewGuid ().ToString (),
				new VersionGreaterThanFeatureSwitchCondition (version));
			Assert.That (feature.IsEnabled, Is.EqualTo (expected));
		}
	}
}
