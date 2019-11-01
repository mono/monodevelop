//
// FeatureSwitchService.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2018 Microsoft Inc (http://microsoft.com)
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
using System.Collections.Immutable;
using System.Linq;
using Mono.Addins;
using MonoDevelop.Core.Addins;

namespace MonoDevelop.Core.FeatureConfiguration
{
	public static class FeatureSwitchService
	{
		static ImmutableDictionary<string, FeatureSwitch> featureSwitches = ImmutableDictionary<string, FeatureSwitch>.Empty;
		static bool initialized = false;

		static void EnsureInitialized ()
		{
			if (initialized)
				return;

			initialized = true;
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/FeatureSwitches", HandleFeatureSwitchExtension);
		}

		static void HandleFeatureSwitchExtension (object sender, ExtensionNodeEventArgs args)
		{
			var fs = args.ExtensionNode as FeatureSwitchExtensionNode;
			if (fs != null) {
				if (args.Change == ExtensionChange.Add && !featureSwitches.TryGetValue (fs.Id, out _)) {
					RegisterFeatureSwitch (fs.Id, fs.Description, fs.DefaultValue);
				} else {
					UnregisterFeatureSwitch (fs.Id);
				}
			}
		}

		public static bool? IsFeatureEnabled (string featureName)
		{
			EnsureInitialized ();

			if (string.IsNullOrEmpty (featureName)) {
				return null;
			}

			var env = Environment.GetEnvironmentVariable ("MD_FEATURES_ENABLED");
			if (env != null && env.Split (';').Contains (featureName)) {
				return true;
			}

			env = Environment.GetEnvironmentVariable ("MD_FEATURES_DISABLED");
			if (env != null && env.Split (';').Contains (featureName)) {
				return false;
			}

			// Fallback to ask extensions, enabling by default
			if (featureSwitches != null) {
				if (featureSwitches.TryGetValue (featureName, out var feature)) {
					return feature.DefaultValue;
				}
			}

			return null;
		}

		#region Internal API for unit tests

		internal static IEnumerable<FeatureSwitch> DescribeFeatures ()
		{
			EnsureInitialized ();
			return featureSwitches?.Values;
		}

		internal static void RegisterFeatureSwitch (string id, string description, bool defaultValue)
		{
			featureSwitches = featureSwitches.Add (id, new FeatureSwitch (id, description, defaultValue));
		}

		internal static void UnregisterFeatureSwitch (string id)
		{
			featureSwitches = featureSwitches.Remove (id);
		}

		#endregion
	}
}
