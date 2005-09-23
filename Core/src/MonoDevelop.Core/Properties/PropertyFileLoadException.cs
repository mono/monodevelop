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
	/// Is thrown when no property file could be loaded.
	/// </summary>
	public class PropertyFileLoadException : Exception
	{
		public PropertyFileLoadException() : base("couldn't load global property file")
		{
		}
	}
}
