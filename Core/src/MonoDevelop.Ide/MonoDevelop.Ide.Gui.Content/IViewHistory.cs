using System;

using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Gui.Content {
	public interface IViewHistory {
		/// <summary>
		/// Builds an INavigationPoint containing the necessary information
		/// for navigating back to the current position within the content.
		/// </summary>
		INavigationPoint BuildNavPoint ();
	}
}
