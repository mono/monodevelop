// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Core.Properties
{
	/// <summary>
	/// Is thrown when an unknown XmlNode in a property file is encountered.
	/// </summary>
	public class UnknownPropertyNodeException : Exception
	{
		public UnknownPropertyNodeException(string nodeName) : base("unknown XmlNode : " + nodeName)
		{
		}
	}
}
