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
		/// Gets the exception that occured in the event of failures
		/// </summary>
		public Exception Error { get; private set; }
	}
}