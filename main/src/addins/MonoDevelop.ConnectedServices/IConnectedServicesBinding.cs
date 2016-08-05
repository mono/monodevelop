using System;
using MonoDevelop.ConnectedServices.Gui.SolutionPad;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Binds a DotNetProject with the IConnectedService instances that support the project
	/// </summary>
	interface IConnectedServicesBinding
	{
		/// <summary>
		/// Gets the project
		/// </summary>
		DotNetProject Project { get; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServicesProject"/> has any services that support the project.
		/// </summary>
		bool HasSupportedServices { get; }

		/// <summary>
		/// Gets the services that support the project
		/// </summary>
		IConnectedService [] SupportedServices { get; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServicesProject"/> has any services that have been added.
		/// </summary>
		bool HasAddedServices { get; }

		/// <summary>
		/// Gets or sets the services node that is shown in the solution pad.
		/// </summary>
		/// <remarks>
		/// Only the node builder should set this.
		/// </remarks>
		ConnectedServiceFolderNode ServicesNode { get; set; }
	}
}