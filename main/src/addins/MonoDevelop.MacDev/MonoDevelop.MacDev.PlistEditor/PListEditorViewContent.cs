// 
// PListEditorViewContent.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoMac.Foundation;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Text;
using System.Text;
using System.IO;	

namespace MonoDevelop.MacDev.PlistEditor
{
	public class PListEditorViewContent : AbstractViewContent, ITextFile
	{
		PObjectContainer pobject;
		IPListDisplayWidget widget;
		
		public override Gtk.Widget Control { get { return (Gtk.Widget)widget; } }
		
		public PListEditorViewContent (IPlistEditingHandler handler, Project proj)
		{
			if (handler != null) {
				widget =  new PListEditorWidget (handler, proj);
			} else {
				widget = new CustomPropertiesWidget ();
			}
		}
		
		public override void Load (string fileName)
		{
			ContentName = fileName;
			if (pobject == null) {
				var dict = new PDictionary ();
				if (dict.Reload (fileName)) {
					pobject = dict;
				} else {
					var arr = new PArray ();
					if (!arr.Reload (fileName)) {
						MessageService.ShowError (GettextCatalog.GetString ("Can't load plist file {0}.", fileName));
						return;
					}
					pobject = arr;
				}
				
				Buffer = null;
				widget.SetPListContainer (pobject);
				pobject.Changed += (sender, e) => {
					Buffer = null;
					IsDirty = true;
				};
			}
			this.IsDirty = false;
		}
		
		public override void Save (string fileName)
		{
			this.IsDirty = false;
			ContentName = fileName;
			try {
				pobject.Save (fileName);
			} catch (Exception e) {
				MessageService.ShowException (e, GettextCatalog.GetString ("Error while writing plist"));
			}
		}
		
		string Buffer {
			get; set;
		}

		string ITextFile.Text {
			get { EnsureBuffer (); return Buffer; }
		}

		char ITextFile.GetCharAt (int position)
		{
			EnsureBuffer ();
			return Buffer [position];
		}
		
		string ITextFile.GetText (int startPosition, int endPosition)
		{
			EnsureBuffer ();
			return Buffer.Substring (startPosition, endPosition - startPosition + 1);
		}

		int ITextFile.GetPositionFromLineColumn (int line, int column)
		{
			EnsureBuffer ();
			int lin = 1;
			int col = 1;
			for (int i = 0; i < Buffer.Length && lin <= line; i++) {
				if (line == lin && column == col)
					return i;
				if (Buffer[i] == '\r') {
					if (i + 1 < Buffer.Length && Buffer[i + 1] == '\n')
						i++;
					lin++; col = 1;
				} else if (Buffer[i] == '\n') {
					lin++; col = 1;
				} else
					col++;
			}
			return -1;
		}

		void ITextFile.GetLineColumnFromPosition (int position, out int line, out int column)
		{
			EnsureBuffer ();
			int lin = 1;
			int col = 1;
			for (int i = 0; i < position; i++) {
				if (Buffer[i] == '\r') {
					if (i + 1 < position && Buffer[i + 1] == '\n')
						i++;
					lin++; col = 1;
				} else if (Buffer[i] == '\n') {
					lin++; col = 1;
				} else
					col++;
			}
			line = lin;
			column = col;
		}
		
		FilePath ITextFile.Name {
			get { return ContentName; }
		}

		int ITextFile.Length {
			get { EnsureBuffer (); return Buffer.Length; }
		}
		
		void EnsureBuffer ()
		{
			if (Buffer == null)
				Buffer = pobject.ToXml () ?? "";
		}
	}
}