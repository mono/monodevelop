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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Core.Assemblies
{
	[TestFixture]
	public class SystemAssemblyServiceTests
	{
		static string GetDllPath(string dllName)
		{
			var directory = Path.GetDirectoryName (typeof(Runtime).Assembly.Location);
			return Path.Combine (directory, dllName);
		}

		[TestCase (true, "System.Collections.Immutable.dll")]
		[TestCase (false, "MonoDevelop.Core.dll")]
		[TestCase (false, "NonExistingDll.dll")]
		public async Task RequiresFacadeAssembliesAsync (bool addFacades, string relativeDllPath)
		{
			var result = await SystemAssemblyService.RequiresFacadeAssembliesAsync (GetDllPath (relativeDllPath));
			Assert.That (result, Is.EqualTo (addFacades));
		}

		[Test]
		public void CheckReferencesAreOk()
		{
			var names = new[] {
				"mscorlib",
				"System.Core",
				"System"
			};

			var cecilPath = GetDllPath ("Mono.Cecil.dll");
			var references = SystemAssemblyService.GetAssemblyReferences(cecilPath);
			Assert.That(references, Is.EquivalentTo(names));
		}

		[Test]
		public void CheckAssemblyReferences ()
		{
			var monoAddinsPath = GetDllPath ("Mono.Addins.dll");
			var result = SystemAssemblyService.GetAssemblyReferences (monoAddinsPath);

			Assert.AreEqual (4, result.Length);
			Assert.That (result, Contains.Item ("mscorlib"));
			Assert.That (result, Contains.Item ("System"));
			Assert.That (result, Contains.Item ("System.Core"));
			Assert.That (result, Contains.Item ("System.Xml"));
		}

		[Test]
		public void GetManifestResources ()
		{
			var mdCorePath = GetDllPath ("MonoDevelop.Core.dll");
			var result = SystemAssemblyService.GetAssemblyManifestResources (mdCorePath).ToArray ();

			Assert.That (result.Length, Is.GreaterThanOrEqualTo (1));

			var addinXml = result.SingleOrDefault (x => x.Name == "MonoDevelop.Core.addin.xml");
			Assert.IsNotNull (addinXml);

			string fromReader, actual;

			using (var streamReader = new StreamReader (addinXml.Open ())) {
				fromReader = streamReader.ReadToEnd ();
			}
			using (var streamReader = new StreamReader (typeof (SystemAssemblyService).Assembly.GetManifestResourceStream ("MonoDevelop.Core.addin.xml"))) {
				actual = streamReader.ReadToEnd ();
			}

			Assert.AreEqual (actual, fromReader);
		}

		[Test]
		public void TestFrameworkVersion ()
		{
			var xwtPath = GetDllPath ("Xwt.dll");
			var result = new SystemAssemblyService ().GetTargetFrameworkForAssembly (null, xwtPath);

			Assert.AreEqual (TargetFrameworkMoniker.ID_NET_FRAMEWORK, result.Identifier);
			Assert.AreEqual ("4.6.1", result.Version);
		}
	}
}
