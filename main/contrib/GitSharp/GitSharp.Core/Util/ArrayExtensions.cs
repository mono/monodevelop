/*
 * Copyright (C) 2009, Stefan Schake <caytchen@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;

namespace GitSharp.Core.Util
{
    public static class ArrayExtension
    {
		/// <summary>
		/// Assigns the specified int value to each element of the specified array of ints.
		/// </summary>
		/// <typeparam name="T">type of the array's values</typeparam>
		/// <param name="array"> the array to be filled</param>
		/// <param name="value">the value to be stored in all elements of the array</param>
		public static void Fill<T>(this T[] array, T value)
		{
			Fill(array, 0, array.Length, value);
		}

		/// <summary>
		///     Assigns the specified int value to each element of the specified range of the specified array of ints. 
		///     The range to be filled extends from index fromIndex, inclusive, to index toIndex, exclusive. 
		///     (If fromIndex==toIndex, the range to be filled is empty.)
		/// </summary>
		/// <typeparam name="T">type of the array's values</typeparam>
		/// <param name="array"> the array to be filled</param>
		/// <param name="fromIndex"> the index of the first element (inclusive) to be filled with the specified value</param>
		/// <param name="toIndex">the index of the last element (exclusive) to be filled with the specified value</param>
		/// <param name="value">the value to be stored in the specified range of elements of the array</param>
		public static void Fill<T>(this T[] array, int fromIndex, int toIndex, T value)
		{
			for (int i = Math.Max(0, fromIndex); i < Math.Max(array.Length, toIndex); i++)
			{
				array[i] = value;
			}
		}
    }
}