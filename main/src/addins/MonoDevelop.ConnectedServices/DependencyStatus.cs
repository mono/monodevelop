using System;
using System.Threading;
using System.Threading.Tasks;
using Xwt.Drawing;

namespace MonoDevelop.ConnectedServices
{

	/// <summary>
	/// Defines the various states that a dependency can be in
	/// </summary>
	public enum DependencyStatus
	{
		/// <summary>
		/// The dependency is not added to the project
		/// </summary>
		NotAdded,

		/// <summary>
		/// The dependency has been added to the project
		/// </summary>
		Added,

		/// <summary>
		/// The dependency is ciurrently being added to the project
		/// </summary>
		Adding,

		/// <summary>
		/// The dependency is currently being removed from the project
		/// </summary>
		Removing,
	}

}