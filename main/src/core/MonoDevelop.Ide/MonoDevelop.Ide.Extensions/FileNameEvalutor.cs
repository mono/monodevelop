//
// FileNameEvalutor.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Linq;
using System.Text.RegularExpressions;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Extensions
{
	abstract class FileNameEvalutor
	{
		public static FileNameEvalutor CreateFileNameEvaluator (IEnumerable<string> patterns, char separator = '|')
		{
			var splitPatterns = SplitPatterns (patterns, separator).ToList ();
			if (EndsWithFileNameEvaluator.IsCompatible (splitPatterns))
				return new EndsWithFileNameEvaluator (splitPatterns);
			if (ExactFileNameEvaluator.IsCompatible (splitPatterns))
				return new ExactFileNameEvaluator (splitPatterns);
			return new RegexFileNameEvaluator (splitPatterns);
		}

		static IEnumerable<string> SplitPatterns (IEnumerable<string> patterns, char separator)
		{
			foreach (var filePattern in patterns) {
				foreach (string pattern in filePattern.Split (separator).Select (p => p.Trim ())) {
					yield return pattern;
				}
			}
		}

		public abstract bool SupportsFile (string fileName);

		class RegexFileNameEvaluator : FileNameEvalutor
		{
			Regex regex;

			public RegexFileNameEvaluator (IEnumerable<string> patterns)
			{
				regex = CreateRegex (patterns);
			}

			Regex CreateRegex (IEnumerable<string> patterns)
			{
				var globalPattern = StringBuilderCache.Allocate ();

				foreach (var filePattern in patterns) {
					string pattern = Regex.Escape (filePattern);
					pattern = pattern.Replace ("\\*", ".*");
					pattern = pattern.Replace ("\\?", ".");
					pattern = "^" + pattern + "$";
					if (globalPattern.Length > 0)
						globalPattern.Append ('|');
					globalPattern.Append (pattern);
				}
				return new Regex (StringBuilderCache.ReturnAndFree (globalPattern), RegexOptions.IgnoreCase);
			}

			public override bool SupportsFile (string fileName)
			{
				return regex.IsMatch (fileName);
			}
		}

		class EndsWithFileNameEvaluator : FileNameEvalutor
		{
			string [] endings;

			public EndsWithFileNameEvaluator (IEnumerable<string> patterns)
			{
				endings = ExtractEndings (patterns);
			}

			string [] ExtractEndings (IEnumerable<string> patterns)
			{
				var result = new List<string> ();
				foreach (var pattern in patterns)
					result.Add (pattern.Substring (1));
				return result.ToArray ();
			}

			public override bool SupportsFile (string fileName)
			{
				foreach (var ending in endings)
					if (fileName.EndsWith (ending, StringComparison.OrdinalIgnoreCase))
						return true;
				return false;
			}

			internal static bool IsCompatible (IEnumerable<string> patterns)
			{
				foreach (var pattern in patterns) {
					if (pattern.Length == 0 || pattern [0] != '*')
						return false;
					if (pattern.IndexOfAny (new [] { '*', '?' }, 1) != -1)
						return false;
				}
				return true;
			}
		}

		class ExactFileNameEvaluator : FileNameEvalutor
		{
			string [] names;

			public ExactFileNameEvaluator (IEnumerable<string> patterns)
			{
				names = patterns.ToArray ();
			}

			public override bool SupportsFile (string fileName)
			{
				foreach (var name in names)
					if (name.Equals (fileName, StringComparison.OrdinalIgnoreCase))
						return true;
				return false;
			}

			internal static bool IsCompatible (IEnumerable<string> patterns)
			{
				foreach (var pattern in patterns) {
					if (pattern.Any (p => p == '*' || p == '?'))
						return false;
				}
				return true;
			}
		}
	}
}
