//  IWorkbench.cs
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
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Codons;

namespace MonoDevelop.Ide.Gui
{
	public class WorkbenchContext
	{
		string id;
		static Hashtable contexts = new Hashtable ();
		
		WorkbenchContext (string id)
		{
			this.id = id;
		}
		
		public static WorkbenchContext GetContext (string id)
		{
			WorkbenchContext ctx = (WorkbenchContext) contexts [id];
			if (ctx == null) {
				ctx = new WorkbenchContext (id);
				contexts [id] = ctx;
			}
			return ctx;
		}
		
		public static WorkbenchContext Edit {
			get { return GetContext ("Edit"); }
		}
		
		public static WorkbenchContext Debug {
			get { return GetContext ("Debug"); }
		}
		
		public string Id {
			get { return id; }
		}
	}
	
	/// <summary>
	/// This is the basic interface to the workspace.
	/// </summary>
	internal interface IWorkbench : IMementoCapable
	{
		/// <summary>
		/// The title shown in the title bar.
		/// </summary>
		string Title {
			get;
			set;
		}
		
		/// <summary>
		/// A collection in which all active workspace windows are saved.
		/// </summary>
		ReadOnlyCollection<IViewContent> ViewContentCollection {
			get;
		}
		
		/// <summary>
		/// A collection in which all active workspace windows are saved.
		/// </summary>
		ReadOnlyCollection<PadCodon> PadContentCollection {
			get;
		}
		
		/// <summary>
		/// The active workbench window.
		/// </summary>
		IWorkbenchWindow ActiveWorkbenchWindow {
			get;
		}
		
		IWorkbenchLayout WorkbenchLayout {
			get;
		}
		
		/// <summary>
		/// Inserts a new <see cref="IViewContent"/> object in the workspace.
		/// </summary>
		void ShowView (IViewContent content, bool bringToFront);
		
		/// <summary>
		/// Inserts a new <see cref="IPadContent"/> object in the workspace.
		/// </summary>
		void ShowPad(PadCodon content);
		void AddPad(PadCodon content);
		
		void CloseContent(IViewContent content);
		
		/// <summary>
		/// Returns a pad from a specific type.
		/// </summary>
		PadCodon GetPad(Type type);
		
		/// <summary>
		/// Returns a pad from an id.
		/// </summary>
		PadCodon GetPad(string id);
		
		/// <summary>
		/// Tries to make the pad visible to the user.
		/// </summary>
		void BringToFront (PadCodon content);
		
		/// <summary>
		/// Closes all views inside the workbench.
		/// </summary>
		void CloseAllViews();
		
		/// <summary>
		/// Re-initializes all components of the workbench, should be called
		/// when a special property is changed that affects layout st	uff.
		/// (like language change) 
		/// </summary>
		void RedrawAllComponents();

		/// <summary>
		/// Is called, when the workbench window which the user has into
		/// the foreground (e.g. editable) changed to a new one.
		/// </summary>
		event EventHandler ActiveWorkbenchWindowChanged;

		/// <summary>
		/// The context the workbench is currently in
		/// </summary>
		WorkbenchContext Context {
			get;
			set;
		}
		
		/// <summary>
		/// Called when the Context property changes
		/// </summary>
		event EventHandler ContextChanged;
	}
}
