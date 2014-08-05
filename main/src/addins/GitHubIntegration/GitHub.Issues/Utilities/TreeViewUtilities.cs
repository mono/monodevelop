using System;

namespace GitHub.Issues
{
	/// <summary>
	/// Tree view utilities - help with handling displaying, populating etc.
	/// </summary>
	public class TreeViewUtilities
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.TreeViewUtilities"/> class.
		/// </summary>
		public TreeViewUtilities ()
		{
		}

		/// <summary>
		/// Toggles the check box renderer inside the TreeView
		/// </summary>
		/// <param name="store">Store.</param>
		/// <param name="column">Column.</param>
		/// <param name="args">Arguments.</param>
		public static void ToggleCheckBoxRenderer (Gtk.ListStore store, int column, Gtk.ToggledArgs args)
		{
			Gtk.TreeIter iterator;

			// Try and find the clicked item in the column list store
			if (store.GetIter (out iterator, new Gtk.TreePath (args.Path))) {
				// Get the current value and set the value to the opposite
				bool oldToggleValue = (bool)store.GetValue (iterator, column);
				store.SetValue (iterator, column, !oldToggleValue);
			}
		}
	}
}

