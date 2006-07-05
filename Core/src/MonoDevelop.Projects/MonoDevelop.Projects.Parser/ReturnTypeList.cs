//
// ReturnTypeList.cs: A collection of IReturnType objects.
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
	public class ReturnTypeList : List<IReturnType>
	{
		public ReturnTypeList()
		{
		}
		
		public ReturnTypeList(IEnumerable<IReturnType> enumerable) : base(enumerable)
		{
		}
		
		public ReturnTypeList(int capacity) : base(capacity)
		{
		}
	}
}
