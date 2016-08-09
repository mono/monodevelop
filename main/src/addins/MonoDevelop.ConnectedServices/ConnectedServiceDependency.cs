using System;
using System.Linq;
using MonoDevelop.PackageManagement;

namespace MonoDevelop.ConnectedServices
{
	public sealed class ConnectedServiceDependency : IConnectedServiceDependency
	{
		public static IConnectedServiceDependency [] Empty = new IConnectedServiceDependency [0];

		readonly IConnectedService service;

		public ConnectedServiceDependency (IConnectedService service, string id, string displayName, string version = null)
		{
			this.service = service;
			this.PackageId = id;
			this.DisplayName = displayName;
			this.PackageVersion = version;
		}

		/// <summary>
		/// Gets the display name of the dependency to present to the user
		/// </summary>
		public string DisplayName { get; private set; }

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
		public bool IsAdded {
			get {
				return PackageManagementServices.ProjectOperations.GetInstalledPackages (this.service.Project).Any (p => p.Id == this.PackageId);
			}
		}
	}
}