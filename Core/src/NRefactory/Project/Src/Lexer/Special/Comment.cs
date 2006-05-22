// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision: 915 $</version>
// </file>

using System;
using System.Text;
using System.CodeDom;
using System.Collections;
using System.Drawing;

namespace ICSharpCode.NRefactory.Parser
{
	public class Comment : AbstractSpecial
	{
		CommentType   commentType;
		string        comment;
		
		public CommentType CommentType {
			get {
				return commentType;
			}
			set {
				commentType = value;
			}
		}
		
		public string CommentText {
			get {
				return comment;
			}
			set {
				comment = value;
			}
		}
		
		public Comment(CommentType commentType, string comment, Point startPosition, Point endPosition)
			: base(startPosition, endPosition)
		{
			this.commentType   = commentType;
			this.comment       = comment;
		}
		
		public override string ToString()
		{
			return String.Format("[{0}: Type = {1}, Text = {2}, Start = {3}, End = {4}]",
			                     GetType().Name, CommentType, CommentText, StartPosition, EndPosition);
		}
		
		public override object AcceptVisitor(ISpecialVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
