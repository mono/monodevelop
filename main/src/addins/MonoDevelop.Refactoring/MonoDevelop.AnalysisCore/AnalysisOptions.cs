// 
// AnalysisOptions.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.AnalysisCore
{
	public static class AnalysisOptions
	{
		public readonly static ConfigurationProperty<bool> EnableFancyFeatures = IdeApp.Preferences.EnableSourceAnalysis;

		public static ConfigurationProperty<bool> AnalysisEnabled {
			get { return EnableFancyFeatures; }
		}
		public static ConfigurationProperty<bool> FullSolutionAnalysisEnabled = IdeApp.Preferences.EnableFullSolutionSourceAnalysis;

		public readonly static ConfigurationProperty<bool> EnableUnitTestEditorIntegration = IdeApp.Preferences.EnableUnitTestEditorIntegration;

	}
}

