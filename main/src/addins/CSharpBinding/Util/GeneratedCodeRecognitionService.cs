//
// GeneratedCodeRecognitionService.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Roslyn.Utilities;

namespace ICSharpCode.NRefactory6.CSharp
{
	static class GeneratedCodeRecognitionService
	{
		public static bool IsFileNameForGeneratedCode(string fileName)
		{
			if (fileName.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			string extension = Path.GetExtension(fileName);
			if (extension != string.Empty)
			{
				fileName = Path.GetFileNameWithoutExtension(fileName);

				if (fileName.EndsWith("AssemblyInfo", StringComparison.OrdinalIgnoreCase) ||
					fileName.EndsWith(".designer", StringComparison.OrdinalIgnoreCase) ||
					fileName.EndsWith(".generated", StringComparison.OrdinalIgnoreCase) ||
					fileName.EndsWith(".g", StringComparison.OrdinalIgnoreCase) ||
					fileName.EndsWith(".g.i", StringComparison.OrdinalIgnoreCase) ||
					fileName.EndsWith(".AssemblyAttributes", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		private static IEnumerable<Location> GetPreferredSourceLocations(ISymbol symbol)
		{
			var locations = symbol.Locations;

			// First return visible source locations if we have them.  Else, go to the non-visible 
			// source locations.  
			var visibleSourceLocations = locations.Where(LocationExtensions.IsVisibleSourceLocation);
			return visibleSourceLocations.Any()
				? visibleSourceLocations
					: locations.Where(loc => loc.IsInSource);
		}

		public static IEnumerable<Location> GetPreferredSourceLocations(Solution solution, ISymbol symbol)
		{
			// Prefer non-generated source locations over generated ones.

			var sourceLocations = GetPreferredSourceLocations(symbol);

			var candidateLocationGroups = from c in sourceLocations
				let doc = solution.GetDocument(c.SourceTree)
					where doc != null
				group c by IsFileNameForGeneratedCode(doc.FilePath);

			var generatedSourceLocations = candidateLocationGroups.SingleOrDefault(g => g.Key) ?? SpecializedCollections.EmptyEnumerable<Location>();
			var nonGeneratedSourceLocations = candidateLocationGroups.SingleOrDefault(g => !g.Key) ?? SpecializedCollections.EmptyEnumerable<Location>();

			return nonGeneratedSourceLocations.Any() ? nonGeneratedSourceLocations : generatedSourceLocations;
		}
	}
}

