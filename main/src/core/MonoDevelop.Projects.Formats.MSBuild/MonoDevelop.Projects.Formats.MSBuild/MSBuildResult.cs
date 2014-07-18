//
// MSBuildResult.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2014 Xamarin Inc.
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

namespace MonoDevelop.Projects.Formats.MSBuild
{
	[Serializable]
	public class MSBuildResult
	{
		readonly MSBuildTargetResult[] errors;
		readonly Dictionary<string,string> properties;
		readonly Dictionary<string,List<MSBuildEvaluatedItem>> items;

		public MSBuildResult (MSBuildTargetResult[] errors)
		{
			this.errors = errors;
			this.properties = new Dictionary<string,string> ();
			this.items = new Dictionary<string,List<MSBuildEvaluatedItem>> ();
		}

		public MSBuildTargetResult[] Errors {
			get { return errors; }
		}

		public Dictionary<string,List<MSBuildEvaluatedItem>> Items {
			get { return items; }
		}

		public Dictionary<string, string> Properties {
			get { return properties; }
		}
	}

}
