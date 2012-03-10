// 
// IDocumentPartitioner.cs
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
using System.Collections.Generic;

namespace Mono.TextEditor
{
	/// <summary>
	/// A document partitioner partitions a document in non overlapping regions.
	/// The document partitioner listens to document changes before the event handlers are called this ensures
	/// that all partitions in the document are up to date.
	/// That requires that all partitioners should do incremental updates instead of re-parsings of the whole document.
	/// 
	/// Examples for partitions are:
	/// Syntax highighting spans, strings, comments, sub-language regions (asp.net <% .. %>) etc.
	/// 
	/// </summary>
	public interface IDocumentPartitioner
	{
		/// <summary>
		/// Gets or sets the document.
		/// </summary>
		/// <value>
		/// The document.
		/// </value>
		Document Document {
			get;
			set;
		}
		
		/// <summary>
		/// Informs the partitioner of a text replacing event. This is called before all other handlers.
		/// </summary>
		/// <param name='args'>
		/// The arguments of the replacing event.
		/// </param>
		void TextReplacing (DocumentChangeEventArgs args);
		
		/// <summary>
		/// Informs the partitioner of a text replaced event. This is called before all other handlers except the line splitter.
		/// Lines have the highest priority - they need always be valid - partitions are 2nd.
		/// </summary>
		/// <param name='args'>
		/// The arguments of the replace event.
		/// </param>
		void TextReplaced (DocumentChangeEventArgs args);
		
		/// <summary>
		/// Gets the partitions between offset and offset + length;
		/// </summary>
		/// <returns>
		/// The partitions.
		/// </returns>
		/// <param name='offset'>
		/// The start offset.
		/// </param>
		/// <param name='length'>
		/// The length.
		/// </param>
		IEnumerable<TypedSegment> GetPartitions (int offset, int length);
		
		/// <summary>
		/// Gets the partitions in a specified segment.
		/// </summary>
		/// <returns>
		/// The partitions.
		/// </returns>
		/// <param name='segment'>
		/// The segment to get the partitions from.
		/// </param>
		IEnumerable<TypedSegment> GetPartitions (ISegment segment);
		
		/// <summary>
		/// Gets the partition from an offset.
		/// </summary>
		/// <returns>
		/// The partition.
		/// </returns>
		/// <param name='offset'>
		/// The offset inside the partition.
		/// </param>
		TypedSegment GetPartition (int offset);
		
		/// <summary>
		/// Gets the partition from a location.
		/// </summary>
		/// <returns>
		/// The partition.
		/// </returns>
		/// <param name='location'>
		/// The location inside the partition.
		/// </param>
		TypedSegment GetPartition (DocumentLocation location);
		
		/// <summary>
		/// Gets the partition from a location.
		/// </summary>
		/// <returns>
		/// The partition.
		/// </returns>
		/// <param name='line'>
		/// The line inside the partition.
		/// </param>
		/// <param name='column'>
		/// The column inside the partition.
		/// </param>
		TypedSegment GetPartition (int line, int column);
	}
}

