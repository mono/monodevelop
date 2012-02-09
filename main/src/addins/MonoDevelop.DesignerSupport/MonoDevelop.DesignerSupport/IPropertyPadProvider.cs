//
// IPropertyPadProvider.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using MonoDevelop.Components.PropertyGrid;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport
{
	// The property pad service looks for objects implementing IPropertyPadProvider
	// and ICustomPropertyPadProvider in the active command route. 
	// The command route is a chain of objects that starts at the
	// object having the focus and goes up to the widget hierarchy (this route can be
	// modified by the objects in it, but that's basically it).
	
	// When one of those interfaces is found, the property pad will be updated to show
	// the information provided by them.
	
	public interface IPropertyPadProvider
	{
		// Returns the component represented by this PadProvider
		object GetActiveComponent ();
		
		// Returns the object which will be used to fill the property grid.
		// It may be different from the object returned by GetActiveComponent.
		// If null, the value of GetActiveComponent is used.
		object GetProvider ();
		
		// Called when all editing in the object has finished
		void OnEndEditing (object obj);
		
		// Called when there is a change in the property grid
		void OnChanged (object obj);
	}
	
	// Use this interface to completely replace the property pad by a custom
	// properties widget.
	public interface ICustomPropertyPadProvider
	{
		Gtk.Widget GetCustomPropertyWidget ();
		void DisposeCustomPropertyWidget ();
	}
	
	/// <summary>
	/// Implement this interface if you need to customize the property grid
	/// </summary>
	public interface IPropertyPadCustomizer
	{
		/// <summary>
		/// Called to customize the property pad. This method is called even
		/// when using a custom property pad provided by ICustomPropertyPadProvider,
		/// in which case propertyGrid is set to null.
		/// </summary>
		/// <param name='padWindow'>
		/// Pad window.
		/// </param>
		/// <param name='propertyGrid'>
		/// Property grid. It will be null when using a custom property pad.
		/// </param>
		void Customize (IPadWindow padWindow, PropertyGrid propertyGrid);
	}
}
