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
using System.Linq;
using Mono.Addins;

namespace MonoDevelop.Core.FeatureConfiguration
{
	public static class FeatureSwitchService
	{
		public static bool? IsFeatureEnabled (string featureName)
		{
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
			var extensions = AddinManager.GetExtensionObjects<IFeatureSwitchController> ("/MonoDevelop/Core/FeatureConfiguration/FeatureSwitchChecks");
			if (extensions != null) {
				bool explicitlyEnabled = false, explicitlyDisabled = false;
				foreach (var ext in extensions) {
					switch (ext.IsFeatureEnabled (featureName)) {
					case true:
						explicitlyEnabled = true;
						break;
					case false:
						explicitlyDisabled = true;
						break;
					}
				}

				if (explicitlyDisabled) return false;
				if (explicitlyEnabled) return true;
			}

			return null;
		}
	}
}
