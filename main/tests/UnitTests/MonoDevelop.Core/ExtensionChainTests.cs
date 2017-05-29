//
// ChainedExtensionTests.cs
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
using System.Linq;
using MonoDevelop.Projects;
using NUnit.Framework;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class ExtensionChainTests
	{
		[Test]
		public void TestOneItemChain()
		{
			var exts = new [] {
				new ChainedExtension (),
			};
			var chain = ExtensionChain.Create (exts);

			Assert.AreEqual (1, chain.GetAllExtensions ().Count ());
			Assert.AreNotSame (exts [0], chain.GetExtension<ChainedExtension> ());
			Assert.IsNull (chain.GetExtension<ChainedExtension> ().Next);
		}

		[Test]
		public void TestExtensionInited ()
		{
			var exts = new [] {
				new BaseExtension (),
			};
			var chain = ExtensionChain.Create (exts);

			Assert.IsFalse (exts[0].Init);

			var exts2 = new [] {
				new BaseExtension (),
				new BaseExtension (),
			};
			var chain2 = ExtensionChain.Create (exts2);

			Assert.IsTrue (exts2[0].Init);
		}

		[Test]
		public void TestOneInherited ()
		{
			var exts = new BaseExtension[] {
				new BaseExtension(),
				new DerivedExtension()
			};
			var chain = ExtensionChain.Create (exts);

			Assert.AreEqual (2, chain.GetAllExtensions ().Count ());

			Assert.AreSame (exts [0], chain.GetExtension<ChainedExtension> ().Next);
			Assert.AreSame (exts [1], chain.GetExtension<ChainedExtension> ().Next.Next);

			Assert.AreSame (exts [1], chain.GetExtension<BaseExtension> ().Next);
			Assert.IsNull (chain.GetExtension<DerivedExtension>().Next);
		}

		[Test]
		public void TestNoInherited ()
		{
			var exts = new ChainedExtension[] {
				new BaseExtension (),
				new NoDerivedExtension (),
			};
			var chain = ExtensionChain.Create (exts);

			Assert.AreEqual (2, chain.GetAllExtensions ().Count ());
			Assert.IsNull (chain.GetExtension<BaseExtension> ().Next);
		}

		class BaseExtension : ChainedExtension
		{
			public bool Init { get; private set; }
			protected internal override void InitializeChain (ChainedExtension next)
			{
				Init = true;
				base.InitializeChain (next);
			}
		}

		class DerivedExtension : BaseExtension
		{
		}

		class NoDerivedExtension : ChainedExtension
		{
		}
	}
}
