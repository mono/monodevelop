// 
// CompletionListWindowTests.cs
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
using System.Text;
using NUnit.Framework;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.Gui
{
	[TestFixture()]
	public class CompletionListWindowTests : UnitTests.TestBase
	{
		class TestCompletionWidget : ICompletionWidget 
		{
			public string CompletedWord {
				get;
				set;
			}
			#region ICompletionWidget implementation
			public event EventHandler CompletionContextChanged {
				add { /* TODO */ }
				remove { /* TODO */ }
			}
			
			public string GetText (int startOffset, int endOffset)
			{
				return sb.ToString ().Substring (startOffset, endOffset - startOffset);
			}
			
			public char GetChar (int offset)
			{
				return sb.ToString () [offset];
			}
			
			public CodeCompletionContext CreateCodeCompletionContext (int triggerOffset)
			{
				return null;
			}
			public CodeCompletionContext CurrentCodeCompletionContext {
				get {
					return null;
				}
			}
			public string GetCompletionText (CodeCompletionContext ctx)
			{
				return "";
			}
			
			public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
			{
				this.CompletedWord = complete_word;
			}
			
			public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int offset)
			{
				this.CompletedWord = complete_word;
			}
			
			public void Replace (int offset, int count, string text)
			{
			}
			
			public int CaretOffset {
				get {
					return sb.Length;
				}
			}
			
			public int TextLength {
				get {
					return sb.Length;
				}
			}
			
			public int SelectedLength {
				get {
					return 0;
				}
			}
			
			public Gtk.Style GtkStyle {
				get {
					return null;
				}
			}
			#endregion
			public void AddChar (char ch)
			{
				sb.Append (ch);
			}
			public void Backspace ()
			{
				sb.Length--;
			}
			StringBuilder sb = new StringBuilder ();
		}
		
		static void SimulateInput (CompletionListWindow listWindow, string input)
		{
			foreach (char ch in input) {
				KeyActions ka;
				switch (ch) {
				case '8':
					listWindow.PreProcessKeyEvent (Gdk.Key.Up, '\0', Gdk.ModifierType.None);
					listWindow.PostProcessKeyEvent (Gdk.Key.Up, '\0', Gdk.ModifierType.None);
					break;
				case '2':
					listWindow.PreProcessKeyEvent (Gdk.Key.Down, '\0', Gdk.ModifierType.None);
					listWindow.PostProcessKeyEvent (Gdk.Key.Down, '\0', Gdk.ModifierType.None);
					break;
				case '4':
					listWindow.PreProcessKeyEvent (Gdk.Key.Left, '\0', Gdk.ModifierType.None);
					listWindow.PostProcessKeyEvent (Gdk.Key.Left, '\0', Gdk.ModifierType.None);
					break;
				case '6':
					listWindow.PreProcessKeyEvent (Gdk.Key.Right, '\0', Gdk.ModifierType.None);
					listWindow.PostProcessKeyEvent (Gdk.Key.Right, '\0', Gdk.ModifierType.None);
					break;
				case '\t':
					listWindow.PreProcessKeyEvent (Gdk.Key.Tab, '\t', Gdk.ModifierType.None);
					listWindow.PostProcessKeyEvent (Gdk.Key.Tab, '\t', Gdk.ModifierType.None);
					break;
				case '\b':
					listWindow.PreProcessKeyEvent (Gdk.Key.BackSpace, '\b', Gdk.ModifierType.None);
					((TestCompletionWidget)listWindow.CompletionWidget).Backspace ();
					listWindow.PostProcessKeyEvent (Gdk.Key.BackSpace, '\b', Gdk.ModifierType.None);
					break;
				case '\n':
					listWindow.PreProcessKeyEvent (Gdk.Key.Return, '\n', Gdk.ModifierType.None);
					listWindow.PostProcessKeyEvent (Gdk.Key.Return, '\n', Gdk.ModifierType.None);
					break;
				default:
					listWindow.PreProcessKeyEvent ((Gdk.Key)ch, ch, Gdk.ModifierType.None);
					((TestCompletionWidget)listWindow.CompletionWidget).AddChar (ch);
					listWindow.PostProcessKeyEvent ((Gdk.Key)ch, ch, Gdk.ModifierType.None);
					break;
				}
			}
		}
		
		class SimulationSettings {
			public string SimulatedInput { get; set; }
			public bool AutoSelect { get; set; }
			public bool CompleteWithSpaceOrPunctuation { get; set; }
			public bool AutoCompleteEmptyMatch { get; set; }
			public string DefaultCompletionString { get; set; }
			
			public string[] CompletionData { get; set; }
		}
		
		static string RunSimulation (string partialWord, string simulatedInput, bool autoSelect, bool completeWithSpaceOrPunctuation, params string[] completionData)
		{
			return RunSimulation (partialWord, simulatedInput, autoSelect, completeWithSpaceOrPunctuation, true, completionData);
		}
		
		static string RunSimulation (string partialWord, string simulatedInput, bool autoSelect, bool completeWithSpaceOrPunctuation, bool autoCompleteEmptyMatch, params string[] completionData)
		{
			return RunSimulation (new SimulationSettings () {
				SimulatedInput = simulatedInput,
				AutoSelect = autoSelect,
				CompleteWithSpaceOrPunctuation = completeWithSpaceOrPunctuation,
				AutoCompleteEmptyMatch = autoCompleteEmptyMatch,
				CompletionData = completionData
			});
		}
		
		static string RunSimulation (SimulationSettings settings)
		{
			CompletionListWindow listWindow = CreateListWindow (settings);
			SimulateInput (listWindow, settings.SimulatedInput);
			return ((TestCompletionWidget)listWindow.CompletionWidget).CompletedWord;
		}

		static CompletionListWindow CreateListWindow (CompletionListWindowTests.SimulationSettings settings)
		{
			CompletionDataList dataList = new CompletionDataList ();
			dataList.AutoSelect = settings.AutoSelect;
			dataList.AddRange (settings.CompletionData);
			dataList.DefaultCompletionString = settings.DefaultCompletionString;
			ListWindow.ClearHistory ();
			CompletionListWindow listWindow = new CompletionListWindow () {
				CompletionDataList = dataList,
				CompletionWidget = new TestCompletionWidget (),
				AutoSelect = settings.AutoSelect,
				CodeCompletionContext = new CodeCompletionContext (),
				AutoCompleteEmptyMatch = settings.AutoCompleteEmptyMatch,
				DefaultCompletionString = settings.DefaultCompletionString
			};
			listWindow.List.FilterWords ();
			listWindow.UpdateWordSelection ();
			listWindow.ResetSizes ();
			return listWindow;
		}

		
		[Test()]
		public void TestPunctuationCompletion ()
		{
			string output = RunSimulation ("", "aaa ", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
			
			output = RunSimulation ("", "aa.", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAb", output);
			
			output = RunSimulation ("", "AbAbA.", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
		}

		[Test()]
		public void TestTabCompletion ()
		{
			string output = RunSimulation ("", "aaa\t", true, false, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
		}
		
		[Test()]
		public void TestTabCompletionWithAutoSelectOff ()
		{
			string output = RunSimulation ("", "aaa\t", false, false, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
		}
		
		[Test()]
		public void TestReturnCompletion ()
		{
			string output = RunSimulation ("", "aaa\n", true, false, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
		}

		[Ignore("\n now always commits")]
		[Test()]
		public void TestReturnCompletionWithAutoSelectOff ()
		{
			string output = RunSimulation ("", "aaa\n", false, false, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual (null, output);
		}
		
		[Test()]
		public void TestAutoSelectionOn ()
		{
			// shouldn't select anything since auto select is disabled.
			string output = RunSimulation ("", "aaa ", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
			
			// now with cursor down
			output = RunSimulation ("", "aaa2 ", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAbAb", output);
		}
		
		[Test()]
		public void TestAutoSelectionOff ()
		{
			// shouldn't select anything since auto select is disabled.
			string output = RunSimulation ("", "aaa ", false, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.IsNull (output);
			
			// now with cursor down (shouldn't change selection)
			output = RunSimulation ("", "aaa2 ", false, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
			
			// now with 2x cursor down - shold select next item.
			output = RunSimulation ("", "aaa22 ", false, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb",
				"AbAbAbAbAb");
			
			Assert.AreEqual ("AbAbAbAb", output);
		}
		
		[Test()]
		public void TestBackspace ()
		{
			string output = RunSimulation ("", "aaaa\b\t", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
			
			output = RunSimulation ("", "aaaa\b\b\b\t", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAb", output);
			
			output = RunSimulation ("", "aaaa\b\b\baaa\t", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAbAb", output);
		}
		
		[Test()]
		public void TestBackspacePreserveAutoSelect ()
		{
			string output = RunSimulation ("", "c\bc ", false, true, 
				"a",
				"b", 
				"c");
			
			Assert.AreEqual (null, output);
		}
		
		[Test()]
		public void TestAutoCompleteEmptyMatchOn ()
		{
			string output = RunSimulation ("", " ", true, true, true,
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAb", output);
			
			output = RunSimulation ("", "\t", true, true, true,
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAb", output);
			
		}
		
		[Test()]
		public void TestAutoCompleteFileNames ()
		{
			string output = RunSimulation ("", "Doc.cs ", true, true, true, "Document.cs");

			Assert.AreEqual ("Document.cs", output);
			
			output = RunSimulation ("", "cwid.cs ", true, true, true,
				"Test.txt",
				"CompletionWidget.cs", 
				"CommandWindow.cs");

			Assert.AreEqual ("CompletionWidget.cs", output);
		}
		
		[Test()]
		public void TestAutoCompleteEmptyMatchOff ()
		{
			string output = RunSimulation ("", " ", true, true, false,
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual (null, output);
			
			output = RunSimulation ("", "\t", true, true, false,
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAb", output);
			
			output = RunSimulation ("", "a ", true, true, false,
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAb", output);
			
		}
		
		string[] punctuationData = {
			"AbAb",
			"/AbAb", 
			"Accc",
			",AbAb",
			",A..bAb",
			",A.bAb",
			"Addd",
		};

		[Test]
		public void TestMatchPunctuation ()
		{
			string output = RunSimulation ("", "/\n", true, false, false, punctuationData);
			Assert.AreEqual ("/AbAb", output);
		}

		[Test]
		public void TestMatchPunctuationCase2 ()
		{
			string output = RunSimulation ("", "A\n", true, false, false, punctuationData);
			Assert.AreEqual ("AbAb", output);
		}

		[Test]
		public void TestMatchPunctuationCase3 ()
		{
			string output = RunSimulation ("", ",A..\n", true, false, false, punctuationData);
			Assert.AreEqual (",A..bAb", output);
		}
		
		[Test]
		public void TestMatchPunctuationCommitOnSpaceAndPunctuation ()
		{
			string output = RunSimulation ("", "Ac ", true, true, false, punctuationData);
			Assert.AreEqual ("Accc", output);
		}

		[Test]
		public void TestMatchPunctuationCommitOnSpaceAndPunctuation2 ()
		{
			var output = RunSimulation ("", "/ ", true, true, false, punctuationData);
			Assert.AreEqual ("/AbAb", output);
		}

		[Ignore("Behavior was changed - commit with '.' now works everytime")]
		[Test]
		public void TestMatchPunctuationCommitOnSpaceAndPunctuation3 ()
		{
			var output = RunSimulation ("", ".", true, true, false, punctuationData);
			Assert.AreEqual (null, output);
		}

		[Test]
		public void TestMatchPunctuationCommitOnSpaceAndPunctuation4 ()
		{
			var output = RunSimulation ("", "A ", true, true, false, punctuationData);
			Assert.AreEqual ("AbAb", output);
		}

		[Test]
		public void TestMatchPunctuationCommitOnSpaceAndPunctuation5 ()
		{
			var output = RunSimulation ("", ",A.b ", true, true, false, punctuationData);
			Assert.AreEqual (",A.bAb", output);
		}
		
		[Test]
		public void TestDefaultCompletionString ()
		{
			string output = RunSimulation (new SimulationSettings {
				SimulatedInput = "\t",
				AutoSelect = true,
				CompleteWithSpaceOrPunctuation = true,
				AutoCompleteEmptyMatch = true,
				CompletionData = new[] {
					"A",
					"B",
					"C"
				},
				DefaultCompletionString = "C"
			});
			
			Assert.AreEqual ("C", output);
			
			output = RunSimulation (new SimulationSettings {
				SimulatedInput = " ",
				AutoSelect = true,
				CompleteWithSpaceOrPunctuation = true,
				AutoCompleteEmptyMatch = true,
				CompletionData = new[] {
					"A",
					"B",
					"C"
				},
				DefaultCompletionString = "C"
			});
			
			Assert.AreEqual ("C", output);
		}
		
		[Test]
		public void TestDefaultCompletionStringList ()
		{
			CompletionListWindow listWindow = CreateListWindow (new SimulationSettings {
				SimulatedInput = "\t",
				AutoSelect = true,
				CompleteWithSpaceOrPunctuation = true,
				AutoCompleteEmptyMatch = false,
				CompletionData = new[] {
					"A",
					"B",
					"C"
				},
				DefaultCompletionString = "C"
			});
			Assert.AreEqual (3, listWindow.FilteredItems.Count);
		}
		
		/// <summary>
		/// Bug 543923 - Completion window should deselect when word is deleted
		/// </summary>
		[Test]
		public void TestBug543923 ()
		{
			string output = RunSimulation (new SimulationSettings {
				SimulatedInput = "i\b ",
				AutoSelect = true,
				CompleteWithSpaceOrPunctuation = true,
				AutoCompleteEmptyMatch = false,
				CompletionData = new[] { "#if", "if", "other" }
			});
			Assert.IsTrue (string.IsNullOrEmpty (output), "output was:"+ output);
		}
		
		
		/// <summary>
		/// Bug 543938 - Completion list up/down broken with single entry
		/// </summary>
		[Test]
		public void TestBug543938 ()
		{
			string output = RunSimulation ("", "2 ", true, true, false, "singleEntry");
			
			Assert.AreEqual ("singleEntry", output);
			
			output = RunSimulation ("", " ", true, true, false, "singleEntry");
			Assert.IsTrue (string.IsNullOrEmpty (output));
		}
		
		/// <summary>
		/// Bug 543984 – Completion window should only accept punctuation when it's an exact match
		/// </summary>
		[Test]
		public void TestBug543984 ()
		{
			string output = RunSimulation ("", "foo b\n", true, true, false, "foo bar", "foo bar baz");
			Assert.AreEqual ("foo bar", output);
		}
		
		[Test]
		public void TestBug595240 ()
		{
			string output = RunSimulation ("", "A\t", true, true, false, "AbCdEf");
			Assert.AreEqual ("AbCdEf", output);
		}

		[Test]
		public void TestBug595240Case2 ()
		{
			var output = RunSimulation ("", "Cd\t", true, true, false, "AbCdEf");
			Assert.AreEqual ("AbCdEf", output);
		}

		[Test]
		public void TestBug595240Case3 ()
		{
			var output = RunSimulation ("", "bC\t", true, true, false, "AbCdEf");
			Assert.AreNotEqual ("AbCdEf", output);
		}
		
		/// <summary>
		/// Bug 613539 - DOBa does not complete to DynamicObjectBase
		/// </summary>
		[Test]
		public void TestBug613539 ()
		{
			string output = RunSimulation ("", "DOB ", true, true, false, "DynamicObject", "DynamicObjectBase");
			Assert.AreEqual ("DynamicObjectBase", output);
		}
		
		/// <summary>
		/// Bug 629361 - Exact completion matches should take account of case
		/// </summary>
		[Test]
		public void TestBug629361 ()
		{
			string output = RunSimulation ("", "unit\t", true, true, false, "Unit", "unit");
			Assert.IsTrue (output == null || "unit" == output);
		}
		
		/// <summary>
		/// Bug 668136 - Subword matching in completion does not work well for xml
		/// </summary>
		[Test]
		public void TestBug668136 ()
		{
			string output = RunSimulation ("", "bar\t", true, true, false, "foo:test", "foo:bar", "foo:foo");
			Assert.AreEqual ("foo:bar", output);
		}
		
		[Test]
		public void TestSubstringMatch ()
		{
			string output = RunSimulation ("", "comcoll\n", true, true, false, "CustomCommandCollection");
			Assert.AreEqual ("CustomCommandCollection", output);
			
			output = RunSimulation ("", "cuscoll\n", true, true, false, "CustomCommandCollection");
			Assert.AreEqual ("CustomCommandCollection", output);
			
			output = RunSimulation ("", "commandcollection\n", true, true, false, "CustomCommandCollection");
			Assert.AreEqual ("CustomCommandCollection", output);
		}
		
		[Test]
		public void TestUpperCase1 ()
		{
			string output = RunSimulation ("", "WR\t", true, true, false, "WriteLine");
			Assert.AreEqual ("WriteLine", output);
		}
		
		[Test]
		public void TestUpperCase2 ()
		{
			string output = RunSimulation ("", "WR\t", true, true, false, "WriteLine", "WriteRaw");
			Assert.AreEqual ("WriteRaw", output);
		}
		
		[Test]
		public void TestDigitSelection ()
		{
			string output = RunSimulation ("", "v1\t", true, true, false, "var", "var1");
			Assert.AreEqual ("var1", output);
		}

		[Test]
		public void TestSelectFirst ()
		{
			string output = RunSimulation ("", "Are\t", true, true, false, "AreDifferent", "Differenx", "AreDiffereny");
			Assert.AreEqual ("AreDifferent", output);
		}

		[Test]
		public void TestPreferStart ()
		{
			string output = RunSimulation ("", "InC\t", true, true, false, "Equals", "InvariantCultureIfo", "GetInvariantCulture");
			Assert.AreEqual ("InvariantCultureIfo", output);
		}

		[Test]
		public void TestPreProcessorDirective ()
		{
			string output = RunSimulation ("", "if\t", true, true, false, "#if", "if");
			Assert.AreEqual ("if", output);
		}

		/// <summary>
		/// Bug 4732 - [Regression] Broken intellisense again 
		/// </summary>
		[Test]
		public void TestBug4732 ()
		{
			string output = RunSimulation ("", "a\t", true, true, false, "_AppDomain", "A");
			Assert.AreEqual ("A", output);
		}


		[Test]
		public void TestFavorFirstSubword ()
		{
			string output = RunSimulation ("", "button\t", true, true, false, "AnotherTestButton", "Button");
			Assert.AreEqual ("Button", output);
		}

		[Test]
		public void TestFavorExactMatch ()
		{
			string output = RunSimulation ("", "View\t", true, true, false, "view", "View");
			Assert.AreEqual ("View", output);
		}

		/// <summary>
		/// Bug 6897 - Case insensitive matching issues
		/// </summary>
		[Test]
		public void TestBug6897 ()
		{
			string output = RunSimulation ("", "io\t", true, true, false, "InvalidOperationException", "IO");
			Assert.AreEqual ("IO", output);
		}

		[Test]
		public void TestBug6897Case2 ()
		{
			string output = RunSimulation ("", "io\t", true, true, false, "InvalidOperationException", "IOException");
			Assert.AreEqual ("IOException", output);
		}

		/// <summary>
		/// Bug 7288 - Completion not selecting the correct entry
		/// </summary>
		[Test]
		public void TestBug7288 ()
		{
			string output = RunSimulation ("", "pages\t", true, true, false, "pages", "PageSystem");
			Assert.AreEqual ("pages", output);
		}

		/// <summary>
		/// Bug 7420 - Prefer properties over named parameters
		/// </summary>
		[Test]
		public void TestBug7420 ()
		{
			string output = RunSimulation ("", "val\t", true, true, false, "Value", "value:");
			Assert.AreEqual ("Value", output);

			output = RunSimulation ("", "val\t", true, true, false, "Value", "value", "value:");
			Assert.AreEqual ("value", output);
		}

		/// <summary>
		/// Bug 7522 - Code completion list should give preference to shorter words
		/// </summary>
		[Test]
		public void TestBug7522 ()
		{
			string output = RunSimulation ("", "vis\t", true, true, false, "VisibilityNotifyEvent", "Visible");
			Assert.AreEqual ("Visible", output);
		}

		/// <summary>
		/// Bug 8257 - Incorrect entry selected in code completion list
		/// </summary>
		[Test]
		public void TestBug8257 ()
		{
			string output = RunSimulation ("", "childr\t", true, true, false, "children", "ChildRequest");
			Assert.AreEqual ("children", output);
		}

		
		/// <summary>
		/// Bug 9114 - Code completion fumbles named parameters 
		/// </summary>
		[Test]
		public void TestBug9114 ()
		{
			string output = RunSimulation ("", "act\t", true, true, false, "act:", "Action");
			Assert.AreEqual ("act:", output);
		}


		[TestFixtureSetUp] 
		public void SetUp()
		{
			Gtk.Application.Init ();
		}
	}
}
