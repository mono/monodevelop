using System;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices.Gui.SolutionPad
{
	/// <summary>
	/// Defines the Connected Services folder node that is shown to the user in the solution pad
	/// </summary>
	sealed class ConnectedServiceFolderNode 
	{
		public ConnectedServiceFolderNode (DotNetProject project)
		{
			Project = project;
		}

		/// <summary>
		/// Gets the project that this component services node is contained in
		/// </summary>
		public DotNetProject Project { get; private set; }

		/// <summary>
		/// Occurs when services that have been added to the project have changed
		/// </summary>
		public event EventHandler<ServicesChangedEventArgs> ServicesChanged;

		internal void NotifyServicesChanged()
		{
			this.OnServicesChanged (new ServicesChangedEventArgs());
		}

		void OnServicesChanged(ServicesChangedEventArgs args)
		{
			var handler = this.ServicesChanged;
			if (handler != null) {
				handler (this, args);
			}
		}
	}

	/// <summary>
	/// Event args that are passed when the services that have been added to a project change.
	/// </summary>
	public class ServicesChangedEventArgs : EventArgs
	{

	}
}
