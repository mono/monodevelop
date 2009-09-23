 /*
 * EventPropertyTab.cs - A PropertyTab that displays events
 * 
 * Part of PropertyGrid - A Gtk# widget that displays and allows 
 * editing of all of an object's public properties 
 * 
 * Authors: 
 * 	Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using MonoDevelop.Core;
 
namespace MonoDevelop.Components.PropertyGrid
{
	public class EventPropertyTab : PropertyTab
	{
		public EventPropertyTab ()
		{
		}
		
		public override string TabName {
			get {return GettextCatalog.GetString ("Events"); }
		}
		
		public override bool CanExtend (object extendee)
		{
			IComponent comp = extendee as IComponent;
			if (comp == null || comp.Site == null)
				return false;
			
			IEventBindingService evtBind = (IEventBindingService) comp.Site.GetService (typeof (IEventBindingService));
			return !(evtBind == null); 
		}
		
		public override PropertyDescriptor GetDefaultProperty (object component)
		{
			IEventBindingService evtBind = GetEventService (component);
			EventDescriptor e = TypeDescriptor.GetDefaultEvent (component);
			
			return (e == null)? null : evtBind.GetEventProperty (e);			
		}
		
		public override PropertyDescriptorCollection GetProperties (object component, Attribute[] attributes)
		{
			IEventBindingService evtBind = GetEventService (component);
			return evtBind.GetEventProperties (TypeDescriptor.GetEvents (component));
		}
		
		private IEventBindingService GetEventService (object component)
		{
			IComponent comp = component as IComponent;
			if (comp == null || comp.Site == null)
				throw new Exception ("Check whether a tab can display a component before displaying it");
			IEventBindingService evtBind = (IEventBindingService) comp.Site.GetService (typeof (IEventBindingService));
			if (evtBind == null)
				throw new Exception ("Check whether a tab can display a component before displaying it");
			return evtBind;
		}
	}
}