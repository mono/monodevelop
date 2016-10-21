using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Represents a set of code dependencies that are added to the project.
	/// </summary>
	public sealed class GroupedCodeDependency : ConnectedServiceDependency
	{
		readonly GroupedDependencyKind kind;
		readonly ConnectedServiceDependency [] dependencies;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:MonoDevelop.ConnectedServices.GroupedCodeDependency"/> class.
		/// </summary>
		public GroupedCodeDependency (IConnectedService service, string displayName, GroupedDependencyKind kind, params ConnectedServiceDependency[] dependencies) : base (service, ConnectedServiceDependency.CodeDependencyCategory, displayName)
		{
			if (dependencies.Any (x => x.Category != ConnectedServiceDependency.CodeDependencyCategory)) {
				throw new ArgumentException ("All dependencies in a group must be Code dependencies", nameof (dependencies));
			}

			this.kind = kind;
			this.dependencies = dependencies;
		}

		/// <summary>
		/// Adds the dependency to the project and returns true if the dependency was added to the project
		/// </summary>
		protected override async Task<bool> OnAddToProject (CancellationToken token)
		{
			if (this.dependencies.Length == 0) {
				return true;
			}

			switch (this.kind) {
				case GroupedDependencyKind.All:
				bool added = true;
				foreach (var dependency in this.dependencies) {
					added &= await dependency.AddToProject (token).ConfigureAwait (false);
				}
				return added;

				case GroupedDependencyKind.Any:
				foreach (var dependency in this.dependencies) {
					if (await dependency.AddToProject (token).ConfigureAwait (false)) {
						return true;
					}
				}

				return false;

				default:
				throw new NotSupportedException (string.Format ("Unsupported GroupedDependencyKind {0}", this.kind));
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.ConnectedServices.IConnectedServiceDependency"/> is added to the project or not.
		/// </summary>
		public override bool IsAdded {
			get {

				switch (this.kind) {
				case GroupedDependencyKind.All:
					return this.dependencies.All (x => x.Status == Status.Added);

				case GroupedDependencyKind.Any:
					return this.dependencies.Any (x => x.Status == Status.Added);

				default:
					throw new NotSupportedException (string.Format ("Unsupported GroupedDependencyKind {0}", this.kind));
				}
			}
		}

		/// <summary>
		/// Removes the dependency from the project
		/// </summary>
		protected override async Task<bool> OnRemoveFromProject (CancellationToken token)
		{
			if (this.dependencies.Length == 0) {
				return true;
			}

			var result = true;
			foreach (var dependency in this.dependencies.Reverse ()) {
				if (dependency.Status == Status.Added) {
					if (!await dependency.RemoveFromProject (token).ConfigureAwait (false)) {
						result = false;
					}
				}
			}

			return result;
		}
	}
}