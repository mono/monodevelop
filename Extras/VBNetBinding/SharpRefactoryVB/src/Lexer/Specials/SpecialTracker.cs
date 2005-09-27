using System;
using System.Text;
using System.CodeDom;
using System.Collections;
using System.Drawing;

namespace ICSharpCode.SharpRefactory.Parser.VB
{
	public class SpecialTracker
	{
		ArrayList currentSpecials = new ArrayList();
		ArrayList tempComments = new ArrayList();
		
		CommentType currentCommentType;
		StringBuilder sb = new StringBuilder();
		Point startPosition;
		
		public ArrayList CurrentSpecials
		{
			get {
				return currentSpecials;
			}
		}
		
		public void InformToken(int kind)
		{
			currentSpecials.Add(kind);
		}
		
		public ArrayList RetrieveSpecials()
		{
			ArrayList tmp = currentSpecials;
			currentSpecials = new ArrayList();
			return tmp;
		}
		
		public ArrayList RetreiveComments()
		{
			ArrayList tmp = tempComments;
			tempComments = new ArrayList();
			return tmp;
		}
		
		public void AddEndOfLine()
		{
			currentSpecials.Add(new NewLine());
		}
		
		public void AddPreProcessingDirective(string cmd, string arg, Point start, Point end)
		{
			currentSpecials.Add(new PreProcessingDirective(cmd, arg, start, end));
		}
		
		// used for comment tracking
		public void StartComment(CommentType commentType, Point startPosition)
		{
			this.currentCommentType = commentType;
			this.startPosition      = startPosition;
			this.sb.Length          = 0; 
		}
		
		public void AddChar(char c)
		{
			sb.Append(c);
		}
		
		public void AddString(string s)
		{
			sb.Append(s);
		}
		
		public void FinishComment()
		{
			Comment comment = new Comment(currentCommentType, sb.ToString(), startPosition);
			currentSpecials.Add(comment);
			tempComments.Add(comment);
		}
	}
}
