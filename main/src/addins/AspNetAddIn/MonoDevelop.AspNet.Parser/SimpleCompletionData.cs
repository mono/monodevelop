// 
// SimpleCompletionData.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.AspNet.Parser
{
	
	public class SimpleCompletionData : ICompletionData
	{
		public SimpleCompletionData (string text) : this (text, null, null) {}
		public SimpleCompletionData (string text, string image) : this (text, image, null) {}
		
		public SimpleCompletionData (string text, string image, string description)
		{
			this.CompletionString = text;
			this.Image = image;
			this.Description = description;
		}
		
		public string Image { get; set; }
		public string[] Text { get { return new string[] { CompletionString }; } }
		public string Description { get; set; }
		public string CompletionString { get; set; }

		public virtual int CompareTo (ICompletionData x)
		{
			return String.Compare (Text[0], x.Text[0], true);
		}		
	}
	
	public class SimpleCompletionDataWithMarkup : SimpleCompletionData, ICompletionDataWithMarkup
	{
		public SimpleCompletionDataWithMarkup (string text) : this (text, null, null, null) {}
		public SimpleCompletionDataWithMarkup (string text, string image) : this (text, image, null, null) {}
		public SimpleCompletionDataWithMarkup (string text, string image, string description)
			: this (text, image, description, null) {}
		
		public SimpleCompletionDataWithMarkup (string text, string image, string description, string descMarkup)
			: base (text, image, description)
		{
			this.DescriptionPango = descMarkup;
		}
		
		public string DescriptionPango { get; set; }
	}
	
	
	public class SimpleCompletionDataProvider : ICompletionDataProvider
	{
		public ICompletionData[] Data { get; set; }
		
		ICompletionData[] ICompletionDataProvider.GenerateCompletionData (ICompletionWidget widget, char charTyped)
		{
			return Data;
		}
		
		void IDisposable.Dispose ()
		{
		}
		
		public string DefaultCompletionString { get; set; }
		
		public SimpleCompletionDataProvider (ICompletionData [] data, string defaultVal)
		{
			
		}
		
		public bool AutoCompleteUniqueMatch {
			get { return false; }
		}
	}
	
	public class LazyCompletionDataProvider : ICompletionDataProvider
	{
		public Func<ICompletionWidget, char, IEnumerable<ICompletionData>> Func { get; set; }
		public string DefaultCompletionString { get; set; }
		
		public LazyCompletionDataProvider (Func<ICompletionWidget, char, IEnumerable<ICompletionData>> func, string defaultVal)
		{
			this.Func = func;
			this.DefaultCompletionString = defaultVal;
		}
		
		public LazyCompletionDataProvider (Func<ICompletionWidget, char, IEnumerable<ICompletionData>> func)
			: this (func, null)
		{
		}
		
		public LazyCompletionDataProvider (Func<IEnumerable<ICompletionData>> func, string defaultVal)
			: this ((ICompletionWidget x, char y) => func (), defaultVal)
		{
		}
		
		public LazyCompletionDataProvider (Func<IEnumerable<ICompletionData>> func)
			: this (func, null)
		{
		}
		
		ICompletionData[] ICompletionDataProvider.GenerateCompletionData (ICompletionWidget widget, char charTyped)
		{
			return Func (widget, charTyped).ToArray ();
		}
		
		void IDisposable.Dispose ()
		{
		}
		
		public bool AutoCompleteUniqueMatch {
			get { return false; }
		}
	}
}
