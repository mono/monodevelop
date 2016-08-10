using System;
using System.Threading.Tasks;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Simple implementation of IConnectedServiceDependency.
	/// </summary>
	public class ConnectedServiceDependency : IConnectedServiceDependency
	{
		public static readonly IConnectedServiceDependency [] Empty = new IConnectedServiceDependency [0];

		public ConnectedServiceDependency (IConnectedService service, string category, string displayName)
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
		/// Adds the nuget to the project
		/// </summary>
		public virtual Task AddToProject ()
		{
			return Task.FromResult (true);
		}
	}
}