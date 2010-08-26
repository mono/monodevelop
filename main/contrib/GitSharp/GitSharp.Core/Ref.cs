/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

namespace GitSharp.Core
{

    public static class RefExtensions
    {
        public static string getName(this Ref @ref)
        {
            return @ref.Name;
        }

        public static bool isSymbolic(this Ref @ref)
        {
            return @ref.IsSymbolic;
        }

        public static Ref getLeaf(this Ref @ref)
        {
            return @ref.Leaf;
        }

        public static Ref getTarget(this Ref @ref)
        {
            return @ref.Target;
        }

        public static ObjectId getObjectId(this Ref @ref)
        {
            return @ref.ObjectId;
        }

        public static ObjectId getPeeledObjectId(this Ref @ref)
        {
            return @ref.PeeledObjectId;
        }

        public static bool isPeeled(this Ref @ref)
        {
            return @ref.IsPeeled;
        }

        public static Storage getStorage(this Ref @ref)
        {
            return @ref.StorageFormat;
        }
    }


    /// <summary>
    /// Pairing of a name and the <seealso cref="ObjectId"/> it currently has.
    /// <para />
    /// A ref in Git is (more or less) a variable that holds a single object
    /// identifier. The object identifier can be any valid Git object (blob, tree,
    /// commit, annotated tag, ...).
    /// <para />
    /// The ref name has the attributes of the ref that was asked for as well as the
    /// ref it was resolved to for symbolic refs plus the object id it points to and
    /// (for tags) the peeled target object id, i.e. the tag resolved recursively
    /// until a non-tag object is referenced.
    /// </summary>
    public interface Ref
    {
        /// <summary>
        /// What this ref is called within the repository.
        /// </summary>
        /// <returns>name of this ref.</returns>
        string Name { get; }

        /// <summary>
        /// Test if this reference is a symbolic reference.
        /// <para/>
        /// A symbolic reference does not have its own {@link ObjectId} value, but
        /// instead points to another {@code Ref} in the same database and always
        /// uses that other reference's value as its own.
        /// </summary>
        /// <returns>
        /// true if this is a symbolic reference; false if this reference
        /// contains its own ObjectId.
        /// </returns>
        bool IsSymbolic { get; }

        /// <summary>
        /// Traverse target references until {@link #isSymbolic()} is false.
        /// <para/>
        /// If {@link #isSymbolic()} is false, returns {@code this}.
        /// <para/>
        /// If {@link #isSymbolic()} is true, this method recursively traverses
        /// {@link #getTarget()} until {@link #isSymbolic()} returns false.
        /// <para/>
        /// This method is effectively
        /// 
        /// <pre>
        /// return isSymbolic() ? getTarget().getLeaf() : this;
        /// </pre>
        /// </summary>
        /// <returns>the reference that actually stores the ObjectId value.</returns>
        Ref Leaf { get; }

        /// <summary>
        /// Get the reference this reference points to, or {@code this}.
        /// <para/>
        /// If {@link #isSymbolic()} is true this method returns the reference it
        /// directly names, which might not be the leaf reference, but could be
        /// another symbolic reference.
        /// <para/>
        /// If this is a leaf level reference that contains its own ObjectId,this
        /// method returns {@code this}.
        /// </summary>
        /// <returns>the target reference, or {@code this}.</returns>
        Ref Target { get; }

        /// <summary>
        /// Cached value of this ref.
        /// </summary>
        /// <returns>the value of this ref at the last time we read it.</returns>
        ObjectId ObjectId { get; }

        /// <summary>
        /// Cached value of <code>ref^{}</code> (the ref peeled to commit).
        /// </summary>
        /// <returns>
        /// if this ref is an annotated tag the id of the commit (or tree or
        /// blob) that the annotated tag refers to; null if this ref does not
        /// refer to an annotated tag.
        /// </returns>
        ObjectId PeeledObjectId { get; }

        /// <returns>whether the Ref represents a peeled tag</returns>
        bool IsPeeled { get; }

        /// <summary>
        /// How was this ref obtained?
        /// <para/>
        /// The current storage model of a Ref may influence how the ref must be
        /// updated or deleted from the repository.
        /// </summary>
        /// <returns>type of ref.</returns>
        Storage StorageFormat { get; }

   }

    /// <summary>
    /// Location where a <see cref="Ref"/> is Stored.
    /// </summary>
    public sealed class Storage
    {
        /// <summary>
        /// The ref does not exist yet, updating it may create it.
        /// <para />
        /// Creation is likely to choose <see cref="Loose"/> storage.
        /// </summary>
        public static readonly Storage New = new Storage("New", true, false);

        /// <summary>
        /// The ref is Stored in a file by itself.
        /// <para />
        /// Updating this ref affects only this ref.
        /// </summary>
        public static readonly Storage Loose = new Storage("Loose", true, false);

        /// <summary>
        /// The ref is stored in the <code>packed-refs</code> file, with others.
        /// <para />
        /// Updating this ref requires rewriting the file, with perhaps many
        /// other refs being included at the same time.
        /// </summary>
        public static readonly Storage Packed = new Storage("Packed", false, true);

        /// <summary>
        /// The ref is both <see cref="Loose"/> and <see cref="Packed"/>.
        /// <para />
        /// Updating this ref requires only updating the loose file, but deletion
        /// requires updating both the loose file and the packed refs file.
        /// </summary>
        public static readonly Storage LoosePacked = new Storage("LoosePacked", true, true);

        /// <summary>
        /// The ref came from a network advertisement and storage is unknown.
        /// <para />
        /// This ref cannot be updated without Git-aware support on the remote
        /// side, as Git-aware code consolidate the remote refs and reported them
        /// to this process.
        /// </summary>
        public static readonly Storage Network = new Storage("Network", false, false);

        public bool IsLoose { get; private set; }
        public bool IsPacked { get; private set; }
        public string Name { get; private set; }

        private Storage(string name, bool loose, bool packed)
        {
            Name = name;
            IsLoose = loose;
            IsPacked = packed;
        }
    }
 
}