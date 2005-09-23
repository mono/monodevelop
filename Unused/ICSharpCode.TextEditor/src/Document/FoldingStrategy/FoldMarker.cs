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
