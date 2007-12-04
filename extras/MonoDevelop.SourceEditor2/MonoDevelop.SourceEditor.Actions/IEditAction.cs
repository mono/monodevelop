//  IEditAction.cs
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
using MonoDevelop.SourceEditor.Gui;

namespace MonoDevelop.SourceEditor.Actions
{
	/// <summary>
	/// To define a new key for the textarea, you must write a class which
	/// implements this interface.
	/// </summary>
	public interface IEditAction
	{
		/// <value>
		/// Whether to pass the event to the base editor
		/// <value>
		bool PassToBase {
			get;
			set;
		}
		
		/// <value>
		/// An array of keys on which this edit action occurs.
		/// </value>
		Gdk.Key Key {
			get;
			set;
		}

		Gdk.ModifierType State {
			get;
			set;
		}
		
		/// <remarks>
		/// When the key which is defined in the addin is pressed, this method will be invoked.
		/// </remarks>
		void Execute (SourceEditorView sourceView);

		/// <remarks>
		/// Invoked before the Execute method
		/// <remarks>
		void PreExecute (SourceEditorView sourceView);

		/// <remarks>
		/// Invoked after the Execute method
		/// <remarks>
		void PostExecute (SourceEditorView sourceView);
	}
	
	/// <summary>
	/// To define a new key for the textarea, you must write a class which
	/// implements this interface.
	/// </summary>
	public abstract class AbstractEditAction : IEditAction
	{
		Gdk.ModifierType modifier = Gdk.ModifierType.None;
		Gdk.Key key;
		bool pass = false;

		// whether to pass the event to the base editor
		public bool PassToBase {
			get { return pass; }
			set { pass = value; }
		}
		
		/// <value>
		/// An array of keys on which this edit action occurs.
		/// </value>
		public Gdk.Key Key
		{
			get { return key; }
			set { key = value; }
		}

		public Gdk.ModifierType State {
			get { return modifier; }
			set { modifier = value; }
		}
		
		/// <remarks>
		/// When the key which is defined in the addin is pressed, this method will be invoked.
		/// </remarks>
		public abstract void Execute (SourceEditorView sourceView);

		/// <remarks>
		/// When the key which is defined in the addin is pressed,
		/// this method will be invoked before Execute ().
		/// </remarks>
		public virtual void PreExecute (SourceEditorView sourceView)
		{
		}
		
		/// <remarks>
		/// When the key which is defined in the addin is pressed,
		/// this method will be invoked after Execute ().
		/// </remarks>
		public virtual void PostExecute (SourceEditorView sourceView)
		{
			// reset the state
			pass = false;
		}
	}		
}

