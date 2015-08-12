//
// ObjectResult.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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
using System.Reflection;
using System.Collections.Generic;

namespace MonoDevelop.Components.AutoTest.Results
{
	public class ObjectResult : AppResult
	{
		object value;

		internal ObjectResult (object value)
		{
			this.value = value;
		}

		public override string ToString ()
		{
			return value != null ? value.ToString () : "null";
		}

		public override string GetResultType  ()
		{
			return value.GetType ().FullName;
		}

		public override AppResult Marked (string mark)
		{
			return null;
		}

		public override AppResult Selected ()
		{
			return null;
		}

		public override AppResult CheckType (Type desiredType)
		{
			return null;
		}

		public override AppResult Text (string text, bool exact)
		{
			return null;
		}

		public override AppResult Model (string column)
		{
			return null;
		}

		public override AppResult Property (string propertyName, object value)
		{
			return null;
		}

		public override List<AppResult> NextSiblings ()
		{
			return null;
		}

		public override ObjectProperties Properties ()
		{
			return GetProperties (value);
		}

		public override bool Select ()
		{
			return false;
		}

		public override bool Click ()
		{
			return false;
		}

		public override bool TypeKey (char key, string state = "")
		{
			return false;
		}

		public override bool TypeKey (string keyString, string state = "")
		{
			return false;
		}

		public override bool EnterText (string text)
		{
			return false;
		}

		public override bool Toggle (bool active)
		{
			return false;
		}

		public override void Flash ()
		{
			
		}
	}
}
