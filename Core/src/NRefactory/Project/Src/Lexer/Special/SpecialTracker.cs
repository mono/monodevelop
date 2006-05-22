// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision: 915 $</version>
// </file>

using System;
using System.Text;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;

namespace ICSharpCode.NRefactory.Parser
{
	public class SpecialTracker
	{
		List<ISpecial> currentSpecials = new List<ISpecial>();
		
		CommentType   currentCommentType;
		StringBuilder sb = new StringBuilder();
		Point         startPosition;
		
		public List<ISpecial> CurrentSpecials {
			get {
				return currentSpecials;
			}
		}
		
		public void InformToken(int kind)
		{
			
		}
		
		/// <summary>
		/// Gets the specials from the SpecialTracker and resets the lists.
		/// </summary>
		public List<ISpecial> RetrieveSpecials()
		{
			List<ISpecial> tmp = currentSpecials;
			currentSpecials = new List<ISpecial>();
			return tmp;
		}
		
		public void AddEndOfLine(Point point)
		{
			currentSpecials.Add(new BlankLine(point));
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
		
		public void FinishComment(Point endPosition)
		{
			currentSpecials.Add(new Comment(currentCommentType, sb.ToString(), startPosition, endPosition));
		}
	}
}
