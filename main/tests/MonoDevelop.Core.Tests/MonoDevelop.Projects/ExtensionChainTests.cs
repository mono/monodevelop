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

		static T [] GetAllExtensions<T> (ExtensionChain chain) => chain.GetAllExtensions ().Cast<T> ().ToArray ();

		static BaseTestChainedExtension [] GetAllExtensions (ExtensionChain chain) => GetAllExtensions<BaseTestChainedExtension> (chain);

		[Test]
		public void ExtensionChainConstruction ()
		{
			var (exts, chain) = CreateTestExtensionChainAndArray (2);

			var allExtensions = GetAllExtensions (chain);

			Assert.AreEqual (2, allExtensions.Length);
			Assert.AreSame (exts[0], allExtensions [0]);
			Assert.AreSame (exts[1], allExtensions [1]);
			Assert.AreSame (exts [1], exts [0].Next);
		}

		[Test]
		public void ExtensionChainModification ()
		{
			var chain = CreateTestExtensionChain (2);

			// Assert default insertion position is at the end.
			var ext = new BaseTestChainedExtension ();
			chain.AddExtension (ext);
			var currentExts = GetAllExtensions (chain);

			Assert.AreEqual (3, currentExts.Length);
			Assert.AreSame (ext, currentExts [currentExts.Length - 1]);

			// Make it insert by default the beginning
			var defaultInsertionPosition = currentExts [0];
			chain.SetDefaultInsertionPosition (defaultInsertionPosition);

			// Assert insert in default position
			var defaultInserted = new BaseTestChainedExtension ();
			chain.AddExtension (defaultInserted);
			currentExts = GetAllExtensions (chain);

			Assert.AreEqual (4, currentExts.Length);
			Assert.AreSame (defaultInserted, currentExts [0]);

			// Assert insert before a given extension
			var beforeExt = new BaseTestChainedExtension ();
			chain.AddExtension (beforeExt, insertBefore: ext);
			currentExts = GetAllExtensions (chain);

			Assert.AreEqual (5, currentExts.Length);
			Assert.AreSame (beforeExt, currentExts[currentExts.Length - 2]);
			Assert.AreSame (ext, currentExts [currentExts.Length - 1]);

			// Assert insert after a given extension
			var afterExt = new BaseTestChainedExtension ();
			chain.AddExtension (afterExt, insertAfter: ext);
			currentExts = GetAllExtensions (chain);

			Assert.AreEqual (6, currentExts.Length);
			Assert.AreSame (beforeExt, currentExts [currentExts.Length - 3]);
			Assert.AreSame (ext, currentExts [currentExts.Length - 2]);
			Assert.AreSame (afterExt, currentExts [currentExts.Length - 1]);

			// Remove the default one and probe it doesn't exist
			chain.RemoveExtension (defaultInsertionPosition);
			currentExts = GetAllExtensions (chain); ;

			Assert.AreEqual (5, currentExts.Length);
			Assert.That (currentExts, Is.Not.Contains (defaultInsertionPosition));

			// Validate that we insert at the end now
			var lastExt = new BaseTestChainedExtension ();
			chain.AddExtension (lastExt);
			currentExts = GetAllExtensions (chain);

			Assert.AreEqual (6, currentExts.Length);
			Assert.AreSame (lastExt, currentExts [currentExts.Length - 1]);
		}

		[Test]
		public void AssertDisposedExtensionChain ()
		{
			var chain = CreateTestExtensionChain (2);
			var exts = GetAllExtensions (chain);

			chain.Dispose ();

			Assert.IsTrue (exts [0].IsDisposed);
			Assert.IsTrue (exts [1].IsDisposed);

			Assert.IsNull (chain.GetAllExtensions ());
			Assert.Throws<NullReferenceException> (() => {
				chain.AddExtension (new BaseTestChainedExtension ());
			});
		}

		[Test]
		public void ExtensionChainInitializedOnCreationAndDisposal ()
		{
			var (array, chain) = CreateTestExtensionChainAndArray (2);

			// The last one doesn't get initialized
			Assert.AreEqual (1, array [0].InitializedCount);
			Assert.AreEqual (0, array [1].InitializedCount);

			chain.Dispose ();

			// Assert disposing does not initialize or rechain
			Assert.AreEqual (1, array [0].InitializedCount);
			Assert.AreEqual (0, array [1].InitializedCount);
		}

		[Test]
		public void ExtensionChainRechainedOnModification ()
		{
			var chain = CreateTestExtensionChain (2);

			chain.SetDefaultInsertionPosition (chain.GetAllExtensions ().Last ());

			chain.AddExtension (new BaseTestChainedExtension ());
			var currentExts = GetAllExtensions (chain);

			Assert.AreEqual (2, currentExts [0].InitializedCount);
			Assert.AreEqual (1, currentExts [1].InitializedCount);
			Assert.AreEqual (0, currentExts [2].InitializedCount);

			chain.AddExtension (new BaseTestChainedExtension ());
			currentExts = GetAllExtensions (chain);

			Assert.AreEqual (3, currentExts [0].InitializedCount);
			Assert.AreEqual (2, currentExts [1].InitializedCount);
			Assert.AreEqual (1, currentExts [2].InitializedCount);
			Assert.AreEqual (0, currentExts [3].InitializedCount);

			chain.RemoveExtension (currentExts [1]);
			currentExts = GetAllExtensions (chain);

			Assert.AreEqual (4, currentExts [0].InitializedCount);
			Assert.AreEqual (2, currentExts [1].InitializedCount);
			Assert.AreEqual (0, currentExts [2].InitializedCount);

			// Check that the item is removed and the chain is rechaiend
			currentExts [0].Dispose ();
			currentExts = GetAllExtensions (chain);

			Assert.AreEqual (3, currentExts [0].InitializedCount);
			Assert.AreEqual (0, currentExts [1].InitializedCount);
		}

		[Test]
		public void ExtensionChainRechainingBatchModification ()
		{
			BaseTestChainedExtension [] currentExts;
			var chain = CreateTestExtensionChain (2);

			chain.SetDefaultInsertionPosition (chain.GetAllExtensions ().Last ());

			using (chain.BatchModify ()) {

				chain.AddExtension (new BaseTestChainedExtension ());
				currentExts = GetAllExtensions (chain);

				Assert.AreEqual (1, currentExts [0].InitializedCount);
				Assert.AreEqual (0, currentExts [1].InitializedCount);
				Assert.AreEqual (0, currentExts [2].InitializedCount);

				chain.AddExtension (new BaseTestChainedExtension ());
				currentExts = GetAllExtensions (chain);

				Assert.AreEqual (1, currentExts [0].InitializedCount);
				Assert.AreEqual (0, currentExts [1].InitializedCount);
				Assert.AreEqual (0, currentExts [2].InitializedCount);
				Assert.AreEqual (0, currentExts [3].InitializedCount);

				chain.RemoveExtension (currentExts [1]);
				currentExts = GetAllExtensions (chain);

				Assert.AreEqual (1, currentExts [0].InitializedCount);
				Assert.AreEqual (0, currentExts [1].InitializedCount);
				Assert.AreEqual (0, currentExts [2].InitializedCount);

				currentExts [1].Dispose ();
				currentExts = GetAllExtensions (chain);
				Assert.AreEqual (1, currentExts [0].InitializedCount);
				Assert.AreEqual (0, currentExts [1].InitializedCount);
			}

			// We should get an initialization here
			currentExts = GetAllExtensions (chain);

			Assert.AreEqual (2, currentExts [0].InitializedCount);
			Assert.AreEqual (1, currentExts [1].InitializedCount);
			Assert.AreEqual (0, currentExts [2].InitializedCount);
		}

		[Test]
		public void ExtensionChainQuerying ()
		{
			var exts = new BaseTestChainedExtension [] {
				new SquareChainedExtension (),
				new RectangleChainedExtension (),
				new ShapeChainedExtension (),
				new GreenChainedExtension (),
				new ColorChainedExtension (),
				new BaseTestChainedExtension (),
			};

			var initialSquare = exts [0];
			var initialGreen = exts [3];
			var initialBase = exts [5];

			var chain = ExtensionChain.Create (exts);

			// Look for shape extensions
			var square = chain.GetExtension<SquareChainedExtension> ();
			var rectangle = chain.GetExtension<RectangleChainedExtension> ();
			var shape = chain.GetExtension<ShapeChainedExtension> ();

			Assert.IsNull (square.Next);
			Assert.AreSame (initialSquare, rectangle.Next);
			Assert.AreSame (initialSquare, shape.Next);

			// Look for color extensions
			var green = chain.GetExtension<GreenChainedExtension> ();
			var color = chain.GetExtension<ColorChainedExtension> ();

			Assert.IsNull (green.Next);
			Assert.AreSame (initialGreen, color.Next);

			var chainedExtension = chain.GetExtension<BaseTestChainedExtension> ();
			Assert.IsNull (chainedExtension.Next);

			chain.Dispose ();

			// Check that the extensions we queried get disposed
		}

		class BaseTestChainedExtension : ChainedExtension
		{
			public int InitializedCount { get; private set; }
			public bool IsDisposed { get; private set; }

			protected internal override void InitializeChain (ChainedExtension next)
			{
				InitializedCount++;
				base.InitializeChain (next);
			}

			public override void Dispose ()
			{
				IsDisposed = true;
				base.Dispose ();
			}
		}

		class ShapeChainedExtension : BaseTestChainedExtension
		{
			public virtual string GetShapeName () => "None";
		}

		class RectangleChainedExtension : ShapeChainedExtension
		{
			public override string GetShapeName () => "Rectangle";
		}

		class SquareChainedExtension : RectangleChainedExtension
		{
			public override string GetShapeName () => "Square";
		}

		class ColorChainedExtension : BaseTestChainedExtension
		{
			public virtual string GetColorName () => "None";
		}

		class GreenChainedExtension : ColorChainedExtension
		{
			public override string GetColorName () => "Green";
		}
	}
}
