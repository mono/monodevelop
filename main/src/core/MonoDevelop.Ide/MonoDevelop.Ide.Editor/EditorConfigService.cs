//
// EditorConfigService.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.CodingConventions;

namespace MonoDevelop.Ide.Editor
{
	static class EditorConfigService
	{
		readonly static object contextCacheLock = new object ();
		readonly static ICodingConventionsManager codingConventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager ();
		static ImmutableDictionary<string, ICodingConventionContext> contextCache = ImmutableDictionary<string, ICodingConventionContext>.Empty;

		public async static Task<ICodingConventionContext> GetEditorConfigContext (string fileName, CancellationToken token)
		{
			if (contextCache.TryGetValue (fileName, out ICodingConventionContext result))
				return result;
			result = await codingConventionsManager.GetConventionContextAsync (fileName, token);
			if (result == null)
				return null;
			lock (contextCacheLock) {
				if (contextCache.ContainsKey (fileName))
					return contextCache [fileName];
				contextCache = contextCache.Add (fileName, result);
				return result;
			}
		}

		public static void RemoveEditConfigContext (string fileName)
		{
			lock (contextCacheLock) {
				contextCache = contextCache.Remove (fileName);
			}
		}
	}
}
