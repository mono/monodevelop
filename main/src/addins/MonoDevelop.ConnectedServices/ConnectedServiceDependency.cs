using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

using Xwt.Drawing;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Abstract implementation of IConnectedServiceDependency.
	/// </summary>
	public abstract class ConnectedServiceDependency : IConnectedServiceDependency
	{
		/// <summary>
		/// The empty set of IConnectedServiceDependencys
		/// </summary>
		public static readonly ImmutableArray<IConnectedServiceDependency> Empty = ImmutableArray.Create<IConnectedServiceDependency> ();

		/// <summary>
		/// The category string for packages, this will be localised to the user
		/// </summary>
		public readonly static ConnectedServiceDependencyCategory PackageDependencyCategory =
			new ConnectedServiceDependencyCategory (GettextCatalog.GetString ("Packages"), Stock.ServicesFolder);

		/// <summary>
		/// The category string for code, this will be localised to the user
		/// </summary>
		public readonly static ConnectedServiceDependencyCategory CodeDependencyCategory =
			new ConnectedServiceDependencyCategory (GettextCatalog.GetString ("Code"), Stock.CodeFolder);

		Status status = (Status)(-1);

		Image icon;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:MonoDevelop.ConnectedServices.ConnectedServiceDependency"/> class.
		/// </summary>
		protected ConnectedServiceDependency (IConnectedService service, ConnectedServiceDependencyCategory category, string displayName)
		{
			this.Service = service;
			this.Category = category;
			this.DisplayName = displayName;
		}

		/// <summary>
		/// Gets the service that this dependency is for
		/// </summary>
		public IConnectedService Service { get; private set; }

		/// <summary>
		/// Gets the category of the dependency which is used to group dependencies together
		/// </summary>
		public ConnectedServiceDependencyCategory Category { get; private set; }

		/// <summary>
		/// Gets the display name of the dependency to present to the user
		/// </summary>
		public string DisplayName { get; private set; }

		/// <summary>
		/// Gets the icon of the dependency to present to the user
		/// </summary>
		public virtual Image Icon {
			get {
				if (icon == null)
					icon = ImageService.GetIcon ("md-dependency");
				return icon;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServiceDependency"/> is added to the project or not.
		/// </summary>
		public virtual bool IsAdded { get { return this.Service.Status == Status.Added; } }

		/// <summary>
		/// Gets the current status of the dependency.
		/// </summary>
		public Status Status { 
			get {
				if ((int)status == -1)
					status = IsAdded ? Status.Added : Status.NotAdded;
				return status;
			}
		}

		/// <summary>
		/// Occurs when the status of the dependency has changed.
		/// </summary>
		public event EventHandler<StatusChangedEventArgs> StatusChanged;

		/// <summary>
		/// Adds the dependency to the project and returns true if the dependency was added to the project
		/// </summary>
		public async Task<bool> AddToProject (CancellationToken token)
		{
			this.ChangeStatus (Status.Adding);

			var result = false;
			try {
				result = await OnAddToProject (token).ConfigureAwait (false);

				this.ChangeStatus (Status.Added);
			} catch (Exception ex) {
				this.ChangeStatus (this.IsAdded ? Status.Added : Status.NotAdded, ex);
				throw;
			}

			return result;
		}
		
		/// <summary>
		/// Removes the dependency from the project
		/// </summary>
		public async Task<bool> RemoveFromProject (CancellationToken token)
		{
			this.ChangeStatus (Status.Removing);

			var result = false;
			try {
				result = await OnRemoveFromProject (token).ConfigureAwait (false);
				this.ChangeStatus (Status.NotAdded);
			} catch (Exception ex) {
				this.ChangeStatus (this.IsAdded ? Status.Added : Status.NotAdded, ex);
				throw;
			}

			return result;
		}

		/// <summary>
		/// Performs the logic of adding the dependency to the project.
		/// </summary>
		protected abstract Task<bool> OnAddToProject (CancellationToken token);

		/// <summary>
		/// Performs the logic of removing the dependency from the project.
		/// </summary>
		protected abstract Task<bool> OnRemoveFromProject (CancellationToken token);

		/// <summary>
		/// Raises the status change event for the new status
		/// </summary>
		protected virtual void OnStatusChange(Status newStatus, Status oldStatus, Exception error = null)
		{
			this.NotifyStatusChanged (newStatus, oldStatus, error);
		}

		/// <summary>
		/// Changes the status of the service and notifies subscribers
		/// </summary>
		protected void ChangeStatus(Status newStatus, Exception error = null)
		{
			var oldStatus = this.Status;
			this.status = newStatus;
			this.OnStatusChange (newStatus, oldStatus, error);
		}

		/// <summary>
		/// Notifies subscribers that the service status has changed
		/// </summary>
		void NotifyStatusChanged (Status newStatus, Status oldStatus, Exception error = null)
		{
			var handler = this.StatusChanged;
			if (handler != null) {
				handler (this, new StatusChangedEventArgs (newStatus, oldStatus, error));
			}
		}
	}
}