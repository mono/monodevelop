/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2007, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

namespace GitSharp.Core.Exceptions
{
	/// <summary>
	/// An inconsistency with respect to handling different object types.
    ///
    /// This most likely signals a programming error rather than a corrupt
    /// object database.
	/// </summary>
    [Serializable]
    public class IncorrectObjectTypeException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

		/// <summary>
		/// Construct and IncorrectObjectTypeException for the specified object id.
		///
		/// Provide the type to make it easier to track down the problem.
		/// </summary>
		/// <param name="id">
		/// SHA-1
		/// </param>
		/// <param name="type">
		/// Object type
		/// </param>
        public IncorrectObjectTypeException(ObjectId id, ObjectType type)
            : base(string.Format("object {0} is not a {1}.", id, type))
        {

        }
		
		/// <summary>
		/// Construct and IncorrectObjectTypeException for the specified object id.
		/// Provide the type to make it easier to track down the problem.
		/// </summary>
		/// <param name="id">SHA-1</param>
		/// <param name="type">Object type</param>
        /// <param name="inner">Inner Exception.</param>
        public IncorrectObjectTypeException(ObjectId id, ObjectType type, Exception inner) 
			: base(string.Format("object {0} is not a {1}.", id, type), inner) { }
		
		/// <summary>
		/// Construct and IncorrectObjectTypeException for the specified object id.
		///
		/// Provide the type to make it easier to track down the problem.
		/// </summary>
		/// <param name="id">
		/// SHA-1
		/// </param>
		/// <param name="type">
		/// Object type
		/// </param>
        public IncorrectObjectTypeException(ObjectId id, int type)
            : base(string.Format("object {0} is not a {1}.", id, (ObjectType)type))
        {

        }


        public IncorrectObjectTypeException(ObjectId id, string type, Exception inner) : base(string.Format("object {0} is not a {1}.", id, type), inner) { }

        /// <summary>
		/// Construct and IncorrectObjectTypeException for the specified object id.
		///
		/// Provide the type to make it easier to track down the problem.
		/// </summary>
		/// <param name="id">
		/// SHA-1
		/// </param>
		/// <param name="type">
		/// Object type
		/// </param>
        public IncorrectObjectTypeException(ObjectId id, string type)
            : base(string.Format("object {0} is not a {1}.", id, type))
        {

        }
		
		/// <summary>
		/// Construct and IncorrectObjectTypeException for the specified object id.
		/// Provide the type to make it easier to track down the problem.
		/// </summary>
		/// <param name="id">SHA-1</param>
		/// <param name="type">Object type</param>
        /// <param name="inner">Inner Exception.</param>
        public IncorrectObjectTypeException(ObjectId id, int type, Exception inner) 
		    : base(string.Format("object {0} is not a {1}.", id, (ObjectType)type), inner) { }
        
		protected IncorrectObjectTypeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

    }
}
