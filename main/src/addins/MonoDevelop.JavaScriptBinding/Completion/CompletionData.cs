//
// CompletionData.cs
//
// Author:
//       Harsimran Bath <harsimranbath@gmail.com>
//
// Copyright (c) 2014 Harsimran
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
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using ICSharpCode.NRefactory.Completion;
using System.Collections.Generic;
using MonoDevelop.Ide.CodeCompletion;
using System.Text;

namespace MonoDevelop.JavaScript
{
	class CompletionData : MonoDevelop.Ide.CodeCompletion.CompletionData
	{
		#region Constructors

		protected CompletionData ()
		{
		}

		public CompletionData (string name, string description)
		{
			Icon = Stock.Literal;
			DisplayText = name;
			CompletionText = DisplayText;
			this.Description = description; // TODO
		}

		#endregion

		#region Properties

		public override IconId Icon { get; set; }

		public override string DisplayText { get; set; }

		public override string Description { get; set; }

		public override string CompletionText { get; set; }

		#endregion

		#region Methods

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			// TODO: We have issue where the word gets appended to an existing word.  I think it should replace the word
			var currentWord = GetCurrentWord (window);
			window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, currentWord, CompletionText);
		}

		#endregion
	}

	class VariableCompletion : CompletionData
	{
		public VariableCompletion (JSVariableDeclaration statement)
		{
			Icon = Stock.Field;
			DisplayText = statement.Name;
			CompletionText = DisplayText;
			Description = string.Empty; // TODO
		}

		public override TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			var tooltip = new TooltipInformation ();

			var blueColor = new Gdk.Color (0, 0, 255);
			var colorString = Mono.TextEditor.HelperMethods.GetColorString (blueColor);

			var content = string.Format ("<span foreground=\"{0}\">var</span> {1}", colorString, DisplayText);

			tooltip.SignatureMarkup = content;
			return tooltip;
		}
	}

	class FunctionCompletion : CompletionData
	{
		private IList<FunctionCompletion> overloads;

		#region Properties

		public override IEnumerable<ICompletionData> OverloadedData {
			get {
				return overloads;
			}
		}

		public override bool HasOverloads {
			get {
				return overloads != null && overloads.Count > 0;
			}
		}

		public string[] Parameters { get; private set; }

		#endregion

		#region Constructor

		public FunctionCompletion (JSFunctionStatement statement)
		{
			Icon = Stock.Method;
			DisplayText = statement.Name;
			CompletionText = DisplayText;
			Description = string.Empty; // TODO
			Parameters = statement.Parameters;
		}

		#endregion

		#region Methods

		public override TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			var tooltip = new TooltipInformation ();

			var blueColor = new Gdk.Color (0, 0, 255);
			var colorString = Mono.TextEditor.HelperMethods.GetColorString (blueColor);

			var builder = new StringBuilder (string.Format ("<span foreground=\"{0}\">function</span> ", colorString));
			builder.Append (string.Concat (DisplayText, " "));
			builder.Append ("(");
			if (Parameters.Length > 0) {
				for (int i = 0; i < Parameters.Length; i++) {
					var currentArgument = Parameters [i];
					if (i + 1 != Parameters.Length) {
						builder.Append (string.Concat (currentArgument, ", "));
					} else {
						builder.Append (string.Concat (currentArgument));
					}
				}
			}
			builder.Append (")");

			tooltip.SignatureMarkup = builder.ToString ();
			return tooltip;
		}

		public override void AddOverload (ICompletionData data)
		{
			var jsData = data as FunctionCompletion;
			if (jsData == null)
				throw new ArgumentException ("Only MonoDevelop.JavaScript.FunctionCompletion Type is supported!", "data");

			if (overloads == null)
				overloads = new List<FunctionCompletion> ();

			if (overloads.FirstOrDefault (i => ((FunctionCompletion)i).Parameters.Length == jsData.Parameters.Length) == null)
				overloads.Add (jsData);
		}

		#endregion
	}
}

