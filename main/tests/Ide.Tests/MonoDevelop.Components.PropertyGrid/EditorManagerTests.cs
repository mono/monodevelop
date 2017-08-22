//
// EditorManagerTests.cs
//
// Author:
//       Marius <>
//
// Copyright (c) 2017 
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
using NUnit.Framework;

namespace MonoDevelop.Components.PropertyGrid
{
	[TestFixture]
	public class EditorManagerTests
	{
		class TestTypes
		{
			public class TypeHasIndexer
			{
				public TypeHasIndexer this [int x] {
					get { return default(TypeHasIndexer); }
				}
			}

			public class TypeHasPublicProperty
			{
				public TypeHasPublicProperty Item { get; set; }
			}

			public class TypeHasPrivateProperty
			{
				TypeHasPrivateProperty Item { get; set; }
			}

			public class TypeHasNoProperty
			{
				public TypeHasNoProperty Item;
			}
		}

		[TestCase (typeof (TestTypes.TypeHasIndexer), typeof(TestTypes.TypeHasIndexer))]
		[TestCase (typeof (TestTypes.TypeHasPublicProperty), null)]
		[TestCase (typeof (TestTypes.TypeHasPrivateProperty), null)]
		[TestCase (typeof (TestTypes.TypeHasNoProperty), null)]
		public void TestProbeForItemProperty(Type testType, Type expectedCollectionType)
		{
			var collectionType = EditorManager.GetCollectionItemType (testType);
			Assert.AreEqual (expectedCollectionType, collectionType);
		}
	}
}
