using System;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide;
using Xwt.Drawing;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// A service dependency that represents a nuget package
	/// </summary>
	public sealed class PackageDependency : ConnectedServiceDependency, IPackageDependency
	{
		Image icon;
		
		public PackageDependency (IConnectedService service, string id, string displayName, string version = null) : base(service, ConnectedServices.PackageDependencyCategory, displayName)
		{
			this.PackageId = id;
			this.PackageVersion = version;

		}

		public override Image Icon {
			get {
				if (icon == null)
					icon = ImageService.GetIcon ("md-connected-service-dependency-package");
				return icon;
			}
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
		protected override Task<bool> OnAddToProject(CancellationToken token)
		{
			return this.Service.Project.AddPackageDependency (this);
		}

		/// <summary>
		/// Removes the dependency from the project
		/// </summary>
		protected override async Task<bool> OnRemoveFromProject (CancellationToken token)
		{
			await this.Service.Project.RemovePackageDependency (this).ConfigureAwait (false);
			return true;
		}
	}
}