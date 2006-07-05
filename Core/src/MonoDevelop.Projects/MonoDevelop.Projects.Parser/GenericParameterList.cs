//
// GenericParameterList.cs: A collection of GenericParameter objects.
//
// Author:
//   Matej Urbas (matej.urbas@gmail.com)
//
// (C) 2006 Matej Urbas
// 

using System;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Parser
{
	public class GenericParameterList : List<GenericParameter>
	{
		public GenericParameterList()
		{
		}
		
		public GenericParameterList(IEnumerable<GenericParameter> enumerable) : base(enumerable)
		{
		}
		
		public GenericParameterList(int capacity) : base(capacity)
		{
		}
	}
}
