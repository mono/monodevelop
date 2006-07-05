// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;

namespace MonoDevelop.Projects.Parser
{
	public interface IMethod : IMember
	{
		IRegion BodyRegion {
			get;
		}
		
		ParameterCollection Parameters {
			get;
		}

		bool IsConstructor {
			get;
		}
		
		/// <summary>
		/// Contains a list of formal parameters to a generic method. 
		/// <p>If this property returns null or an empty collection, the method
		/// is not generic.</p>
		/// </summary>
		GenericParameterList GenericParameters {
			get;
		}
	}
}
