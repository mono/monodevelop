 /* 
 * TypeDescriptorFilterService.cs - performs property/event/attribute filtering 
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
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
using System.ComponentModel.Design;
using System.Web.UI.Design;
using System.ComponentModel;

namespace AspNetEdit.Editor.ComponentModel
{
	public class TypeDescriptorFilterService : ITypeDescriptorFilterService
	{
		public bool FilterAttributes (IComponent component, System.Collections.IDictionary attributes)
		{
			//get the IDesignerFilter interface
			IDesignerHost host = component.Site.GetService (typeof (IDesignerHost)) as IDesignerHost;
			if (host == null)
				return true;
			IDesignerFilter designer = host.GetDesigner (component) as IDesignerFilter;
			if (designer == null)
				return true;

			//Invoke the filtering methods
			designer.PreFilterAttributes (attributes);
			designer.PostFilterAttributes (attributes);

			return true;
		}

		public bool FilterEvents (IComponent component, System.Collections.IDictionary events)
		{
			//get the IDesignerFilter interface
			IDesignerHost host = component.Site.GetService (typeof (IDesignerHost)) as IDesignerHost;
			if (host == null)
				return false;
			IDesignerFilter designer = host.GetDesigner (component) as IDesignerFilter;
			if (designer == null)
				return false;

			//Invoke the filtering methods
			designer.PreFilterEvents (events);
			designer.PostFilterEvents (events);

			return true;
		}

		public bool FilterProperties (IComponent component, System.Collections.IDictionary properties)
		{
			//get the IDesignerFilter interface
			IDesignerHost host = component.Site.GetService (typeof (IDesignerHost)) as IDesignerHost;
			if (host == null)
				return false;
			IDesignerFilter designer = host.GetDesigner (component) as IDesignerFilter;
			if (designer == null)
				return false;

			//Invoke the filtering methods
			designer.PreFilterProperties (properties);
			designer.PostFilterProperties (properties);

			return true;
		}
	}
}
