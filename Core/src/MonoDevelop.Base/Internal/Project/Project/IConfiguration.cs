// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System.Collections;
using MonoDevelop.Internal.Project;

namespace MonoDevelop.Internal.Project
{
	/// <summary>
	/// This is the base interfaces for all configurations (projects and combines)
	/// </summary>
	public interface IConfiguration : System.ICloneable
	{
		/// <summary>
		/// The name of the configuration
		/// </summary>
		string Name {
			get;
		}
		
		void CopyFrom (IConfiguration configuration);
	}
}
