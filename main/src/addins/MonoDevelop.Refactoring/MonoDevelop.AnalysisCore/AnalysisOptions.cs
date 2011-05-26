// 
// AnalysisOptions.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Core;

namespace MonoDevelop.AnalysisCore
{
	public static class AnalysisOptions
	{
		const string OptionsProperty = "MonoDevelop.AnalysisCore";
		
		static Properties properties;
		
		static AnalysisOptions ()
		{
			properties = PropertyService.Get (OptionsProperty, new Properties());
			LoadProperties ();
		}
		
		static EventHandler changed;
		
		public static event EventHandler Changed {
			add {
				lock (properties) {
					if (changed == null)
						properties.PropertyChanged += HandlePropertiesChanged;
					changed += value;
				}
			}
			remove {
				lock (properties) {
					changed -= value;
					if (changed == null)
						properties.PropertyChanged -= HandlePropertiesChanged;
				}
			}
		}

		static void HandlePropertiesChanged (object sender, PropertyChangedEventArgs e)
		{
			LoadProperties ();
			changed (null, EventArgs.Empty);
		}
		
		static void LoadProperties ()
		{
			analysisEnabled = properties.Get ("AnalysisEnabled", true);
		}
		
		static bool analysisEnabled;
		
		public static bool AnalysisEnabled {
			get { return analysisEnabled; }
			set { properties.Set ("AnalysisEnabled", value); }
		}
	}
}

