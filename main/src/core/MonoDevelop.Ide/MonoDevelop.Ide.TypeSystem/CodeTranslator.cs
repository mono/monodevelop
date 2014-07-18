//
// CodeTranslator.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Collections.Generic;
using Mono.Addins;

namespace MonoDevelop.Ide.TypeSystem
{
	public abstract class CodeTranslator
	{
		static readonly List<CodeTranslator> parsers;

		static CodeTranslator ()
		{
			parsers = new List<CodeTranslator> ();
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/TypeSystem/CodeTranslator", delegate (object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					parsers.Add ((CodeTranslator)args.ExtensionObject);
					break;
				case ExtensionChange.Remove:
					parsers.Remove ((CodeTranslator)args.ExtensionObject);
					break;
				}
			});
		}

		public abstract bool CanTranslate (string fromMimeType, string toMimeType);

		public abstract CodeMapping GetMapping (string fromMimeType, string toMimeType, SourceText textSource);
	}

	public struct OffsetInfo 
	{
		public readonly int OriginalOffset;
		public readonly int TranslatedOffset;
		public readonly int Length;

		public OffsetInfo (int orginalOffset, int translatedOffset, int length)
		{
			OriginalOffset = orginalOffset;
			TranslatedOffset = translatedOffset;
			Length = length;
		}

		public bool OriginalContains (int offset)
		{
			var translatedOffset = offset - OriginalOffset;
			return 0 <= translatedOffset && translatedOffset < Length;
		}

		public bool TranslatedContains (int offset)
		{
			var translatedOffset = offset - OriginalOffset;
			return 0 <= translatedOffset && translatedOffset < Length;
		}

		public int Translate (int offsetInOrigin)
		{
			return offsetInOrigin - OriginalOffset + TranslatedOffset;
		}
	}

	public class CodeMapping
	{
		public static readonly CodeMapping Invalid = new InvalidMapping ();

		readonly SourceText text;
		readonly ImmutableArray<OffsetInfo> projections;

		public virtual bool IsInvalid {
			get {
				return false;
			}
		}

		public SourceText Text {
			get {
				return text;
			}
		}

		CodeMapping ()
		{
		}

		CodeMapping (SourceText text, ImmutableArray<OffsetInfo> projections)
		{
			this.text = text;
			this.projections = projections;
		}

		public static CodeMapping CreateProjectionMapping (SourceText text, ImmutableArray<OffsetInfo> projections)
		{
			if (text == null)
				throw new ArgumentNullException ("text");
			return new CodeMapping (text, projections);
		}

		public static CodeMapping CreateIdentityMapping (SourceText text)
		{
			if (text == null)
				throw new ArgumentNullException ("text");
			return new CodeMapping (text, ImmutableArray.Create (new OffsetInfo (0, 0, text.Length)));
		}

		public CodeMapping UpdateMapping (MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			for (int i = 0; i < projections.Length; i++) {
				var projection = projections [i];
				if (projection.OriginalContains (e.Offset)) {

					var newProjections = projections.SetItem (i, 
						new OffsetInfo (projection.OriginalOffset, projection.TranslatedOffset, projection.Length + e.ChangeDelta)
					); 

					var newText = text.Replace (
						projection.Translate (e.Offset),
						e.RemovalLength,
						e.InsertedText.Text
					); 

					return new CodeMapping (newText, newProjections);
				}
			}
			return CodeMapping.Invalid;
		}

		class InvalidMapping : CodeMapping
		{
			public override bool IsInvalid {
				get {
					return true;
				}
			}
		}
	}
}