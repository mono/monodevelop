//
// HexEditorVisualizer.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using System.Text;

using Gtk;

using Mono.Debugging.Client;
using MonoDevelop.Debugger;
using MonoDevelop.Core;

using Mono.MHex.Data;

namespace MonoDevelop.HexEditor
{
	public class HexEditorVisualizer : ValueVisualizer
	{
		Mono.MHex.HexEditor hexEditor;

		public HexEditorVisualizer ()
		{
		}

		#region IValueVisualizer implementation

		public override string Name {
			get { return GettextCatalog.GetString ("HexEdit"); }
		}

		public override bool CanVisualize (ObjectValue val)
		{
			switch (val.TypeName) {
			case "sbyte[]": return true;
			case "byte[]": return true;
			case "char[]": return true;
			case "string": return true;
			default: return false;
			}
		}

		public override bool IsDefaultVisualizer (ObjectValue val)
		{
			switch (val.TypeName) {
			case "sbyte[]":
			case "byte[]": return true;
			case "char[]": 
			case "string": return false;
			default: return false;
			}
		}

		void SetHexEditorOptions ()
		{
			hexEditor.Options.FontName = MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.FontName;
			hexEditor.PurgeLayoutCaches ();
			hexEditor.Repaint ();
		}

		public override Widget GetVisualizerWidget (ObjectValue val)
		{
			hexEditor = new Mono.MHex.HexEditor ();

			IBuffer buffer = null;

			if (val.TypeName != "string") {
				var raw = (RawValueArray) val.GetRawValue ();

				switch (val.TypeName) {
				case "sbyte[]":
					buffer = new RawSByteArrayBuffer (raw);
					break;
				case "char[]":
					buffer = new RawCharArrayBuffer (raw);
					break;
				case "byte[]":
					buffer = new RawByteArrayBuffer (raw);
					break;
				}
			} else {
				var ops = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
				ops.ChunkRawStrings = true;

				buffer = new RawStringBuffer ((RawValueString) val.GetRawValue (ops));
			}

			hexEditor.HexEditorData.Buffer = buffer;
			hexEditor.Sensitive = CanEdit (val);

			var xwtScrollView = new Xwt.ScrollView (hexEditor);
			var scrollWidget = (Widget) Xwt.Toolkit.CurrentEngine.GetNativeWidget (xwtScrollView);
			SetHexEditorOptions ();
			hexEditor.SetFocus ();

			return scrollWidget;
		}

		public override bool StoreValue (ObjectValue val)
		{
			switch (val.TypeName) {
			case "byte[]":
				// HACK: make sure to load the full byte stream...
				long length = hexEditor.HexEditorData.Length;
				hexEditor.HexEditorData.GetBytes (length - 1, 1);

				val.SetRawValue (hexEditor.HexEditorData.Bytes);
				return true;
			default:
				return false;
			}
		}

		public override bool CanEdit (ObjectValue val)
		{
			switch (val.TypeName) {
			case "byte[]": return true;
			default: return false;
			}
		}

		#endregion
	}

	class RawStringBuffer : Mono.MHex.Data.IBuffer
	{
		readonly RawValueString array;
		long offset;

		public RawStringBuffer (RawValueString raw)
		{
			Bytes = new byte[raw.Length * 2];
			Length = raw.Length * 2;
			array = raw;
			offset = 0;
		}

		#region IBuffer implementation

		public long Length {
			get; private set;
		}

		public byte[] Bytes {
			get; private set;
		}

		public byte[] GetBytes (long index, int count)
		{
			while (index < 0 && count > 0) {
				index++;
				count--;
			}

			if (count == 0)
				return new byte[0];

			count = (int) Math.Min (Length - index, count);
			var bytes = new byte[count];
			int i = 0;

			while (index + i < offset && i < count) {
				bytes[i] = Bytes[index + i];
				i++;
			}

			if (i < count) {
				var chunk = array.Substring ((int) offset / 2, ((count + 1) - i) / 2);
				var buf = Encoding.Unicode.GetBytes (chunk);

				for (int j = 0; j < buf.Length; j++)
					Bytes[offset++] = buf[j];

				while (index + i < offset && i < count) {
					bytes[i] = Bytes[index + i];
					i++;
				}
			}

			return bytes;
		}

		#endregion
	}

	abstract class RawArrayBuffer : Mono.MHex.Data.IBuffer
	{
		readonly RawValueArray array;
		protected long Offset;

		protected RawArrayBuffer (RawValueArray raw, int multiplier)
		{
			Bytes = new byte[raw.Length * multiplier];
			Length = raw.Length;
			array = raw;
			Offset = 0;
		}

		protected abstract void AppendBytes (Array values);

		#region IBuffer implementation

		public long Length {
			get; private set;
		}

		public byte[] Bytes {
			get; private set;
		}

		public byte[] GetBytes (long index, int count)
		{
			while (index < 0 && count > 0) {
				index++;
				count--;
			}

			if (count == 0)
				return new byte[0];

			count = (int) Math.Min (Length - index, count);
			var bytes = new byte[count];
			int i = 0;

			while (index + i < Offset && i < count) {
				bytes[i] = Bytes[index + i];
				i++;
			}

			if (i < count) {
				var chunk = array.GetValues ((int) Offset, (count - i));
				AppendBytes (chunk);

				while (index + i < Offset && i < count) {
					bytes[i] = Bytes[index + i];
					i++;
				}
			}

			return bytes;
		}

		#endregion
	}

	class RawByteArrayBuffer : RawArrayBuffer
	{
		public RawByteArrayBuffer (RawValueArray raw) : base (raw, 1)
		{
		}

		protected override void AppendBytes (Array values)
		{
			var bytes = (byte[]) values;

			for (int i = 0; i < bytes.Length; i++)
				Bytes[Offset++] = bytes[i];
		}
	}

	class RawSByteArrayBuffer : RawArrayBuffer
	{
		public RawSByteArrayBuffer (RawValueArray raw) : base (raw, 1)
		{
		}

		protected override void AppendBytes (Array values)
		{
			var bytes = (sbyte[]) values;

			for (int i = 0; i < bytes.Length; i++)
				Bytes[Offset++] = (byte) bytes[i];
		}
	}

	class RawCharArrayBuffer : RawArrayBuffer
	{
		readonly Encoder encoder;

		public RawCharArrayBuffer (RawValueArray raw) : base (raw, 2)
		{
			encoder = Encoding.Unicode.GetEncoder ();
		}

		protected override void AppendBytes (Array values)
		{
			var chars = (char[]) values;
			int n = encoder.GetBytes (chars, 0, chars.Length, Bytes, (int) Offset, Offset + (chars.Length * 2) == Length);
			Offset += n;
		}
	}
}
