using System;
using System.Threading.Tasks;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Represents a NuGet dependency of a connected serviuce. 
	/// </summary>
	public interface IPackageDependency : IConnectedServiceDependency
	{
		/// <summary>
		/// Gets the nuget package id of the dependency that is added to the project
		/// </summary>
		string PackageId { get; }

		/// <summary>
		/// Gets the nuget package version of the dependency that is added to the project. Return null for the latest version
		/// </summary>
		string PackageVersion { get; }
	}
}