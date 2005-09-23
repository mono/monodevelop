// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Specialized;

using MonoDevelop.Core.Properties;

namespace MonoDevelop.Core.Services
{
	public interface IStringTagProvider 
	{
		string[] Tags {
			get;
		}
		
		string Convert(string tag);
	}
}
