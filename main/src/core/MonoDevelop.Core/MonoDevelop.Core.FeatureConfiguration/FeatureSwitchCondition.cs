//
// FeatureSwitchCondition.cs
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
using System.Collections.Immutable;

namespace MonoDevelop.Core.FeatureConfiguration
{
	/// <summary>
	/// Base class for feature switch conditions.
	/// </summary>
	public abstract class FeatureSwitchCondition
	{
		internal abstract bool Evaluate ();
	}

	/// <summary>
	/// Base class for version-based checks
	/// </summary>
	public abstract class VersionBasedFeatureSwitchCondition : FeatureSwitchCondition
	{
		protected static Version CurrentVersion = Version.Parse (BuildInfo.FullVersion);
	}

	/// <summary>
	/// Switch condition to check than current version is greater than or equal to
	/// a given version (provided in the constructor)
	/// </summary>
	public class VersionGreaterThanFeatureSwitchCondition : VersionBasedFeatureSwitchCondition
	{
		readonly Version versionToCheck;

		public VersionGreaterThanFeatureSwitchCondition (string version)
		{
			versionToCheck = Version.Parse (version);
		}

		public VersionGreaterThanFeatureSwitchCondition (Version version)
		{
			versionToCheck = version;
		}

		internal override bool Evaluate () => CurrentVersion >= versionToCheck;
	}

	public class EnvVarExistsFeatureSwitchCondition : FeatureSwitchCondition
	{
		readonly string nameToCheck, valueToCheck;

		public EnvVarExistsFeatureSwitchCondition (string name, string value)
		{
			nameToCheck = name;
			valueToCheck = value;
		}

		internal override bool Evaluate ()
		{
			if (!string.IsNullOrEmpty (nameToCheck)) {
				var v = Environment.GetEnvironmentVariable (nameToCheck);
				return string.IsNullOrEmpty (valueToCheck) ? !string.IsNullOrEmpty (v) : v == valueToCheck;
			}

			return true;
		}
	}

	/// <summary>
	/// Allows aggregation of several switch conditions into a single condition.
	/// </summary>
	public class AggregatedFeatureSwitchCondition : FeatureSwitchCondition
	{
		readonly ImmutableArray<FeatureSwitchCondition> conditions;
		readonly bool allMustPass;

		public AggregatedFeatureSwitchCondition (bool allMustPass, params FeatureSwitchCondition [] conditions)
		{
			this.conditions = conditions.ToImmutableArray ();
			this.allMustPass = allMustPass;
		}

		internal override bool Evaluate ()
		{
			bool result = false;

			foreach (var cond in conditions) {
				bool r = cond.Evaluate ();
				if (allMustPass && !r) return false;
				if (!allMustPass && r) return true;

				result |= r;
			}

			return result;
		}
	}
}
