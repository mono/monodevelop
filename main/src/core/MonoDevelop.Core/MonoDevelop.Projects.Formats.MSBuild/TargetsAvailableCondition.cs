// 
// TargetAvailableCondition.cs
//  
// Author: Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using System.IO;
using System.Text;
using System.Collections.Generic;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class TargetsAvailableCondition : ConditionType
	{
		public override bool Evaluate (NodeElement conditionNode)
		{
			var target = conditionNode.GetAttribute ("target");
			
			if (string.IsNullOrEmpty (target))
				return false;
			
			string msbuild = Runtime.SystemAssemblyService.CurrentRuntime.GetMSBuildExtensionsPath ();
			Dictionary<string, string> variables = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase) {
				{ "MSBuildExtensionsPath64", msbuild },
				{ "MSBuildExtensionsPath32", msbuild },
				{ "MSBuildExtensionsPath",   msbuild }
			};
			
			string path = StringParserService.Parse (target, variables);
			if (Path.DirectorySeparatorChar != '\\')
				path = path.Replace ('\\', Path.DirectorySeparatorChar);
			
			return File.Exists (path);
		}
	}
}
