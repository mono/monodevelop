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
using MonoDevelop.Ide.Editor.Extension;
using System.Threading.Tasks;

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
				set {
					throw new NotSupportedException ();
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

			double ICompletionWidget.ZoomLevel {
				get {
					return 1;
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
		
		static async Task SimulateInput (CompletionListWindow listWindow, string input)
		{
			var testCompletionWidget = ((TestCompletionWidget)listWindow.CompletionWidget);
			bool isClosed = false;
			listWindow.WindowClosed += delegate {
				isClosed = true;
			};
			foreach (char ch in input) {
				switch (ch) {
				case '8':
					await listWindow.PreProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.Up, '\0', Gdk.ModifierType.None));
					await listWindow.PostProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.Up, '\0', Gdk.ModifierType.None));
					break;
				case '2':
					await listWindow.PreProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.Down, '\0', Gdk.ModifierType.None));
					await listWindow.PostProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.Down, '\0', Gdk.ModifierType.None));
					break;
				case '4':
					await listWindow.PreProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.Left, '\0', Gdk.ModifierType.None));
					await listWindow.PostProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.Left, '\0', Gdk.ModifierType.None));
					break;
				case '6':
					await listWindow.PreProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.Right, '\0', Gdk.ModifierType.None));
					await listWindow.PostProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.Right, '\0', Gdk.ModifierType.None));
					break;
				case '\t':
					await listWindow.PreProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.Tab, '\t', Gdk.ModifierType.None));
					await listWindow.PostProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.Tab, '\t', Gdk.ModifierType.None));
					break;
				case '\b':
					await listWindow.PreProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.BackSpace, '\b', Gdk.ModifierType.None));
					testCompletionWidget.Backspace ();
					await listWindow.PostProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.BackSpace, '\b', Gdk.ModifierType.None));
					break;
				case '\n':
					await listWindow.PreProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.Return, '\n', Gdk.ModifierType.None));
					await listWindow.PostProcessKeyEvent (KeyDescriptor.FromGtk (Gdk.Key.Return, '\n', Gdk.ModifierType.None));
					break;
				default:
					await listWindow.PreProcessKeyEvent (KeyDescriptor.FromGtk ((Gdk.Key)ch, ch, Gdk.ModifierType.None));
					testCompletionWidget.AddChar (ch);
					await listWindow.PostProcessKeyEvent (KeyDescriptor.FromGtk ((Gdk.Key)ch, ch, Gdk.ModifierType.None));
					break;
				}
				// window closed.
				if (isClosed)
					break;
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
		
		static Task<string> RunSimulation (string partialWord, string simulatedInput, bool autoSelect, bool completeWithSpaceOrPunctuation, params string[] completionData)
		{
			return RunSimulation (partialWord, simulatedInput, autoSelect, completeWithSpaceOrPunctuation, true, completionData);
		}
		
		static Task<string> RunSimulation (string partialWord, string simulatedInput, bool autoSelect, bool completeWithSpaceOrPunctuation, bool autoCompleteEmptyMatch, params string[] completionData)
		{
			return RunSimulation (new SimulationSettings () {
				SimulatedInput = simulatedInput,
				AutoSelect = autoSelect,
				CompleteWithSpaceOrPunctuation = completeWithSpaceOrPunctuation,
				AutoCompleteEmptyMatch = autoCompleteEmptyMatch,
				CompletionData = completionData
			});
		}
		
		static async Task<string> RunSimulation (SimulationSettings settings)
		{
			CompletionListWindow listWindow = CreateListWindow (settings);
			var testCompletionWidget = (TestCompletionWidget)listWindow.CompletionWidget;
			await SimulateInput (listWindow, settings.SimulatedInput);
			return testCompletionWidget.CompletedWord;
		}

		static CompletionListWindow CreateListWindow (CompletionListWindowTests.SimulationSettings settings)
		{
			CompletionDataList dataList = new CompletionDataList ();
			dataList.AutoSelect = settings.AutoSelect;
			dataList.AddRange (settings.CompletionData);
			dataList.DefaultCompletionString = settings.DefaultCompletionString;
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
		public async Task TestPunctuationCompletion ()
		{
			string output = await RunSimulation ("", "aaa ", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
			
			output = await RunSimulation ("", "aa.", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAb", output);
			
			output = await RunSimulation ("", "AbAbA.", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
		}

		[Test()]
		public async Task TestTabCompletion ()
		{
			string output = await RunSimulation ("", "aaa\t", true, false, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
		}
		
		[Test()]
		public async Task TestTabCompletionWithAutoSelectOff ()
		{
			string output = await RunSimulation ("", "aaa\t", false, false, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
		}
		
		[Test()]
		public async Task TestReturnCompletion ()
		{
			string output = await RunSimulation ("", "aaa\n", true, false, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
		}

		[Ignore("\n now always commits")]
		[Test()]
		public async Task TestReturnCompletionWithAutoSelectOff ()
		{
			string output = await RunSimulation ("", "aaa\n", false, false, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual (null, output);
		}
		
		[Test()]
		public async Task TestAutoSelectionOn ()
		{
			// shouldn't select anything since auto select is disabled.
			string output = await RunSimulation ("", "aaa ", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
			
			// now with cursor down
			output = await RunSimulation ("", "aaa2 ", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAbAb", output);
		}
		
		[Test()]
		public async Task TestAutoSelectionOff ()
		{
			// shouldn't select anything since auto select is disabled.
			string output = await RunSimulation ("", "aaa ", false, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.IsNull (output);
			
			// now with cursor down (shouldn't change selection)
			output = await RunSimulation ("", "aaa2 ", false, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
			
			// now with 2x cursor down - shold select next item.
			output = await RunSimulation ("", "aaa22 ", false, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb",
				"AbAbAbAbAb");
			
			Assert.AreEqual ("AbAbAbAb", output);
		}
		
		[Test()]
		public async Task TestBackspace ()
		{
			string output = await RunSimulation ("", "aaaa\b\t", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAb", output);
			
			output = await RunSimulation ("", "aaaa\b\b\b\t", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAb", output);
			
			output = await RunSimulation ("", "aaaa\b\b\baaa\t", true, true, 
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAbAbAb", output);
		}
		
		[Test()]
		public async Task TestBackspacePreserveAutoSelect ()
		{
			string output = await RunSimulation ("", "c\bc ", false, true, 
				"a",
				"b", 
				"c");
			
			Assert.AreEqual (null, output);
		}
		
		[Test()]
		public async Task TestAutoCompleteEmptyMatchOn ()
		{
			string output = await RunSimulation ("", " ", true, true, true,
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAb", output);
			
			output = await RunSimulation ("", "\t", true, true, true,
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAb", output);
			
		}
		
		[Test()]
		public async Task TestAutoCompleteFileNames ()
		{
			string output = await RunSimulation ("", "Doc.cs ", true, true, true, "Document.cs");

			Assert.AreEqual ("Document.cs", output);
			
			output = await RunSimulation ("", "cwid.cs ", true, true, true,
				"Test.txt",
				"CompletionWidget.cs", 
				"CommandWindow.cs");

			Assert.AreEqual ("CompletionWidget.cs", output);
		}
		
		[Test()]
		public async Task TestAutoCompleteEmptyMatchOff ()
		{
			string output = await RunSimulation ("", " ", true, true, false,
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual (null, output);
			
			output = await RunSimulation ("", "\t", true, true, false,
				"AbAb",
				"AbAbAb", 
				"AbAbAbAb");
			
			Assert.AreEqual ("AbAb", output);
			
			output = await RunSimulation ("", "a ", true, true, false,
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
		public async Task TestMatchPunctuation ()
		{
			string output = await RunSimulation ("", "/\n", true, false, false, punctuationData);
			Assert.AreEqual ("/AbAb", output);
		}

		[Test]
		public async Task TestMatchPunctuationCase2 ()
		{
			string output = await RunSimulation ("", "A\n", true, false, false, punctuationData);
			Assert.AreEqual ("AbAb", output);
		}

		[Test]
		public async Task TestMatchPunctuationCase3 ()
		{
			string output = await RunSimulation ("", ",A..\n", true, false, false, punctuationData);
			Assert.AreEqual (",A..bAb", output);
		}
		
		[Test]
		public async Task TestMatchPunctuationCommitOnSpaceAndPunctuation ()
		{
			string output = await RunSimulation ("", "Ac ", true, true, false, punctuationData);
			Assert.AreEqual ("Accc", output);
		}

		[Test]
		public async Task TestMatchPunctuationCommitOnSpaceAndPunctuation2 ()
		{
			var output = await RunSimulation ("", "/ ", true, true, false, punctuationData);
			Assert.AreEqual ("/AbAb", output);
		}

		[Ignore]
		[Test]
		public async Task TestMatchPunctuationCommitOnSpaceAndPunctuation3 ()
		{
			var output = await RunSimulation ("", ".", true, true, false, punctuationData);
			Assert.AreEqual (null, output);
		}

		[Test]
		public async Task TestMatchPunctuationCommitOnSpaceAndPunctuation4 ()
		{
			var output = await RunSimulation ("", "A ", true, true, false, punctuationData);
			Assert.AreEqual ("AbAb", output);
		}

		[Test]
		public async Task TestMatchPunctuationCommitOnSpaceAndPunctuation5 ()
		{
			var output = await RunSimulation ("", ",A.b ", true, true, false, punctuationData);
			Assert.AreEqual (",A.bAb", output);
		}
		
		[Test]
		public async Task TestDefaultCompletionString ()
		{
			string output = await RunSimulation (new SimulationSettings {
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
			
			output = await RunSimulation (new SimulationSettings {
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
		public async Task TestBug543923 ()
		{
			string output = await RunSimulation (new SimulationSettings {
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
		public async Task TestBug543938 ()
		{
			string output = await RunSimulation ("", "2 ", true, true, false, "singleEntry");
			
			Assert.AreEqual ("singleEntry", output);
			
			output = await RunSimulation ("", " ", true, true, false, "singleEntry");
			Assert.IsTrue (string.IsNullOrEmpty (output));
		}
		
		/// <summary>
		/// Bug 543984 – Completion window should only accept punctuation when it's an exact match
		/// </summary>
		[Test]
		public async Task TestBug543984 ()
		{
			string output = await RunSimulation ("", "foo#b\n", true, true, false, "foo#bar", "foo#bar#baz");
			Assert.AreEqual ("foo#bar", output);
		}
		
		[Test]
		public async Task TestBug595240 ()
		{
			string output = await RunSimulation ("", "A\t", true, true, false, "AbCdEf");
			Assert.AreEqual ("AbCdEf", output);
		}

		[Test]
		public async Task TestBug595240Case2 ()
		{
			var output = await RunSimulation ("", "Cd\t", true, true, false, "AbCdEf");
			Assert.AreEqual ("AbCdEf", output);
		}

		[Test]
		public async Task TestBug595240Case3 ()
		{
			var output = await RunSimulation ("", "bC\t", true, true, false, "AbCdEf");
			Assert.AreNotEqual ("AbCdEf", output);
		}
		
		/// <summary>
		/// Bug 613539 - DOBa does not complete to DynamicObjectBase
		/// </summary>
		[Test]
		public async Task TestBug613539 ()
		{
			string output = await RunSimulation ("", "DOB ", true, true, false, "DynamicObject", "DynamicObjectBase");
			Assert.AreEqual ("DynamicObjectBase", output);
		}
		
		/// <summary>
		/// Bug 629361 - Exact completion matches should take account of case
		/// </summary>
		[Test]
		public async Task TestBug629361 ()
		{
			string output = await RunSimulation ("", "unit\t", true, true, false, "Unit", "unit");
			Assert.IsTrue (output == null || "unit" == output);
		}
		
		/// <summary>
		/// Bug 668136 - Subword matching in completion does not work well for xml
		/// </summary>
		[Test]
		public async Task TestBug668136 ()
		{
			string output = await RunSimulation ("", "bar\t", true, true, false, "foo:test", "foo:bar", "foo:foo");
			Assert.AreEqual ("foo:bar", output);
		}
		
		[Test]
		public async Task TestSubstringMatch ()
		{
			string output = await RunSimulation ("", "comcoll\n", true, true, false, "CustomCommandCollection");
			Assert.AreEqual ("CustomCommandCollection", output);
			
			output = await RunSimulation ("", "cuscoll\n", true, true, false, "CustomCommandCollection");
			Assert.AreEqual ("CustomCommandCollection", output);
			
			output = await RunSimulation ("", "commandcollection\n", true, true, false, "CustomCommandCollection");
			Assert.AreEqual ("CustomCommandCollection", output);
		}
		
		[Test]
		public async Task TestUpperCase1 ()
		{
			string output = await RunSimulation ("", "WR\t", true, true, false, "WriteLine");
			Assert.AreEqual ("WriteLine", output);
		}
		
		[Test]
		public async Task TestUpperCase2 ()
		{
			string output = await RunSimulation ("", "WR\t", true, true, false, "WriteLine", "WriteRaw");
			Assert.AreEqual ("WriteRaw", output);
		}
		
		[Test]
		public async Task TestDigitSelection ()
		{
			string output = await RunSimulation ("", "v1\t", true, true, false, "var", "var1");
			Assert.AreEqual ("var1", output);
		}

		[Test]
		public async Task TestSelectFirst ()
		{
			string output = await RunSimulation ("", "Are\t", true, true, false, "AreDifferent", "Differenx", "AreDiffereny");
			Assert.AreEqual ("AreDifferent", output);
		}

		[Test]
		public async Task TestPreferStart ()
		{
			string output = await RunSimulation ("", "InC\t", true, true, false, "Equals", "InvariantCultureIfo", "GetInvariantCulture");
			Assert.AreEqual ("InvariantCultureIfo", output);
		}

		[Test]
		public async Task TestPreProcessorDirective ()
		{
			string output = await RunSimulation ("", "if\t", true, true, false, "#if", "if");
			Assert.AreEqual ("if", output);
		}

		/// <summary>
		/// Bug 4732 - [Regression] Broken intellisense again 
		/// </summary>
		[Test]
		public async Task TestBug4732 ()
		{
			string output = await RunSimulation ("", "a\t", true, true, false, "_AppDomain", "A");
			Assert.AreEqual ("A", output);
		}


		[Test]
		public async Task TestFavorFirstSubword ()
		{
			string output = await RunSimulation ("", "button\t", true, true, false, "AnotherTestButton", "Button");
			Assert.AreEqual ("Button", output);
		}

		[Test]
		public async Task TestFavorExactMatch ()
		{
			string output = await RunSimulation ("", "View\t", true, true, false, "view", "View");
			Assert.AreEqual ("View", output);
		}

		/// <summary>
		/// Bug 6897 - Case insensitive matching issues
		/// </summary>
		[Test]
		public async Task TestBug6897 ()
		{
			string output = await RunSimulation ("", "io\t", true, true, false, "InvalidOperationException", "IO");
			Assert.AreEqual ("IO", output);
		}

		[Test]
		public async Task TestBug6897Case2 ()
		{
			string output = await RunSimulation ("", "io\t", true, true, false, "InvalidOperationException", "IOException");
			Assert.AreEqual ("IOException", output);
		}

		/// <summary>
		/// Bug 7288 - Completion not selecting the correct entry
		/// </summary>
		[Test]
		public async Task TestBug7288 ()
		{
			string output = await RunSimulation ("", "pages\t", true, true, false, "pages", "PageSystem");
			Assert.AreEqual ("pages", output);
		}

		/// <summary>
		/// Bug 7420 - Prefer properties over named parameters
		/// </summary>
		[Test]
		public async Task TestBug7420 ()
		{
			string output = await RunSimulation ("", "val\t", true, true, false, "Value", "value:");
			Assert.AreEqual ("Value", output);

			output = await RunSimulation ("", "val\t", true, true, false, "Value", "value", "value:");
			Assert.AreEqual ("value", output);
		}

		/// <summary>
		/// Bug 7522 - Code completion list should give preference to shorter words
		/// </summary>
		[Test]
		public async Task TestBug7522 ()
		{
			string output = await RunSimulation ("", "vis\t", true, true, false, "VisibilityNotifyEvent", "Visible");
			Assert.AreEqual ("Visible", output);
		}

		/// <summary>
		/// Bug 8257 - Incorrect entry selected in code completion list
		/// </summary>
		[Test]
		public async Task TestBug8257 ()
		{
			string output = await RunSimulation ("", "childr\t", true, true, false, "children", "ChildRequest");
			Assert.AreEqual ("children", output);
		}

		
		/// <summary>
		/// Bug 9114 - Code completion fumbles named parameters 
		/// </summary>
		[Test]
		public async Task TestBug9114 ()
		{
			string output = await RunSimulation ("", "act\t", true, true, false, "act:", "Action");
			Assert.AreEqual ("act:", output);
		}

		/// <summary>
		/// Bug 36451 - Text input is weird.
		/// </summary>
		[Test]
		public async Task TestBug36451 ()
		{
			string output = await RunSimulation ("", "x\"", true, true, false, "X");
			Assert.AreEqual ("X", output);
		}

		/// <summary>
		/// Bug 17779 - Symbol names with multiple successive letters are filtered out too early
		/// </summary>
		[Test]
		public async Task TestBug17779 ()
		{
			string output = await RunSimulation ("", "ID11\t", true, true, false, "ID11Tag");
			Assert.AreEqual ("ID11Tag", output);
		}

		/// <summary>
		/// Bug 21121 - Aggressive completion for delegates
		/// </summary>
		[Test]
		public async Task TestBug21121 ()
		{
			string output = await RunSimulation ("", "d)", true, true, false, "d", "delegate ()");
			Assert.AreEqual ("d", output);
		}

		[Test]
		public async Task TestSpaceCommits ()
		{
			string output = await RunSimulation ("", "over ", true, true, 
				"override",
				"override foo");

			Assert.AreEqual ("override", output);
		}



		[Test]
		public async Task TestNumberInput ()
		{
			string output = await RunSimulation ("", "1.", true, true, false, "foo1");
			Assert.IsTrue (string.IsNullOrEmpty (output), "output was " + output);
		}

		static async Task<TestCompletionWidget> ContinueSimulation (CompletionListWindow listWindow, ICompletionDataList list, string simulatedInput)
		{
			TestCompletionWidget testCompletionWidget;
			listWindow.ResetState ();
			listWindow.CodeCompletionContext = new CodeCompletionContext ();
			listWindow.CompletionDataList = list;
			listWindow.CompletionWidget = testCompletionWidget = new TestCompletionWidget ();
			listWindow.List.FilterWords ();
			listWindow.ResetSizes ();
			listWindow.UpdateWordSelection ();
			await SimulateInput (listWindow, simulatedInput);
			await listWindow.CompleteWord ();
			return testCompletionWidget;
		}

		[Test]
		public async Task TestMruSimpleLastItem ()
		{
			var settings = new SimulationSettings () {
				AutoSelect = true,
				CompleteWithSpaceOrPunctuation = true,
				AutoCompleteEmptyMatch = true,
				CompletionData = new[] { "FooBar1", "Bar", "FooFoo2"}
			};

			var listWindow = CreateListWindow (settings);
			var list = listWindow.CompletionDataList;
			var testCompletionWidget = (TestCompletionWidget)listWindow.CompletionWidget;

			await SimulateInput (listWindow, "FooBar\t");
			Assert.AreEqual ("FooBar1", testCompletionWidget.CompletedWord);

			testCompletionWidget = await ContinueSimulation (listWindow, list, "FooFoo\t");
			Assert.AreEqual ("FooFoo2", testCompletionWidget.CompletedWord);

			testCompletionWidget = await ContinueSimulation (listWindow, list, "F\t");
			Assert.AreEqual ("FooFoo2", testCompletionWidget.CompletedWord);
		}

		[Test]
		public async Task TestMruEmptyMatch ()
		{
			var settings = new SimulationSettings () {
				AutoSelect = true,
				CompleteWithSpaceOrPunctuation = true,
				AutoCompleteEmptyMatch = true,
				CompletionData = new[] { "Foo", "Bar", "Test"}
			};

			var listWindow = CreateListWindow (settings);
			var list = listWindow.CompletionDataList;
			var testCompletionWidget = (TestCompletionWidget)listWindow.CompletionWidget;
			await SimulateInput (listWindow, "Foo\t");
			testCompletionWidget = await ContinueSimulation (listWindow, list, "F\t");
			Assert.AreEqual ("Foo", testCompletionWidget.CompletedWord);

			testCompletionWidget = await ContinueSimulation (listWindow, list, "Bar\t");
			Assert.AreEqual ("Bar", testCompletionWidget.CompletedWord);

			testCompletionWidget = await ContinueSimulation (listWindow, list, "\t");
			Assert.AreEqual ("Bar", testCompletionWidget.CompletedWord);
		}

		[Test]
		public async Task TestCloseWithPunctiation ()
		{
			var output = await RunSimulation ("", "\"\t", true, true, false, punctuationData);
			Assert.AreEqual (null, output);
		}

		[Test]
		public async Task TestPreference ()
		{
			string output = await RunSimulation ("", "expr\t", true, true, false, "expression", "PostfixExpressionStatementSyntax");
			Assert.AreEqual ("expression", output);
		}

		/// <summary>
		/// Bug 30591 - [Roslyn] Enum code-completion doesn't generate type on "."(dot)
		/// </summary>
		[Test]
		public async Task TestBug0591 ()
		{
			var output = await RunSimulation ("", ".", false, false, false, new [] { "foo" } );
			Assert.AreEqual ("foo", output);
		}

		/// <summary>
		/// Bug 37985 - Code completion is selecting 'int32' instead of letting me type '2' 
		/// </summary>
		[Test]
		public async Task TestBug37985 ()
		{
			var output = await RunSimulation ("", "3\t", false, false, false, new [] { "Int32" } );
			Assert.AreEqual (null, output);
		}

		/// <summary>
		/// Bug 38180 - Code completion should be case sensitive 
		/// </summary>
		[Test]
		public async Task TestBug38180 ()
		{
			var settings = new SimulationSettings () {
				AutoSelect = true,
				CompleteWithSpaceOrPunctuation = true,
				AutoCompleteEmptyMatch = true,
				CompletionData = new [] { "Test", "test" }
			};

			var listWindow = CreateListWindow (settings);
			var list = listWindow.CompletionDataList;
			var testCompletionWidget = (TestCompletionWidget)listWindow.CompletionWidget;

			await SimulateInput (listWindow, "test\t");
			Assert.AreEqual ("test", testCompletionWidget.CompletedWord);

			testCompletionWidget = await ContinueSimulation (listWindow, list, "t\t");
			Assert.AreEqual ("test", testCompletionWidget.CompletedWord);

			testCompletionWidget = await ContinueSimulation (listWindow, list, "T\t");
			Assert.AreEqual ("Test", testCompletionWidget.CompletedWord);
		}

		[TestFixtureSetUp] 
		public void SetUp()
		{
			Gtk.Application.Init ();
		}
	}
}
