//
// IDockFrameController.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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

namespace MonoDevelop.Components.Docking
{
	interface IDockFrameController
	{
		DockVisualStyle DefaultVisualStyle { get; }

		/// <summary>
		/// Gets the style for a dock object, which will inherit values from all region/style definitions
		/// </summary>
		DockVisualStyle GetRegionStyleForObject (DockObject obj);

		/// <summary>
		/// Gets the style assigned to a specific position of the layout
		/// </summary>
		/// <returns>
		/// The region style for position.
		/// </returns>
		/// <param name='parentGroup'>
		/// Group which contains the position
		/// </param>
		/// <param name='childIndex'>
		/// Index of the position inside the group
		/// </param>
		/// <param name='insertingPosition'>
		/// If true, the position will be inserted (meaning that the objects in childIndex will be shifted 1 position)
		/// </param>
		DockVisualStyle GetRegionStyleForPosition (DockGroup parentGroup, int childIndex, bool insertingPosition);

		int DefaultItemWidth { get; }

		int DefaultItemHeight { get; }

		uint AutoShowDelay { get; }

		uint AutoHideDelay { get; }
	}
}

