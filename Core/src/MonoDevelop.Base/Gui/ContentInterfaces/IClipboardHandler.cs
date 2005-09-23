// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using MonoDevelop.Internal.Undo;

namespace MonoDevelop.Gui
{
	public interface IClipboardHandler
	{
		bool EnableCut {
			get;
		}
		bool EnableCopy {
			get;
		}
		bool EnablePaste {
			get;
		}
		bool EnableDelete {
			get;
		}
		bool EnableSelectAll {
			get;
		}
		
		void Cut(object sender, EventArgs e);
		void Copy(object sender, EventArgs e);
		void Paste(object sender, EventArgs e);
		void Delete(object sender, EventArgs e);
		void SelectAll(object sender, EventArgs e);
	}
}
