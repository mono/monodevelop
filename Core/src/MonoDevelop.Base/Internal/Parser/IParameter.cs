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

	public interface IParameter: IComparable
	{
		string Name {
			get;
		}

		IReturnType ReturnType {
			get;
		}

		AttributeCollection AttributeCollection {
			get;
		}

		ParameterModifier Modifier {
			get;
		}

		string Documentation {
			get;
		}

		bool IsOut {
			get;
		}

		bool IsRef {
			get;
		}

		bool IsParams {
			get;
		}
	}
}
