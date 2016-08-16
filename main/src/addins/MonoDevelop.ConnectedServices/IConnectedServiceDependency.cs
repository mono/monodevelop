using System;
using System.Threading;
using System.Threading.Tasks;

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
		string Category { get; }

		/// <summary>
		/// Gets the display name of the dependency to present to the user
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServiceDependency"/> is added to the project or not.
		/// </summary>
		bool IsAdded { get; }

		/// <summary>
		/// Adds the nuget to the project and returns true if the dependency was added to the project
		/// </summary>
		Task<bool> AddToProject (CancellationToken token);
	}
}