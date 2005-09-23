// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Xml;

namespace MonoDevelop.Core.AddIns.Codons
{
	public class CodonNotFoundException : System.Exception
	{
		public CodonNotFoundException(string message) : base(message)
		{
		}
	}
}
