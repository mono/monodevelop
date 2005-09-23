// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Xml;

namespace MonoDevelop.Core.AddIns.Conditions
{
	public class ConditionNotFoundException : System.Exception
	{
		public ConditionNotFoundException(string message) : base(message)
		{
		}
	}
}
