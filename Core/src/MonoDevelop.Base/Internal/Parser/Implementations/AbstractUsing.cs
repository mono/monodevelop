// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using MonoDevelop.Services;

namespace MonoDevelop.Internal.Parser
{
	[Serializable]
	public abstract class AbstractUsing : MarshalByRefObject, IUsing
	{
		protected IRegion region;
		
		protected StringCollection usings  = new StringCollection();
		protected SortedList       aliases = new SortedList();
		
		public IRegion Region {
			get {
				return region;
			}
		}
		
		public StringCollection Usings {
			get {
				return usings;
			}
		}
		
		public SortedList Aliases {
			get {
				return aliases;
			}
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
