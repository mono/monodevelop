using System;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Event args that describe the current status of the service or dependency
	/// </summary>
	public sealed class StatusChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MonoDevelop.ConnectedServices.StatusChangedEventArgs"/> class.
		/// </summary>
		public StatusChangedEventArgs (Status newStatus, Status oldStatus, Exception error = null)
		{
			this.NewStatus = newStatus;
			this.OldStatus = oldStatus;
			this.Error = error;
		}

		/// <summary>
		/// Gets the new status of the service or dependency at the time of the event
		/// </summary>
		public Status NewStatus { get; private set; }

		/// <summary>
		/// Gets the old status of the service or dependency at the time of the event
		/// </summary>
		public Status OldStatus { get; private set; }

		/// <summary>
		/// Gets the exception that occurred in the event of failures
		/// </summary>
		public Exception Error { get; private set; }

		/// <summary>
		/// Gets a value indicating if the status change was a result of being added to the project
		/// </summary>
		public bool WasAdded { get { return this.NewStatus == Status.Added && (this.OldStatus == Status.Adding || this.OldStatus == Status.NotAdded); }}

		/// <summary>
		/// Gets a value indicating if the status change was a result of being added to the project
		/// </summary>
		public bool WasRemoved { get { return this.NewStatus == Status.NotAdded && (this.OldStatus == Status.Removing || this.OldStatus == Status.Added); } }

		/// <summary>
		/// Gets a value indicating if the current status is that of being added to the project
		/// </summary>
		public bool IsAdding { get { return this.NewStatus == Status.Adding && this.OldStatus == Status.NotAdded; } }

		/// <summary>
		/// Gets a value indicating if the current status is that of being removed from the project
		/// </summary>
		public bool IsRemoving { get { return this.NewStatus == Status.Removing && this.OldStatus == Status.Added; } }

		/// <summary>
		/// Gets a value indicating if the current status is that the adding to the project failed
		/// </summary>
		public bool DidAddingFail { get { return this.NewStatus == Status.NotAdded && this.OldStatus == Status.Adding; } }

		/// <summary>
		/// Gets a value indicating if the current status is that the removing from the project failed
		/// </summary>
		public bool DidRemovingFail { get { return this.NewStatus == Status.NotAdded && this.OldStatus == Status.Adding; } }
	}
}