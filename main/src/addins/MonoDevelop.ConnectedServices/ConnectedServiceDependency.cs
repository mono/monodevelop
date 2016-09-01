using System;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide;
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
		public static readonly IConnectedServiceDependency [] Empty = new IConnectedServiceDependency [0];

		Image icon;

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
		public virtual bool IsAdded { get { return this.Service.IsAdded; } }

		/// <summary>
		/// Adds the dependency to the project and returns true if the dependency was added to the project
		/// </summary>
		public async Task<bool> AddToProject (bool licensesAccepted, CancellationToken token)
		{
			Adding?.Invoke (this, EventArgs.Empty);
			bool result;
			try {
				result = await OnAddToProject (licensesAccepted, token).ConfigureAwait (false);
			} catch {
				AddingFailed?.Invoke (this, EventArgs.Empty);
				throw;
			}
			Added?.Invoke (this, EventArgs.Empty);
			return result;
		}
		
		/// <summary>
		/// Removes the dependency from the project
		/// </summary>
		public async Task<bool> RemoveFromProject (CancellationToken token)
		{
			Removing?.Invoke (this, EventArgs.Empty);
			bool result;
			try {
				result = await OnRemoveFromProject (token).ConfigureAwait (false);
			} catch {
				RemovingFailed?.Invoke (this, EventArgs.Empty);
				throw;
			}
			Removed?.Invoke (this, EventArgs.Empty);
			return result;
		}

		/// <summary>
		/// Performs the logic of adding the service to the project. This is called after the dependencies have been added.
		/// </summary>
		protected abstract Task<bool> OnAddToProject (bool licensesAccepted, CancellationToken token);

		/// <summary>
		/// Performs the logic of adding the service to the project. This is called after the dependencies have been added.
		/// </summary>
		protected abstract Task<bool> OnRemoveFromProject (CancellationToken token);

		/// <summary>
		/// Occurs before the dependency is added to the project
		/// </summary>
		public event EventHandler<EventArgs> Adding;

		/// <summary>
		/// Occurs when adding the dependency to the project has failed
		/// </summary>
		public event EventHandler<EventArgs> AddingFailed;

		/// <summary>
		/// Occurs when dependency has been added to the project
		/// </summary>
		public event EventHandler<EventArgs> Added;

		/// <summary>
		/// Occurs before the dependency is being removed from the project
		/// </summary>
		public event EventHandler<EventArgs> Removing;

		/// <summary>
		/// Occurs when the dependency has been removed to the project
		/// </summary>
		public event EventHandler<EventArgs> Removed;

		/// <summary>
		/// Occurs when removing the dependency to the project has failed
		/// </summary>
		public event EventHandler<EventArgs> RemovingFailed;
	}
}