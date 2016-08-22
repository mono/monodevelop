using System;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Provides IConnectedService instances for a given project. 
	/// </summary>
	/// <remarks>
	/// Implement this to be able to have your IConnectedService attached to a project
	/// </remarks>
	public interface IConnectedServiceProvider
	{
		/// <summary>
		/// Gets a new instance of IConnectedService for the given project, or null if the project is not supported.
		/// </summary>
		IConnectedService GetConnectedService (DotNetProject project);
	}
}