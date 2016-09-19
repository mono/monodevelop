using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices.Gui.SolutionPad
{
	/// <summary>
	/// Defines the Connected Services folder node that is shown to the user in the solution pad
	/// </summary>
	sealed class ConnectedServiceFolderNode 
	{
		List<ConnectedServiceNode> childNodes = new List<ConnectedServiceNode>();

		public ConnectedServiceFolderNode (DotNetProject project)
		{
			Project = project;
			CreateChildNodes ();
		}

		public bool HasChildNodes {
			get {
				return Project.GetConnectedServicesBinding ()?.HasAddedServices == true;
			}
		}

		public ConnectedServiceNode GetServiceNode (IConnectedService service)
		{
			return childNodes.Find (node => node.Id == service.Id);
		}

		public IEnumerable<ConnectedServiceNode> GetChildNodes ()
		{
			return childNodes;
		}

		void CreateChildNodes ()
		{
			childNodes.Clear ();
			foreach (var service in Project.GetConnectedServicesBinding ()?.SupportedServices.Where (x => x.Status == Status.Added))
				childNodes.Add (new ConnectedServiceNode (this.Project, service.Id, service.SolutionPadDisplayName));
		}

		/// <summary>
		/// Gets the project that this component services node is contained in
		/// </summary>
		public DotNetProject Project { get; private set; }

		/// <summary>
		/// Occurs when services that have been added to the project have changed
		/// </summary>
		public event EventHandler<ServicesChangedEventArgs> ServicesChanged;

		public event EventHandler SelectRequested;

		public event EventHandler ExpandRequested;

		public void Select ()
		{
			SelectRequested?.Invoke (this, EventArgs.Empty);
		}

		public void Expand ()
		{
			ExpandRequested?.Invoke (this, EventArgs.Empty);
		}

		internal void NotifyServicesChanged()
		{
			CreateChildNodes ();
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
