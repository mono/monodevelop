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

	public interface IParameter: ILanguageItem, IComparable
	{
		IReturnType ReturnType {
			get;
		}

		AttributeCollection AttributeCollection {
			get;
		}

		ParameterModifier Modifier {
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
		
		IMember DeclaringMember {
			get;
		}
	}
}
