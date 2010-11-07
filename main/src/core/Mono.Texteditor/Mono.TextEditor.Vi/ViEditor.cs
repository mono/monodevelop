// 
// ViEditorContext.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using System.Text;
using Gdk;

namespace Mono.TextEditor.Vi
{
	public class ViEditor
	{
		NewViEditMode editMode;
		
		#region Register constants
		const char REG_YANK_DEFAULT = '0';
		const char REG_DELETE_DEFAULT = '1';
		const char REG_PASTE_DEFAULT = '"';
		const char REG_DELETE_SMALL = '-';
		const char REG_BLACKHOLE = '_';
		const char REG_LAST_INSERTED_TEXT = '.';
		const char REG_FILENAME_CURRENT = '%';
		const char REG_FILENAME_ALTERNATE = '#';
		const char REG_LAST_COMMANDLINE = ':';
		const char REG_X11_PRIMARY = '*';
		const char REG_X11_CLIPBOARD = '+';
		const char REG_X11_LAST_DROP = '~';
		const char REG_LAST_SEARCH = '/';
		#endregion

		Dictionary<char,string> registers = new Dictionary<char,string> ();
		Dictionary<char,ViMark> marks = new Dictionary<char, ViMark> ();
		
		public TextEditor Editor { get { return editMode.Editor; } }
		public TextEditorData Data { get { return editMode.Data; } }
		public Document Document { get { return Data.Document; } }
		ViBuilderContext Context { get; set; }
		public string Message { get; private set; }
		public ViEditorMode Mode { get; private set; }

		//shared between editors in the same process
		//TODO: maybe move these into some kind of shared context to pass around explicitly
		static char lastRegisterUsed = '0';
		static string lastPattern;
		static string lastCommandline;
		static string lastInsertedText;
		
		public string LastCommandline {
			get { return lastCommandline; }
			set { lastCommandline = value; }
		}

		public string LastPattern {
			get { return lastPattern; }
			set { lastPattern = value; }
		}
		
		public string LastInsertedText {
			get { return lastInsertedText; }
			set { lastInsertedText = value; }
		}
		
		public void Reset (string message)
		{
			Context = ViBuilderContext.Create (this);
			Message = message;
			
			if (Data.Caret.Mode != CaretMode.Block) {
				Data.Caret.Mode = CaretMode.Block;
				if (Data.Caret.Column > DocumentLocation.MinColumn)
					Data.Caret.Column--;
			}
			ViActions.RetreatFromLineEnd (Data);
		}
		
		public void SetMode (ViEditorMode mode)
		{
			if (this.Mode == mode)
				return;
			this.Mode = mode;
			
			if (Data == null)
				return;
			
			switch (mode) {
			case ViEditorMode.Insert:
				Message = "-- INSERT --";
				editMode.SetCaretMode (CaretMode.Insert);
				break;
			case ViEditorMode.Normal:
				editMode.SetCaretMode (CaretMode.Block);
				break;
			case ViEditorMode.Replace:
				Message = "-- REPLACE --";
				editMode.SetCaretMode (CaretMode.Underscore);
				break;
			}
		}
		
		public void OnCaretPositionChanged ()
		{
			switch (Mode) {
			case ViEditorMode.Insert:
			case ViEditorMode.Replace:
			case ViEditorMode.Visual:
				return;
			case ViEditorMode.Normal:
				ViActions.RetreatFromLineEnd (Data);
				break;
			default:
				Reset ("");
				break;
			}
		}
		
		public void ProcessKey (Gdk.ModifierType modifiers, Key key, char ch)
		{
			if (Context == null)
				Reset ("");
			
			Context.ProcessKey (key, ch, modifiers);
			if (Context.Completed) {
				Reset (Context.Message);
			} else {
				Message = Context.Message;
			}
		}
		
		public Dictionary<char,ViMark> Marks { get { return marks; } }

		public bool SearchBackward { get; set; }
		public string LastReplacement { get; set; }

		public ViEditor (NewViEditMode editMode)
		{
			this.editMode = editMode;
			SetMode (ViEditorMode.Normal);
		}

		public string GetRegisterContents (char register)
		{
			if (register == '"')
				register = lastRegisterUsed;

			switch (register) {
				case REG_LAST_INSERTED_TEXT:
					return LastInsertedText;

				case REG_FILENAME_ALTERNATE:
				case REG_FILENAME_CURRENT:
					return Document.FileName;
				
				case REG_LAST_COMMANDLINE:
					return LastCommandline;

				case REG_LAST_SEARCH:
					return LastPattern;

				case REG_X11_PRIMARY:
				case REG_X11_CLIPBOARD:
				case REG_X11_LAST_DROP:
					throw new NotImplementedException("X11 registers");

				case REG_DELETE_SMALL:
					break;

				default:
					int regInt = (int)register;
					if (((int)'a' <= regInt && regInt <= (int)'z')
						|| ((int)'A' <= regInt && regInt <= (int)'Z')
						|| ((int)'0' <= regInt && regInt <= (int)'9'))
						break;
					else
						throw new InvalidOperationException ("Invalid register '" + register + "'.");
			}

			string value;
			registers.TryGetValue (register, out value);
			return value;
		}
		
		//FIXME: add others when implemented
		static string validSetRegisters = "\"-";
		static string validGetRegisters = ".%#:/-";
		static string validRegisters = "\".%#:/-";
		
		public static bool IsValidRegister (char c)
		{
			int regInt = (int) c;
			return (((int)'a' <= regInt && regInt <= (int)'z')
				|| ((int)'A' <= regInt && regInt <= (int)'Z')
				|| ((int)'0' <= regInt && regInt <= (int)'9')
			    || validRegisters.Contains (c));
		}
		
		public static bool IsValidSetRegister (char c)
		{
			int regInt = (int) c;
			return (((int)'a' <= regInt && regInt <= (int)'z')
				|| ((int)'A' <= regInt && regInt <= (int)'Z')
				|| ((int)'0' <= regInt && regInt <= (int)'9')
			    || validSetRegisters.Contains (c));
		}
		
		public static bool IsValidGetRegister (char c)
		{
			int regInt = (int) c;
			return (((int)'a' <= regInt && regInt <= (int)'z')
				|| ((int)'A' <= regInt && regInt <= (int)'Z')
				|| ((int)'0' <= regInt && regInt <= (int)'9')
			    || validGetRegisters.Contains (c));
		}

		public void SetRegisterContents (char register, string value)
		{
			switch (register) {
				case REG_X11_PRIMARY:
				case REG_X11_CLIPBOARD:
					throw new NotImplementedException ("GUI clipboard");

				//delete/change registers
				case '1': case '2': case '3': case '4': case '5':
				case '6': case '7': case '8':
					//move registers 1-8 downwards if needed
					for (int i = (int)'8'; i >= (int)register; i--)
						registers[(char)(i + 1)] = registers[(char)i];
					break;

				case '0':
				case '9':
				case REG_DELETE_SMALL:
					break;

				case REG_PASTE_DEFAULT:
					register = '0';
					break;

				default:
					int regInt = (int) register;
					if (((int)'a' <= regInt && regInt <= (int)'z')
						|| ((int)'A' <= regInt && regInt <= (int)'Z'))
						break;
					else
						throw new InvalidOperationException ("Cannot write to register '" + register + "'.");
			}

			lastRegisterUsed = register;
			registers[register] = value;
		}
	}
	
	public enum ViEditorMode
	{
		Normal = 0,
		Insert,						// I
		Replace,					// R
		Visual,						// v
		VisualLine,					// V
		VisualBlock	      			// ^V TODO
	}
}

