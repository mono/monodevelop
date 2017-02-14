//
// SolutionSizeTracker.cs
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.Ide.TypeSystem
{
	sealed class SolutionSizeTracker
	{
		public static async Task<long> GetSolutionSizeAsync (Workspace workspace, SolutionId id, CancellationToken cancellationToken)
		{
			long result = 0;
			foreach (var project in workspace.CurrentSolution.Projects) {
				result += await GetProjectSizeAsync (project, cancellationToken).ConfigureAwait (false);
			}
			return result;
		}

		static async Task<long> GetProjectSizeAsync (Project project, CancellationToken cancellationToken)
		{
			if (project == null)
				return 0;

			var sum = 0L;
			foreach (var document in project.Documents)
				sum += await GetDocumentSizeAsync (document, cancellationToken).ConfigureAwait (false);

			return sum;
		}

		static async Task<long> GetDocumentSizeAsync (Document document, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			if (document == null)
				return 0;

			var result = GetFileSize (document.FilePath);
			if (result >= 0) {
				return result;
			}

			// not a physical file, in that case, use text as a fallback.
			var text = await document.GetTextAsync (CancellationToken.None).ConfigureAwait (false);
			return text.Length;
		}

		static long GetFileSize (string filepath)
		{
			if (filepath == null)
				return -1;

			try {
				// just to reduce exception thrown
				if (!File.Exists (filepath)) {
					return -1;
				}

				return new FileInfo (filepath).Length;
			} catch {
				return -1;
			}
		}
	}
}