// WatchPad.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Xml;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger
{
	public class WatchPad: IPadContent, IMementoCapable, ICustomXmlSerializer
	{
		ObjectValueTreeView tree;
		ScrolledWindow scrolled;
		List<string> storedVars;
		
		public Gtk.Widget Control {
			get {
				return scrolled;
			}
		}
		
		public WatchPad()
		{
			scrolled = new ScrolledWindow ();
			scrolled.HscrollbarPolicy = PolicyType.Automatic;
			scrolled.VscrollbarPolicy = PolicyType.Automatic;
			
			tree = new ObjectValueTreeView ();
			tree.AllowEditing = true;
			tree.AllowAdding = true;
			tree.HeadersVisible = true;
			tree.RulesHint = true;
			scrolled.Add (tree);
			scrolled.ShowAll ();
			
			IdeApp.Services.DebuggingService.CurrentFrameChanged += OnFrameChanged;
			IdeApp.Services.DebuggingService.PausedEvent += OnDebuggerPaused;
			IdeApp.Services.DebuggingService.ResumedEvent += OnDebuggerResumed;
		}

		public void Dispose ()
		{
			IdeApp.Services.DebuggingService.CurrentFrameChanged -= OnFrameChanged;
			IdeApp.Services.DebuggingService.PausedEvent -= OnDebuggerPaused;
			IdeApp.Services.DebuggingService.ResumedEvent -= OnDebuggerResumed;
		}

		public void Initialize (IPadWindow container)
		{
		}

		public void RedrawContent ()
		{
		}
		
		void OnFrameChanged (object s, EventArgs a)
		{
			if (IdeApp.Services.DebuggingService.CurrentFrame != null) {
				tree.Frame = IdeApp.Services.DebuggingService.CurrentFrame;
			}
		}
		
		void OnDebuggerPaused (object s, EventArgs a)
		{
			tree.Sensitive = true;
		}
		
		void OnDebuggerResumed (object s, EventArgs a)
		{
			tree.Sensitive = false;
		}
		
		#region IMementoCapable implementation 
		
		public ICustomXmlSerializer CreateMemento ()
		{
			return this;
		}
		
		public void SetMemento (ICustomXmlSerializer memento)
		{
			if (tree != null && storedVars != null)
				tree.AddExpressions (storedVars);
		}
		
		void ICustomXmlSerializer.WriteTo (System.Xml.XmlWriter writer)
		{
			if (tree != null) {
				writer.WriteStartElement ("Values");
				foreach (string name in tree.Expressions)
					writer.WriteElementString ("Value", name);
				writer.WriteEndElement ();
			}
		}
		
		ICustomXmlSerializer ICustomXmlSerializer.ReadFrom (System.Xml.XmlReader reader)
		{
			storedVars = new List<string> ();
			
			reader.MoveToContent ();
			if (reader.IsEmptyElement) {
				reader.Read ();
				return null;
			}
			reader.ReadStartElement ();
			reader.MoveToContent ();
			while (reader.NodeType != XmlNodeType.EndElement) {
				if (reader.NodeType == XmlNodeType.Element) {
					storedVars.Add (reader.ReadElementString ());
				} else
					reader.Skip ();
			}
			reader.ReadEndElement ();
			return null;
		}
		
		#endregion 
	}
}
