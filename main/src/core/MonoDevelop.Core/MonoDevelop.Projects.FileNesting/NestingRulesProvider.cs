//
// NestingRulesFile.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2019, Microsoft Inc. (http://microsoft.com)
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

namespace MonoDevelop.Projects.FileNesting
{
	internal class NestingRulesProvider
	{
		readonly List<NestingRule> nestingRules = new List<NestingRule> ();

		public NestingRulesProvider ()
		{
			// FIXME: this is temporary, remove when we load rules from json files
			nestingRules.Add (new NestingRule (NestingRuleKind.AddedExtension, NestingRule.AllFilesWildcard, null));
			nestingRules.Add (new NestingRule (NestingRuleKind.ExtensionToExtension, ".js", new [] { ".ts", ".tsx" }));
			nestingRules.Add (new NestingRule (NestingRuleKind.FileSuffixToExtension, "-vsdoc.js", new [] { ".js" }));
			nestingRules.Add (new NestingRule (NestingRuleKind.PathSegment, NestingRule.AllFilesWildcard, new [] { ".js", ".css", ".html", ".cs" }));
			nestingRules.Add (new NestingRule (NestingRuleKind.AllExtensions, NestingRule.AllFilesWildcard, new [] { ".tt" }));
			nestingRules.Add (new NestingRule (NestingRuleKind.FileToFile, ".bowerrc", new [] { "bower.json" }));
		}

		public string GetParentFile (string inputFile)
		{
			foreach (var rule in nestingRules) {
				string parentFile = rule.GetParentFile (inputFile);
				if (!String.IsNullOrEmpty (parentFile)) {
					// Stop at the 1st rule found
					return parentFile;
				}
			}

			return null;
		}

	}
}
