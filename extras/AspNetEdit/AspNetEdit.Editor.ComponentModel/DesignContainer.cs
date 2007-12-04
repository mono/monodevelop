 /* 
 * DesignContainer.cs - Tracks ASP.NET controls in the DesignerHost
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
using System.ComponentModel.Design.Serialization;
using System.Collections;
using System.Web.UI;

namespace AspNetEdit.Editor.ComponentModel
{
	
	internal class DesignContainer : IContainer, IComponentChangeService
	{
		public DesignContainer (DesignerHost host)
		{
			this.host = host;
			this.designers = new Hashtable ();
			this.components = new ArrayList ();
		}

		#region IContainer Members

		private DesignerHost host;
		private Hashtable designers;
		private ArrayList components;

		public void Add (IComponent component)
		{
			Add (component, null);
		}

		public void Add (IComponent component, string name)
		{
			IDesigner designer = null;

			//input checks
			if (component == null)
				throw new ArgumentException ("Cannot add null component to container", "component");
			if (!(component is Control))
				throw new ArgumentException ("This Container only accepts System.Web.UI.Control-derived components", "component");
			if (component.Site != null && component.Site.Container != this)
				component.Site.Container.Remove (component);

			//Check the name and create one if necessary
			INameCreationService nameService = host.GetService (typeof (INameCreationService)) as INameCreationService;
			if (nameService == null)
				throw new Exception ("The container must have access to a INameCreationService implementation");
			
			if (name == null || !nameService.IsValidName (name)) {
				name = nameService.CreateName (this, component.GetType ());
				System.Diagnostics.Trace.WriteLine("Generated name for component: "+name);
			}

			//check we don't already have component with same name
			if (GetComponent (name) != null)
				throw new ArgumentException ("There is already a component with this name in the container", "name");

			//we're definately adding it now, so broadcast
			OnComponentAdding (component);

			//get a site and set ID property
			//this way (not PropertyDescriptor.SetValue) won't fire change events
			((Control) component).ID = name;
			component.Site = new DesignSite (component, this);

			//Get designer. If first component, designer must be an IRootDesigner
			if (components.Count == 0)
			{
				host.SetRootComponent (component);
				designer = new RootDesigner (component);
			}

			//FIXME: Give Mono some base designers to find! We should never encounter this!
			//else
			//	designer = TypeDescriptor.CreateDesigner (component, typeof(System.ComponentModel.Design.IDesigner));

			
			if (designer == null) {
				//component.Site = null;
				//throw new Exception ("Designer could not be obtained for this component.");
			}
			else
			{
				//track and initialise it
				designers.Add (component, designer);
				designer.Initialize (component);
			}
			
			//add references to referenceManager, unless root component
			if (components.Count != 1)
				host.WebFormReferenceManager.AddReference (component.GetType ());

			//Finally put in container
			components.Add (component);

			//and broadcast completion
			OnComponentAdded (component);
		}

		internal IDesigner GetDesigner (IComponent component)
		{
			if (GetComponent (component.Site.Name) == null)
				return null;
				//throw new ArgumentException ("That component is not in the container", "component");
			return designers[component] as IDesigner;
		}

		public IComponent GetComponent (string name)
		{
			foreach (IComponent component in components)
				if (component.Site.Name == name)
					return component;
			return null;
		}


		protected System.Object GetService (System.Type service)
		{
			return host.GetService (service);
		}

		public void Remove (System.ComponentModel.IComponent component)
		{
			//safety checks
			if (component == null)
				throw new ArgumentNullException ("component");
			if (component.Site == null || component.Site.Container != this)
				throw new ArgumentException ("Component is not sited in this container");

			//broadcast start of removal process
			OnComponentRemoving (component);

			//clean up component and designer
			components.Remove (component);
			IDesigner designer = GetDesigner (component);
			if (designer != null)
			{
				designers.Remove (component);
				designer.Dispose ();
			}
			component.Site = null;

			//if someone tries to kill root component, must destroy all children too
			if (component == host.RootComponent)
			{
				//clean everything up
				foreach (System.Web.UI.Control control in Components)
					host.DestroyComponent (control);
				host.SetRootComponent (null);
				host.Reset ();
			}
			
			//TODO: remove references from referenceManager
			
			//clean up selection service
			ISelectionService sel = (ISelectionService) this.GetService (typeof (ISelectionService));
			if (sel != null && sel.GetComponentSelected (component))
				sel.SetSelectedComponents (new IComponent[] {}); 
			
			//broadcast completion of removal process
			OnComponentRemoved (component);
		}

		public ComponentCollection Components {
			get { return new ComponentCollection ((IComponent[]) components.ToArray(typeof (IComponent))); }
		}

		#endregion

		#region IComponentChangeService Members

		public event ComponentEventHandler ComponentAdded;

		public event ComponentEventHandler ComponentAdding;

		public event ComponentChangedEventHandler ComponentChanged;

		public event ComponentChangingEventHandler ComponentChanging;

		public event ComponentEventHandler ComponentRemoved;

		public event ComponentEventHandler ComponentRemoving;

		public event ComponentRenameEventHandler ComponentRename;

		public void OnComponentChanged(object component, MemberDescriptor member, object oldValue, object newValue)
		{
			if (oldValue == newValue)
				return;
			
			//the names of components in this container are actually the same as their IDs
			//so if a change to the ID occurs, we hook in and change it back if not valid
			if(component is Control && ((Control) component).Site.Container == this && member.Name == "ID") {
				//check name is valid
				bool goodVal = true;
				if (newValue == null || !(newValue is String))
					goodVal = false;
				else
					foreach (IComponent Comp in this.Components)
						if (Comp != component && Comp.Site != null && Comp.Site.Name == ((string) newValue))
							goodVal = false;

				if (goodVal)
					//success, raise change event
					OnComponentRename (component, (string) oldValue, (string) newValue);
				else
					//bad value, change back
					((PropertyDescriptor) member).SetValue (component, oldValue);			
			}
			
			if (ComponentChanged != null)
				ComponentChanged (this, new ComponentChangedEventArgs (component, member, oldValue, newValue));
			
		}

		public void OnComponentChanging (object component, MemberDescriptor member)
		{
			if (ComponentChanging != null)
				ComponentChanging (this, new ComponentChangingEventArgs (component, member));
		}

		protected void OnComponentAdded(IComponent component)
		{
			if (ComponentAdded != null)
				ComponentAdded (this, new ComponentEventArgs(component));
		}

		protected void OnComponentAdding(IComponent component)
		{
			if (ComponentAdding != null)
				ComponentAdding (this, new ComponentEventArgs(component));
		}

		protected void OnComponentRemoved(IComponent component)
		{
			if (ComponentRemoved != null)
				ComponentRemoved (this, new ComponentEventArgs(component));
		}

		protected void OnComponentRemoving (IComponent component)
		{
			if (ComponentRemoving != null)
				ComponentRemoving (this, new ComponentEventArgs(component));
		}

		protected void OnComponentRename(object component, string oldName, string newName)
		{
			host.RootDocument.RenameControl (oldName, newName);

			if (ComponentRename != null)
				ComponentRename (this, new ComponentRenameEventArgs (component, oldName, newName));
		}

		#endregion

		//the ISite implementation we use to site the components in this container
		//IDictionaryService is site-specific, so why not implement in site
		private class DesignSite : ISite, IDictionaryService
		{
			private DesignContainer container;
			private IComponent component;

			public DesignSite (IComponent component, DesignContainer container)
			{
				this.container = container;
				this.component = component;
			}

			#region ISite Members

			public IComponent Component {
				get { return component; }
			}

			public IContainer Container {
				get { return container; }
			}

			public bool DesignMode {
				get { return true; }
			}

			public string Name {
				get { return ((Control) component).ID; }
				set { ((Control) component).ID = value; }
			}
			
			#endregion

			#region IServiceProvider Members

			public object GetService (Type serviceType) {
				if (serviceType == typeof (IDictionaryService)) {
					if (dict == null)
						dict = new Hashtable ();
					return this;
				}
				return container.GetService (serviceType);
			}

			#endregion
			
			#region IDictionaryService members
			
			private Hashtable dict = null;
			
			public void SetValue (object key, object value)
			{
				//TODO: more efficient, but is behaviour as expected?
				//if ((value == null) && (dict.ContainsKey (key)))
				//	dict.Remove (key);
				//else
					dict[key] = value;
			}
			
			public object GetValue (object key)
			{
				return dict[key];
			}
			
			public object GetKey (object value)
			{
				foreach (DictionaryEntry entry in dict)
					if (entry.Value == value)
						return entry.Key;
				return null;
			}
			
			
			#endregion
		}

		#region IDisposable Members

		private bool disposed = false;

		public void Dispose ()
		{
			this.Dispose (true);
		}

		public void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing)
			{
				foreach (IComponent comp in components)
					comp.Dispose ();
				components.Clear ();

				foreach (IDesigner des in designers)
					des.Dispose ();
				designers.Clear ();

				this.components = null;
				this.designers = null;
			}
		}

		~DesignContainer ()
		{
			this.Dispose (false);
		}

		#endregion
}
}
