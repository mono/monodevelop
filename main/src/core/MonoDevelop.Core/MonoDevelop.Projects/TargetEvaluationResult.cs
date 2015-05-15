//
// TargetResult.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Projects
{
	public class TargetEvaluationResult
	{
		readonly BuildResult buildResult;
		readonly IReadOnlyPropertySet properties;
		readonly IEnumerable<IMSBuildItemEvaluated> items;

		public TargetEvaluationResult (BuildResult buildResult)
		{
			this.buildResult = buildResult;
		}

		public TargetEvaluationResult (BuildResult buildResult, IEnumerable<IMSBuildItemEvaluated> items, IReadOnlyPropertySet properties)
		{
			this.buildResult = buildResult;
			this.items = items;
			this.properties = properties;
		}

		public BuildResult BuildResult {
			get { return buildResult; }
		}

		public IEnumerable<IMSBuildItemEvaluated> Items {
			get { return items; }
		}

		public IReadOnlyPropertySet Properties {
			get { return properties; }
		}
	}
}

