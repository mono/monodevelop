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

