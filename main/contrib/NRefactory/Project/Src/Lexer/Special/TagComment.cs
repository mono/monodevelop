// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision: 4482 $</version>
// </file>

using System;

namespace ICSharpCode.NRefactory.Parser
{
	/// <summary>
	/// Description of TagComment.	
	/// </summary>
	public class TagComment : Comment
	{
		string tag;
		
		public string Tag {
			get {
				return tag;
			}
			set {
				tag = value;
			}
		}
		
		public TagComment(string tag, string comment, bool commentStartsLine, Location startPosition, Location endPosition) : base(CommentType.SingleLine, comment, commentStartsLine, startPosition, endPosition)
		{
			this.tag = tag;
		}
	}
}
