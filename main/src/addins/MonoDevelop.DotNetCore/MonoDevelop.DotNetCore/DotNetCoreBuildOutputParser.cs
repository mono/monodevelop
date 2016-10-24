//
// DotNetCoreBuildOutputParser.cs
//
// Based on mono's ToolTask.cs: Base class for command line tool tasks. 
//
// Authors:
//   Marek Sieradzki (marek.sieradzki@gmail.com)
//   Ankit Jain (jankit@novell.com)
//   Marek Safar (marek.safar@gmail.com)
//
// (C) 2005 Marek Sieradzki
// Copyright 2009 Novell, Inc (http://www.novell.com)
// Copyright 2014 Xamarin Inc
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
using System.Text;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreBuildOutputParser
	{
		StringBuilder outputBuilder = new StringBuilder ();
		List<MSBuildErrorParser.Result> results = new List<MSBuildErrorParser.Result> ();

		public void ProcessLastLine ()
		{
			if (outputBuilder.Length == 0)
				return;

			if (!outputBuilder.ToString ().EndsWith (Environment.NewLine))
				// last line, but w/o an trailing newline, so add that
				outputBuilder.Append (Environment.NewLine);

			ProcessLine (null);
		}

		public void ProcessLine (string line)
		{
			// Add to any line fragment from previous call
			if (line != null)
				outputBuilder.Append (line);

			// Don't remove empty lines!
			var lines = outputBuilder.ToString ().Split (new string [] {Environment.NewLine}, StringSplitOptions.None);

			// Clear the builder. If any incomplete line is found,
			// then it will get added back
			outputBuilder.Length = 0;
			for (int i = 0; i < lines.Length; i ++) {
				string singleLine = lines [i];
				if (i == lines.Length - 1 && !singleLine.EndsWith (Environment.NewLine)) {
					// Last line doesn't end in newline, could be part of
					// a bigger line. Save for later processing
					outputBuilder.Append (singleLine);
					continue;
				}

				ParseBuildError (singleLine);
			}
		}

		protected virtual void ParseBuildError (string singleLine)
		{
			var result = MSBuildErrorParser.TryParseLine (singleLine);
			if (result != null) {
				results.Add (result);
			}
		}

		public IEnumerable<BuildError> GetBuildErrors (DotNetProject project)
		{
			foreach (var result in results) {
				yield return new BuildError {
					FileName = result.Origin ?? String.Empty,
					Line = result.Line,
					Column = result.Column,
					EndLine = result.EndLine,
					EndColumn = result.EndColumn,
					ErrorNumber = result.Code,
					ErrorText = result.Message,
					SourceTarget = project,
					Subcategory = result.Subcategory,
					IsWarning = !result.IsError
				};
			}
		}
	}
}
