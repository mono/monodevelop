// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui
{
	public abstract class AbstractViewContent : AbstractBaseViewContent, IViewContent
	{
		string untitledName = "";
		string contentName  = null;
		Project project = null;
		
		bool   isDirty  = false;
		bool   isViewOnly = false;

		public override string TabPageLabel {
			get { return GettextCatalog.GetString ("Change me"); }
		}
		
		public virtual string UntitledName {
			get {
				return untitledName;
			}
			set {
				untitledName = value;
			}
		}
		
		public virtual string ContentName {
			get {
				return contentName;
			}
			set {
				if (contentName != value) {
					contentName = value;
					OnContentNameChanged(EventArgs.Empty);
				}
			}
		}
		
		public bool IsUntitled {
			get {
				return contentName == null;
			}
		}
		
		public virtual bool IsDirty {
			get {
				return isDirty;
			}
			set {
				isDirty = value;
				OnDirtyChanged(EventArgs.Empty);
			}
		}
		
		public virtual bool IsReadOnly {
			get {
				return false;
			}
		}		
		
		public virtual bool IsViewOnly {
			get {
				return isViewOnly;
			}
			set {
				isViewOnly = value;
			}
		}
		
		public virtual void Save()
		{
			OnBeforeSave(EventArgs.Empty);
			Save(contentName);
		}
		
		public virtual void Save(string fileName)
		{
			throw new System.NotImplementedException();
		}
		
		public abstract void Load(string fileName);

		public virtual Project Project
		{
			get
			{
				return project;
			}
			set
			{
				project = value;
			}
		}
		
		public override bool CanReuseView (string fileName)
		{
			return (ContentName == fileName);
		}
		
		public string PathRelativeToProject
		{
			get
			{
				if (project != null) {
					return Runtime.FileUtilityService.AbsoluteToRelativePath (project.BaseDirectory, ContentName).Substring (2);
				}
				return null;
			}
		}

		protected virtual void OnDirtyChanged(EventArgs e)
		{
			if (DirtyChanged != null) {
				DirtyChanged(this, e);
			}
		}
		
		protected virtual void OnContentNameChanged(EventArgs e)
		{
			if (ContentNameChanged != null) {
				ContentNameChanged(this, e);
			}
		}
		
		protected virtual void OnBeforeSave(EventArgs e)
		{
			if (BeforeSave != null) {
				BeforeSave(this, e);
			}
		}

		protected virtual void OnContentChanged (EventArgs e)
		{
			if (ContentChanged != null) {
				ContentChanged (this, e);
			}
		}
		
		public event EventHandler ContentNameChanged;
		public event EventHandler DirtyChanged;
		public event EventHandler BeforeSave;
		public event EventHandler ContentChanged;
	}
}
