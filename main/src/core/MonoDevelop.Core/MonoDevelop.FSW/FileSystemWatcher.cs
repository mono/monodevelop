//
// FileSystemWatcher.cs
//
// Author:
//       ludovic <ludovic.henry@xamarin.com>
//
// Copyright (c) 2017 ludovic
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
using System.Runtime.InteropServices;

namespace MonoDevelop.FSW
{
	internal class FileSystemWatcher : System.ComponentModel.Component, System.ComponentModel.ISupportInitialize
	{
		static Platform _platform;

		enum Platform
		{
			OSX,
			Mono,
		}

		[DllImport (OSX.Interop.Libraries.SystemNative, EntryPoint = "SystemNative_HasOSXSupport")]
		private static extern bool HasOSXSupport ();

		static FileSystemWatcher ()
		{
			try {
				if (Core.Platform.IsMac && HasOSXSupport ()) {
					_platform = Platform.OSX;
					return;
				}
			} catch (EntryPointNotFoundException) {
			}

			_platform = Platform.Mono;
		}

		OSX.FileSystemWatcher _osxFsw;
		Mono.FileSystemWatcher _monoFsw;

		public FileSystemWatcher ()
		{
			switch (_platform) {
			case Platform.Mono:
				_monoFsw = new Mono.FileSystemWatcher ();
				break;
			case Platform.OSX:
				_osxFsw = new OSX.FileSystemWatcher ();
				break;
			default:
				throw new NotImplementedException ();
			}
		}

		public FileSystemWatcher (string path)
		{
			switch (_platform) {
			case Platform.Mono:
				_monoFsw = new Mono.FileSystemWatcher (path);
				break;
			case Platform.OSX:
				_osxFsw = new OSX.FileSystemWatcher (path);
				break;
			default:
				throw new NotImplementedException ();
			}
		}

		public FileSystemWatcher (string path, string filter)
		{
			switch (_platform) {
			case Platform.Mono:
				_monoFsw = new Mono.FileSystemWatcher (path, filter);
				break;
			case Platform.OSX:
				_osxFsw = new OSX.FileSystemWatcher (path, filter);
				break;
			default: throw new NotImplementedException ();
			}
		}

		public bool EnableRaisingEvents {
			get {
				switch (_platform) {
				case Platform.Mono:
					return _monoFsw.EnableRaisingEvents;
				case Platform.OSX:
					return _osxFsw.EnableRaisingEvents;
				default:
					throw new NotImplementedException ();
				}
			}
			set {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.EnableRaisingEvents = value;
					break;
				case Platform.OSX:
					_osxFsw.EnableRaisingEvents = value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}
		public string Filter {
			get {
				switch (_platform) {
				case Platform.Mono:
					return _monoFsw.Filter;
				case Platform.OSX:
					return _osxFsw.Filter;
				default:
					throw new NotImplementedException ();
				}
			}
			set {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.Filter = value;
					break;
				case Platform.OSX:
					_osxFsw.Filter = value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public bool IncludeSubdirectories {
			get {
				switch (_platform) {
				case Platform.Mono:
					return _monoFsw.IncludeSubdirectories;
				case Platform.OSX:
					return _osxFsw.IncludeSubdirectories;
				default:
					throw new NotImplementedException ();
				}
			}
			set {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.IncludeSubdirectories = value;
					break;
				case Platform.OSX:
					_osxFsw.IncludeSubdirectories = value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public int InternalBufferSize {
			get {
				switch (_platform) {
				case Platform.Mono:
					return _monoFsw.InternalBufferSize;
				case Platform.OSX:
					return _osxFsw.InternalBufferSize;
				default:
					throw new NotImplementedException ();
				}
			}
			set {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.InternalBufferSize = value;
					break;
				case Platform.OSX:
					_osxFsw.InternalBufferSize = value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public System.IO.NotifyFilters NotifyFilter {
			get {
				switch (_platform) {
				case Platform.Mono:
					return _monoFsw.NotifyFilter;
				case Platform.OSX:
					return _osxFsw.NotifyFilter;
				default:
					throw new NotImplementedException ();
				}
			}
			set {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.NotifyFilter = value;
					break;
				case Platform.OSX:
					_osxFsw.NotifyFilter = value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public string Path {
			get {
				switch (_platform) {
				case Platform.Mono:
					return _monoFsw.Path;
				case Platform.OSX:
					return _osxFsw.Path;
				default:
					throw new NotImplementedException ();
				}
			}
			set {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.Path = value;
					break;
				case Platform.OSX:
					_osxFsw.Path = value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public event System.IO.FileSystemEventHandler Changed {
			add {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.Changed += value;
					break;
				case Platform.OSX:
					_osxFsw.Changed += value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
			remove {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.Changed -= value;
					break;
				case Platform.OSX:
					_osxFsw.Changed -= value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public event System.IO.FileSystemEventHandler Created {
			add {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.Created += value;
					break;
				case Platform.OSX:
					_osxFsw.Created += value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
			remove {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.Created -= value;
					break;
				case Platform.OSX:
					_osxFsw.Created -= value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public event System.IO.FileSystemEventHandler Deleted {
			add {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.Deleted += value;
					break;
				case Platform.OSX:
					_osxFsw.Deleted += value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
			remove {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.Deleted -= value;
					break;
				case Platform.OSX:
					_osxFsw.Deleted -= value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public event System.IO.ErrorEventHandler Error {
			add {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.Error += value;
					break;
				case Platform.OSX:
					_osxFsw.Error += value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
			remove {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.Error -= value;
					break;
				case Platform.OSX:
					_osxFsw.Error -= value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public event System.IO.RenamedEventHandler Renamed {
			add {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.Renamed += value;
					break;
				case Platform.OSX:
					_osxFsw.Renamed += value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
			remove {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.Renamed -= value;
					break;
				case Platform.OSX:
					_osxFsw.Renamed -= value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		protected internal void OnChanged (System.IO.FileSystemEventArgs e)
		{
			switch (_platform) {
			case Platform.Mono:
				_monoFsw.OnChanged (e);
				break;
			case Platform.OSX:
				_osxFsw.OnChanged (e);
				break;
			default:
				throw new NotImplementedException ();
			}
		}

		protected internal void OnCreated (System.IO.FileSystemEventArgs e)
		{
			switch (_platform) {
			case Platform.Mono:
				_monoFsw.OnChanged (e);
				break;
			case Platform.OSX:
				_osxFsw.OnChanged (e);
				break;
			default:
				throw new NotImplementedException ();
			}
		}

		protected internal void OnDeleted (System.IO.FileSystemEventArgs e)
		{
			switch (_platform) {
			case Platform.Mono:
				_monoFsw.OnDeleted (e);
				break;
			case Platform.OSX:
				_osxFsw.OnDeleted (e);
				break;
			default:
				throw new NotImplementedException ();
			}
		}

		protected internal void OnError (System.IO.ErrorEventArgs e)
		{
			switch (_platform) {
			case Platform.Mono:
				_monoFsw.OnError (e);
				break;
			case Platform.OSX:
				_osxFsw.OnError (e);
				break;
			default:
				throw new NotImplementedException ();
			}
		}

		protected internal void OnRenamed (System.IO.RenamedEventArgs e)
		{
			switch (_platform) {
			case Platform.Mono:
				_monoFsw.OnRenamed (e);
				break;
			case Platform.OSX:
				_osxFsw.OnRenamed (e);
				break;
			default:
				throw new NotImplementedException ();
			}
		}

		public System.IO.WaitForChangedResult WaitForChanged (System.IO.WatcherChangeTypes changeType)
		{
			switch (_platform) {
			case Platform.Mono:
				return _monoFsw.WaitForChanged (changeType);
			case Platform.OSX:
				return _osxFsw.WaitForChanged (changeType);
			default:
				throw new NotImplementedException ();
			}
		}

		public System.IO.WaitForChangedResult WaitForChanged (System.IO.WatcherChangeTypes changeType, int timeout)
		{
			switch (_platform) {
			case Platform.Mono:
				return _monoFsw.WaitForChanged (changeType, timeout);
			case Platform.OSX:
				return _osxFsw.WaitForChanged (changeType, timeout);
			default:
				throw new NotImplementedException ();
			}
		}

		public override System.ComponentModel.ISite Site {
			get {
				switch (_platform) {
				case Platform.Mono:
					return _monoFsw.Site;
				case Platform.OSX:
					return _osxFsw.Site;
				default:
					throw new NotImplementedException ();
				}
			}
			set {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.Site = value;
					break;
				case Platform.OSX:
					_osxFsw.Site = value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public System.ComponentModel.ISynchronizeInvoke SynchronizingObject {
			get {
				switch (_platform) {
				case Platform.Mono:
					return _monoFsw.SynchronizingObject;
				case Platform.OSX:
					return _osxFsw.SynchronizingObject;
				default:
					throw new NotImplementedException ();
				}
			}
			set {
				switch (_platform) {
				case Platform.Mono:
					_monoFsw.SynchronizingObject = value;
					break;
				case Platform.OSX:
					_osxFsw.SynchronizingObject = value;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public void BeginInit ()
		{
			switch (_platform) {
			case Platform.Mono:
				_monoFsw.BeginInit ();
				break;
			case Platform.OSX:
				_osxFsw.BeginInit ();
				break;
			default:
				throw new NotImplementedException ();
			}
		}

		protected override void Dispose (bool disposing)
		{
			switch (_platform) {
			case Platform.Mono:
				_monoFsw.Dispose (disposing);
				break;
			case Platform.OSX:
				_osxFsw.Dispose (disposing);
				break;
			default:
				throw new NotImplementedException ();
			}
		}

		public void EndInit ()
		{
			switch (_platform) {
			case Platform.Mono:
				_monoFsw.EndInit ();
				break;
			case Platform.OSX:
				_osxFsw.EndInit ();
				break;
			default:
				throw new NotImplementedException ();
			}
		}
	}
}
