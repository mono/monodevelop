using System;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Represents a change that is made to the project when the dependencies are added to the project. 
	/// </summary>
	public interface ICodeDependency
	{
		/// <summary>
		/// Gets a description of the code that will be added when the dependencies are added (or was added).
		/// </summary>
		string CodeDescription { get; }
	}
}