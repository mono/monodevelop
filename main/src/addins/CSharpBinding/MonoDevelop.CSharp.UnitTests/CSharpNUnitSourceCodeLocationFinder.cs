//
// CSharpNUnitSourceCodeLocationFinder.cs
//
// Author:
//       mkrueger <>
//
// Copyright (c) 2016 mkrueger
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
using MonoDevelop.UnitTesting;
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using Microsoft.CodeAnalysis;
using System.Linq;
using MonoDevelop.Projects;

namespace MonoDevelop.CSharp.UnitTests
{
	class CSharpNUnitSourceCodeLocationFinder : NUnitSourceCodeLocationFinder
	{
		public override async System.Threading.Tasks.Task<SourceCodeLocation> GetSourceCodeLocationAsync (MonoDevelop.Projects.Project project, string fixtureTypeNamespace, string fixtureTypeName, string testName, System.Threading.CancellationToken cancellationToken)
		{
			var ctx = await TypeSystemService.GetCompilationAsync (project, cancellationToken).ConfigureAwait (false);
			var cls = ctx?.Assembly?.GetTypeByMetadataName (string.IsNullOrEmpty (fixtureTypeNamespace) ? fixtureTypeName : fixtureTypeNamespace + "." + fixtureTypeName);
			if (cls == null)
				return null;
			if (cls.Name != testName) {
				foreach (var met in cls.GetMembers (testName).OfType<IMethodSymbol> ()) {
					var loc = met.Locations.FirstOrDefault (l => l.IsInSource);
					return ConvertToSourceCodeLocation (loc);
				}

				int idx = testName != null ? testName.IndexOf ('(') : -1;
				if (idx > 0) {
					testName = testName.Substring (0, idx);
					foreach (var met in cls.GetMembers (testName).OfType<IMethodSymbol> ()) {
						var loc = met.Locations.FirstOrDefault (l => l.IsInSource);
						return ConvertToSourceCodeLocation (loc);
					}
				}
			}
			var classLoc = cls.Locations.FirstOrDefault (l => l.IsInSource);
			return ConvertToSourceCodeLocation (classLoc);
		}


		SourceCodeLocation ConvertToSourceCodeLocation (Location loc)
		{
			var lineSpan = loc.GetLineSpan ();
			return new SourceCodeLocation (loc.SourceTree.FilePath, lineSpan.StartLinePosition.Line, lineSpan.StartLinePosition.Character);
		}

	}
}

