//
// ByteBufferModel.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui.Documents;

namespace Mono.MHex.Data
{
	class ByteBufferModel: FileModel
	{
		public ByteBuffer ByteBuffer => GetRepresentation<ByteBufferModelRepresentation> ().ByteBuffer;

		public event EventHandler ByteBufferInstanceChanged;

		protected override void OnRepresentationChanged ()
		{
			base.OnRepresentationChanged ();
			ByteBufferInstanceChanged?.Invoke (this, EventArgs.Empty);
		}

		protected override Type RepresentationType => typeof (ByteBufferModelRepresentation);

		protected class ByteBufferModelRepresentation : FileModelRepresentation
		{
			ByteBuffer byteBuffer;

			internal ByteBuffer ByteBuffer => byteBuffer;

			protected override Stream OnGetContent ()
			{
				return new MemoryStream (byteBuffer.Bytes);
			}

			protected override async Task OnLoad ()
			{
				byteBuffer = new ByteBuffer ();
				byteBuffer.Replaced += ByteBuffer_Replaced;
				using (var stream = File.OpenRead (FilePath)) {
					byteBuffer.Buffer = await ArrayBuffer.LoadAsync (stream);
				}
			}

			protected override void OnCreateNew ()
			{
				byteBuffer = new ByteBuffer ();
				byteBuffer.Replaced += ByteBuffer_Replaced;
				byteBuffer.Buffer = new ArrayBuffer (new byte [0]);
			}

			protected override Task OnSave ()
			{
				File.WriteAllBytes (FilePath, byteBuffer.Bytes);
				return Task.CompletedTask;
			}

			protected override async Task OnSetContent (Stream content)
			{
				byteBuffer.Buffer = await ArrayBuffer.LoadAsync (content);
			}

			void ByteBuffer_Replaced (object sender, ReplaceEventArgs e)
			{
				NotifyChanged ();
			}
		}
	}

	public class UndoOperationEventArgs : EventArgs
	{
		public long BufferOffset {
			get;
			private set;
		}

		public UndoOperationEventArgs (long offset)
		{
			this.BufferOffset = offset;
		}
	}
}
