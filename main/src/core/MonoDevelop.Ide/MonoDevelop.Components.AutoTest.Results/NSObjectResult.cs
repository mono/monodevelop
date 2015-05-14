//
// NSViewResult.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc
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
#if MAC
using System;
using System.Collections.Generic;
using System.Reflection;
using AppKit;
using Foundation;

namespace MonoDevelop.Components.AutoTest.Results
{
	public class NSObjectResult : AppResult
	{
		NSObject ResultObject;

		public NSObjectResult (NSObject resultObject)
		{
			ResultObject = resultObject;
		}

		public override AppResult Marked (string mark)
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

		object GetPropertyValue (string propertyName)
		{
			return AutoTestService.CurrentSession.UnsafeSync (delegate {
				PropertyInfo propertyInfo = ResultObject.GetType().GetProperty(propertyName);
				if (propertyInfo != null) {
					var propertyValue = propertyInfo.GetValue (ResultObject);
					if (propertyValue != null) {
						return propertyValue;
					}
				}

				return null;
			});
		}

		public override AppResult Property (string propertyName, object value)
		{
			return (GetPropertyValue (propertyName) == value) ? this : null;
		}

		public override List<AppResult> NextSiblings ()
		{
			return null;
		}

		public override bool Select ()
		{
			return false;
		}

		public override bool Click ()
		{
			return false;
		}

		public override bool EnterText (string text)
		{
			return false;
		}

		public override bool TypeKey (char key, string state)
		{
			return false;
		}

		public override bool Toggle (bool active)
		{
			return false;
		}
	}
}

#endif