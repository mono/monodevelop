//
// DockFrameBackend.cs
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

namespace MonoDevelop.Components.Docking.Internal
{
	interface IDockFrameBackend
	{
		void Initialize (IDockFrameController controller);

		/// <summary>
		/// Loads the layout, placing the pads at the correct locations. All pads included in the layout
		/// are visible and docked.
		/// </summary>
		void LoadLayout (IDockLayout dl);

		/// <summary>
		/// Refresh a section of the layout loaded with LoadLayout.
		/// </summary>
		void Refresh (IDockObject obj);

		/// <summary>
		/// Creates a backend for a DockItem
		/// </summary>
		IDockItemBackend CreateItemBackend (DockItem item);

		/// <summary>
		/// Size and position of the frame, in screen coordinates
		/// </summary>
		Xwt.Rectangle GetAllocation ();

		/// <summary>
		/// Adds a widget that covers the whole frame area (used for the welcome page)
		/// </summary>
		/// <param name="widget">The widget to show.</param>
		/// <param name="animate">If set to <c>true</c>, show the overlay using an animation.</param>
		void AddOverlayWidget (Control widget, bool animate);

		/// <summary>
		/// Removes the overlay widget added with AddOverlayWidget.
		/// </summary>
		/// <param name="animate">If set to <c>true</c>, animate the removal of the widget.</param>
		void RemoveOverlayWidget (bool animate);
	}
}

