using System;
using System.Threading;
using System.Threading.Tasks;
using Xwt.Drawing;

namespace MonoDevelop.ConnectedServices
{

	/// <summary>
	/// Event args that describe the current status of the dependency
	/// </summary>
	public sealed class DependencyStatusChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MonoDevelop.ConnectedServices.DependencyStatusChangedEventArgs"/> class.
		/// </summary>
		public DependencyStatusChangedEventArgs (DependencyStatus newStatus, DependencyStatus oldStatus, Exception error = null)
		{
			this.NewStatus = newStatus;
			this.OldStatus = oldStatus;
			this.Error = error;
		}

		/// <summary>
		/// Gets the status of the dependency at the time of the event
		/// </summary>
		public DependencyStatus NewStatus { get; private set; }

		/// <summary>
		/// Gets the old status of the dependency at the time of the event
		/// </summary>
		public DependencyStatus OldStatus { get; private set; }

		/// <summary>
		/// Gets the exception that occured in the event of failures
		/// </summary>
		public Exception Error { get; private set; }
	}

}