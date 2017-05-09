// 
// Reference.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.Ide.FindInFiles
{
	[Flags]
	public enum ReferenceUsageType {
		Unknown    = 0,
		Read       = 1,
		Write      = 2,
		[Obsolete("Please use Declaration")]
		Declariton = 4,
		Declaration = 4,
		Keyword    = 8,
		ReadWrite  = Read | Write
	}

	public class MemberReference : SearchResult
	{
		public override FileProvider FileProvider {
			get {
				return new MonoDevelop.Ide.FindInFiles.FileProvider (FileName);
			}
		}
		readonly string fileName;
		public override string FileName {
			get {
				return fileName;
			}
		}

		public ReferenceUsageType ReferenceUsageType { get; set; }
		public object EntityOrVariable { get; private set;}
		
		public MemberReference (object entity, string fileName, int offset, int length) : base (offset, length)
		{
			if (entity == null)
				throw new System.ArgumentNullException ("entity");
			if (fileName == null)
				throw new ArgumentNullException ("fileName");
			EntityOrVariable = entity;
			this.fileName = fileName;
		}

		public string GetName ()
		{
			if (EntityOrVariable is IEntity) {
				return ((IEntity)EntityOrVariable).Name;
			} 
			if (EntityOrVariable is ITypeParameter) {
				return ((ITypeParameter)EntityOrVariable).Name;
			} 
			if (EntityOrVariable is INamespace) {
				return ((INamespace)EntityOrVariable).Name;
			} 
			return ((IVariable)EntityOrVariable).Name;
		}

		public override Components.HslColor GetBackgroundMarkerColor (EditorTheme style)
		{
			var key = (ReferenceUsageType & ReferenceUsageType.Write) != 0 ||
				(ReferenceUsageType & ReferenceUsageType.Declaration) != 0 ?
				EditorThemeColors.ChangingUsagesRectangle : EditorThemeColors.UsagesRectangle;

			return SyntaxHighlightingService.GetColor (style, key);
		}
	}
}

