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
					icon = ImageService.GetIcon ("md-reference");
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
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:MonoDevelop.ConnectedServices.PackageDependency"/>.
		/// </summary>
		public override string ToString ()
		{
			return string.Format ("Package Dependency - {0}", this.PackageId);
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

		protected override void OnStatusChange (DependencyStatus newStatus, DependencyStatus oldStatus, Exception error = null)
		{
			// suppress Added or removed events
			if ((newStatus == DependencyStatus.Added && oldStatus == DependencyStatus.Adding) ||
			    (newStatus == DependencyStatus.NotAdded && oldStatus == DependencyStatus.Removing)) {
				return;
			}

			base.OnStatusChange (newStatus, oldStatus, error);
		}

		internal void HandlePackageStatusChanged ()
		{
			if (IsAdded) {
				base.OnStatusChange (DependencyStatus.Added, DependencyStatus.Adding, null);
			}
			else {
				base.OnStatusChange (DependencyStatus.NotAdded, DependencyStatus.Removing, null);
			}

			(Service.DependenciesSection as DependenciesSection)?.HandleDependenciesChanged ();
		}
	}
}