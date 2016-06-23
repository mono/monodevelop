//
// IUnitTestMarkers.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Gtk;
using MonoDevelop.Components;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Projects;
using Mono.Addins;

namespace MonoDevelop.UnitTesting
{

	/// <summary>
	/// Markers that can be used to identify a method as a unit test
	/// </summary>
	public interface IUnitTestMarkers
	{
		/// <summary>
		/// Type of attribute that a method needs to have to be considered to be a test method
		/// </summary>
		/// <value>The test method attribute marker.</value>
		string TestMethodAttributeMarker { get; }

		/// <summary>
		/// Type of attribute that describes a test case for a test method. It has to be applied to a test method.
		/// </summary>
		/// <value>The test method attribute marker.</value>
		string TestCaseMethodAttributeMarker { get; }

		/// <summary>
		/// Type of attribute used to mark a test method to be ignored
		/// </summary>
		/// <value>The ignore test method attribute marker.</value>
		string IgnoreTestMethodAttributeMarker { get; }

		/// <summary>
		/// Type of attribute used to mark a test class to be ignored
		/// </summary>
		/// <value>The ignore test method attribute marker.</value>
		string IgnoreTestClassAttributeMarker { get; }
	}
}
