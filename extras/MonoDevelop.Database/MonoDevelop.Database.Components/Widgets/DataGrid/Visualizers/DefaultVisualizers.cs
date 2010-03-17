//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
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

using Gtk;
using System;
using System.Data;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Database.Components
{
	public class ImageVisualizer : IDataGridVisualizer
	{
		public string Name {
			get { return AddinCatalog.GetString ("Image"); }
		}
		
		public bool CanVisualize (object dataObject)
		{
			if (dataObject == null)
				return false;
			
			return dataObject.GetType () == typeof (byte[]);
		}
		
		public void Visualize (object dataObject)
		{
			ImageVisualizerView view = new ImageVisualizerView ();
			view.Load (dataObject);
			IdeApp.Workbench.OpenDocument (view, true);
		}
	}
	
	public class TextVisualizer : IDataGridVisualizer
	{
		public string Name {
			get { return AddinCatalog.GetString ("Text"); }
		}
		
		public bool CanVisualize (object dataObject)
		{
			if (dataObject == null)
				return false;
			
			Type type = dataObject.GetType ();
			return type == typeof (string) || type == typeof (byte[]) || type.IsPrimitive;
		}
		
		public void Visualize (object dataObject)
		{
			TextVisualizerView view = new TextVisualizerView ();
			view.Load (dataObject);
			IdeApp.Workbench.OpenDocument (view, true);
		}
	}
}