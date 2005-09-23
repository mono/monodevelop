// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;

namespace MonoDevelop.Internal.Parser
{
	[Serializable]
	public class Tag : Comment
	{
		string key;
		
		public string Key {
			get {
				return key;
			}
		}
		
		public Tag(string key, IRegion region) : base(region)
		{
			this.key = key;
		}
	}
}
