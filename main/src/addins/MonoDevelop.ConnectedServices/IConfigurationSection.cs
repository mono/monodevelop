using System;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Components;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Represents a section to be displayed to the user in the service details page.
	/// Each section has the concept of being able to be "added" to the project, that is,
	/// to perform some action on the project when 'AddToProject' is invoked. It is the responsibility of
	/// the section to store state appropriately and to return `IsAdded` as required. The Added event should
	/// be trigered once the task has been completed.
	/// 
	/// Sections that do not perform an action on the project can return false from CanBeAdded.
	/// </summary>
	public interface IConfigurationSection 
	{
		/// <summary>
		/// Gets the service for this section
		/// </summary>
		IConnectedService Service { get; }

		/// <summary>
		/// Gets the name of the section to display to the user.
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Gets the description of the section to display to the user.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Gets a value indidating if this section represents something that can be added to the project.
		/// </summary>
		bool CanBeAdded { get; }

		/// <summary>
		/// Gets a value indicating that whatever changes to the project that can be added by this section have been added.
		/// </summary>
		bool IsAdded { get; }

		/// <summary>
		/// Gets the widget to display to the user
		/// </summary>
		Control GetSectionWidget ();

		/// <summary>
		/// Performs the tasks necessary to add the components that this section represents to the project
		/// </summary>
		Task<bool> AddToProject (CancellationToken token);

		/// <summary>
		/// Occurs when the status of the section changes
		/// </summary>
		event EventHandler<StatusChangedEventArgs> StatusChanged;
	}
}