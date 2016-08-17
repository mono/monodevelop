using System;
using System.Threading;
using System.Threading.Tasks;

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

		protected ConnectedServiceDependency (IConnectedService service, string category, string displayName)
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
		public string Category { get; private set; }

		/// <summary>
		/// Gets the display name of the dependency to present to the user
		/// </summary>
		public string DisplayName { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServiceDependency"/> is added to the project or not.
		/// </summary>
		public virtual bool IsAdded { get { return this.Service.IsAdded; } }

		/// <summary>
		/// Adds the dependency to the project and returns true if the dependency was added to the project
		/// </summary>
		public async Task<bool> AddToProject (CancellationToken token)
		{
			Adding?.Invoke (this, EventArgs.Empty);
			bool result;
			try {
				result = await OnAddToProject (token).ConfigureAwait (false);
			} catch {
				AddingFailed?.Invoke (this, EventArgs.Empty);
				throw;
			}
			Added?.Invoke (this, EventArgs.Empty);
			return result;
		}

		/// <summary>
		/// Performs the logic of adding the service to the project. This is called after the dependencies have been added.
		/// </summary>
		protected abstract Task<bool> OnAddToProject (CancellationToken token);

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
	}
}