//
// MSBuildPropertyEvaluated.cs
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
using System.Xml;
using Microsoft.Build.BuildEngine;
using System.Xml.Linq;
using MonoDevelop.Core;
using System.Globalization;

namespace MonoDevelop.Projects.MSBuild
{

	class MSBuildPropertyEvaluated: MSBuildPropertyCore, IMSBuildPropertyEvaluated
	{
		string value;
		string evaluatedValue;
		string name;

		internal MSBuildPropertyEvaluated (MSBuildProject project, string name, string value, string evaluatedValue)
		{
			ParentProject = project;
			this.evaluatedValue = evaluatedValue;
			this.value = value;
			this.name = name;
		}

		internal override string GetName ()
		{
			return name;
		}

		public bool IsImported { get; set; }

		public override string UnevaluatedValue {
			get { return value; }
		}

		internal override string GetPropertyValue ()
		{
			return evaluatedValue;
		}
	}
	
}
