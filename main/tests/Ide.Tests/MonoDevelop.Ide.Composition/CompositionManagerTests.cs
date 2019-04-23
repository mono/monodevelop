//
// CompositionManagerTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.Composition
{
	[TestFixture]
	public class CompositionManagerTests : IdeTestBase
	{
		[Test]
		public async Task ValidateRuntimeCompositionIsValid ()
		{
			var mefAssemblies = CompositionManager.ReadAssembliesFromAddins ();
			var caching = new CompositionManager.Caching (mefAssemblies, file => {
				var tmpDir = Path.Combine (Util.TmpDir, "mef", nameof(ValidateRuntimeCompositionIsValid));
				if (Directory.Exists (tmpDir)) {
					Directory.Delete (tmpDir, true);
				}
				Directory.CreateDirectory (tmpDir);
				return Path.Combine (tmpDir, file);
			}, exceptionHandler: new ThrowingHandler ());

			var (composition, catalog) = await CompositionManager.CreateRuntimeCompositionFromDiscovery (caching);
			var cacheManager = new CachedComposition ();

			await caching.Write (composition, catalog, cacheManager);
		}

		class ThrowingHandler : CompositionManager.RuntimeCompositionExceptionHandler
		{
			public override void HandleException (string message, Exception e)
			{
				throw e;
			}
		}
	}
}
