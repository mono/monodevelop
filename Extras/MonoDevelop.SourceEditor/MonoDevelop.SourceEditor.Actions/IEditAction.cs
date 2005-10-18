// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

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
	}
	
	/// <summary>
	/// To define a new key for the textarea, you must write a class which
	/// implements this interface.
	/// </summary>
	public abstract class AbstractEditAction : IEditAction
	{
		Gdk.ModifierType modifier = Gdk.ModifierType.None;
		Gdk.Key key;
		
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
	}		
}

