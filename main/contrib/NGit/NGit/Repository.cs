/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Dircache;
using NGit.Errors;
using NGit.Events;
using NGit.Revwalk;
using NGit.Storage.File;
using NGit.Treewalk;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>Represents a Git repository.</summary>
	/// <remarks>
	/// Represents a Git repository.
	/// <p>
	/// A repository holds all objects and refs used for managing source code (could
	/// be any type of file, but source code is what SCM's are typically used for).
	/// <p>
	/// This class is thread-safe.
	/// </remarks>
	public abstract class Repository
	{
		private static readonly ListenerList globalListeners = new ListenerList();

		/// <returns>the global listener list observing all events in this JVM.</returns>
		public static ListenerList GetGlobalListenerList()
		{
			return globalListeners;
		}

		private readonly AtomicInteger useCnt = new AtomicInteger(1);

		/// <summary>Metadata directory holding the repository's critical files.</summary>
		/// <remarks>Metadata directory holding the repository's critical files.</remarks>
		private readonly FilePath gitDir;

		/// <summary>File abstraction used to resolve paths.</summary>
		/// <remarks>File abstraction used to resolve paths.</remarks>
		private readonly FS fs;

		private GitIndex index;

		private readonly ListenerList myListeners = new ListenerList();

		/// <summary>If not bare, the top level directory of the working files.</summary>
		/// <remarks>If not bare, the top level directory of the working files.</remarks>
		private readonly FilePath workTree;

		/// <summary>If not bare, the index file caching the working file states.</summary>
		/// <remarks>If not bare, the index file caching the working file states.</remarks>
		private readonly FilePath indexFile;

		/// <summary>Initialize a new repository instance.</summary>
		/// <remarks>Initialize a new repository instance.</remarks>
		/// <param name="options">options to configure the repository.</param>
		protected internal Repository(BaseRepositoryBuilder options)
		{
			gitDir = options.GetGitDir();
			fs = options.GetFS();
			workTree = options.GetWorkTree();
			indexFile = options.GetIndexFile();
		}

		/// <returns>listeners observing only events on this repository.</returns>
		public virtual ListenerList Listeners
		{
			get
			{
				return myListeners;
			}
		}

		/// <summary>Fire an event to all registered listeners.</summary>
		/// <remarks>
		/// Fire an event to all registered listeners.
		/// <p>
		/// The source repository of the event is automatically set to this
		/// repository, before the event is delivered to any listeners.
		/// </remarks>
		/// <param name="event">the event to deliver.</param>
		public virtual void FireEvent<_T0>(RepositoryEvent<_T0> @event) where _T0:RepositoryListener
		{
			@event.SetRepository(this);
			myListeners.Dispatch(@event);
			globalListeners.Dispatch(@event);
		}

		/// <summary>Create a new Git repository.</summary>
		/// <remarks>
		/// Create a new Git repository.
		/// <p>
		/// Repository with working tree is created using this method. This method is
		/// the same as
		/// <code>create(false)</code>
		/// .
		/// </remarks>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		/// <seealso cref="Create(bool)">Create(bool)</seealso>
		public virtual void Create()
		{
			Create(false);
		}

		/// <summary>
		/// Create a new Git repository initializing the necessary files and
		/// directories.
		/// </summary>
		/// <remarks>
		/// Create a new Git repository initializing the necessary files and
		/// directories.
		/// </remarks>
		/// <param name="bare">
		/// if true, a bare repository (a repository without a working
		/// directory) is created.
		/// </param>
		/// <exception cref="System.IO.IOException">in case of IO problem</exception>
		public abstract void Create(bool bare);

		/// <returns>local metadata directory; null if repository isn't local.</returns>
		public virtual FilePath Directory
		{
			get
			{
				return gitDir;
			}
		}

		/// <returns>the object database which stores this repository's data.</returns>
		public abstract NGit.ObjectDatabase ObjectDatabase
		{
			get;
		}

		/// <returns>
		/// a new inserter to create objects in
		/// <see cref="ObjectDatabase()">ObjectDatabase()</see>
		/// 
		/// </returns>
		public virtual ObjectInserter NewObjectInserter()
		{
			return ObjectDatabase.NewInserter();
		}

		/// <returns>
		/// a new reader to read objects from
		/// <see cref="ObjectDatabase()">ObjectDatabase()</see>
		/// 
		/// </returns>
		public virtual ObjectReader NewObjectReader()
		{
			return ObjectDatabase.NewReader();
		}

		/// <returns>the reference database which stores the reference namespace.</returns>
		public abstract NGit.RefDatabase RefDatabase
		{
			get;
		}

		/// <returns>the configuration of this repository</returns>
		public abstract StoredConfig GetConfig();

		/// <returns>the used file system abstraction</returns>
		public virtual FS FileSystem
		{
			get
			{
				return fs;
			}
		}

		/// <param name="objectId"></param>
		/// <returns>
		/// true if the specified object is stored in this repo or any of the
		/// known shared repositories.
		/// </returns>
		public virtual bool HasObject(AnyObjectId objectId)
		{
			try
			{
				return ObjectDatabase.Has(objectId);
			}
			catch (IOException)
			{
				// Legacy API, assume error means "no"
				return false;
			}
		}

		/// <summary>Open an object from this repository.</summary>
		/// <remarks>
		/// Open an object from this repository.
		/// <p>
		/// This is a one-shot call interface which may be faster than allocating a
		/// <see cref="NewObjectReader()">NewObjectReader()</see>
		/// to perform the lookup.
		/// </remarks>
		/// <param name="objectId">identity of the object to open.</param>
		/// <returns>
		/// a
		/// <see cref="ObjectLoader">ObjectLoader</see>
		/// for accessing the object.
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">the object does not exist.</exception>
		/// <exception cref="System.IO.IOException">the object store cannot be accessed.</exception>
		public virtual ObjectLoader Open(AnyObjectId objectId)
		{
			return ObjectDatabase.Open(objectId);
		}

		/// <summary>Open an object from this repository.</summary>
		/// <remarks>
		/// Open an object from this repository.
		/// <p>
		/// This is a one-shot call interface which may be faster than allocating a
		/// <see cref="NewObjectReader()">NewObjectReader()</see>
		/// to perform the lookup.
		/// </remarks>
		/// <param name="objectId">identity of the object to open.</param>
		/// <param name="typeHint">
		/// hint about the type of object being requested;
		/// <see cref="ObjectReader.OBJ_ANY">ObjectReader.OBJ_ANY</see>
		/// if the object type is not known,
		/// or does not matter to the caller.
		/// </param>
		/// <returns>
		/// a
		/// <see cref="ObjectLoader">ObjectLoader</see>
		/// for accessing the object.
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">the object does not exist.</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// typeHint was not OBJ_ANY, and the object's actual type does
		/// not match typeHint.
		/// </exception>
		/// <exception cref="System.IO.IOException">the object store cannot be accessed.</exception>
		public virtual ObjectLoader Open(AnyObjectId objectId, int typeHint)
		{
			return ObjectDatabase.Open(objectId, typeHint);
		}

		/// <summary>Create a command to update, create or delete a ref in this repository.</summary>
		/// <remarks>Create a command to update, create or delete a ref in this repository.</remarks>
		/// <param name="ref">name of the ref the caller wants to modify.</param>
		/// <returns>
		/// an update command. The caller must finish populating this command
		/// and then invoke one of the update methods to actually make a
		/// change.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// a symbolic ref was passed in and could not be resolved back
		/// to the base ref, as the symbolic ref could not be read.
		/// </exception>
		public virtual RefUpdate UpdateRef(string @ref)
		{
			return UpdateRef(@ref, false);
		}

		/// <summary>Create a command to update, create or delete a ref in this repository.</summary>
		/// <remarks>Create a command to update, create or delete a ref in this repository.</remarks>
		/// <param name="ref">name of the ref the caller wants to modify.</param>
		/// <param name="detach">true to create a detached head</param>
		/// <returns>
		/// an update command. The caller must finish populating this command
		/// and then invoke one of the update methods to actually make a
		/// change.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// a symbolic ref was passed in and could not be resolved back
		/// to the base ref, as the symbolic ref could not be read.
		/// </exception>
		public virtual RefUpdate UpdateRef(string @ref, bool detach)
		{
			return RefDatabase.NewUpdate(@ref, detach);
		}

		/// <summary>Create a command to rename a ref in this repository</summary>
		/// <param name="fromRef">name of ref to rename from</param>
		/// <param name="toRef">name of ref to rename to</param>
		/// <returns>an update command that knows how to rename a branch to another.</returns>
		/// <exception cref="System.IO.IOException">the rename could not be performed.</exception>
		public virtual RefRename RenameRef(string fromRef, string toRef)
		{
			return RefDatabase.NewRename(fromRef, toRef);
		}

		/// <summary>Parse a git revision string and return an object id.</summary>
		/// <remarks>
		/// Parse a git revision string and return an object id.
		/// Combinations of these operators are supported:
		/// <ul>
		/// <li><b>HEAD</b>, <b>MERGE_HEAD</b>, <b>FETCH_HEAD</b></li>
		/// <li><b>SHA-1</b>: a complete or abbreviated SHA-1</li>
		/// <li><b>refs/...</b>: a complete reference name</li>
		/// <li><b>short-name</b>: a short reference name under
		/// <code>refs/heads</code>
		/// ,
		/// <code>refs/tags</code>
		/// , or
		/// <code>refs/remotes</code>
		/// namespace</li>
		/// <li><b>tag-NN-gABBREV</b>: output from describe, parsed by treating
		/// <code>ABBREV</code>
		/// as an abbreviated SHA-1.</li>
		/// <li><i>id</i><b>^</b>: first parent of commit <i>id</i>, this is the same
		/// as
		/// <code>id^1</code>
		/// </li>
		/// <li><i>id</i><b>^0</b>: ensure <i>id</i> is a commit</li>
		/// <li><i>id</i><b>^n</b>: n-th parent of commit <i>id</i></li>
		/// <li><i>id</i><b>~n</b>: n-th historical ancestor of <i>id</i>, by first
		/// parent.
		/// <code>id~3</code>
		/// is equivalent to
		/// <code>id^1^1^1</code>
		/// or
		/// <code>id^^^</code>
		/// .</li>
		/// <li><i>id</i><b>:path</b>: Lookup path under tree named by <i>id</i></li>
		/// <li><i>id</i><b>^{commit}</b>: ensure <i>id</i> is a commit</li>
		/// <li><i>id</i><b>^{tree}</b>: ensure <i>id</i> is a tree</li>
		/// <li><i>id</i><b>^{tag}</b>: ensure <i>id</i> is a tag</li>
		/// <li><i>id</i><b>^{blob}</b>: ensure <i>id</i> is a blob</li>
		/// </ul>
		/// <p>
		/// The following operators are specified by Git conventions, but are not
		/// supported by this method:
		/// <ul>
		/// <li><b>ref@{n}</b>: n-th version of ref as given by its reflog</li>
		/// <li><b>ref@{time}</b>: value of ref at the designated time</li>
		/// </ul>
		/// </remarks>
		/// <param name="revstr">A git object references expression</param>
		/// <returns>an ObjectId or null if revstr can't be resolved to any ObjectId</returns>
		/// <exception cref="NGit.Errors.AmbiguousObjectException">
		/// <code>revstr</code>
		/// contains an abbreviated ObjectId and this
		/// repository contains more than one object which match to the
		/// input abbreviation.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the id parsed does not meet the type required to finish
		/// applying the operators in the expression.
		/// </exception>
		/// <exception cref="NGit.Errors.RevisionSyntaxException">
		/// the expression is not supported by this implementation, or
		/// does not meet the standard syntax.
		/// </exception>
		/// <exception cref="System.IO.IOException">on serious errors</exception>
		public virtual ObjectId Resolve(string revstr)
		{
			RevWalk rw = new RevWalk(this);
			try
			{
				return Resolve(rw, revstr);
			}
			finally
			{
				rw.Release();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ObjectId Resolve(RevWalk rw, string revstr)
		{
			char[] rev = revstr.ToCharArray();
			RevObject @ref = null;
			for (int i = 0; i < rev.Length; ++i)
			{
				switch (rev[i])
				{
					case '^':
					{
						if (@ref == null)
						{
							@ref = ParseSimple(rw, new string(rev, 0, i));
							if (@ref == null)
							{
								return null;
							}
						}
						if (i + 1 < rev.Length)
						{
							switch (rev[i + 1])
							{
								case '0':
								case '1':
								case '2':
								case '3':
								case '4':
								case '5':
								case '6':
								case '7':
								case '8':
								case '9':
								{
									int j;
									@ref = rw.ParseCommit(@ref);
									for (j = i + 1; j < rev.Length; ++j)
									{
										if (!char.IsDigit(rev[j]))
										{
											break;
										}
									}
									string parentnum = new string(rev, i + 1, j - i - 1);
									int pnum;
									try
									{
										pnum = System.Convert.ToInt32(parentnum);
									}
									catch (FormatException)
									{
										throw new RevisionSyntaxException(JGitText.Get().invalidCommitParentNumber, revstr
											);
									}
									if (pnum != 0)
									{
										RevCommit commit = (RevCommit)@ref;
										if (pnum > commit.ParentCount)
										{
											@ref = null;
										}
										else
										{
											@ref = commit.GetParent(pnum - 1);
										}
									}
									i = j - 1;
									break;
								}

								case '{':
								{
									int k;
									string item = null;
									for (k = i + 2; k < rev.Length; ++k)
									{
										if (rev[k] == '}')
										{
											item = new string(rev, i + 2, k - i - 2);
											break;
										}
									}
									i = k;
									if (item != null)
									{
										if (item.Equals("tree"))
										{
											@ref = rw.ParseTree(@ref);
										}
										else
										{
											if (item.Equals("commit"))
											{
												@ref = rw.ParseCommit(@ref);
											}
											else
											{
												if (item.Equals("blob"))
												{
													@ref = rw.Peel(@ref);
													if (!(@ref is RevBlob))
													{
														throw new IncorrectObjectTypeException(@ref, Constants.TYPE_BLOB);
													}
												}
												else
												{
													if (item.Equals(string.Empty))
													{
														@ref = rw.Peel(@ref);
													}
													else
													{
														throw new RevisionSyntaxException(revstr);
													}
												}
											}
										}
									}
									else
									{
										throw new RevisionSyntaxException(revstr);
									}
									break;
								}

								default:
								{
									@ref = rw.ParseAny(@ref);
									if (@ref is RevCommit)
									{
										RevCommit commit = ((RevCommit)@ref);
										if (commit.ParentCount == 0)
										{
											@ref = null;
										}
										else
										{
											@ref = commit.GetParent(0);
										}
									}
									else
									{
										throw new IncorrectObjectTypeException(@ref, Constants.TYPE_COMMIT);
									}
									break;
								}
							}
						}
						else
						{
							@ref = rw.Peel(@ref);
							if (@ref is RevCommit)
							{
								RevCommit commit = ((RevCommit)@ref);
								if (commit.ParentCount == 0)
								{
									@ref = null;
								}
								else
								{
									@ref = commit.GetParent(0);
								}
							}
							else
							{
								throw new IncorrectObjectTypeException(@ref, Constants.TYPE_COMMIT);
							}
						}
						break;
					}

					case '~':
					{
						if (@ref == null)
						{
							@ref = ParseSimple(rw, new string(rev, 0, i));
							if (@ref == null)
							{
								return null;
							}
						}
						@ref = rw.Peel(@ref);
						if (!(@ref is RevCommit))
						{
							throw new IncorrectObjectTypeException(@ref, Constants.TYPE_COMMIT);
						}
						int l;
						for (l = i + 1; l < rev.Length; ++l)
						{
							if (!char.IsDigit(rev[l]))
							{
								break;
							}
						}
						string distnum = new string(rev, i + 1, l - i - 1);
						int dist;
						try
						{
							dist = System.Convert.ToInt32(distnum);
						}
						catch (FormatException)
						{
							throw new RevisionSyntaxException(JGitText.Get().invalidAncestryLength, revstr);
						}
						while (dist > 0)
						{
							RevCommit commit = (RevCommit)@ref;
							if (commit.ParentCount == 0)
							{
								@ref = null;
								break;
							}
							commit = commit.GetParent(0);
							rw.ParseHeaders(commit);
							@ref = commit;
							--dist;
						}
						i = l - 1;
						break;
					}

					case '@':
					{
						int m;
						string time = null;
						for (m = i + 2; m < rev.Length; ++m)
						{
							if (rev[m] == '}')
							{
								time = new string(rev, i + 2, m - i - 2);
								break;
							}
						}
						if (time != null)
						{
							throw new RevisionSyntaxException(JGitText.Get().reflogsNotYetSupportedByRevisionParser
								, revstr);
						}
						i = m - 1;
						break;
					}

					case ':':
					{
						RevTree tree;
						if (@ref == null)
						{
							// We might not yet have parsed the left hand side.
							ObjectId id;
							try
							{
								if (i == 0)
								{
									id = Resolve(rw, Constants.HEAD);
								}
								else
								{
									id = Resolve(rw, new string(rev, 0, i));
								}
							}
							catch (RevisionSyntaxException)
							{
								throw new RevisionSyntaxException(revstr);
							}
							if (id == null)
							{
								return null;
							}
							tree = rw.ParseTree(id);
						}
						else
						{
							tree = rw.ParseTree(@ref);
						}
						if (i == rev.Length - i)
						{
							return tree.Copy();
						}
						TreeWalk tw = TreeWalk.ForPath(rw.GetObjectReader(), new string(rev, i + 1, rev.Length
							 - i - 1), tree);
						return tw != null ? tw.GetObjectId(0) : null;
					}

					default:
					{
						if (@ref != null)
						{
							throw new RevisionSyntaxException(revstr);
						}
						break;
					}
				}
			}
			return @ref != null ? @ref.Copy() : ResolveSimple(revstr);
		}

		private static bool IsHex(char c)
		{
			return ('0' <= c && c <= '9') || ('a' <= c && c <= 'f') || ('A' <= c && c <= 'F');
		}

		//
		//
		private static bool IsAllHex(string str, int ptr)
		{
			while (ptr < str.Length)
			{
				if (!IsHex(str[ptr++]))
				{
					return false;
				}
			}
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private RevObject ParseSimple(RevWalk rw, string revstr)
		{
			ObjectId id = ResolveSimple(revstr);
			return id != null ? rw.ParseAny(id) : null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ObjectId ResolveSimple(string revstr)
		{
			if (ObjectId.IsId(revstr))
			{
				return ObjectId.FromString(revstr);
			}
			Ref r = RefDatabase.GetRef(revstr);
			if (r != null)
			{
				return r.GetObjectId();
			}
			if (AbbreviatedObjectId.IsId(revstr))
			{
				return ResolveAbbreviation(revstr);
			}
			int dashg = revstr.IndexOf("-g");
			if (4 < revstr.Length && 0 <= dashg && IsHex(revstr[dashg + 2]) && IsHex(revstr[dashg
				 + 3]) && IsAllHex(revstr, dashg + 4))
			{
				// Possibly output from git describe?
				string s = Sharpen.Runtime.Substring(revstr, dashg + 2);
				if (AbbreviatedObjectId.IsId(s))
				{
					return ResolveAbbreviation(s);
				}
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.AmbiguousObjectException"></exception>
		private ObjectId ResolveAbbreviation(string revstr)
		{
			AbbreviatedObjectId id = AbbreviatedObjectId.FromString(revstr);
			ObjectReader reader = NewObjectReader();
			try
			{
				ICollection<ObjectId> matches = reader.Resolve(id);
				if (matches.Count == 0)
				{
					return null;
				}
				else
				{
					if (matches.Count == 1)
					{
						return matches.Iterator().Next();
					}
					else
					{
						throw new AmbiguousObjectException(id, matches);
					}
				}
			}
			finally
			{
				reader.Release();
			}
		}

		/// <summary>
		/// Increment the use counter by one, requiring a matched
		/// <see cref="Close()">Close()</see>
		/// .
		/// </summary>
		public virtual void IncrementOpen()
		{
			useCnt.IncrementAndGet();
		}

		/// <summary>Decrement the use count, and maybe close resources.</summary>
		/// <remarks>Decrement the use count, and maybe close resources.</remarks>
		public virtual void Close()
		{
			if (useCnt.DecrementAndGet() == 0)
			{
				DoClose();
			}
		}

		/// <summary>
		/// Invoked when the use count drops to zero during
		/// <see cref="Close()">Close()</see>
		/// .
		/// <p>
		/// The default implementation closes the object and ref databases.
		/// </summary>
		protected internal virtual void DoClose()
		{
			ObjectDatabase.Close();
			RefDatabase.Close();
		}

		public override string ToString()
		{
			string desc;
			if (Directory != null)
			{
				desc = Directory.GetPath();
			}
			else
			{
				desc = GetType().Name + "-" + Runtime.IdentityHashCode(this);
			}
			return "Repository[" + desc + "]";
		}

		/// <summary>
		/// Get the name of the reference that
		/// <code>HEAD</code>
		/// points to.
		/// <p>
		/// This is essentially the same as doing:
		/// <pre>
		/// return getRef(Constants.HEAD).getTarget().getName()
		/// </pre>
		/// Except when HEAD is detached, in which case this method returns the
		/// current ObjectId in hexadecimal string format.
		/// </summary>
		/// <returns>
		/// name of current branch (for example
		/// <code>refs/heads/master</code>
		/// ) or
		/// an ObjectId in hex format if the current branch is detached.
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual string GetFullBranch()
		{
			Ref head = GetRef(Constants.HEAD);
			if (head == null)
			{
				return null;
			}
			if (head.IsSymbolic())
			{
				return head.GetTarget().GetName();
			}
			if (head.GetObjectId() != null)
			{
				return head.GetObjectId().Name;
			}
			return null;
		}

		/// <summary>
		/// Get the short name of the current branch that
		/// <code>HEAD</code>
		/// points to.
		/// <p>
		/// This is essentially the same as
		/// <see cref="GetFullBranch()">GetFullBranch()</see>
		/// , except the
		/// leading prefix
		/// <code>refs/heads/</code>
		/// is removed from the reference before
		/// it is returned to the caller.
		/// </summary>
		/// <returns>
		/// name of current branch (for example
		/// <code>master</code>
		/// ), or an
		/// ObjectId in hex format if the current branch is detached.
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual string GetBranch()
		{
			string name = GetFullBranch();
			if (name != null)
			{
				return ShortenRefName(name);
			}
			return name;
		}

		/// <summary>
		/// Objects known to exist but not expressed by
		/// <see cref="GetAllRefs()">GetAllRefs()</see>
		/// .
		/// <p>
		/// When a repository borrows objects from another repository, it can
		/// advertise that it safely has that other repository's references, without
		/// exposing any other details about the other repository.  This may help
		/// a client trying to push changes avoid pushing more than it needs to.
		/// </summary>
		/// <returns>unmodifiable collection of other known objects.</returns>
		public virtual ICollection<ObjectId> GetAdditionalHaves()
		{
			return Sharpen.Collections.EmptySet<ObjectId>();
		}

		/// <summary>Get a ref by name.</summary>
		/// <remarks>Get a ref by name.</remarks>
		/// <param name="name">
		/// the name of the ref to lookup. May be a short-hand form, e.g.
		/// "master" which is is automatically expanded to
		/// "refs/heads/master" if "refs/heads/master" already exists.
		/// </param>
		/// <returns>the Ref with the given name, or null if it does not exist</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual Ref GetRef(string name)
		{
			return RefDatabase.GetRef(name);
		}

		/// <returns>mutable map of all known refs (heads, tags, remotes).</returns>
		public virtual IDictionary<string, Ref> GetAllRefs()
		{
			try
			{
				return RefDatabase.GetRefs(NGit.RefDatabase.ALL);
			}
			catch (IOException)
			{
				return new Dictionary<string, Ref>();
			}
		}

		/// <returns>
		/// mutable map of all tags; key is short tag name ("v1.0") and value
		/// of the entry contains the ref with the full tag name
		/// ("refs/tags/v1.0").
		/// </returns>
		public virtual IDictionary<string, Ref> GetTags()
		{
			try
			{
				return RefDatabase.GetRefs(Constants.R_TAGS);
			}
			catch (IOException)
			{
				return new Dictionary<string, Ref>();
			}
		}

		/// <summary>Peel a possibly unpeeled reference to an annotated tag.</summary>
		/// <remarks>
		/// Peel a possibly unpeeled reference to an annotated tag.
		/// <p>
		/// If the ref cannot be peeled (as it does not refer to an annotated tag)
		/// the peeled id stays null, but
		/// <see cref="Ref.IsPeeled()">Ref.IsPeeled()</see>
		/// will be true.
		/// </remarks>
		/// <param name="ref">The ref to peel</param>
		/// <returns>
		/// <code>ref</code> if <code>ref.isPeeled()</code> is true; else a
		/// new Ref object representing the same data as Ref, but isPeeled()
		/// will be true and getPeeledObjectId will contain the peeled object
		/// (or null).
		/// </returns>
		public virtual Ref Peel(Ref @ref)
		{
			try
			{
				return RefDatabase.Peel(@ref);
			}
			catch (IOException)
			{
				// Historical accident; if the reference cannot be peeled due
				// to some sort of repository access problem we claim that the
				// same as if the reference was not an annotated tag.
				return @ref;
			}
		}

		/// <returns>a map with all objects referenced by a peeled ref.</returns>
		public virtual IDictionary<AnyObjectId, ICollection<Ref>> GetAllRefsByPeeledObjectId
			()
		{
			IDictionary<string, Ref> allRefs = GetAllRefs();
			IDictionary<AnyObjectId, ICollection<Ref>> ret = new Dictionary<AnyObjectId, ICollection
				<Ref>>(allRefs.Count);
			foreach (Ref iref in allRefs.Values)
			{
				Ref @ref = Peel(iref);
				AnyObjectId target = @ref.GetPeeledObjectId();
				if (target == null)
				{
					target = @ref.GetObjectId();
				}
				// We assume most Sets here are singletons
				ICollection<Ref> oset = ret.Put(target, Sharpen.Collections.Singleton(@ref));
				if (oset != null)
				{
					// that was not the case (rare)
					if (oset.Count == 1)
					{
						// Was a read-only singleton, we must copy to a new Set
						oset = new HashSet<Ref>(oset);
					}
					ret.Put(target, oset);
					oset.AddItem(@ref);
				}
			}
			return ret;
		}

		/// <returns>
		/// a representation of the index associated with this
		/// <see cref="Repository">Repository</see>
		/// </returns>
		/// <exception cref="System.IO.IOException">if the index can not be read</exception>
		/// <exception cref="NGit.Errors.NoWorkTreeException">
		/// if this is bare, which implies it has no working directory.
		/// See
		/// <see cref="IsBare()">IsBare()</see>
		/// .
		/// </exception>
		public virtual GitIndex GetIndex()
		{
			if (IsBare)
			{
				throw new NoWorkTreeException();
			}
			if (index == null)
			{
				index = new GitIndex(this);
				index.Read();
			}
			else
			{
				index.RereadIfNecessary();
			}
			return index;
		}

		/// <returns>the index file location</returns>
		/// <exception cref="NGit.Errors.NoWorkTreeException">
		/// if this is bare, which implies it has no working directory.
		/// See
		/// <see cref="IsBare()">IsBare()</see>
		/// .
		/// </exception>
		public virtual FilePath GetIndexFile()
		{
			if (IsBare)
			{
				throw new NoWorkTreeException();
			}
			return indexFile;
		}

		/// <summary>Create a new in-core index representation and read an index from disk.</summary>
		/// <remarks>
		/// Create a new in-core index representation and read an index from disk.
		/// <p>
		/// The new index will be read before it is returned to the caller. Read
		/// failures are reported as exceptions and therefore prevent the method from
		/// returning a partially populated index.
		/// </remarks>
		/// <returns>
		/// a cache representing the contents of the specified index file (if
		/// it exists) or an empty cache if the file does not exist.
		/// </returns>
		/// <exception cref="NGit.Errors.NoWorkTreeException">
		/// if this is bare, which implies it has no working directory.
		/// See
		/// <see cref="IsBare()">IsBare()</see>
		/// .
		/// </exception>
		/// <exception cref="System.IO.IOException">the index file is present but could not be read.
		/// 	</exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// the index file is using a format or extension that this
		/// library does not support.
		/// </exception>
		public virtual DirCache ReadDirCache()
		{
			return DirCache.Read(GetIndexFile(), FileSystem);
		}

		/// <summary>Create a new in-core index representation, lock it, and read from disk.</summary>
		/// <remarks>
		/// Create a new in-core index representation, lock it, and read from disk.
		/// <p>
		/// The new index will be locked and then read before it is returned to the
		/// caller. Read failures are reported as exceptions and therefore prevent
		/// the method from returning a partially populated index.
		/// </remarks>
		/// <returns>
		/// a cache representing the contents of the specified index file (if
		/// it exists) or an empty cache if the file does not exist.
		/// </returns>
		/// <exception cref="NGit.Errors.NoWorkTreeException">
		/// if this is bare, which implies it has no working directory.
		/// See
		/// <see cref="IsBare()">IsBare()</see>
		/// .
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// the index file is present but could not be read, or the lock
		/// could not be obtained.
		/// </exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// the index file is using a format or extension that this
		/// library does not support.
		/// </exception>
		public virtual DirCache LockDirCache()
		{
			return DirCache.Lock(GetIndexFile(), FileSystem);
		}

		internal static byte[] GitInternalSlash(byte[] bytes)
		{
			if (FilePath.separatorChar == '/')
			{
				return bytes;
			}
			for (int i = 0; i < bytes.Length; ++i)
			{
				if (bytes[i] == FilePath.separatorChar)
				{
					bytes[i] = (byte)('/');
				}
			}
			return bytes;
		}

		/// <returns>an important state</returns>
		public virtual RepositoryState GetRepositoryState()
		{
			if (IsBare || Directory == null)
			{
				return RepositoryState.BARE;
			}
			// Pre Git-1.6 logic
			if (new FilePath(WorkTree, ".dotest").Exists())
			{
				return RepositoryState.REBASING;
			}
			if (new FilePath(Directory, ".dotest-merge").Exists())
			{
				return RepositoryState.REBASING_INTERACTIVE;
			}
			// From 1.6 onwards
			if (new FilePath(Directory, "rebase-apply/rebasing").Exists())
			{
				return RepositoryState.REBASING_REBASING;
			}
			if (new FilePath(Directory, "rebase-apply/applying").Exists())
			{
				return RepositoryState.APPLY;
			}
			if (new FilePath(Directory, "rebase-apply").Exists())
			{
				return RepositoryState.REBASING;
			}
			if (new FilePath(Directory, "rebase-merge/interactive").Exists())
			{
				return RepositoryState.REBASING_INTERACTIVE;
			}
			if (new FilePath(Directory, "rebase-merge").Exists())
			{
				return RepositoryState.REBASING_MERGE;
			}
			// Both versions
			if (new FilePath(Directory, Constants.MERGE_HEAD).Exists())
			{
				// we are merging - now check whether we have unmerged paths
				try
				{
					if (!ReadDirCache().HasUnmergedPaths())
					{
						// no unmerged paths -> return the MERGING_RESOLVED state
						return RepositoryState.MERGING_RESOLVED;
					}
				}
				catch (IOException e)
				{
					// Can't decide whether unmerged paths exists. Return
					// MERGING state to be on the safe side (in state MERGING
					// you are not allow to do anything)
					Sharpen.Runtime.PrintStackTrace(e);
				}
				return RepositoryState.MERGING;
			}
			if (new FilePath(Directory, "BISECT_LOG").Exists())
			{
				return RepositoryState.BISECTING;
			}
			if (new FilePath(Directory, Constants.CHERRY_PICK_HEAD).Exists())
			{
				try
				{
					if (!ReadDirCache().HasUnmergedPaths())
					{
						// no unmerged paths
						return RepositoryState.CHERRY_PICKING_RESOLVED;
					}
				}
				catch (IOException e)
				{
					// fall through to CHERRY_PICKING
					Sharpen.Runtime.PrintStackTrace(e);
				}
				return RepositoryState.CHERRY_PICKING;
			}
			return RepositoryState.SAFE;
		}

		/// <summary>Check validity of a ref name.</summary>
		/// <remarks>
		/// Check validity of a ref name. It must not contain character that has
		/// a special meaning in a Git object reference expression. Some other
		/// dangerous characters are also excluded.
		/// For portability reasons '\' is excluded
		/// </remarks>
		/// <param name="refName"></param>
		/// <returns>true if refName is a valid ref name</returns>
		public static bool IsValidRefName(string refName)
		{
			int len = refName.Length;
			if (len == 0)
			{
				return false;
			}
			if (refName.EndsWith(".lock"))
			{
				return false;
			}
			int components = 1;
			char p = '\0';
			for (int i = 0; i < len; i++)
			{
				char c = refName[i];
				if (c <= ' ')
				{
					return false;
				}
				switch (c)
				{
					case '.':
					{
						switch (p)
						{
							case '\0':
							case '/':
							case '.':
							{
								return false;
							}
						}
						if (i == len - 1)
						{
							return false;
						}
						break;
					}

					case '/':
					{
						if (i == 0 || i == len - 1)
						{
							return false;
						}
						components++;
						break;
					}

					case '{':
					{
						if (p == '@')
						{
							return false;
						}
						break;
					}

					case '~':
					case '^':
					case ':':
					case '?':
					case '[':
					case '*':
					case '\\':
					{
						return false;
					}
				}
				p = c;
			}
			return components > 1;
		}

		/// <summary>Strip work dir and return normalized repository path.</summary>
		/// <remarks>Strip work dir and return normalized repository path.</remarks>
		/// <param name="workDir">Work dir</param>
		/// <param name="file">File whose path shall be stripped of its workdir</param>
		/// <returns>
		/// normalized repository relative path or the empty
		/// string if the file is not relative to the work directory.
		/// </returns>
		public static string StripWorkDir(FilePath workDir, FilePath file)
		{
			string filePath = file.GetPath();
			string workDirPath = workDir.GetPath();
			if (filePath.Length <= workDirPath.Length || filePath[workDirPath.Length] != FilePath
				.separatorChar || !filePath.StartsWith(workDirPath))
			{
				FilePath absWd = workDir.IsAbsolute() ? workDir : workDir.GetAbsoluteFile();
				FilePath absFile = file.IsAbsolute() ? file : file.GetAbsoluteFile();
				if (absWd == workDir && absFile == file)
				{
					return string.Empty;
				}
				return StripWorkDir(absWd, absFile);
			}
			string relName = Sharpen.Runtime.Substring(filePath, workDirPath.Length + 1);
			if (FilePath.separatorChar != '/')
			{
				relName = relName.Replace(FilePath.separatorChar, '/');
			}
			return relName;
		}

		/// <returns>true if this is bare, which implies it has no working directory.</returns>
		public virtual bool IsBare
		{
			get
			{
				return workTree == null;
			}
		}

		/// <returns>
		/// the root directory of the working tree, where files are checked
		/// out for viewing and editing.
		/// </returns>
		/// <exception cref="NGit.Errors.NoWorkTreeException">
		/// if this is bare, which implies it has no working directory.
		/// See
		/// <see cref="IsBare()">IsBare()</see>
		/// .
		/// </exception>
		public virtual FilePath WorkTree
		{
			get
			{
				if (IsBare)
				{
					throw new NoWorkTreeException();
				}
				return workTree;
			}
		}

		/// <summary>Force a scan for changed refs.</summary>
		/// <remarks>Force a scan for changed refs.</remarks>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public abstract void ScanForRepoChanges();

		/// <param name="refName"></param>
		/// <returns>a more user friendly ref name</returns>
		public static string ShortenRefName(string refName)
		{
			if (refName.StartsWith(Constants.R_HEADS))
			{
				return Sharpen.Runtime.Substring(refName, Constants.R_HEADS.Length);
			}
			if (refName.StartsWith(Constants.R_TAGS))
			{
				return Sharpen.Runtime.Substring(refName, Constants.R_TAGS.Length);
			}
			if (refName.StartsWith(Constants.R_REMOTES))
			{
				return Sharpen.Runtime.Substring(refName, Constants.R_REMOTES.Length);
			}
			return refName;
		}

		/// <param name="refName"></param>
		/// <returns>
		/// a
		/// <see cref="NGit.Storage.File.ReflogReader">NGit.Storage.File.ReflogReader</see>
		/// for the supplied refname, or null if the
		/// named ref does not exist.
		/// </returns>
		/// <exception cref="System.IO.IOException">the ref could not be accessed.</exception>
		public abstract ReflogReader GetReflogReader(string refName);

		/// <summary>Return the information stored in the file $GIT_DIR/MERGE_MSG.</summary>
		/// <remarks>
		/// Return the information stored in the file $GIT_DIR/MERGE_MSG. In this
		/// file operations triggering a merge will store a template for the commit
		/// message of the merge commit.
		/// </remarks>
		/// <returns>
		/// a String containing the content of the MERGE_MSG file or
		/// <code>null</code>
		/// if this file doesn't exist
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		/// <exception cref="NGit.Errors.NoWorkTreeException">
		/// if this is bare, which implies it has no working directory.
		/// See
		/// <see cref="IsBare()">IsBare()</see>
		/// .
		/// </exception>
		public virtual string ReadMergeCommitMsg()
		{
			if (IsBare || Directory == null)
			{
				throw new NoWorkTreeException();
			}
			FilePath mergeMsgFile = new FilePath(Directory, Constants.MERGE_MSG);
			try
			{
				return RawParseUtils.Decode(IOUtil.ReadFully(mergeMsgFile));
			}
			catch (FileNotFoundException)
			{
				// MERGE_MSG file has disappeared in the meantime
				// ignore it
				return null;
			}
		}

		/// <summary>Write new content to the file $GIT_DIR/MERGE_MSG.</summary>
		/// <remarks>
		/// Write new content to the file $GIT_DIR/MERGE_MSG. In this file operations
		/// triggering a merge will store a template for the commit message of the
		/// merge commit. If <code>null</code> is specified as message the file will
		/// be deleted
		/// </remarks>
		/// <param name="msg">
		/// the message which should be written or <code>null</code> to
		/// delete the file
		/// </param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void WriteMergeCommitMsg(string msg)
		{
			FilePath mergeMsgFile = new FilePath(gitDir, Constants.MERGE_MSG);
			if (msg != null)
			{
				FileOutputStream fos = new FileOutputStream(mergeMsgFile);
				try
				{
					fos.Write(Sharpen.Runtime.GetBytesForString(msg, Constants.CHARACTER_ENCODING));
				}
				finally
				{
					fos.Close();
				}
			}
			else
			{
				FileUtils.Delete(mergeMsgFile);
			}
		}

		/// <summary>Return the information stored in the file $GIT_DIR/MERGE_HEAD.</summary>
		/// <remarks>
		/// Return the information stored in the file $GIT_DIR/MERGE_HEAD. In this
		/// file operations triggering a merge will store the IDs of all heads which
		/// should be merged together with HEAD.
		/// </remarks>
		/// <returns>
		/// a list of commits which IDs are listed in the MERGE_HEAD
		/// file or
		/// <code>null</code>
		/// if this file doesn't exist. Also if the file
		/// exists but is empty
		/// <code>null</code>
		/// will be returned
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		/// <exception cref="NGit.Errors.NoWorkTreeException">
		/// if this is bare, which implies it has no working directory.
		/// See
		/// <see cref="IsBare()">IsBare()</see>
		/// .
		/// </exception>
		public virtual IList<ObjectId> ReadMergeHeads()
		{
			if (IsBare || Directory == null)
			{
				throw new NoWorkTreeException();
			}
			byte[] raw = ReadGitDirectoryFile(Constants.MERGE_HEAD);
			if (raw == null)
			{
				return null;
			}
			List<ObjectId> heads = new List<ObjectId>();
			for (int p = 0; p < raw.Length; )
			{
				heads.AddItem(ObjectId.FromString(raw, p));
				p = RawParseUtils.NextLF(raw, p + Constants.OBJECT_ID_STRING_LENGTH);
			}
			return heads;
		}

		/// <summary>Write new merge-heads into $GIT_DIR/MERGE_HEAD.</summary>
		/// <remarks>
		/// Write new merge-heads into $GIT_DIR/MERGE_HEAD. In this file operations
		/// triggering a merge will store the IDs of all heads which should be merged
		/// together with HEAD. If <code>null</code> is specified as list of commits
		/// the file will be deleted
		/// </remarks>
		/// <param name="heads">
		/// a list of commits which IDs should be written to
		/// $GIT_DIR/MERGE_HEAD or <code>null</code> to delete the file
		/// </param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void WriteMergeHeads(IList<ObjectId> heads)
		{
			WriteHeadsFile(heads, Constants.MERGE_HEAD);
		}

		/// <summary>Return the information stored in the file $GIT_DIR/CHERRY_PICK_HEAD.</summary>
		/// <remarks>Return the information stored in the file $GIT_DIR/CHERRY_PICK_HEAD.</remarks>
		/// <returns>
		/// object id from CHERRY_PICK_HEAD file or
		/// <code>null</code>
		/// if this file
		/// doesn't exist. Also if the file exists but is empty
		/// <code>null</code>
		/// will be returned
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		/// <exception cref="NGit.Errors.NoWorkTreeException">
		/// if this is bare, which implies it has no working directory.
		/// See
		/// <see cref="IsBare()">IsBare()</see>
		/// .
		/// </exception>
		public virtual ObjectId ReadCherryPickHead()
		{
			if (IsBare || Directory == null)
			{
				throw new NoWorkTreeException();
			}
			byte[] raw = ReadGitDirectoryFile(Constants.CHERRY_PICK_HEAD);
			if (raw == null)
			{
				return null;
			}
			return ObjectId.FromString(raw, 0);
		}

		/// <summary>Write cherry pick commit into $GIT_DIR/CHERRY_PICK_HEAD.</summary>
		/// <remarks>
		/// Write cherry pick commit into $GIT_DIR/CHERRY_PICK_HEAD. This is used in
		/// case of conflicts to store the cherry which was tried to be picked.
		/// </remarks>
		/// <param name="head">
		/// an object id of the cherry commit or <code>null</code> to
		/// delete the file
		/// </param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void WriteCherryPickHead(ObjectId head)
		{
			IList<ObjectId> heads = (head != null) ? Sharpen.Collections.SingletonList(head) : 
				null;
			WriteHeadsFile(heads, Constants.CHERRY_PICK_HEAD);
		}

		/// <summary>Read a file from the git directory.</summary>
		/// <remarks>Read a file from the git directory.</remarks>
		/// <param name="filename"></param>
		/// <returns>the raw contents or null if the file doesn't exist or is empty</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		private byte[] ReadGitDirectoryFile(string filename)
		{
			FilePath file = new FilePath(Directory, filename);
			try
			{
				byte[] raw = IOUtil.ReadFully(file);
				return raw.Length > 0 ? raw : null;
			}
			catch (FileNotFoundException)
			{
				return null;
			}
		}

		/// <summary>Write the given heads to a file in the git directory.</summary>
		/// <remarks>Write the given heads to a file in the git directory.</remarks>
		/// <param name="heads">
		/// a list of object ids to write or null if the file should be
		/// deleted.
		/// </param>
		/// <param name="filename"></param>
		/// <exception cref="System.IO.FileNotFoundException">System.IO.FileNotFoundException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		private void WriteHeadsFile(IList<ObjectId> heads, string filename)
		{
			FilePath headsFile = new FilePath(Directory, filename);
			if (heads != null)
			{
				BufferedOutputStream bos = new BufferedOutputStream(new FileOutputStream(headsFile
					));
				try
				{
					foreach (ObjectId id in heads)
					{
						id.CopyTo(bos);
						bos.Write('\n');
					}
				}
				finally
				{
					bos.Close();
				}
			}
			else
			{
				FileUtils.Delete(headsFile, FileUtils.SKIP_MISSING);
			}
		}
	}
}
