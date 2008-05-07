//
// IToolboxConsumer.cs: Interface for classes that can use toolbox items.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using System.Collections.Generic;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	public interface IToolboxConsumer
	{
		//This is run when an item is activated from the toolbox service.
		void ConsumeItem (ItemToolboxNode item);
		
		// The table of targets that the drag will support
		Gtk.TargetEntry[] DragTargets { get; }
		
		// Called when an item is dragged
		void DragItem (ItemToolboxNode item, Gtk.Widget source, Gdk.DragContext ctx);
		
		//Toolbox service uses this to filter toolbox items.
		System.ComponentModel.ToolboxItemFilterAttribute[] ToolboxFilterAttributes {
			get;
		}
		
		//Used if ToolboxItemFilterAttribute demands ToolboxItemFilterType.Custom
		//If not expecting it, should just return false
		bool CustomFilterSupports (ItemToolboxNode item);
		
		// Returns the item domain to show by default in the component selector dialog
		// when this consumer is active.
		string DefaultItemDomain { get; }
	}
	
	//allows consumer to fully override all filtering
	public interface ICustomFilteringToolboxConsumer : IToolboxConsumer
	{
		bool SupportsItem (ItemToolboxNode item);
	}
}
