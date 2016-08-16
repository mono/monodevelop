using System;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// A service dependency that represents a nuget package
	/// </summary>
	public sealed class PackageDependency : ConnectedServiceDependency, IPackageDependency
	{
		public PackageDependency (IConnectedService service, string id, string displayName, string version = null) : base(service, ConnectedServices.PackageDependencyCategory, displayName)
		{
			this.PackageId = id;
			this.PackageVersion = version;
		}

		/// <summary>
		/// Gets the nuget package id of the dependency that is added to the project
		/// </summary>
		public string PackageId { get; private set; }

		/// <summary>
		/// Gets the nuget package version of the dependency that is added to the project
		/// </summary>
		public string PackageVersion { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServiceDependency"/> is added to the project or not.
		/// </summary>
		public override bool IsAdded {
			get {
				return this.Service.Project.PackageAdded (this);
			}
		}

		/// <summary>
		/// Adds the package to the project and returns true if the package was added to the project
		/// </summary>
		public override Task<bool> AddToProject(CancellationToken token)
		{
			return this.Service.Project.AddPackageDependency (this);
		}
	}
}