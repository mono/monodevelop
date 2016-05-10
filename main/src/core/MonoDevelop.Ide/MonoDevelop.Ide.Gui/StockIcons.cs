// Stock.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Ide.Gui
{
	public class Stock
	{
		public static readonly IconId AssetsFolder = "md-folder-assets";
		public static readonly IconId AddNamespace = "md-add-namespace";
		public static readonly IconId BreakPoint = "md-break-point";
		public static readonly IconId BuildCombine = "md-build-combine";
		public static readonly IconId Class = "md-class";
		public static readonly IconId ClearAllBookmarks = "md-bookmark-clear-all";
		public static readonly IconId CloseAllDocuments = "md-close-all-documents";
		public static readonly IconId CloseCombine = "md-close-combine-icon";
		public static readonly IconId CloseIcon = Gtk.Stock.Close;
		public static readonly IconId ClosedFolder = "md-closed-folder";
		public static readonly IconId ClosedReferenceFolder = "md-closed-reference-folder";
		public static readonly IconId ClosedResourceFolder = "md-closed-resource-folder";
		public static readonly IconId Solution = "md-solution";
		public static readonly IconId Workspace = "md-workspace";
		public static readonly IconId CopyIcon = Gtk.Stock.Copy;
		public static readonly IconId CutIcon = Gtk.Stock.Cut;
		public static readonly IconId Delegate = "md-delegate";
		public static readonly IconId DeleteIcon = Gtk.Stock.Delete;
		public static readonly IconId Empty = "md-empty";
		public static readonly IconId EmptyFileIcon = "md-empty-file-icon";
		public static readonly IconId Enum = "md-enum";
		public static readonly IconId Error = "md-error";
		public static readonly IconId Event = "md-event";
		public static readonly IconId Field = "md-field";
		public static readonly IconId FileXmlIcon = "md-file-xml-icon";
		public static readonly IconId FindIcon = "md-magnify";
		public static readonly IconId FindNextIcon = "md-find-next";
		public static readonly IconId FindPrevIcon = "md-find-prev";
		public static readonly IconId FullScreen = Gtk.Stock.Fullscreen;
		public static readonly IconId GotoNextbookmark = "md-bookmark-next";
		public static readonly IconId GotoPrevbookmark = "md-bookmark-prev";
		public static readonly IconId Information = "md-information";
		public static readonly IconId Interface = "md-interface";
		public static readonly IconId InternalClass = "md-internal-class";
		public static readonly IconId InternalDelegate = "md-internal-delegate";
		public static readonly IconId InternalEnum = "md-internal-enum";
		public static readonly IconId InternalEvent = "md-internal-event";
		public static readonly IconId InternalField = "md-internal-field";
		public static readonly IconId InternalInterface = "md-internal-interface";
		public static readonly IconId InternalMethod = "md-internal-method";
		public static readonly IconId InternalProperty = "md-internal-property";
		public static readonly IconId InternalStruct = "md-internal-struct";
		public static readonly IconId Literal = "md-literal";
		public static readonly IconId Method = "md-method";
		public static readonly IconId GenericFile = "md-regular-file";
		public static readonly IconId NameSpace = "md-name-space";
		public static readonly IconId NewDocumentIcon = Gtk.Stock.New;
		public static readonly IconId NextWindowIcon = Gtk.Stock.GoForward;
		public static readonly IconId OpenFileIcon = Gtk.Stock.Open;
		public static readonly IconId OpenFolder = "md-open-folder";
		public static readonly IconId OpenReferenceFolder = "md-open-reference-folder";
		public static readonly IconId OpenResourceFolder = "md-open-resource-folder";
		public static readonly IconId Options = "md-preferences";
		public static readonly IconId OutputIcon = "md-output-icon";
		public static readonly IconId PasteIcon = Gtk.Stock.Paste;
		public static readonly IconId PreView = Gtk.Stock.PrintPreview;
		public static readonly IconId PrevWindowIcon = Gtk.Stock.GoBack;
		public static readonly IconId Print = Gtk.Stock.Print;
		public static readonly IconId PrivateClass = "md-private-class";
		public static readonly IconId PrivateDelegate = "md-private-delegate";
		public static readonly IconId PrivateEnum = "md-private-enum";
		public static readonly IconId PrivateEvent = "md-private-event";
		public static readonly IconId PrivateField = "md-private-field";
		public static readonly IconId PrivateInterface = "md-private-interface";
		public static readonly IconId PrivateMethod = "md-private-method";
		public static readonly IconId PrivateProperty = "md-private-property";
		public static readonly IconId PrivateStruct = "md-private-struct";
		public static readonly IconId Property = "md-property";
		public static readonly IconId Properties = "md-preferences";
		public static readonly IconId ProtectedClass = "md-protected-class";
		public static readonly IconId ProtectedDelegate = "md-protected-delegate";
		public static readonly IconId ProtectedEnum = "md-protected-enum";
		public static readonly IconId ProtectedEvent = "md-protected-event";
		public static readonly IconId ProtectedField = "md-protected-field";
		public static readonly IconId ProtectedInterface = "md-protected-interface";
		public static readonly IconId ProtectedMethod = "md-protected-method";
		public static readonly IconId ProtectedProperty = "md-protected-property";
		public static readonly IconId ProtectedStruct = "md-protected-struct";
		public static readonly IconId PinDown = "md-pin-down";
		public static readonly IconId PinUp = "md-pin-up";
		public static readonly IconId Question = Gtk.Stock.DialogQuestion;
		public static readonly IconId QuitIcon = Gtk.Stock.Quit;
		public static readonly IconId RedoIcon = Gtk.Stock.Redo;
		public static readonly IconId Reference = "md-reference";
		public static readonly IconId ReferenceWarning = "md-reference-warning";
		public static readonly IconId Region = "md-region";
		public static readonly IconId ReplaceIcon = Gtk.Stock.FindAndReplace;
		public static readonly IconId ResourceFileIcon = "md-resource-file-icon";
		public static readonly IconId Console = "md-console";
		public static readonly IconId RunProgramIcon = Gtk.Stock.Execute;
		public static readonly IconId SaveAllIcon = "md-save-all";
		public static readonly IconId SaveAsIcon = Gtk.Stock.SaveAs;
		public static readonly IconId SaveIcon = Gtk.Stock.Save;
		public static readonly IconId MonoDevelop = "md-monodevelop";
		public static readonly IconId Project = "md-project";
		public static readonly IconId Struct = "md-struct";
		public static readonly IconId TaskListIcon = "md-task-list";
		public static readonly IconId TextFileIcon = "md-text-file-icon";
		public static readonly IconId ToggleBookmark = "md-bookmark-toggle";
		public static readonly IconId UndoIcon = Gtk.Stock.Undo;
		public static readonly IconId Warning = "md-warning";
		public static readonly IconId XmlFileIcon = "md-xml-file-icon";
		public static readonly IconId SolutionFolderOpen = "md-solution-folder-open";
		public static readonly IconId SolutionFolderClosed = "md-solution-folder-closed";
		public static readonly IconId Package = "md-package";
		public static readonly IconId StatusSolutionOperation = "md-status-open";
		public static readonly IconId StatusDownload = "md-status-download";
		public static readonly IconId StatusUpload = "md-status-upload";
		public static readonly IconId StatusSearch = "md-status-search";
		public static readonly IconId StatusBuild = "md-status-build";
		public static readonly IconId StatusSteady = BrandingService.StatusSteadyIconId;
		public static readonly IconId StatusSuccess = "md-status-success";
		public static readonly IconId StatusWarning = "md-status-warning";
		public static readonly IconId StatusError = "md-status-error";
		public static readonly IconId StatusConnecting = "md-status-connecting";
		public static readonly IconId StatusDeviceDeploying = "md-status-device-deploying";
		public static readonly IconId StatusWorking = "md-status-waiting";
		public static readonly IconId StatusUpdatesDownloading = "md-status-updates-downloading";
		public static readonly IconId StatusUpdatesPaused = "md-status-updates-paused";
		public static readonly IconId StatusUpdatesReady = "md-status-updates-ready";
		public static readonly IconId StatusInstrumentation = "md-status-instrumentation";
		public static readonly IconId Broom = "md-clear";
		public static readonly IconId Stop = "md-stop";
		public static readonly IconId MessageLog = "md-message-log";
		public static readonly IconId SortAlphabetically = "md-sort-alphabetically";
		public static readonly IconId GroupByCategory = "md-group-by-category";
		public static readonly IconId Help = "md-help";
		public static readonly IconId Add = "md-add";
		public static readonly IconId Clear = "md-clear";
		public static readonly IconId Execute = Gtk.Stock.Execute;
		public static readonly IconId SearchboxSearch = "md-searchbox-search";
		public static readonly IconId Updates = "md-updates";
		public static readonly IconId PadDownload = "md-pad-download";
		public static readonly IconId PadUpload = "md-pad-upload";
		public static readonly IconId PadDeviceDeployment = "md-pad-device-deployment";
	}
}
