using System;
using System.Text;
using System.CodeDom;
using System.Collections;
using System.Drawing;

namespace ICSharpCode.SharpRefactory.Parser
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
		
		public TagComment(string tag, string comment, Point startPosition) : base(CommentType.SingleLine, comment, startPosition)
		{
			this.tag = tag;
		}
	}
}
