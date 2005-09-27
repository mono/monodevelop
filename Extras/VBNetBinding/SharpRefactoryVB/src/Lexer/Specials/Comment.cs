using System;
using System.Text;
using System.CodeDom;
using System.Collections;
using System.Drawing;

namespace ICSharpCode.SharpRefactory.Parser.VB
{
	public class Comment
	{
		CommentType   commentType;
		string        comment;
		Point         startPosition;
		
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
		
		public Point StartPosition {
			get {
				return startPosition;
			}
			set {
				startPosition = value;
			}
		}
		
		public Comment(CommentType commentType, string comment, Point startPosition)
		{
			this.commentType   = commentType;
			this.comment       = comment;
			this.startPosition = startPosition;
		}
	}
}
