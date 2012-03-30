
using System;
using System.Collections.Generic;
using System.Text;
namespace Mono.TextEditor
{
	/// <summary>
	/// Simple implementation of the buffer interface to support fast read-only documents.
	/// </summary>
	public class StringBuffer : IBuffer
	{
		string buffer;

		public StringBuffer (string buffer)
		{
			this.buffer = buffer;
		}

		#region IBuffer Members
		int IBuffer.Length {
			get { return buffer.Length; }
		}

		string IBuffer.Text {
			get { return buffer; }
			set { buffer = value; }
		}

		void IBuffer.Replace (int offset, int count, string value)
		{
			throw new NotSupportedException ("Operation not supported on this buffer.");
		}

		string IBuffer.GetTextAt (int offset, int count)
		{
			return buffer.Substring (offset, count);
		}

		char IBuffer.GetCharAt (int offset)
		{
			return buffer[offset];
		}

		IEnumerable<int> IBuffer.SearchForward (string pattern, int startIndex)
		{
			throw new NotImplementedException();
		}

		IEnumerable<int> IBuffer.SearchForwardIgnoreCase (string pattern, int startIndex)
		{
			throw new NotImplementedException();
		}

		IEnumerable<int> IBuffer.SearchBackward (string pattern, int startIndex)
		{
			throw new NotImplementedException();
		}

		IEnumerable<int> IBuffer.SearchBackwardIgnoreCase (string pattern, int startIndex)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
