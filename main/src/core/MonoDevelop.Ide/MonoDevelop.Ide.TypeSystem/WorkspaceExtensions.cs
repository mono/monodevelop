//
// WorkspaceExtensions.cs
//
// Author:
//       Kirill Osenkov <https://github.com/KirillOsenkov>
//
// Copyright (c) 2018 Microsoft
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Editor.Undo;
using System;

namespace MonoDevelop.Ide.TypeSystem
{
	static class WorkspaceExtensions
	{
		internal static void RegisterSolutionCrawler (Workspace workspace)
		{
			var solutionCrawlerRegistrationService = workspace.Services.GetService<ISolutionCrawlerRegistrationService> ();
			solutionCrawlerRegistrationService.Register (workspace);
		}

		internal static void UnregisterSolutionCrawler (Workspace workspace)
		{
			var solutionCrawlerRegistrationService = workspace.Services.GetService<ISolutionCrawlerRegistrationService> ();
			solutionCrawlerRegistrationService.Unregister (workspace);
		}

		/// <summary>
		/// Create a global undo transaction for the given workspace. if the host doesn't support undo transaction,
		/// useFallback flag can be used to indicate whether it should fallback to base implementation or not.
		/// </summary>
		public static IWorkspaceGlobalUndoTransaction OpenGlobalUndoTransaction (this Workspace workspace, string description, bool useFallback = true)
		{
			var undoService = workspace.Services.GetService<IGlobalUndoService> ();

			try {
				// try using global undo service from host
				return undoService.OpenGlobalUndoTransaction (workspace, description);
			} catch (ArgumentException) {
				// it looks like it is not supported. 
				// check whether we should use fallback mechanism or not
				if (useFallback) {
					return NoOpGlobalUndoServiceFactory.Transaction;
				}

				throw;
			}
		}
	}
}