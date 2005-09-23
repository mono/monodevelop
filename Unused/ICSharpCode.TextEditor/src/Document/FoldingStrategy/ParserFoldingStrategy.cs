// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Collections;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// A simple folding strategy which calculates the folding level
	/// using the indent level of the line.
	/// </summary>
	public class ParserFoldingStrategy : IFoldingStrategy
	{
		/// <remarks>
		/// Calculates the fold level of a specific line.
		/// </remarks>
		public ArrayList GenerateFoldMarkers(IDocument document, string fileName, object parseInformation)
		{
//			if (parseInformation == null || parseInformation.MostRecentCompilationUnit == null) {
				return null;
//			}
//			
//			ArrayList foldMarkers = new ArrayList();
//			ICompilationUnit cu = (ICompilationUnit)parseInformation.MostRecentCompilationUnit;
//			foreach (IClass c in cu.Classes) {
//				foldMarkers.Add(new FoldMarker(c.Region.BeginLine - 1, c.Region.BeginColumn - 1,
//				                               c.Region.EndLine - 1, c.Region.EndColumn, FoldType.TypeBody));
//				foreach (IMethod m in c.Methods) {
//					foldMarkers.Add(new FoldMarker(m.Region.EndLine - 1, m.Region.EndColumn,
//					                               m.BodyRegion.EndLine - 1, m.BodyRegion.EndColumn - 1, FoldType.MemberBody));
////					Console.WriteLine("Add folding from {0} to {1}", m.Region, m.BodyRegion);
//				}
//				
//				foreach (IIndexer indexer in c.Indexer) {
//					foldMarkers.Add(new FoldMarker(indexer.Region.EndLine - 1,    indexer.Region.EndColumn - 1,
//					                               indexer.BodyRegion.EndLine- 1, indexer.BodyRegion.EndColumn - 1, FoldType.MemberBody));
//				}
//				
//				foreach (IProperty p in c.Properties) {
//					foldMarkers.Add(new FoldMarker(p.Region.EndLine - 1, p.Region.EndColumn - 1,
//					                               p.BodyRegion.EndLine- 1, p.BodyRegion.EndColumn - 1, FoldType.MemberBody));
//				}
//				
//				foreach (IEvent evt in c.Events) {
//					if (evt.BodyRegion != null) {
//						foldMarkers.Add(new FoldMarker(evt.Region.EndLine - 1, evt.Region.EndColumn - 1,
//						                               evt.BodyRegion.EndLine- 1, evt.BodyRegion.EndColumn - 1, FoldType.MemberBody));
//					}
//				}
//			}
//			
//			if (cu.DokuComments != null) {
//				foreach (Comment c in cu.DokuComments) {
//					foldMarkers.Add(new FoldMarker(c.Region.BeginLine- 1, c.Region.BeginColumn,
//					                               c.Region.EndLine- 1, c.Region.EndColumn));
//				}
//			}
//			
//			return foldMarkers;
		}
	}
}
