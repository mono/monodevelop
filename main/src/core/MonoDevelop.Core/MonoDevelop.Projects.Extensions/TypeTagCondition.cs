//
// TypeTagCondition.cs
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
using System.Linq;
using Mono.Addins;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Projects.Extensions
{
	public class ProjectTypeIdCondition: ConditionType
	{
		string[] tags;

		public ProjectTypeIdCondition (Project project)
		{
			tags = project.FlavorGuids.Concat (Enumerable.Repeat (project.TypeGuid, 1)).ToArray ();
		}

		public override bool Evaluate (NodeElement conditionNode)
		{
			var val = conditionNode.GetAttribute ("value");
			if (val.IndexOf ('|') != -1) {
				string[] ors = val.Split ('|');
				foreach (var cond in ors) {
					if (EvalAnd (cond))
						return true;
				}
				return false;
			}
			return EvalAnd (val);
		}

		bool EvalAnd (string val)
		{
			if (val.IndexOf ('&') != -1) {
				var ands = val.Split ('&');
				foreach (var tag in ands) {
					var id = MSBuildProjectService.ConvertTypeAliasToGuid (tag.Trim ());
					if (!tags.Contains (id))
						return false;
				}
				return true;
			}
			return tags.Contains (MSBuildProjectService.ConvertTypeAliasToGuid (val.Trim ()));
		}
	}
}

