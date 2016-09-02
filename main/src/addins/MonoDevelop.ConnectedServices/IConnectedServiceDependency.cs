using System;
using System.Threading;
using System.Threading.Tasks;
using Xwt.Drawing;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Represents a dependency of a connected serviuce. All dependencies are assumed to be added to the project
	/// when the service is added to the project
	/// </summary>
	public interface IConnectedServiceDependency
	{
		/// <summary>
		/// Gets the category of the dependency which is used to group dependencies together
		/// </summary>
		ConnectedServiceDependencyCategory Category { get; }

		/// <summary>
		/// Gets the display name of the dependency to present to the user
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Gets the icon of the dependency to present to the user
		/// </summary>
		Image Icon { get; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServiceDependency"/> is added to the project or not.
		/// </summary>
		bool IsAdded { get; }

		/// <summary>
		/// Gets the current status of the dependency.
		/// </summary>
		DependencyStatus Status { get; }

		/// <summary>
		/// Occurs when the status of the dependency has changed.
		/// </summary>
		event EventHandler<DependencyStatusChangedEventArgs> StatusChanged;

		/// <summary>
		/// Adds the dependency to the project and returns true if the dependency was added to the project
		/// </summary>
		/// <returns> <c>true</c> if the dependency has been added successfully; otherwise <c>false</c> </returns>
		Task<bool> AddToProject (CancellationToken token);

		/// <summary>
		/// Removes the dependency from the project
		/// </summary>
		/// <returns> <c>true</c> if the dependency has been removed successfully; otherwise <c>false</c> </returns>
		Task<bool> RemoveFromProject (CancellationToken token);
	}
}