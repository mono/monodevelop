//
// SystemAssemblyServiceTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2017 2017
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
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Core.Assemblies
{
	[TestFixture]
	public class SystemAssemblyServiceTests
	{
		[TestCase(true, "System.Collections.Immutable.dll")]
		[TestCase(false, "MonoDevelop.Core.dll")]
		public void ImmutableCollectionsContainReferenceToSystemRuntime (bool withSystemRuntime, string relativeDllPath)
		{
			var result = SystemAssemblyService.ContainsReferenceToSystemRuntime(relativeDllPath);
			Assert.That(result, Is.EqualTo(withSystemRuntime));
		}

		[TestCase(true, "System.Collections.Immutable.dll")]
		[TestCase(false, "MonoDevelop.Core.dll")]
		public async Task ImmutableCollectionsContainReferenceToSystemRuntimeAsync (bool withSystemRuntime, string relativeDllPath)
		{
			var result = await SystemAssemblyService.ContainsReferenceToSystemRuntimeAsync(relativeDllPath);
			Assert.That(result, Is.EqualTo(withSystemRuntime));
		}

		[Test]
		public void CheckReferencesAreOk()
		{
			var names = new[] {
				"mscorlib",
				"System.Core",
				"System"
			};

			var references = SystemAssemblyService.GetAssemblyReferences("Mono.Cecil.dll");
			Assert.That(references, Is.EquivalentTo(names));
		}
	}
}
