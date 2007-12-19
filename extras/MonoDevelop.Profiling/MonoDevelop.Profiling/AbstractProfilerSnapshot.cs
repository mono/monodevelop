//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
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
using System.IO;
using System.ComponentModel;

using MonoDevelop.Core;

namespace MonoDevelop.Profiling
{
	public abstract class AbstractProfilingSnapshot : IProfilingSnapshot
	{
		public event EventHandler NameChanged;
		
		protected IProfiler profiler;
		protected string filename;
		
		protected AbstractProfilingSnapshot (IProfiler profiler, string filename)
		{
			this.profiler = profiler;
			this.filename = filename;
		}
		
		public IProfiler Profiler {
			get { return profiler; }
		}

		public virtual string Name {
			get { return Path.GetFileNameWithoutExtension (filename); }
			set {
				if (value == null)
					throw new ArgumentNullException ("Name");
				
				string ext = Path.GetExtension (filename);
				string dir = Path.GetDirectoryName (filename);
				string dest = Path.Combine (dir, value + ext);

				FileService.MoveFile (filename, dest);
				filename = dest;
				
				OnNameChanged (EventArgs.Empty);
			}
		}
		
		[DefaultValue (false)]
		[Category ("Summary")]
		[DisplayName ("Filename")]
		[Description ("The location of the snapshot.")]
		[Browsable (true)]
		public string FileName {
			get { return filename; }
		}
		
		protected virtual void OnNameChanged (EventArgs args)
		{
			if (NameChanged != null)
				NameChanged (this, args);
		}
	}
}