 /* 
 * DesignerHost.cs - IDesignerHost implementation. Designer transactions
 *  and service host. One level up from DesignContainer, tracks RootComponent. 
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
using System.Reflection;
using System.Collections;
using System.Drawing.Design;
using System.IO;
using System.Web.UI;
using System.Web.UI.Design;
using AspNetEdit.Editor.Persistence;

namespace AspNetEdit.Editor.ComponentModel
{
	public class DesignerHost : IDesignerHost, IDisposable
	{
		private ServiceContainer parentServices;
		private WebFormReferenceManager referenceManager;

		public DesignerHost (ServiceContainer parentServices)
		{
			this.parentServices = parentServices;
			container = new DesignContainer (this);
			referenceManager = new WebFormReferenceManager (this);

			//register services
			parentServices.AddService (typeof (IDesignerHost), this);
			parentServices.AddService (typeof (IComponentChangeService), container);
			parentServices.AddService (typeof (IWebFormReferenceManager), referenceManager);		
		}

		public WebFormReferenceManager WebFormReferenceManager
		{
			get { return referenceManager; }
		}

		#region Component management

		private DesignContainer container;
		private IComponent rootComponent = null;
		private Document rootDocument;

		public IContainer Container
		{
			get { return container; }
		}
		
		public IComponent CreateComponent (Type componentClass, string name)
		{
			//add to document, unless loading
			bool addToDoc = (this.RootDocument != null);
			return CreateComponent (componentClass, name, addToDoc);
		}
		
		internal IComponent CreateComponent (Type componentClass, string name, bool addToDoc)
		{
			System.Diagnostics.Trace.WriteLine("Attempting to create component "+name);
			//check arguments
			if (componentClass == null)
				throw new ArgumentNullException ("componentClass");
			if (!componentClass.IsSubclassOf (typeof (System.Web.UI.Control)) && componentClass != typeof (System.Web.UI.Control))
				throw new ArgumentException ("componentClass must be a subclass of System.Web.UI.Control, but is a " + componentClass.ToString (), "componentClass");

			if (componentClass.IsSubclassOf (typeof (System.Web.UI.Page)))
				throw new InvalidOperationException ("You cannot directly add a page to the host. Use NewFile() instead");

			//create the object
			IComponent component = (IComponent) Activator.CreateInstance (componentClass);

			//and add to container
			container.Add (component, name);
			
			if (addToDoc) {
				((Control)RootComponent).Controls.Add ((Control) component);
				RootDocument.AddControl ((Control)component);
			
				//select it
				ISelectionService sel = this.GetService (typeof (ISelectionService)) as ISelectionService;
				if (sel != null)
					sel.SetSelectedComponents (new IComponent[] {component});
			}
			
			System.Diagnostics.Trace.WriteLine("Created component "+name);
			return component;
		}

		public IComponent CreateComponent (Type componentClass)
		{
			return CreateComponent (componentClass, null);
		}

		public void DestroyComponent (IComponent component)
		{
			//deselect it if selected
			ISelectionService sel = this.GetService (typeof (ISelectionService)) as ISelectionService;
			bool found = false;
			if (sel != null)
				foreach (IComponent c in sel.GetSelectedComponents ())
					if (c == component) {
						found = true;
						break;
					}
			//can't modify selection in loop
			if (found) sel.SetSelectedComponents (null);
						
			if (component != RootComponent) {
				//remove from component and document
				((Control) RootComponent).Controls.Remove ((Control) component);
				RootDocument.RemoveControl ((Control)component);
			}

			//remove from container if still sited
			if (component.Site != null)
				container.Remove (component);
			
			component.Dispose ();
		}

		public IDesigner GetDesigner (IComponent component)
		{
			if (component == null)
				throw new ArgumentNullException ("component");
			else
				return container.GetDesigner (component);
		}

		public Type GetType (string typeName)
		{
			//use ITypeResolutionService if we have it, else Type.GetType();
			object typeResSvc = GetService (typeof (ITypeResolutionService));
			if (typeResSvc != null)
				return (typeResSvc as ITypeResolutionService).GetType (typeName);
			else
				return Type.GetType (typeName);
		}

		public IComponent RootComponent {
			get { return rootComponent; }
		}

		public Document RootDocument
		{
			get { return rootDocument; }
		}

		internal void SetRootComponent (IComponent rootComponent)
		{
			this.rootComponent = rootComponent;
			if (rootComponent == null) {
				rootDocument = null;
				return;
			}

			if (!(rootComponent is Control))
				throw new InvalidOperationException ("The root component must be a Control");
		}

		public string RootComponentClassName {
			get { return RootComponent.GetType ().Name; }
		}

		#endregion

		#region Transaction stuff

		private Stack transactionStack = new Stack ();

		public DesignerTransaction CreateTransaction (string description)
		{
			OnTransactionOpening ();
			Transaction trans = new Transaction (this, description);
			transactionStack.Push (trans);
			OnTransactionOpened ();

			return trans;
		}

		public DesignerTransaction CreateTransaction ()
		{
			return CreateTransaction (null);
		}

		public bool InTransaction
		{
			get { return (transactionStack.Count > 0); }
		}

		public string TransactionDescription
		{
			get {
				if (transactionStack.Count == 0)
					return null;
				else 
					return (transactionStack.Peek () as DesignerTransaction).Description;
			}
		}

		public event DesignerTransactionCloseEventHandler TransactionClosed;
		public event DesignerTransactionCloseEventHandler TransactionClosing;
		public event EventHandler TransactionOpened;
		public event EventHandler TransactionOpening;

		internal void OnTransactionClosed (bool commit, DesignerTransaction trans)
		{
			DesignerTransaction t = (DesignerTransaction) transactionStack.Pop();
			if (t != trans)
				throw new Exception ("Transactions cannot be closed out of order");
				
			if (TransactionClosed != null)
				TransactionClosed (this, new DesignerTransactionCloseEventArgs(commit));
		}

		internal void OnTransactionClosing (bool commit)
		{
			if (TransactionClosing != null)
				TransactionClosing (this, new DesignerTransactionCloseEventArgs(commit));
		}

		protected void OnTransactionOpening()
		{
			if (TransactionOpening != null)
				TransactionOpening (this, EventArgs.Empty);
		}

		protected void OnTransactionOpened ()
		{
			if (TransactionOpened != null)
				TransactionOpened (this, EventArgs.Empty);
		}
		
		#endregion

		#region Loading etc

		private bool loading = false;
		private bool activated = false;

		public event EventHandler Activated;
		public event EventHandler Deactivated;
		public event EventHandler LoadComplete;

		public void Activate ()
		{
			if (activated)
				throw new InvalidOperationException ("The host is already activated");

			//select the root component
			ISelectionService sel = GetService (typeof (ISelectionService)) as ISelectionService;
			if (sel == null)
				throw new Exception ("Could not obtain ISelectionService.");
			if (this.RootComponent == null)
				throw new InvalidOperationException ("The document must be loaded before the host can be activated");
			sel.SetSelectedComponents (new object[] {this.RootComponent});

			activated = true;
			OnActivated ();
		}

		public bool Loading {
			get { return loading; }
		}

		protected void OnLoadComplete ()
		{
			if (LoadComplete != null)
				LoadComplete (this, EventArgs.Empty);
		}

		protected void OnActivated ()
		{
			if (Activated != null)
				Activated (this, EventArgs.Empty);
		}

		protected void OnDeactivated ()
		{
			if (Deactivated != null)
				Deactivated (this, EventArgs.Empty);
		}

		#endregion 

		#region Wrapping parent ServiceContainer

		public void AddService (Type serviceType, ServiceCreatorCallback callback, bool promote)
		{
			parentServices.AddService (serviceType, callback, promote);
		}

		public void AddService (Type serviceType, object serviceInstance, bool promote)
		{
			parentServices.AddService (serviceType, serviceInstance, promote);
		}

		public void AddService (Type serviceType, ServiceCreatorCallback callback)
		{
			parentServices.AddService (serviceType, callback);
		}

		public void AddService (Type serviceType, object serviceInstance)
		{
			parentServices.AddService (serviceType, serviceInstance);
		}

		public void RemoveService (Type serviceType, bool promote)
		{
			parentServices.RemoveService (serviceType, promote);
		}

		public void RemoveService (Type serviceType)
		{
			parentServices.RemoveService (serviceType);
		}

		public object GetService (Type serviceType)
		{
			object service = parentServices.GetService (serviceType);
			if (service != null)
				return service;
			else
				return null;
		}

		#endregion

		#region IDisposable Members

		private bool disposed = false;

		public void Dispose ()
		{
			if (!this.disposed) {
				//clean up the services we've registered
				parentServices.RemoveService (typeof (IComponentChangeService));
				parentServices.RemoveService (typeof (IDesignerHost));

				//and the container
				container.Dispose ();

				disposed = true;
			}
		}

		#endregion


		public void NewFile ()
		{
			if (activated || RootComponent != null)
				throw new InvalidOperationException ("You must reset the host before loading another file.");
			loading = true;

			this.Container.Add (new WebFormPage ());
			this.rootDocument = new Document ((Control)rootComponent, this, "New Document");

			loading = false;
			OnLoadComplete ();
		}

		public void Load (Stream file, string fileName)
		{
			using (TextReader reader = new StreamReader (file))
			{
				Load (reader.ReadToEnd (), fileName);
			}
		}
		
		public void Load (string document, string fileName)
		{
			if (activated || RootComponent != null)
				throw new InvalidOperationException ("You must reset the host before loading another file.");
			loading = true;

			this.Container.Add (new WebFormPage());
			this.rootDocument = new Document ((Control)rootComponent, this, document, fileName);

			loading = false;
			OnLoadComplete ();
		}

		public void Reset ()
		{
			//container automatically destroys all children when this happens
			if (rootComponent != null)
				DestroyComponent (rootComponent);

			if (activated) {
				OnDeactivated ();
				this.activated = false;
			}
		}

		public void SaveDocumentToFile (Stream file)
		{
			StreamWriter writer = new StreamWriter (file);

			writer.Write(RootDocument.PersistDocument ());
			writer.Flush ();
		}
		
		public string PersistDocument ()
		{
			return RootDocument.PersistDocument ();
		}
		
		/*TODO: Some .NET 2.0 System.Web.UI.Design.WebFormsRootDesigner methods
		public abstract void RemoveControlFromDocument(Control control);
		public virtual void SetControlID(Control control, string id);
		public abstract string AddControlToDocument(Control newControl,	Control referenceControl, ControlLocation location);
		public virtual string GenerateEmptyDesignTimeHtml(Control control);
		public virtual string GenerateErrorDesignTimeHtml(Control control, Exception e, string errorMessage);
		*/
	}
}
