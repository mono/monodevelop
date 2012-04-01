
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
		int IBuffer.TextLength {
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

		int IBuffer.IndexOf (char c, int startIndex, int count)
		{
			return buffer.IndexOf (c, startIndex, count);
		}

		int IBuffer.IndexOfAny (char[] anyOf, int startIndex, int count)
		{
			return buffer.IndexOfAny (anyOf, startIndex, count);
		}

		public int IndexOf (string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			return buffer.IndexOf (searchText, startIndex, count, comparisonType);
		}

		int IBuffer.LastIndexOf (char c, int startIndex, int count)
		{
			return buffer.LastIndexOf (c, startIndex, count);
		}

		public int LastIndexOf (string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			return buffer.LastIndexOf (searchText, startIndex, count, comparisonType);
		}
		#endregion
	}
}
