// 
// IPrettyPrinter.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Text
{
	public interface IPrettyPrinter
	{
		bool CanFormat (string mimeType);
		
		bool SupportsOnTheFlyFormatting {
			get;
		}
		
		/// <summary>
		/// Formats a text document directly with insert/remove operations.
		/// </summary>
		/// <param name="textEditorData">
		/// A <see cref="System.Object"/> that must be from type Mono.TextEditorData.
		/// </param>
		/// <param name="dom">
		/// A <see cref="ProjectDom"/>
		/// </param>
		/// <param name="unit">
		/// A <see cref="ICompilationUnit"/>
		/// </param>
		/// <param name="caretLocation">
		/// A <see cref="DomLocation"/> that should be the end location to which the parsing should occur.
		/// </param>
		void OnTheFlyFormat (object textEditorData, IType callingType, IMember callingMember, ProjectDom dom, ICompilationUnit unit, DomLocation endLocation);
		
		string FormatText (SolutionItem policyParent, string mimeType, string input);
		string FormatText (SolutionItem policyParent, string mimeType, string input, int fromOffest, int toOffset);
	}
	
	public abstract class AbstractPrettyPrinter : IPrettyPrinter
	{
		public abstract bool CanFormat (string mimeType);
		
		public virtual bool SupportsOnTheFlyFormatting {
			get {
				return false;
			}
		}
		
		public virtual void OnTheFlyFormat (object textEditorData, IType callingType, IMember callingMember, ProjectDom dom, ICompilationUnit unit, DomLocation endLocation)
		{
			throw new NotSupportedException ();
		}
		
		protected abstract string InternalFormat (SolutionItem policyParent, string mimeType, string text, int fromOffest, int toOffset);
		
		public string FormatText (SolutionItem policyParent, string mimeType, string input)
		{
			if (string.IsNullOrEmpty (input))
				return input;
			return FormatText (policyParent, mimeType, input, 0, input.Length - 1);
		}
		
		public string FormatText (SolutionItem policyParent, string mimeType, string input, int fromOffest, int toOffset)
		{
			if (string.IsNullOrEmpty (input))
				return input;
			return InternalFormat (policyParent, mimeType, input, fromOffest, toOffset);
		}
	}
}
