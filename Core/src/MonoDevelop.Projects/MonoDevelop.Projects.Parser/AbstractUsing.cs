// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public class DefaultUsing : IUsing
	{
		protected IRegion region;
		
		List<string> usings  = new List<string>();
		SortedList<string, IReturnType> aliases;
		
		public IRegion Region {
			get {
				return region;
			}
		}
		
		public List<string> Usings {
			get {
				if (usings == null)
					usings = new List<string> ();
				return usings;
			}
		}
		
		public SortedList<string, IReturnType> Aliases {
			get {
				if (aliases == null)
					aliases = new SortedList<string, IReturnType> ();
				return aliases;
			}
		}
		
		IEnumerable<string> IUsing.Usings {
			get {
				return usings;
			}
		}

		IEnumerable<string> IUsing.Aliases {
			get {
				if (aliases == null)
					return new string[0];
				return aliases.Keys;
			}
		}
		
		public IReturnType GetAlias (string name)
		{
			IReturnType rt;
			if (aliases != null && aliases.TryGetValue (name, out rt))
				return rt;
			else
				return null;
		}
		
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder("[AbstractUsing: using list=");
			foreach (string str in usings) {
				builder.Append(str);
				builder.Append(", ");
			}
			builder.Append("]");
			return builder.ToString();
		}
	}
}
