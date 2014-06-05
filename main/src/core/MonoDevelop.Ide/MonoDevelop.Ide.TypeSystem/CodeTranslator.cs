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
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MonoDevelop.Ide.TypeSystem
{
	public abstract class CodeTranslator
	{
		public abstract bool CanTranslate (string fromMimeType, string toMimeType);

		public abstract CodeMapping GetMapping (string fromMimeType, string toMimeType, SourceText textSource);
	}

	public class CodeMapping
	{
		public static readonly CodeMapping Invalid = new InvalidMapping ();

		readonly SourceText text;
		readonly ImmutableArray<Tuple<TextSpan, TextSpan>> projections;

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

		CodeMapping (SourceText text, ImmutableArray<Tuple<TextSpan, TextSpan>> projections)
		{
			this.text = text;
			this.projections = projections;
		}

		public static CodeMapping CreateProjectionMapping (SourceText text, ImmutableArray<Tuple<TextSpan, TextSpan>> projections)
		{
			if (text == null)
				throw new ArgumentNullException ("text");
			return new CodeMapping (text, projections);
		}

		public static CodeMapping CreateIdentityMapping (SourceText text)
		{
			if (text == null)
				throw new ArgumentNullException ("text");
			var span = TextSpan.FromBounds (0, text.Length);
			return new CodeMapping (text, ImmutableArray.Create (Tuple.Create (span, span)));
		}

		public CodeMapping UpdateMapping (Mono.TextEditor.DocumentChangeEventArgs e)
		{
			for (int i = 0; i < projections.Length; i++) {
				var projection = projections [i];
				if (projection.Item1.Contains (e.Offset)) {

					var newProjections = projections.SetItem (i, Tuple.Create (
						TextSpan.FromBounds (projection.Item1.Start, projection.Item1.End + e.ChangeDelta),
						TextSpan.FromBounds (projection.Item2.Start, projection.Item2.End + e.ChangeDelta)
					)); 

					var newText = text.Replace (
						projection.Item2.Start + projection.Item1.Start - e.Offset,
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