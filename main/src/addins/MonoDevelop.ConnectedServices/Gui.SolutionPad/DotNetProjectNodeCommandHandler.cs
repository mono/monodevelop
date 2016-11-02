using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices.Gui.SolutionPad
{
	/// <summary>
	/// NodeCommandHandler derived class that adds a helper for obtaining the DotNetProject that the node is attached to
	/// </summary>
	class DotNetProjectNodeCommandHandler : NodeCommandHandler
	{
		/// <summary>
		/// Gets the Project of the node
		/// </summary>
		public DotNetProject Project { 
			get {
				if (this.CurrentNode == null) {
					return null;
				}

				return (DotNetProject)this.CurrentNode.GetParentDataItem (typeof (DotNetProject), true);
			}
		}
	}
}
