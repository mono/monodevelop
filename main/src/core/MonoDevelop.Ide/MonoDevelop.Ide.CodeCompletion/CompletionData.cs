// 
// CompletionData.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.Completion;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class CompletionData : ICompletionData, IComparable
	{
		protected CompletionData () {}
		
		public virtual IconId Icon { get; set; }
		public virtual string DisplayText { get; set; }
		public virtual string Description { get; set; }
		public virtual string CompletionText { get; set; }

		public virtual string GetDisplayDescription (bool isSelected)
		{
			return null;
		}

		public virtual string GetRightSideDescription (bool isSelected)
		{
			return "";
		}

		public virtual CompletionCategory CompletionCategory { get; set; }
		public virtual DisplayFlags DisplayFlags { get; set; }

		public virtual TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			var tt = new TooltipInformation ();
			if (!string.IsNullOrEmpty (Description))
				tt.AddCategory (null, Description);
			return tt;
		}

		public virtual bool HasOverloads { 
			get {
				return false;
			}
		}
		
		public virtual IEnumerable<ICompletionData> OverloadedData {
			get {
				throw new InvalidOperationException ();
			}
		}
		
		public virtual void AddOverload (ICompletionData data)
		{
			throw new InvalidOperationException ();
		}
		
		public CompletionData (string text) : this (text, null, null) {}
		public CompletionData (string text, IconId icon) : this (text, icon, null) {}
		public CompletionData (string text, IconId icon, string description) : this (text, icon, description, text) {}
		
		public CompletionData (string displayText, IconId icon, string description, string completionText)
		{
			this.DisplayText = displayText;
			this.Icon = icon;
			this.Description = description;
			this.CompletionText = completionText;
		}
		
		public static string GetCurrentWord (CompletionListWindow window)
		{
			int partialWordLength = window.PartialWord != null ? window.PartialWord.Length : 0;
			int replaceLength = window.CodeCompletionContext.TriggerWordLength + partialWordLength - window.InitialWordLength;
			int endOffset = Math.Min (window.CodeCompletionContext.TriggerOffset + replaceLength, window.CompletionWidget.TextLength);
			var result = window.CompletionWidget.GetText (window.CodeCompletionContext.TriggerOffset, endOffset);
			return result;
		}

		public virtual void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			var currentWord = GetCurrentWord (window);
			window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, currentWord, CompletionText);
		}
		
		public override string ToString ()
		{
			return string.Format ("[CompletionData: Icon={0}, DisplayText={1}, Description={2}, CompletionText={3}, DisplayFlags={4}]", Icon, DisplayText, Description, CompletionText, DisplayFlags);
		}

		#region IComparable implementation

		public virtual int CompareTo (object obj)
		{
			if (!(obj is ICompletionData))
				return 0;
			return Compare (this, (ICompletionData)obj);
		}

		public static int Compare (ICompletionData a, ICompletionData b)
		{
			var result =  ((a.DisplayFlags & DisplayFlags.Obsolete) == (b.DisplayFlags & DisplayFlags.Obsolete)) ? StringComparer.OrdinalIgnoreCase.Compare (a.DisplayText, b.DisplayText) : (a.DisplayFlags & DisplayFlags.Obsolete) != 0 ? 1 : -1;
			if (result == 0) {
				var aIsImport = (a.DisplayFlags & DisplayFlags.IsImportCompletion) != 0;
				var bIsImport = (b.DisplayFlags & DisplayFlags.IsImportCompletion) != 0;
				if (!aIsImport && bIsImport)
					return -1;
				if (aIsImport && !bIsImport)
					return 1;
				if (aIsImport && bIsImport)
					return StringComparer.Ordinal.Compare (a.Description, b.Description);
				var ca = a as CompletionData;
				var cb = b as CompletionData;
				if (ca != null && cb != null && !ca.Icon.IsNull && !cb.Icon.IsNull) {
					return cb.Icon.Name.CompareTo (ca.Icon.Name);
				}
			}
			return result;
		}

		#endregion
	}
}
