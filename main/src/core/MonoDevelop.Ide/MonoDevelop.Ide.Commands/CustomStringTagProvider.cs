// CustomStringTagProvider.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.Ide.Commands
{
	// The path name should not be required here. This is a workaround to a Mono.Addins bug (fixed in the last version)
	[Mono.Addins.Extension ("/MonoDevelop.Core/TypeExtensions/MonoDevelop.Core.StringParsing.IStringTagProvider")]
	class DefaultStringTagProvider : StringTagProvider<Workbench>
	{
		public override IEnumerable<StringTagDescription> GetTags ()
		{
			yield return new StringTagDescription ("FilePath", GettextCatalog.GetString ("File Path"));
			yield return new StringTagDescription ("FileDir", GettextCatalog.GetString ("File Directory"));
			yield return new StringTagDescription ("FileName", GettextCatalog.GetString ("File Name"));
			yield return new StringTagDescription ("FileNamePrefix", GettextCatalog.GetString ("File Name Without Extension"));
			yield return new StringTagDescription ("FileExt", GettextCatalog.GetString ("File Extension"));
			yield return new StringTagDescription ("CurLine", GettextCatalog.GetString ("Cursor Line"), false);
			yield return new StringTagDescription ("CurColumn", GettextCatalog.GetString ("Cursor Column"), false);
			yield return new StringTagDescription ("CurOffset", GettextCatalog.GetString ("Cursor Offset"), false);
			yield return new StringTagDescription ("CurText", GettextCatalog.GetString ("Selected Editor Text"), false);
			yield return new StringTagDescription ("EditorText", GettextCatalog.GetString ("Editor Text"), false);
			yield return new StringTagDescription ("StartupPath", GettextCatalog.GetString ("MonoDevelop Startup Directory"), false);
			yield return new StringTagDescription ("ConfigDir", GettextCatalog.GetString ("MonoDevelop Configuration Directory"), false);
			yield return new StringTagDescription ("DataDir", GettextCatalog.GetString ("MonoDevelop User Data Directory"), false);
			yield return new StringTagDescription ("LogDir", GettextCatalog.GetString ("MonoDevelop Log Directory"), false);
		}
		
		public override object GetTagValue (Workbench wb, string tag)
		{
			switch (tag) {
				case "FILEPATH":
					if (wb.ActiveDocument != null)
						return !wb.ActiveDocument.IsFile ? String.Empty : wb.ActiveDocument.Name;
					return null;

				case "FILEDIR":
					if (wb.ActiveDocument != null)
						return !wb.ActiveDocument.IsFile ? FilePath.Empty : wb.ActiveDocument.FileName.ParentDirectory;
					return null;

				case "FILENAME":
					if (wb.ActiveDocument != null)
						return !wb.ActiveDocument.IsFile ? String.Empty : wb.ActiveDocument.FileName.FileName;
					return null;

				case "FILENAMEPREFIX":
					if (wb.ActiveDocument != null)
						return !wb.ActiveDocument.IsFile ? String.Empty : wb.ActiveDocument.FileName.FileNameWithoutExtension;
					return null;

				case "FILEEXT":
					if (wb.ActiveDocument != null)
						return !wb.ActiveDocument.IsFile ? String.Empty : wb.ActiveDocument.FileName.Extension;
					return null;
					
				case "CURLINE":
					if (wb.ActiveDocument != null && wb.ActiveDocument.Editor != null)
						return wb.ActiveDocument.Editor.CaretLocation.Line;
					return null;
					
				case "CURCOLUMN":
					if (wb.ActiveDocument != null && wb.ActiveDocument.Editor != null)
						return wb.ActiveDocument.Editor.CaretLocation.Column;
					return null;
					
				case "CUROFFSET":
					if (wb.ActiveDocument != null && wb.ActiveDocument.Editor != null)
						return wb.ActiveDocument.Editor.CaretOffset;
					return null;
					
				case "CURTEXT":
					if (wb.ActiveDocument != null && wb.ActiveDocument.Editor != null)
						return wb.ActiveDocument.Editor.SelectedText;
					return null;
					
				case "EDITORTEXT":
					if (wb.ActiveDocument != null && wb.ActiveDocument.Editor != null)
						return wb.ActiveDocument.Editor.Text;
					return null;
					
				case "STARTUPPATH":
					return AppDomain.CurrentDomain.BaseDirectory;
					
				case "CONFIGDIR":
					return UserProfile.Current.ConfigDir;
				
				case "DATADIR":
					return UserProfile.Current.UserDataRoot;
				
				case "LOGDIR":
					return UserProfile.Current.LogDir;
			}
			throw new NotSupportedException ();
        }
	}
}
