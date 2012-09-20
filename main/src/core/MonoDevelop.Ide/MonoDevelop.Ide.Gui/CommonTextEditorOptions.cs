// 
// CommonTextEditorOptions.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// Text editor options that subscribe to MonoDevelop common settings.
	/// </summary>
	public class CommonTextEditorOptions : Mono.TextEditor.TextEditorOptions
	{
		bool disposed = false;
		
		public CommonTextEditorOptions ()
		{
			PropertyService.PropertyChanged += PropertyServiceChanged;
			base.FontName = PropertyService.Get ("FontName", MonoDevelop.Ide.DesktopService.DefaultMonospaceFont);
			base.ColorScheme = IdeApp.Preferences.ColorScheme;
			base.UseAntiAliasing = PropertyService.Get ("UseAntiAliasing", true);
			FontService.RegisterFontChangedCallback ("Editor", UpdateFont);
		}
		
		public override void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			PropertyService.PropertyChanged -= PropertyServiceChanged;
			FontService.RemoveCallback (UpdateFont);
		}
		
		void UpdateFont ()
		{
			base.FontName = FontName;
			this.OnChanged (EventArgs.Empty);
		}
		
		void PropertyServiceChanged (object sender, PropertyChangedEventArgs e)
		{
			switch (e.Key) {
			case "ColorScheme": {
				string val = (string) e.NewValue;
				if (string.IsNullOrEmpty (val))
					val = "Default";
				base.ColorScheme = val;
				break;
			}
			case "UseAntiAliasing":
				base.UseAntiAliasing = (bool) e.NewValue;
				break;
			}
		}
		
		public override bool UseAntiAliasing {
			set { throw new InvalidOperationException ("Set via global source editor options"); }
		}
		
		public override string ColorScheme {
			set { throw new InvalidOperationException ("Set via global source editor options"); }
		}
		
		public override string FontName {
			get {
				return FontService.FilterFontName (FontService.GetUnderlyingFontName ("Editor"));
			}
			set {
				throw new InvalidOperationException ("Set via global source editor options");
			}
		}
	}
}