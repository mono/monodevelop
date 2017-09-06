//
// AddPackagesResult.cs
//
// Author:
//       manish <manish.sinha@xamarin.com>
//
// Copyright (c) 2017 (c) Manish Sinha
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
using System.Collections.Generic;
using System.Reflection;
using Gtk;
using MonoDevelop.Core;

namespace MonoDevelop.Components.AutoTest.Results
{
	public class AddPackagesResult : GtkWidgetResult
	{
		public AddPackagesResult(Widget widget) : base(widget)
		{
		}

		public override bool Select()
		{
			LoggingService.LogInfo("In AddPackagesResult.Select: trying to get CheckSelectedPackage MethodInfo");
			var checkSelectedPackage = this.resultWidget.GetType().GetMethod("CheckSelectedPackage",
				BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
			if(checkSelectedPackage != null)
			{
				LoggingService.LogInfo($"Found CheckSelectedPackage MethodInfo. Invoking");
				checkSelectedPackage.Invoke(this.resultWidget, new object [] {});
				return true;
			}

			return false;
		}
	}
}
