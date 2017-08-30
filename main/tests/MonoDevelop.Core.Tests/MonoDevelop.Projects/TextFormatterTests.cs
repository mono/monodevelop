// 
// TextFormatterTests.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using MonoDevelop.Projects.Text;
using NUnit.Framework;

namespace MonoDevelop.Projects
{
	[TestFixture()]
	public class TextFormatterTests
	{
		string Text;
		TextFormatter formatter = new TextFormatter ();
		
		void AssertFormat (string result, params object[] args)
		{
			foreach (object ob in args) {
				if (ob is string)
					formatter.IndentString = (string) ob;
				if (ob is WrappingType)
					formatter.Wrap = (WrappingType) ob;
				if (ob is bool)
					formatter.TabsAsSpaces = (bool) ob;
				if (ob is int)
					formatter.MaxColumns = (int) ob;
			}
			
			string id = result + string.Format (" IndentString:{0}, LeftMargin:{1}, MaxColumns:{2}, TabsAsSpaces:{3}, TabWidth:{4}, Wrap:{5}", formatter.IndentString, formatter.LeftMargin, formatter.MaxColumns, formatter.TabsAsSpaces, formatter.TabWidth, formatter.Wrap);
			id = id.Replace ("\n","\\n").Replace ("\t","\\t");
			
			formatter.Clear ();
			formatter.Append (Text);
			Assert.AreEqual (result, formatter.ToString (), id);
		}
		
		[Test()]
		public void WordWrapping ()
		{
			formatter.MaxColumns = 14;
			formatter.TabWidth = 4;
			formatter.LeftMargin = 0;
			formatter.Wrap = WrappingType.Word;
			
			//                 |123456|123456|123456789 |123456789
			Text = "aaaaaa bbbbbb cccccc ddd eeeeee";
			AssertFormat ("aaaaaa\nbbbbbb\ncccccc ddd\neeeeee", "", false, 10);
			AssertFormat ("  \taaaaaa\n  \tbbbbbb\n  \tcccccc ddd\n  \teeeeee", "  \t", 14);
			AssertFormat ("    aaaaaa\n    bbbbbb\n    cccccc ddd\n    eeeeee", true);
			
			
			//                 |123456789  |123456789 |123456789  |123456789
			Text = "aaaaaaaaaa\nbbbbbbbbbb cccccccccc\tdddddddddd";
			AssertFormat ("aaaaaaaaaa\nbbbbbbbbbb\ncccccccccc\ndddddddddd", "", false, 10);
			AssertFormat ("  \taaaaaaaaaa\n  \tbbbbbbbbbb\n  \tcccccccccc\n  \tdddddddddd", "  \t", 14);
			AssertFormat ("    aaaaaaaaaa\n    bbbbbbbbbb\n    cccccccccc\n    dddddddddd", true);
			
			formatter.Clear ();
			
			//                 |123456789  |123456789 |123456789  |123456789
			Text = "aaaaaaaaaabbbbbbbbbb ccccccccccdddddddddd";
			AssertFormat ("aaaaaaaaaabbbbbbbbbb\nccccccccccdddddddddd", "", false, 10);
			AssertFormat ("  \taaaaaaaaaabbbbbbbbbb\n  \tccccccccccdddddddddd", "  \t", 14);
			AssertFormat ("    aaaaaaaaaabbbbbbbbbb\n    ccccccccccdddddddddd", true);
		}
		
		[Test()]
		public void CharWrapping ()
		{
			formatter.MaxColumns = 14;
			formatter.TabWidth = 4;
			formatter.LeftMargin = 0;
			formatter.Wrap = WrappingType.Char;
			
			//      |123456789|123456789|123456789
			Text = "aaaaaa bbbbbb cccccc ddd eeeeee";
			AssertFormat ("aaaaaa bbb\nbbb cccccc\nddd eeeeee", "", false, 10);
			AssertFormat ("  \taaaaaa bbb\n  \tbbb cccccc\n  \tddd eeeeee", "  \t", 14);
			AssertFormat ("    aaaaaa bbb\n    bbb cccccc\n    ddd eeeeee", true);
			
			
			//      |123456789  |123456789 |123456789  |123456789|123456789
			Text = "aaaaaaaaaa\nbbbbbbbbbb cccccccccc\tdddddddddd";
			AssertFormat ("aaaaaaaaaa\nbbbbbbbbbb\ncccccccccc\ndddddddddd", "", false, 10);
			AssertFormat ("  \taaaaaaaaaa\n  \tbbbbbbbbbb\n  \tcccccccccc\n  \tdddddddddd", "  \t", 14);
			AssertFormat ("    aaaaaaaaaa\n    bbbbbbbbbb\n    cccccccccc\n    dddddddddd", true);
			
			formatter.Clear ();
			
			//      |123456789  |123456789 |123456789  |123456789
			Text = "aaaaaaaaaabbbbbbbbbb ccccccccccdddddddddd";
			AssertFormat ("aaaaaaaaaa\nbbbbbbbbbb\ncccccccccc\ndddddddddd", "", false, 10);
			AssertFormat ("  \taaaaaaaaaa\n  \tbbbbbbbbbb\n  \tcccccccccc\n  \tdddddddddd", "  \t", 14);
			AssertFormat ("    aaaaaaaaaa\n    bbbbbbbbbb\n    cccccccccc\n    dddddddddd", true);
		}
		
		[Test()]
		public void WordCharWrapping ()
		{
			formatter.MaxColumns = 14;
			formatter.TabWidth = 4;
			formatter.LeftMargin = 0;
			formatter.Wrap = WrappingType.WordChar;
			
			//                 |123456|123456|123456789 |123456789
			Text = "aaaaaa bbbbbb cccccc ddd eeeeee";
			AssertFormat ("aaaaaa\nbbbbbb\ncccccc ddd\neeeeee", "", false, 10);
			AssertFormat ("  \taaaaaa\n  \tbbbbbb\n  \tcccccc ddd\n  \teeeeee", "  \t", 14);
			AssertFormat ("    aaaaaa\n    bbbbbb\n    cccccc ddd\n    eeeeee", true);
			
			
			//                 |123456789  |123456789 |123456789  |123456789
			Text = "aaaaaaaaaa\nbbbbbbbbbb cccccccccc\tdddddddddd";
			AssertFormat ("aaaaaaaaaa\nbbbbbbbbbb\ncccccccccc\ndddddddddd", "", false, 10);
			AssertFormat ("  \taaaaaaaaaa\n  \tbbbbbbbbbb\n  \tcccccccccc\n  \tdddddddddd", "  \t", 14);
			AssertFormat ("    aaaaaaaaaa\n    bbbbbbbbbb\n    cccccccccc\n    dddddddddd", true);
			
			formatter.Clear ();
			
			//                 |123456789  |123456789 |123456789  |123456789
			Text = "aaaaaaaaaabbbbbbbbbb ccccccccccdddddddddd";
			AssertFormat ("aaaaaaaaaa\nbbbbbbbbbb\ncccccccccc\ndddddddddd", "", false, 10);
			AssertFormat ("  \taaaaaaaaaa\n  \tbbbbbbbbbb\n  \tcccccccccc\n  \tdddddddddd", "  \t", 14);
			AssertFormat ("    aaaaaaaaaa\n    bbbbbbbbbb\n    cccccccccc\n    dddddddddd", true);
		}
		
		[Test()]
		public void NoWrapping ()
		{
			formatter.MaxColumns = 14;
			formatter.TabWidth = 4;
			formatter.LeftMargin = 0;
			formatter.Wrap = WrappingType.None;
			
			//                 |123456|123456|123456789 |123456789
			Text = "aaaaaa bbbbbb cccccc ddd eeeeee";
			AssertFormat ("aaaaaa bbbbbb cccccc ddd eeeeee", "", false, 10);
			AssertFormat ("  \taaaaaa bbbbbb cccccc ddd eeeeee", "  \t", 14);
			AssertFormat ("    aaaaaa bbbbbb cccccc ddd eeeeee", true);
			
			
			//                 |123456789  |123456789 |123456789  |123456789
			Text = "aaaaaaaaaa\nbbbbbbbbbb cccccccccc\tdddddddddd";
			AssertFormat ("aaaaaaaaaa\nbbbbbbbbbb cccccccccc\tdddddddddd", "", false, 10);
			AssertFormat ("  \taaaaaaaaaa\n  \tbbbbbbbbbb cccccccccc\tdddddddddd", "  \t", 14);
			AssertFormat ("    aaaaaaaaaa\n    bbbbbbbbbb cccccccccc   dddddddddd", true);
			formatter.Clear ();
			
			//                 |123456789  |123456789 |123456789  |123456789
			Text = "aaaaaaaaaabbbbbbbbbb ccccccccccdddddddddd";
			AssertFormat ("aaaaaaaaaabbbbbbbbbb ccccccccccdddddddddd", "", false, 10);
			AssertFormat ("  \taaaaaaaaaabbbbbbbbbb ccccccccccdddddddddd", "  \t", 14);
			AssertFormat ("    aaaaaaaaaabbbbbbbbbb ccccccccccdddddddddd", true);
		}
		
		string[] files;
		
		void AddFiles (string prefix, string sep1, string sep2, string postfix, int margin)
		{
			formatter.Clear ();
			formatter.Wrap = WrappingType.Word;
			formatter.LeftMargin = margin;
			formatter.ParagraphStartMargin = 0;
			
			formatter.BeginWord ();
			formatter.Append (prefix);
			
			for (int n=0; n<files.Length; n++) {
				if (n > 0) {
					formatter.Append (sep1);
					formatter.EndWord ();
					formatter.BeginWord ();
					formatter.Append (sep2);
				}
				formatter.Append (files [n]);
			}
			formatter.Append (postfix);
			formatter.EndWord ();

			formatter.Append ("cccc dddd eeeeee ffff gggg hh iiii jjjj kkkk");
		}
		
		[Test()]
		public void CombinedWrapping ()
		{
			files = new string[] {"aaaaaaaaaaaaaaa", "bbbbbbbbbbbbbbb", "ccccc", "dd", "ee"};
			formatter.MaxColumns = 10;
			formatter.TabWidth = 4;
			formatter.IndentString = "";
		
			AddFiles ("* ", ":\n", "* ", ": ", 0);
			Assert.AreEqual ("* aaaaaaaaaaaaaaa:\n* bbbbbbbbbbbbbbb:\n* ccccc:\n* dd:\n* ee: cccc\ndddd\neeeeee\nffff gggg\nhh iiii\njjjj kkkk", formatter.ToString ());
		
			AddFiles ("* ", ", ", "", ": ", 0);
			Assert.AreEqual ("* aaaaaaaaaaaaaaa,\nbbbbbbbbbbbbbbb,\nccccc, dd,\nee: cccc\ndddd\neeeeee\nffff gggg\nhh iiii\njjjj kkkk", formatter.ToString ());
			
			AddFiles ("* ", ":\n", "* ", ": ", 2);
			Assert.AreEqual ("* aaaaaaaaaaaaaaa:\n* bbbbbbbbbbbbbbb:\n* ccccc:\n* dd:\n* ee: cccc\n  dddd\n  eeeeee\n  ffff\n  gggg hh\n  iiii\n  jjjj\n  kkkk", formatter.ToString ());
		
			AddFiles ("* ", ", ", "", ": ", 2);
			Assert.AreEqual ("* aaaaaaaaaaaaaaa,\n  bbbbbbbbbbbbbbb,\n  ccccc,\n  dd, ee:\n  cccc\n  dddd\n  eeeeee\n  ffff\n  gggg hh\n  iiii\n  jjjj\n  kkkk", formatter.ToString ());
		
			AddFiles ("* ", ":\n", "* ", ":\n", 0);
			Assert.AreEqual ("* aaaaaaaaaaaaaaa:\n* bbbbbbbbbbbbbbb:\n* ccccc:\n* dd:\n* ee:\ncccc dddd\neeeeee\nffff gggg\nhh iiii\njjjj kkkk", formatter.ToString ());
		
			AddFiles ("* ", ", ", "", ":\n", 0);
			Assert.AreEqual ("* aaaaaaaaaaaaaaa,\nbbbbbbbbbbbbbbb,\nccccc, dd,\nee:\ncccc dddd\neeeeee\nffff gggg\nhh iiii\njjjj kkkk", formatter.ToString ());
			
			AddFiles ("* ", ":\n", "* ", ":\n  ", 2);
			Assert.AreEqual ("* aaaaaaaaaaaaaaa:\n* bbbbbbbbbbbbbbb:\n* ccccc:\n* dd:\n* ee:\n  cccc\n  dddd\n  eeeeee\n  ffff\n  gggg hh\n  iiii\n  jjjj\n  kkkk", formatter.ToString ());
		
			AddFiles ("* ", ", ", "", ":\n  ", 2);
			Assert.AreEqual ("* aaaaaaaaaaaaaaa,\n  bbbbbbbbbbbbbbb,\n  ccccc,\n  dd, ee:\n  cccc\n  dddd\n  eeeeee\n  ffff\n  gggg hh\n  iiii\n  jjjj\n  kkkk", formatter.ToString ());
		}
		
		[Test()]
		public void CombinedWrapping2 ()
		{
			files = new string[] {"ccccc", "dd", "ee", "aaaaaaaaaaaaaaa", "bbbbbbbbbbbbbbb"};
			formatter.MaxColumns = 10;
			formatter.TabWidth = 4;
			formatter.IndentString = "";
		
			AddFiles ("* ", ":\n", "* ", ": ", 0);
			Assert.AreEqual ("* ccccc:\n* dd:\n* ee:\n* aaaaaaaaaaaaaaa:\n* bbbbbbbbbbbbbbb:\ncccc dddd\neeeeee\nffff gggg\nhh iiii\njjjj kkkk", formatter.ToString ());
		
			AddFiles ("* ", ", ", "", ": ", 0);
			Assert.AreEqual ("* ccccc,\ndd, ee,\naaaaaaaaaaaaaaa,\nbbbbbbbbbbbbbbb:\ncccc dddd\neeeeee\nffff gggg\nhh iiii\njjjj kkkk", formatter.ToString ());
			
			AddFiles ("* ", ":\n", "* ", ": ", 2);
			Assert.AreEqual ("* ccccc:\n* dd:\n* ee:\n* aaaaaaaaaaaaaaa:\n* bbbbbbbbbbbbbbb:\n  cccc\n  dddd\n  eeeeee\n  ffff\n  gggg hh\n  iiii\n  jjjj\n  kkkk", formatter.ToString ());
		
			AddFiles ("* ", ", ", "", ": ", 2);
			Assert.AreEqual ("* ccccc,\n  dd, ee,\n  aaaaaaaaaaaaaaa,\n  bbbbbbbbbbbbbbb:\n  cccc\n  dddd\n  eeeeee\n  ffff\n  gggg hh\n  iiii\n  jjjj\n  kkkk", formatter.ToString ());
		
			AddFiles ("* ", ":\n", "* ", ":\n", 0);
			Assert.AreEqual ("* ccccc:\n* dd:\n* ee:\n* aaaaaaaaaaaaaaa:\n* bbbbbbbbbbbbbbb:\ncccc dddd\neeeeee\nffff gggg\nhh iiii\njjjj kkkk", formatter.ToString ());
		
			AddFiles ("* ", ", ", "", ":\n", 0);
			Assert.AreEqual ("* ccccc,\ndd, ee,\naaaaaaaaaaaaaaa,\nbbbbbbbbbbbbbbb:\ncccc dddd\neeeeee\nffff gggg\nhh iiii\njjjj kkkk", formatter.ToString ());
			
			AddFiles ("* ", ":\n", "* ", ":\n  ", 2);
			Assert.AreEqual ("* ccccc:\n* dd:\n* ee:\n* aaaaaaaaaaaaaaa:\n* bbbbbbbbbbbbbbb:\n  cccc\n  dddd\n  eeeeee\n  ffff\n  gggg hh\n  iiii\n  jjjj\n  kkkk", formatter.ToString ());
		
			AddFiles ("* ", ", ", "", ":\n  ", 2);
			Assert.AreEqual ("* ccccc,\n  dd, ee,\n  aaaaaaaaaaaaaaa,\n  bbbbbbbbbbbbbbb:\n  cccc\n  dddd\n  eeeeee\n  ffff\n  gggg hh\n  iiii\n  jjjj\n  kkkk", formatter.ToString ());
		}	
	}
}
