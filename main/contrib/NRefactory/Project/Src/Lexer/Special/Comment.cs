// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision: 4482 $</version>
// </file>

using System;

namespace ICSharpCode.NRefactory
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
		
		/// <value>
		/// Is true, when the comment is at line start or only whitespaces
		/// between line and comment start.
		/// </value>
		public bool CommentStartsLine {
			get;
			set;
		}
		
		public Comment(CommentType commentType, string comment, bool commentStartsLine, Location startPosition, Location endPosition)
			: base(startPosition, endPosition)
		{
			this.commentType   = commentType;
			this.comment       = comment;
			this.CommentStartsLine = commentStartsLine;
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
