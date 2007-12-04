//  FoldMarker.cs
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
	public enum FoldType {
		Unspecified,
		MemberBody,
		Region,
		TypeBody
	}
	
	public class FoldMarker
	{
		int startLine;
		int startColumn;
		
		int endLine;
		int endColumn;
		
		bool isFolded = false;
		string   foldText = "...";
		FoldType foldType = FoldType.Unspecified;
		
		
		public override string ToString()
		{
			return String.Format("[FoldMarker: StartLine={0}, StartColumn={1}, EndLine={2}, EndColumn={3}, IsFolded={4}, FoldText=\"{5}\", FoldType={6}]", 
			                     startLine, startColumn, endLine, endColumn, isFolded, foldText, foldType);
		}
		
		public FoldType FoldType {
			get {
				return foldType;
			}
			set {
				foldType = value;
			}
		}
		
		
		public int StartLine {
			get {
				return startLine;
			}
			set {
				startLine = value;
			}
		}
		
		public int StartColumn {
			get {
				return startColumn;
			}
			set {
				startColumn = value;
			}
		}
		
		public int EndLine {
			get {
				return endLine;
			}
			set {
				endLine = value;
			}
		}
		public int EndColumn {
			get {
				return endColumn;
			}
			set {
				endColumn = value;
			}
		}
		
		public bool IsFolded {
			get {
				return isFolded;
			}
			set {
				isFolded = value;
			}
		}
		
		public string FoldText {
			get {
				return foldText;
			}
			set {
				foldText = value;
			}
		}
		
		public FoldMarker(int startLine, int startColumn, int endLine, int endColumn)
		{
			this.startLine = startLine;
			this.startColumn = startColumn;
			this.endLine = endLine;
			this.endColumn = endColumn;
		}
		
		public FoldMarker(int startLine, int startColumn, int endLine, int endColumn, FoldType foldType)
		{
			this.startLine = startLine;
			this.startColumn = startColumn;
			this.endLine = endLine;
			this.endColumn = endColumn;
			this.foldType = foldType;
		}
	}
}
