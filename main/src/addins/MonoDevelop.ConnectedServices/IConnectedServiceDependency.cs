using System;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Represents a NuGet dependency of a connected serviuce. 
	/// </summary>
	public interface IConnectedServiceDependency
	{
		/// <summary>
		/// Gets the display name of the dependency to present to the user
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Gets the nuget package id of the dependency that is added to the project
		/// </summary>
		string PackageId { get; }

		/// <summary>
		/// Gets the nuget package version of the dependency that is added to the project. Return null for the latest version
		/// </summary>
		string PackageVersion { get; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServiceDependency"/> is added to the project or not.
		/// </summary>
		bool IsAdded { get; }
	}
}