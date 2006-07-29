 /* 
 * RootDesigner.cs - a root designer for ASP.NET WebForms pages
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
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Web.UI;
using System.Drawing.Design;
using AspNetEdit.Editor.UI;

namespace AspNetEdit.Editor.ComponentModel
{
	internal class RootDesigner : IRootDesigner, IToolboxUser
	{
		private IComponent component;
		private RootDesignerView view;

		public RootDesigner (IComponent component)
		{
			System.Diagnostics.Trace.WriteLine ("Creating RootDesigner");
			view = RootDesignerView.GetInstance (component.Site.GetService (typeof (IDesignerHost)) as IDesignerHost);
		}

		#region IRootDesigner Members

		public object GetView (ViewTechnology technology) {
			if (technology == ViewTechnology.Passthrough)
				return view;
			else return null;
		}

		public ViewTechnology[] SupportedTechnologies {
			get {
				ViewTechnology[] tech = { ViewTechnology.Passthrough };
				return tech;
			}
		}

		#endregion

		#region IDesigner Members

		public IComponent Component {
			get { return component; }
		}

		public void DoDefaultAction ()
		{
			throw new NotImplementedException ();
		}

		public void Initialize (IComponent component)
		{
			if ( !(component is WebFormPage))
				throw new ArgumentException ("component is not a page", "component");

			this.component = component;
		}

		public DesignerVerbCollection Verbs {
			get { return new DesignerVerbCollection (); }
		}

		#endregion

		#region IDisposable Members

		public void Dispose ()
		{
			view.Destroy ();
			view.Dispose ();
		}

		#endregion

		#region IToolboxUser Members

		public bool GetToolSupported (ToolboxItem tool)
		{
			//TODO: Fix toolbox selection
			return true;
		}

		public void ToolPicked (ToolboxItem tool)
		{
			throw new NotImplementedException ();
		}

		#endregion
	}
}
