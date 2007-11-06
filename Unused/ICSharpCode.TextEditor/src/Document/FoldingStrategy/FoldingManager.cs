//  FoldingManager.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Drawing;
using System.Collections;

namespace MonoDevelop.TextEditor.Document
{
	public class FoldingManager
	{
		ArrayList           foldMarker      = new ArrayList();
		IFoldingStrategy    foldingStrategy = null;
		IDocument document;
		public ArrayList FoldMarker {
			get {
				return foldMarker;
			}
		}
		
		public IFoldingStrategy FoldingStrategy {
			get {
				return foldingStrategy;
			}
			set {
				foldingStrategy = value;
			}
		}
		
		public FoldingManager(IDocument document)
		{
			//Console.WriteLine ("new FoldingManager");
			this.document = document;
//			foldMarker.Add(new FoldMarker(0, 5, 3, 5));
//			
//			foldMarker.Add(new FoldMarker(5, 5, 10, 3));
//			foldMarker.Add(new FoldMarker(6, 0, 8, 2));
//			
//			FoldMarker fm1 = new FoldMarker(10, 4, 10, 7);
//			FoldMarker fm2 = new FoldMarker(10, 10, 10, 14);
//			
//			fm1.IsFolded = true;
//			fm2.IsFolded = true;
//			
//			foldMarker.Add(fm1);
//			foldMarker.Add(fm2);
		}
		
		public ArrayList GetFoldingsWithStart(int lineNumber)
		{
			ArrayList foldings = new ArrayList();
			
			int c = foldMarker.Count;
			for (int i = 0; i < c; i ++) {
				FoldMarker fm = (FoldMarker)(foldMarker [i]);
				if (fm.StartLine == lineNumber) {
					foldings.Add(fm);
				}
			}
			return foldings;
		}
		
		public ArrayList GetFoldingsWithEnd(int lineNumber)
		{
			ArrayList foldings = new ArrayList();
			
			int c = foldMarker.Count;
			for (int i = 0; i < c; i ++) {
				FoldMarker fm = (FoldMarker)(foldMarker [i]);
				if (fm.EndLine == lineNumber) {
					foldings.Add(fm);
				}
			}
			return foldings;
		}

		public bool IsFoldStart(int lineNumber)
		{
			int c = foldMarker.Count;
			for (int i = 0; i < c; i ++) {
				FoldMarker fm = (FoldMarker)(foldMarker [i]);
				if (fm.StartLine == lineNumber) {
					return true;
				}
			}
			return false;
		}
		
		public bool IsFoldEnd(int lineNumber)
		{
			int c = foldMarker.Count;
			for (int i = 0; i < c; i ++) {
				FoldMarker fm = (FoldMarker)(foldMarker [i]);
				if (fm.EndLine == lineNumber) {
					return true;
				}
			}
			return false;
		}
		
		public bool IsBetweenFolding(int lineNumber)
		{
			int c = foldMarker.Count;
			for (int i = 0; i < c; i ++) {
				FoldMarker fm = (FoldMarker)(foldMarker [i]);
				if (fm.StartLine < lineNumber && lineNumber < fm.EndLine) {
					return true;
				}
			}
			return false;
		}
		
		public bool IsLineVisible(int lineNumber)
		{
			int c = foldMarker.Count;
			for (int i = 0; i < c; i ++) {
				FoldMarker fm = (FoldMarker)(foldMarker [i]);
				if (fm.IsFolded && fm.StartLine < lineNumber && lineNumber <= fm.EndLine) {
					return false;
				}
			}
			return true;
		}
		
		ArrayList GetTopLevelFoldedFoldings()
		{
			ArrayList foldings = new ArrayList();
			int c = foldMarker.Count;
			for (int i = 0; i < c; i ++) {
				FoldMarker fm = (FoldMarker)(foldMarker [i]);
				if (fm.IsFolded) {
					foldings.Add(fm);
				}
			}
			return foldings;
		}
		
		public void UpdateFoldings(string fileName, object parseInfo)
		{
			//Console.WriteLine (foldingStrategy);
			ArrayList newFoldings = foldingStrategy.GenerateFoldMarkers(document, fileName, parseInfo);
			if (newFoldings != null) {
//				foreach (object o in newFoldings)  {
//					Console.WriteLine(o);
//				}
				// TODO : merge!!!
				this.foldMarker = newFoldings;
			}
		}
	}
}
