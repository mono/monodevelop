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
	public class Comment : System.MarshalByRefObject
	{
		string commentString;
		IRegion region;
		
		public string CommentString {
			get {
				return commentString;
			}
			set {
				commentString = value;
			}
		}
		
		public IRegion Region {
			get {
				return region;
			}
			set {
				region = value;
			}
		}
		
		public Comment(IRegion region)
		{
			this.region = region;
		}
		public Comment (IRegion region, string comment) : this(region)
		{
			commentString = comment;
		}
	}
}
