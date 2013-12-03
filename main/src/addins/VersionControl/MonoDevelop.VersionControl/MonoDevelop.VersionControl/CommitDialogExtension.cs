
using System;

namespace MonoDevelop.VersionControl
{
	/// <summary>
	/// Base class for commit dialog extensions.
	/// </summary>
	public class CommitDialogExtension: Gtk.EventBox
	{
		/// <summary>
		/// Initialize the extension.
		/// </summary>
		/// <param name='changeSet'>
		/// The changeSet being committed
		/// </param>
		/// <returns>
		/// True if the extension is valid for the provided change set.
		/// False otherwise (the OnBeginCommit and OnEndCommit methods
		/// won't be called).
		/// </returns>
		public virtual bool Initialize (ChangeSet changeSet)
		{
			return true;
		}

		public virtual void CommitMessageTextViewHook (Gtk.TextView textView)
		{
		}
		
		/// <summary>
		/// Called when the commit operation starts.
		/// </summary>
		/// <param name='changeSet'>
		/// The changeSet being committed
		/// </param>
		/// <returns>
		/// False if the commit cannot continue.
		/// </returns>
		public virtual bool OnBeginCommit (ChangeSet changeSet)
		{
			return true;
		}
		
		/// <summary>
		/// Called when the commit operation ends
		/// </summary>
		/// <param name='changeSet'>
		/// The changeSet being committed
		/// </param>
		/// <param name='success'>
		/// True if the commit succeeded.
		/// </param>
		public virtual void OnEndCommit (ChangeSet changeSet, bool success)
		{
		}
		
		internal event EventHandler AllowCommitChanged;
		
		bool allowCommit = true;
		
		/// <summary>
		/// Gets or sets a value indicating whether commit can proceed.
		/// </summary>
		/// <remarks>
		/// This property can be set to False to prevent the commit to proceed
		/// (the commit button in the commit dialog will be disabled). Use it
		/// for example when the extension requires some data that has not been
		/// provided by the user.
		/// </remarks>
		protected internal bool AllowCommit {
			get { return allowCommit; }
			protected set {
				if (value == allowCommit)
					return;
				allowCommit = value;
				if (AllowCommitChanged != null)
					AllowCommitChanged (this, EventArgs.Empty);
			}
		}
	}
}
