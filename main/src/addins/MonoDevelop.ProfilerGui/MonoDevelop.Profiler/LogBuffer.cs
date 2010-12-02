// 
// LogBuffer.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Threading;

namespace MonoDevelop.Profiler
{
	public class LogBuffer : IDisposable
	{
		public Header Header { get; private set; }
		public List<long> bufferPositions = new List<long> ();
		
		FileStream stream;
		BinaryReader reader;
		Thread thread;
		bool running = true;
		object fileLock = new object ();
		string fileName;
		
		public string FileName {
			get {
				return this.fileName;
			}
		}
		
		public EventVisitor Visitor {
			get;
			private set;
		}
		
		public LogBuffer (string fileName, EventVisitor visitor)
		{
			this.fileName = fileName;
			this.Visitor = visitor;
			
	//		thread = new Thread (delegate () {
			
				long position = 0;
//				while (running) {
					bool shouldUpdate = false;
//					lock (fileLock) {
						do {
							try {
								stream = new FileStream (fileName, FileMode.Open, FileSystemRights.Read, FileShare.Read, 1024, FileOptions.RandomAccess);
							} catch (Exception) {
						//		Thread.Sleep (100);
							}
						} while (running && stream == null);
						if (!running)
							return;
						reader = new BinaryReader (stream);
						
						if (this.Header == null) {
								try {
									stream.Position = 0;
									this.Header = Header.Read (reader);
									position = reader.BaseStream.Position;
								} catch (Exception e) {
									System.Console.WriteLine (e);
									Thread.Sleep (200);
//									continue;
								}
						}
						stream.Position = position;
						while (stream.Position < stream.Length) {
							try {
								var buffer = Buffer.Read (reader);
								bufferPositions.Add (position);
								position = stream.Position;
								if (Visitor != null) {
									Visitor.CurrentBuffer = buffer;
									buffer.Events.ForEach (e => e.Accept (Visitor));
								}
								shouldUpdate = true;
							} catch (Exception e) {
								System.Console.WriteLine (e);
								Thread.Sleep (200);
								continue;
							}
						}
//						reader.Close ();
//						stream.Close ();
//					}
					if (shouldUpdate)
						OnUpdated (EventArgs.Empty);
//					Thread.Sleep (1000);
//				}
//			});
//			thread.IsBackground = true;
//			thread.Start ();
		}
		
		public int BufferCount {
			get {
				return bufferPositions.Count;
			}
		}
		
		public Buffer ReadBuffer (int bufferNumber)
		{
			stream.Position = bufferPositions [bufferNumber];
			var result = Buffer.Read (reader);
			return result;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			if (running) {
				running = false;
				thread.Join ();
			}
		}
		#endregion
		
		protected virtual void OnUpdated (EventArgs e)
		{
			EventHandler handler = this.Updated;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler Updated;
	}
}

