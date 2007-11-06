//  AbstractViewContent.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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
		
		public virtual bool IsFile {
			get { return true; }
		}

		public virtual string StockIconId {
			get {
				return null;
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
					return FileService.AbsoluteToRelativePath (project.BaseDirectory, ContentName).Substring (2);
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
