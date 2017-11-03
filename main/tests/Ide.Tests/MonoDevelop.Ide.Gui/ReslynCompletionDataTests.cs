// 
// CompletionListWindowTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Ide.CodeCompletion;
using NUnit.Framework;

namespace MonoDevelop.Ide.Gui
{
	[TestFixture]
	public class ReslynCompletionDataTests : IdeTestBase
	{
		[Test]
		public void TestCache()
		{
			var types = new HashSet<string> (RoslynCompletionData.roslynCompletionTypeTable.Values);
			var mods = new HashSet<string> (RoslynCompletionData.modifierTypeTable.Values);
			var hashes = new Dictionary<int, string> ();
			foreach (var type in types) {
				foreach (var mod in mods) {
					var hash = RoslynCompletionData.CalculateHashCode (mod, type);
					var id = mod + type;
					if (hashes.ContainsKey (hash))
						Assert.Fail ("Hash is already there: " + id +  " equals  " + hashes[hash]);
					hashes.Add (hash, id);
				}
			}
			System.Console.WriteLine (hashes.Count);
		}

	}
}
