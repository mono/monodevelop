//
// ExtensionChainTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using NUnit.Framework;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class ExtensionChainTests
	{
		static (BaseTestChainedExtension[], ExtensionChain) CreateTestExtensionChainAndArray (int count)
		{
			var arr = new BaseTestChainedExtension [count];
			for (int i = 0; i < count; ++i)
				arr [i] = new BaseTestChainedExtension ();
			return (arr, ExtensionChain.Create (arr));
		}

		static ExtensionChain CreateTestExtensionChain (int count)
		{
			var (_, chain) = CreateTestExtensionChainAndArray (count);
			return chain;
		}

		[Test]
		public void ExtensionChainConstruction ()
		{
			var (exts, chain) = CreateTestExtensionChainAndArray (2);

			var allExtensions = chain.GetAllExtensions ().ToArray ();

			Assert.AreEqual (2, allExtensions.Length);
			Assert.AreSame (exts[0], allExtensions [0]);
			Assert.AreSame (exts[1], allExtensions [1]);
		}

		[Test]
		public void ExtensionChainInitializedOnCreation ()
		{
			var (array, chain) = CreateTestExtensionChainAndArray (2);

			Assert.AreEqual (1, array [0].InitializedCount);

			// The equivalent of the default extension.
			Assert.AreEqual (0, array [1].InitializedCount);
		}

		[Test]
		public void ExtensionChainModification ()
		{
			var (initial, chain) = CreateTestExtensionChainAndArray (2);

			// Assert default insertion position is at the end.
			var ext = new BaseTestChainedExtension ();
			chain.AddExtension (ext);
			var currentExts = chain.GetAllExtensions ().ToArray ();

			Assert.AreEqual (3, currentExts.Length);
			Assert.AreSame (ext, currentExts[currentExts.Length - 1]);

			// Make it insert by default the beginning
			chain.SetDefaultInsertionPosition (currentExts [0]);

			// Assert insert in default position
			var defaultInserted = new BaseTestChainedExtension ();
			chain.AddExtension (defaultInserted);
			currentExts = chain.GetAllExtensions ().ToArray ();

			Assert.AreEqual (4, currentExts.Length);
			Assert.AreSame (defaultInserted, currentExts [0]);

			// Assert insert before a given extension
			var beforeExt = new BaseTestChainedExtension ();
			chain.AddExtension (beforeExt, insertBefore: ext);
			currentExts = chain.GetAllExtensions ().ToArray ();

			Assert.AreEqual (5, currentExts.Length);
			Assert.AreSame (beforeExt, currentExts[currentExts.Length - 2]);
			Assert.AreSame (ext, currentExts [currentExts.Length - 1]);

			// Assert insert after a given extension
			var afterExt = new BaseTestChainedExtension ();
			chain.AddExtension (afterExt, insertAfter: ext);
			currentExts = chain.GetAllExtensions ().ToArray ();

			Assert.AreEqual (6, currentExts.Length);
			Assert.AreSame (beforeExt, currentExts [currentExts.Length - 3]);
			Assert.AreSame (ext, currentExts [currentExts.Length - 2]);
			Assert.AreSame (afterExt, currentExts [currentExts.Length - 1]);

			// Remove the default one and probe it doesn't exist
			chain.RemoveExtension (defaultInserted);
			currentExts = chain.GetAllExtensions ().ToArray ();

			Assert.AreEqual (5, currentExts.Length);
			Assert.That (currentExts, Is.Not.Contains (defaultInserted));

			// Validate that we insert at the end now
			var lastExt = new BaseTestChainedExtension ();
			chain.AddExtension (lastExt);
			currentExts = chain.GetAllExtensions ().ToArray ();

			Assert.AreEqual (6, currentExts.Length);
			Assert.AreSame (lastExt, currentExts [currentExts.Length - 1]);
		}

		[Test]
		public void AssertDisposedExtensionChain ()
		{
			var chain = CreateTestExtensionChain (2);

			chain.Dispose ();

			Assert.IsNull (chain.GetAllExtensions ());
			Assert.Throws<NullReferenceException> (() => {
				chain.AddExtension (new BaseTestChainedExtension ());
			});
			Assert.Throws<NullReferenceException> (() => {
				chain.RemoveExtension (new BaseTestChainedExtension ());
			});
		}

		[Test]
		public void ExtensionChainRechainedOnModification ()
		{
		}

		class BaseTestChainedExtension : ChainedExtension
		{
			public int InitializedCount { get; private set; }
			public bool Disposed { get; private set; }

			protected internal override void InitializeChain (ChainedExtension next)
			{
				InitializedCount++;
				base.InitializeChain (next);
			}

			public override void Dispose ()
			{
				Disposed = true;
				base.Dispose ();
			}
		}
	}
}
