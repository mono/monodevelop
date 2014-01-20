// IWorkspaceObject.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Collections;
using MonoDevelop.Projects.Extensions;
using Mono.Addins;
using System.Linq;
using System.Threading.Tasks;


namespace MonoDevelop.Projects
{
	public abstract class WorkspaceObject: IExtendedDataItem, IFolderItem, IDisposable
	{
		Hashtable extendedProperties;
		bool initializeCalled;

		protected void Initialize<T> (T instance)
		{
			if (instance.GetType () != typeof(T))
				return;
			initializeCalled = true;
			OnInitialize ();
			InitializeExtensionChain ();
			OnExtensionChainInitialized ();
		}

		public string Name {
			get { return OnGetName (); }
		}

		public FilePath ItemDirectory {
			get { return OnGetItemDirectory (); }
		}

		public FilePath BaseDirectory {
			get { return OnGetBaseDirectory (); }
		}

		public WorkspaceObject ParentObject { get; protected set; }

		public IEnumerable<WorkspaceObject> GetChildren ()
		{
			return OnGetChildren ();
		}

		public IEnumerable<T> GetAllItems<T> () where T: WorkspaceObject
		{
			if (this is T)
				yield return (T)this;
			foreach (var c in OnGetChildren ()) {
				foreach (var r in c.GetAllItems<T> ())
					yield return r;
			}
		}

		/// <summary>
		/// Gets extended properties.
		/// </summary>
		/// <remarks>
		/// This dictionary can be used by add-ins to store arbitrary information about this solution item.
		/// Keys and values can be of any type.
		/// If a value implements IDisposable, the value will be disposed when this item is disposed.
		/// </remarks>
		public IDictionary ExtendedProperties {
			get {
				return OnGetExtendedProperties ();
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="MonoDevelop.Projects.SolutionItem"/> has been disposed.
		/// </summary>
		/// <value>
		/// <c>true</c> if disposed; otherwise, <c>false</c>.
		/// </value>
		internal protected bool Disposed { get; private set; }

		public virtual void Dispose ()
		{
			if (Disposed)
				return;

			Disposed = true;

			if (extensionChain != null) {
				extensionChain.Dispose ();
				extensionChain = null;
			}

			if (extendedProperties != null) {
				foreach (object ob in extendedProperties.Values) {
					IDisposable disp = ob as IDisposable;
					if (disp != null)
						disp.Dispose ();
				}
				extendedProperties = null;
			}
		}

		/// <summary>
		/// Gets a service instance of a given type
		/// </summary>
		/// <returns>
		/// The service.
		/// </returns>
		/// <typeparam name='T'>
		/// Type of the service
		/// </typeparam>
		/// <remarks>
		/// This method looks for an imlpementation of a service of the given type.
		/// </remarks>
		public T GetService<T> ()
		{
			return (T) GetService (typeof(T));
		}

		/// <summary>
		/// Gets a service instance of a given type
		/// </summary>
		/// <returns>
		/// The service.
		/// </returns>
		/// <param name='t'>
		/// Type of the service
		/// </param>
		/// <remarks>
		/// This method looks for an imlpementation of a service of the given type.
		/// </remarks>
		public object GetService (Type t)
		{
			return ItemExtension.GetService (t);
		}

		/// <summary>
		/// Gets a value indicating whether the extension chain for this object has already been created and initialized
		/// </summary>
		protected bool IsExtensionChainCreated {
			get { return extensionChain != null; }
		}

		ExtensionChain extensionChain;
		protected ExtensionChain ExtensionChain {
			get {
				if (extensionChain == null) {
					if (!initializeCalled)
						throw new InvalidOperationException ("The constructor of type " + GetType () + " must call Initialize(this)");
					else
						throw new InvalidOperationException ("The extension chain can't be used before OnExtensionChainInitialized() method is called");
				}
				return extensionChain;
			}
		}

		WorkspaceObjectExtension itemExtension;

		WorkspaceObjectExtension ItemExtension {
			get {
				if (itemExtension == null)
					itemExtension = ExtensionChain.GetExtension<WorkspaceObjectExtension> ();
				return itemExtension;
			}
		}

		void InitializeExtensionChain ()
		{
			// Create an initial empty extension chain. This avoid crashes in case a call to SupportsObject ends
			// calling methods from the extension

			var tempExtensions = new List<WorkspaceObjectExtension> ();
			tempExtensions.AddRange (CreateDefaultExtensions ().Reverse ());
			extensionChain = ExtensionChain.Create (tempExtensions.ToArray ());
			foreach (var e in tempExtensions)
				e.Init (this);

			// Collect extensions that support this object

			var extensions = new List<WorkspaceObjectExtension> ();
			foreach (ProjectModelExtensionNode node in AddinManager.GetExtensionNodes (ProjectService.ProjectModelExtensionsPath)) {
				if (node.CanHandleObject (this)) {
					var ext = node.CreateExtension ();
					if (ext.SupportsObject (this))
						extensions.Add (ext);
					else
						ext.Dispose ();
				}
			}

			foreach (var e in tempExtensions)
				e.Dispose ();

			// Now create the final extension chain

			extensions.Reverse ();
			extensions.AddRange (CreateDefaultExtensions ().Reverse ());
			extensionChain = ExtensionChain.Create (extensions.ToArray ());
			foreach (var e in extensions)
				e.Init (this);
		}

		protected virtual IEnumerable<WorkspaceObjectExtension> CreateDefaultExtensions ()
		{
			yield return new DefaultWorkspaceObjectExtension ();
		}

		/// <summary>
		/// Called after the object is created, but before the extension chain has been created.
		/// </summary>
		protected virtual void OnInitialize ()
		{
		}

		/// <summary>
		/// Called when the extension chain for this object has been created. This method can be overriden
		/// to do initializations on the object that require access to the extension chain
		/// </summary>
		protected virtual void OnExtensionChainInitialized ()
		{
		}

		protected virtual IDictionary OnGetExtendedProperties ()
		{
			if (extendedProperties == null)
				extendedProperties = new Hashtable ();
			return extendedProperties;
		}

		protected virtual IEnumerable<WorkspaceObject> OnGetChildren ()
		{
			yield break;
		}

		protected virtual object OnGetService (Type t)
		{
			return t.IsInstanceOfType (this) ? this : null;
		}

		protected abstract string OnGetName ();

		protected abstract string OnGetItemDirectory ();

		protected abstract string OnGetBaseDirectory ();

		internal class DefaultWorkspaceObjectExtension: WorkspaceObjectExtension
		{
			internal protected override object GetService (Type t)
			{
				return Owner.OnGetService (t);
			}
		}
	}

	public static class WorkspaceObjectExtensions
	{
		public static T As<T> (this WorkspaceObject ob) where T:class
		{
			return ob != null ? ob.GetService<T> () : null;
		}
	}
	
	public interface IWorkspaceFileObject: IFileItem, IDisposable
	{
		FileFormat FileFormat { get; }
		Task ConvertToFormat (FileFormat format, bool convertChildren);
		bool SupportsFormat (FileFormat format);
		IEnumerable<FilePath> GetItemFiles (bool includeReferencedFiles);
		new FilePath FileName { get; set; }
		bool NeedsReload { get; set; }
		bool ItemFilesChanged { get; }
		Task SaveAsync (ProgressMonitor monitor);
		string Name { get; set; }
		FilePath BaseDirectory { get; }
		FilePath ItemDirectory { get; }
	}
}
