// 
// PObjectTests.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright (c) 2012, Xamarin Inc.
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
using MonoDevelop.MacDev.PlistEditor;

namespace MonoDevelop.MacDev.Tests
{
	[TestFixture]
	public class PObjectTests
	{
		[Test]
		public void ChangeEvent_Array ()
		{
			Func<PArray, PArray> addValue = a => { a.Add (new PNumber (5)); return a; };
			AssertChangeEmitted (new PArray (), array => array.Add (new PNumber (2)), "#1");
			AssertChangeEmitted (new PArray (), array => array.AssignStringList ("foo,bar,baz"), "#2");
			AssertChangeEmitted (new PArray (), array => array.Clear (), "#3");
			AssertChangeEmitted (addValue (new PArray ()), array => array.Remove (array [0]), "#4");
			AssertChangeEmitted (addValue (new PArray ()), array => array.Replace (array [0], new PNumber (5)), "#5");
		}

		[Test]
		public void ChangeEvent_Bool ()
		{
			AssertChangeEmitted (new PBoolean (true), b => b.Value = false, "#1");
		}

		[Test]
		public void ChangeEvent_PDictionary ()
		{
			Func<PDictionary, PDictionary> addKey = d => { d.Add ("key", new PNumber (1)); return d; };
			AssertChangeEmitted (new PDictionary (), d => addKey (d), "#1");
			AssertChangeEmitted (addKey (new PDictionary ()), d => d.InsertAfter ("key", "key2", new PNumber (1)), "#2");
			AssertChangeEmitted (addKey (new PDictionary ()), d => d.Remove ("key"), "#3");
			AssertChangeEmitted (addKey (new PDictionary ()), d => d ["key"].Replace (new PNumber (77)), "#4");
			AssertChangeEmitted (addKey (new PDictionary ()), d => d ["key"] = new PNumber (77), "#6");
		}

		[Test]
		public void ChangeEvent_PNumber ()
		{
			AssertChangeEmitted (new PNumber (5), n => n.Value = 6, "#1");
		}

		void AssertChangeEmitted<T> (T obj, Action<T> action, string message)
			where T : PObject
		{
			bool changed = false;
			obj.Changed += (sender, e) => changed = true;
			action (obj);
			if (!changed)
				Assert.Fail ("The changed event was not emitted: {0}", message);
		}
	}
}
