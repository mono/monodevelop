// BaseViewContent.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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
using System.Linq;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Components;
using MonoDevelop.Core;
using System.Collections.Immutable;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui
{
	public abstract class BaseViewContent : IDisposable
	{
		IWorkbenchWindow workbenchWindow;
		Project project;
		SolutionItem owner;

		public abstract Control Control { get; }

		public IWorkbenchWindow WorkbenchWindow {
			get { return workbenchWindow; }
			set {
				if (workbenchWindow != value) {
					workbenchWindow = value;
					OnWorkbenchWindowChanged ();
				}
			}
		}

		public virtual string TabPageLabel {
			get {
				return GettextCatalog.GetString ("Content");
			}
		}

		public virtual string TabAccessibilityDescription {
			get {
				return string.Empty;
			}
		}

		public virtual bool CanReuseView (string fileName)
		{
			return false;
		}

		public object GetContent (Type type)
		{
			return GetContents (type).FirstOrDefault ();
		}

		public T GetContent<T> () where T : class
		{
			return GetContents<T> ().FirstOrDefault ();
		}

		public IEnumerable<T> GetContents<T> () where T : class
		{
			return OnGetContents (typeof (T)).Cast<T> ();
		}

		public IEnumerable<object> GetContents (Type type)
		{
			return OnGetContents (type);
		}

		protected virtual object OnGetContent (Type type)
		{
			if (type.IsInstanceOfType (this))
				return this;
			else
				return null;
		}

		protected virtual IEnumerable<object> OnGetContents (Type type)
		{
			var c = OnGetContent (type);
			if (c != null)
				yield return c;
		}

		public virtual void Dispose ()
		{
		}

		protected virtual void OnWorkbenchWindowChanged ()
		{
		}

		internal protected virtual void OnSelected ()
		{
		}

		internal protected virtual void OnDeselected ()
		{
		}

		/// <summary>
		/// Gets or sets the project bound to the view
		/// </summary>
		public Project Project {
			get {
				return project;
			}
			set {
				OnSetProject (value);
			}
		}

		/// <summary>
		/// Called to update the project bound to the view.
		/// </summary>
		/// <param name="project">
		/// New project assigned to the view. It can be null.
		/// </param>
		protected virtual void OnSetProject (Project project)
		{
			this.project = project;
		}

		public SolutionItem Owner {
			get {
				return owner;
			}
			set {
				OnSetOwner (value);
			}
		}

		protected virtual void OnSetOwner (SolutionItem owner)
		{
			this.owner = owner;
		}

		/// <summary>
		/// Gets the capability of this view for being reassigned a project
		/// </summary>
		/// <value>The project reload capability.</value>
		public virtual ProjectReloadCapability ProjectReloadCapability {
			get {
				return ProjectReloadCapability.None;
			}
		}

		/// <summary>
		/// Gets the display binding of this view.
		/// </summary>
		/// <value>The display binding used to create this view.</value>
		public IDisplayBinding Binding { get; internal set; }
	}

	public enum ProjectReloadCapability
	{
		None = 0,

		/// <summary>
		/// It can keep unsaved data. Some status (such as undo queue) may be lost.
		/// </summary>
		UnsavedData = 1,

		/// <summary>
		/// It can keep unsaved data and status.
		/// </summary>
		Full = 2
	}
}
