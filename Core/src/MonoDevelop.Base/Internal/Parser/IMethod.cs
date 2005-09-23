// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;

namespace MonoDevelop.Internal.Parser
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
	}
}
