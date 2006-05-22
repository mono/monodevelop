// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 1252 $</version>
// </file>

using System;
using System.Drawing;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.PrettyPrinter
{
	public class SpecialOutputVisitor : ISpecialVisitor
	{
		IOutputFormatter formatter;
		
		public SpecialOutputVisitor(IOutputFormatter formatter)
		{
			this.formatter = formatter;
		}
		
		public bool ForceWriteInPreviousLine;
		
		public object Visit(ISpecial special, object data)
		{
			Console.WriteLine("Warning: SpecialOutputVisitor.Visit(ISpecial) called with " + special);
			return data;
		}
		
		public object Visit(BlankLine special, object data)
		{
			formatter.NewLine();
			return data;
		}
		
		public object Visit(Comment special, object data)
		{
			formatter.PrintComment(special, ForceWriteInPreviousLine);
			return data;
		}
		
		public object Visit(PreProcessingDirective special, object data)
		{
			formatter.PrintPreProcessingDirective(special, ForceWriteInPreviousLine);
			return data;
		}
	}
	
	/// <summary>
	/// This class inserts specials between INodes.
	/// </summary>
	public class SpecialNodesInserter : IDisposable
	{
		IEnumerator<ISpecial> enumerator;
		SpecialOutputVisitor visitor;
		bool available; // true when more specials are available
		
		public SpecialNodesInserter(IEnumerable<ISpecial> specials, SpecialOutputVisitor visitor)
		{
			if (specials == null) throw new ArgumentNullException("specials");
			if (visitor == null) throw new ArgumentNullException("visitor");
			enumerator = specials.GetEnumerator();
			this.visitor = visitor;
			available = enumerator.MoveNext();
		}
		
		void WriteCurrent()
		{
			enumerator.Current.AcceptVisitor(visitor, null);
			available = enumerator.MoveNext();
		}
		
		/// <summary>
		/// Writes all specials up to the start position of the node.
		/// </summary>
		public void AcceptNodeStart(INode node)
		{
			AcceptPoint(node.StartLocation);
		}
		
		/// <summary>
		/// Writes all specials up to the end position of the node.
		/// </summary>
		public void AcceptNodeEnd(INode node)
		{
			visitor.ForceWriteInPreviousLine = true;
			AcceptPoint(node.EndLocation);
			visitor.ForceWriteInPreviousLine = false;
		}
		
		/// <summary>
		/// Writes all specials up to the specified location.
		/// </summary>
		public void AcceptPoint(Point a)
		{
			while (available) {
				Point b = enumerator.Current.StartPosition;
				if (b.Y < a.Y || (b.Y == a.Y && b.X <= a.X)) {
					WriteCurrent();
				} else {
					break;
				}
			}
		}
		
		/// <summary>
		/// Outputs all missing specials to the writer.
		/// </summary>
		public void Finish()
		{
			while (available) {
				WriteCurrent();
			}
		}
		
		void IDisposable.Dispose()
		{
			Finish();
		}
		
		/// <summary>
		/// Registers a new SpecialNodesInserter with the output visitor.
		/// Make sure to call Finish() (or Dispose()) on the returned SpecialNodesInserter
		/// when the output is finished.
		/// </summary>
		public static SpecialNodesInserter Install(IEnumerable<ISpecial> specials, IOutputASTVisitor outputVisitor)
		{
			SpecialNodesInserter sni = new SpecialNodesInserter(specials, new SpecialOutputVisitor(outputVisitor.OutputFormatter));
			outputVisitor.NodeTracker.NodeVisiting += sni.AcceptNodeStart;
			outputVisitor.NodeTracker.NodeVisited  += sni.AcceptNodeEnd;
			return sni;
		}
	}
}
