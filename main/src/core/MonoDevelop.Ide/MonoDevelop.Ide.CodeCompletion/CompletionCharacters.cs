//
// CompletionCharacters.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class CompletionCharacters
	{
		public string Language { get; set; }
		public bool CompleteOnSpace { get; set; }
		public string CompleteOnChars { get; set; }

		public CompletionCharacters ()
		{
		}

		public CompletionCharacters (string language, bool completeOnSpace, string completeOnChars)
		{
			this.Language = language;
			this.CompleteOnSpace = completeOnSpace;
			this.CompleteOnChars = completeOnChars;
		}

		public bool CompleteOn (char keyChar)
		{
			return keyChar == ' ' && CompleteOnSpace || CompleteOnChars.IndexOf (keyChar) >= 0;
		}


		static CompletionCharacters ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/CompletionCharacters", OnCompletionCharsAdded);

		}

		static void OnCompletionCharsAdded (object sender, ExtensionNodeEventArgs args)
		{
			var codon = (CompletionCharacterCodon)args.ExtensionNode;
			var c = codon.CreateCompletionChar ();

			c.CompleteOnSpace = PropertyService.Get ("CompletionCharacters." + c.Language + ".CompleteOnSpace", c.CompleteOnSpace);
			c.CompleteOnChars = PropertyService.Get ("CompletionCharacters." + c.Language + ".CompleteOnChars", c.CompleteOnChars);

			completionChars.Add (c); 
		}

		internal static readonly CompletionCharacters FallbackCompletionCharacters = new CompletionCharacters ("Other", true, "{}[]().,:;+-*/%&|^!~=<>?@#'\"\\");
		public static CompletionCharacters Get (string completionLanguage)
		{
			return GetCompletionCharacters ().FirstOrDefault (c => c.Language == completionLanguage) ?? FallbackCompletionCharacters;
		}

		static List<CompletionCharacters> completionChars = new List<CompletionCharacters>();

		public static IEnumerable<CompletionCharacters> GetCompletionCharacters ()
		{
			return completionChars;
		}

		public static IEnumerable<CompletionCharacters> GetDefaultCompletionCharacters ()
		{
			foreach (var node in AddinManager.GetExtensionNodes ("/MonoDevelop/Ide/CompletionCharacters")) {
				var codon = (CompletionCharacterCodon)node;
				yield return codon.CreateCompletionChar ();
			}
		}

		public static void SetCompletionCharacters (IEnumerable<CompletionCharacters> chars)
		{
			completionChars.Clear ();
			foreach (var c in chars) {
				PropertyService.Set ("CompletionCharacters." + c.Language + ".CompleteOnSpace", c.CompleteOnSpace);
				PropertyService.Set ("CompletionCharacters." + c.Language + ".CompleteOnChars", c.CompleteOnChars);

				completionChars.Add (c);
			}
		}

		public override string ToString ()
		{
			return string.Format ("[CompletionCharacters: Language={0}, CompleteOnSpace={1}, CompleteOnChars={2}]", Language, CompleteOnSpace, CompleteOnChars);
		}
	}
}

