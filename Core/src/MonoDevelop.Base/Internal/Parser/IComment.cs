// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System.Collections.Specialized;

namespace MonoDevelop.Internal.Parser
{
	public interface IComment
	{
		bool IsBlockComment {
			get;
		}
		
		string CommentTag {
			get;
		}
		
		string CommentText {
			get;
		}
		
		IRegion Region {
			get;
		}
	}
}
