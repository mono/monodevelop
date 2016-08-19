using System;

namespace MonoDevelop.ConnectedServices.Gui.SolutionPad
{
	/// <summary>
	/// Defines a specific service shown under the Connected Services node in the solution pad
	/// </summary>
	sealed class ConnectedServiceNode
	{
		public ConnectedServiceNode (string serviceId, string displayName)
		{
			this.Id = serviceId;
			this.DisplayName = displayName;
		}

		/// <summary>
		/// Gets the id of the service
		/// </summary>
		public string Id { get; private set; }

		/// <summary>
		/// Gets the display name to show as the text of the node
		/// </summary>
		public string DisplayName { get; private set; }

		/// <summary>
		/// Occurs when the node requests to be selected.
		/// </summary>
		public event EventHandler SelectRequested;

		/// <summary>
		/// Select this node in the parent tree.
		/// </summary>
		public void Select ()
		{
			SelectRequested?.Invoke (this, EventArgs.Empty);
		}
	}
}
