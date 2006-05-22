// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 915 $</version>
// </file>

using System;
using System.Drawing;

namespace ICSharpCode.NRefactory.Parser
{
	/// <summary>
	/// Interface for all specials.
	/// </summary>
	public interface ISpecial
	{
		Point StartPosition { get; }
		Point EndPosition { get; }
		
		object AcceptVisitor(ISpecialVisitor visitor, object data);
	}
	
	public interface ISpecialVisitor
	{
		object Visit(ISpecial special, object data);
		object Visit(BlankLine special, object data);
		object Visit(Comment special, object data);
		object Visit(PreProcessingDirective special, object data);
	}
	
	public abstract class AbstractSpecial : ISpecial
	{
		public abstract object AcceptVisitor(ISpecialVisitor visitor, object data);
		
		Point startPosition, endPosition;
		
		public AbstractSpecial(Point position)
		{
			this.startPosition = position;
			this.endPosition = position;
		}
		
		public AbstractSpecial(Point startPosition, Point endPosition)
		{
			this.startPosition = startPosition;
			this.endPosition = endPosition;
		}
		
		public Point StartPosition {
			get {
				return startPosition;
			}
			set {
				startPosition = value;
			}
		}
		
		public Point EndPosition {
			get {
				return endPosition;
			}
			set {
				endPosition = value;
			}
		}
		
		public override string ToString()
		{
			return String.Format("[{0}: Start = {1}, End = {2}]",
			                     GetType().Name, StartPosition, EndPosition);
		}
	}
}
