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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class CompletionData : IComparable
	{
		protected CompletionData () {}
		
		public virtual IconId Icon { get; set; }
		public virtual string DisplayText { get; set; }
		public virtual string Description { get; set; }
		public virtual string CompletionText { get; set; }

		/// <summary>
		/// int.MaxValue == highest prioriy,
		/// -int.MaxValue == lowest priority
		/// </summary>
		/// <value>The priority group.</value>
		public virtual int PriorityGroup { get { return 0; } }

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

		public virtual Task<TooltipInformation> CreateTooltipInformation (bool smartWrap, CancellationToken cancelToken)
		{
			var tt = new TooltipInformation ();
			if (!string.IsNullOrEmpty (Description))
				tt.AddCategory (null, Description);
			return Task.FromResult (tt);
		}

		public virtual bool HasOverloads { 
			get {
				return false;
			}
		}
		
		public virtual IReadOnlyList<CompletionData> OverloadedData {
			get {
				throw new InvalidOperationException ();
			}
		}
		
		public virtual void AddOverload (CompletionData data)
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
			int endOffset = Math.Min (window.StartOffset + replaceLength, window.CompletionWidget.TextLength);
			var result = window.CompletionWidget.GetText (window.StartOffset, endOffset);
			return result;
		}

		public virtual void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
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
			if (!(obj is CompletionData))
				return 0;
			return Compare (this, (CompletionData)obj);
		}

		public static int Compare (CompletionData a, CompletionData b)
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
					return StringComparer.Ordinal.Compare (((CompletionData)a).Description, ((CompletionData)b).Description);
				var ca = a as CompletionData;
				var cb = b as CompletionData;
				if (ca != null && cb != null && !ca.Icon.IsNull && !cb.Icon.IsNull) {
					return cb.Icon.Name.CompareTo (ca.Icon.Name);
				}
			}
			return result;
		}

		#endregion

		protected string ApplyDiplayFlagsFormatting (string markup)
		{
			if (!HasOverloads && (DisplayFlags & DisplayFlags.Obsolete) != 0 || HasOverloads && OverloadedData.All (data => (data.DisplayFlags & DisplayFlags.Obsolete) != 0))
				return "<s>" + markup + "</s>";
			if ((DisplayFlags & DisplayFlags.MarkedBold) != 0)
				return "<b>" + markup + "</b>";
			return markup;
		}

		public virtual string GetDisplayTextMarkup ()
		{
			return ApplyDiplayFlagsFormatting (GLib.Markup.EscapeText (DisplayText));
		}
	}
}
