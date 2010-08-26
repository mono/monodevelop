/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;
using GitSharp.Core.Util.JavaHelper;

namespace GitSharp.Core
{
    /// <summary>
    /// Represents a Git repository. A repository holds all objects and refs used for
    /// managing source code (could by any type of file, but source code is what
    /// SCM's are typically used for).
    /// <para />
    /// In Git terms all data is stored in GIT_DIR, typically a directory called
    /// .git. A work tree is maintained unless the repository is a bare repository.
    /// Typically the .git directory is located at the root of the work dir.
    /// <ul>
    /// <li>GIT_DIR
    /// 	<ul>
    /// 		<li>objects/ - objects</li>
    /// 		<li>refs/ - tags and heads</li>
    /// 		<li>config - configuration</li>
    /// 		<li>info/ - more configurations</li>
    /// 	</ul>
    /// </li>
    /// </ul>
    /// <para />
    /// This class is thread-safe.
    /// <para />
    /// This implementation only handles a subtly undocumented subset of git features.
    /// </summary>
    public class Repository : IDisposable
    {
        private AtomicInteger _useCnt = new AtomicInteger(1);
        internal readonly RefDatabase _refDb; // [henon] need internal for API

        private readonly ObjectDirectory _objectDatabase;

        private GitIndex _index;

        // private readonly List<DirectoryInfo> _objectsDirs; // never used.

        private List<RepositoryListener> listeners = new List<RepositoryListener>(); //TODO: make thread safe
        static private List<RepositoryListener> allListeners = new List<RepositoryListener>(); //TODO: make thread safe

        private DirectoryInfo workDir;

        private FileInfo indexFile;
        private readonly object _padLock = new object();

        /// <summary>
        /// Construct a representation of a Git repository.
        /// The work tree, object directory, alternate object directories and index file locations are deduced from the given git directory and the default rules.
        /// </summary>
        /// <param name="d">GIT_DIR (the location of the repository metadata).</param>
        public Repository(DirectoryInfo d)
            : this(d, null, null, null, null)  // go figure it out
        {
        }

        /// <summary>
        /// Construct a representation of a Git repository.
        /// The work tree, object directory, alternate object directories and index file locations are deduced from the given git directory and the default rules.
        /// </summary>
        /// <param name="d">GIT_DIR (the location of the repository metadata).</param>
        /// <param name="workTree">GIT_WORK_TREE (the root of the checkout). May be null for default value.</param>
        public Repository(DirectoryInfo d, DirectoryInfo workTree)
            : this(d, workTree, null, null, null) // go figure it out 
        {
        }

        /// <summary>
        /// Construct a representation of a Git repository using the given parameters possibly overriding default conventions..
        /// </summary>
        /// <param name="d">GIT_DIR (the location of the repository metadata). May be null for default value in which case it depends on GIT_WORK_TREE.</param>
        /// <param name="workTree">GIT_WORK_TREE (the root of the checkout). May be null for default value if GIT_DIR is</param>
        /// <param name="objectDir">GIT_OBJECT_DIRECTORY (where objects and are stored). May be null for default value. Relative names ares resolved against GIT_WORK_TREE</param>
        /// <param name="alternateObjectDir">GIT_ALTERNATE_OBJECT_DIRECTORIES (where more objects are read from). May be null for default value. Relative names ares resolved against GIT_WORK_TREE</param>
        /// <param name="indexFile">GIT_INDEX_FILE (the location of the index file). May be null for default value. Relative names ares resolved against GIT_WORK_TREE.</param>
        public Repository(DirectoryInfo d, DirectoryInfo workTree, DirectoryInfo objectDir,
                DirectoryInfo[] alternateObjectDir, FileInfo indexFile)
        {
            if (workTree != null)
            {
                workDir = workTree;
                if (d == null)
                    Directory = PathUtil.CombineDirectoryPath(workTree, Constants.DOT_GIT);
                else
                    Directory = d;
            }
            else
            {
                if (d != null)
                    Directory = d;
                else
                {
                    Dispose();
                    throw new ArgumentException("Either GIT_DIR or GIT_WORK_TREE must be passed to Repository constructor");
                }
            }

            var userConfig = SystemReader.getInstance().openUserConfig();

            try
            {
                userConfig.load();
            }
            catch (ConfigInvalidException e1)
            {
                Dispose();

                throw new IOException("User config file "
                    + userConfig.getFile().FullName + " invalid: "
                    + e1, e1);
            }

            Config = new RepositoryConfig(userConfig, (FileInfo)FS.resolve(Directory, "config"));

            try
            {
                Config.load();
            }
            catch (ConfigInvalidException e1)
            {
                Dispose();
                throw new IOException("Unknown repository format", e1);
            }

            if (workDir == null)
            {
                String workTreeConfig = Config.getString("core", null, "worktree");
                if (workTreeConfig != null)
                {
                    workDir = (DirectoryInfo)FS.resolve(d, workTreeConfig);
                }
                else
                {
                    workDir = Directory.Parent;
                }
            }

            _refDb = new RefDirectory(this);
            if (objectDir != null)
                _objectDatabase = new ObjectDirectory(PathUtil.CombineDirectoryPath(objectDir, ""),
                        alternateObjectDir);
            else
                _objectDatabase = new ObjectDirectory(PathUtil.CombineDirectoryPath(Directory, "objects"),
                        alternateObjectDir);

            if (indexFile != null)
                this.indexFile = indexFile;
            else
                this.indexFile = PathUtil.CombineFilePath(Directory, "index");

            if (_objectDatabase.exists())
            {
                string repositoryFormatVersion = Config.getString("core", null, "repositoryFormatVersion");

                if (!"0".Equals(repositoryFormatVersion))
                {
                    Dispose();
                    throw new IOException("Unknown repository format \""
                                          + repositoryFormatVersion + "\"; expected \"0\".");
                }
            }
        }

        /// <summary>
        /// Create a new Git repository initializing the necessary files and
        /// directories.
        /// </summary>
        public void Create()
        {
            lock (_padLock)
            {
                Create(false);
            }
        }

        /// <summary>
        /// Create a new Git repository initializing the necessary files and
        /// directories.
        /// </summary>
        /// <param name="bare">if true, a bare repository is created.</param>
        public bool Create(bool bare)
        {
            var reinit = false;

            if (Config.getFile().Exists)
            {
                reinit = true;
            }

            if (!reinit)
            {
                Directory.Mkdirs();
                _refDb.create();
                _objectDatabase.create();

                RefUpdate head = UpdateRef(Constants.HEAD);
                head.disableRefLog();
                head.link(Constants.R_HEADS + Constants.MASTER);
            }

            Config.setInt("core", null, "repositoryformatversion", 0);
            Config.setBoolean("core", null, "filemode", true);
            Config.setBoolean("core", null, "bare", bare);
            Config.setBoolean("core", null, "logallrefupdates", !bare);
            Config.setBoolean("core", null, "autocrlf", false);

            Config.save();

            return reinit;
        }

        public DirectoryInfo ObjectsDirectory
        {
            get { return _objectDatabase.getDirectory(); }
        }

        public DirectoryInfo Directory { get; private set; }
        public DirectoryInfo WorkingDirectory { get { return workDir; } }

        /// <summary>
        /// Override default workdir
        /// </summary>
        /// <param name="workTree">the work tree directory</param>
        public void setWorkDir(DirectoryInfo workTree)
        {
            workDir = workTree;
        }

        public RepositoryConfig Config { get; private set; }

        /// <summary>
        /// Construct a filename where the loose object having a specified SHA-1
        /// should be stored. If the object is stored in a shared repository the path
        /// to the alternative repo will be returned. If the object is not yet store
        /// a usable path in this repo will be returned. It is assumed that callers
        /// will look for objects in a pack first.
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns>Suggested file name</returns>
        public FileInfo ToFile(AnyObjectId objectId)
        {
            return _objectDatabase.fileFor(objectId);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns>
        /// true if the specified object is stored in this repo or any of the
        /// known shared repositories.
        /// </returns>
        public bool HasObject(AnyObjectId objectId)
        {
            return _objectDatabase.hasObject(objectId);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="windowCursor">
        /// Temporary working space associated with the calling thread.
        /// </param>
        /// <param name="id">SHA-1 of an object.</param>
        /// <returns>
        /// A <see cref="ObjectLoader"/> for accessing the data of the named
        /// object, or null if the object does not exist.
        /// </returns>
        public ObjectLoader OpenObject(WindowCursor windowCursor, AnyObjectId id)
        {
            return _objectDatabase.openObject(windowCursor, id);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="id">SHA-1 of an object.</param>
        /// <returns>
        /// A <see cref="ObjectLoader"/> for accessing the data of the named
        /// object, or null if the object does not exist.
        /// </returns>
        public ObjectLoader OpenObject(AnyObjectId id)
        {
            var wc = new WindowCursor();
            try
            {
                return OpenObject(wc, id);
            }
            finally
            {
                wc.Release();
            }
        }

        /// <summary>
        /// Open object in all packs containing specified object.
        /// </summary>
        /// <param name="objectId">id of object to search for</param>
        /// <param name="windowCursor">
        /// Temporary working space associated with the calling thread.
        /// </param>
        /// <returns>
        /// Collection of loaders for this object, from all packs containing
        /// this object
        /// </returns>
        public IEnumerable<PackedObjectLoader> OpenObjectInAllPacks(AnyObjectId objectId, WindowCursor windowCursor)
        {
            var result = new List<PackedObjectLoader>();
            OpenObjectInAllPacks(objectId, result, windowCursor);
            return result;
        }

        /// <summary>
        /// Open object in all packs containing specified object.
        /// </summary>
        /// <param name="objectId"><see cref="ObjectId"/> of object to search for</param>
        /// <param name="resultLoaders">
        /// Result collection of loaders for this object, filled with
        /// loaders from all packs containing specified object
        /// </param>
        /// <param name="windowCursor">
        /// Temporary working space associated with the calling thread.
        /// </param>
        public void OpenObjectInAllPacks(AnyObjectId objectId, ICollection<PackedObjectLoader> resultLoaders,
                                         WindowCursor windowCursor)
        {
            _objectDatabase.OpenObjectInAllPacks(resultLoaders, windowCursor, objectId);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="id">SHA'1 of a blob</param>
        /// <returns>
        /// An <see cref="ObjectLoader"/> for accessing the data of a named blob
        /// </returns>
        public ObjectLoader OpenBlob(ObjectId id)
        {
            return OpenObject(id);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="id">SHA'1 of a tree</param>
        /// <returns>
        /// An <see cref="ObjectLoader"/> for accessing the data of a named tree
        /// </returns>
        public ObjectLoader OpenTree(ObjectId id)
        {
            return OpenObject(id);
        }

        /// <summary>
        /// Access a Commit object using a symbolic reference. This reference may
        /// be a SHA-1 or ref in combination with a number of symbols translating
        /// from one ref or SHA1-1 to another, such as HEAD^ etc.
        /// </summary>
        /// <param name="resolveString">a reference to a git commit object</param>
        /// <returns>A <see cref="Commit"/> named by the specified string</returns>
        public Commit MapCommit(string resolveString)
        {
            ObjectId id = Resolve(resolveString);
            return id != null ? MapCommit(id) : null;
        }

        /// <summary>
        /// Access a Commit by SHA'1 id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Commit or null</returns>
        public Commit MapCommit(ObjectId id)
        {
            ObjectLoader or = OpenObject(id);
            if (or == null)
            {
                return null;
            }

            byte[] raw = or.Bytes;
            if (Constants.OBJ_COMMIT == or.Type)
            {
                return new Commit(this, id, raw);
            }

            throw new IncorrectObjectTypeException(id, ObjectType.Commit);
        }

        /// <summary>
        /// Access any type of Git object by id and
        /// </summary>
        /// <param name="id">SHA-1 of object to read</param>
        /// <param name="refName">optional, only relevant for simple tags</param>
        /// <returns>The Git object if found or null</returns>
        public object MapObject(ObjectId id, string refName)
        {
            ObjectLoader or = OpenObject(id);

            if (or == null)
            {
                return null;
            }

            byte[] raw = or.Bytes;
            switch ((ObjectType)(or.Type))
            {
                case ObjectType.Tree:
                    return MakeTree(id, raw);

                case ObjectType.Commit:
                    return MakeCommit(id, raw);

                case ObjectType.Tag:
                    return MakeTag(id, refName, raw);

                case ObjectType.Blob:
                    return raw;

                default:
                    throw new IncorrectObjectTypeException(id,
                        "COMMIT nor TREE nor BLOB nor TAG");
            }
        }

        private object MakeCommit(ObjectId id, byte[] raw)
        {
            return new Commit(this, id, raw);
        }

        /// <summary>
        /// Access a Tree object using a symbolic reference. This reference may
        /// be a SHA-1 or ref in combination with a number of symbols translating
        /// from one ref or SHA1-1 to another, such as HEAD^{tree} etc.
        /// </summary>
        /// <param name="revstr">a reference to a git commit object</param>
        /// <returns>a Tree named by the specified string</returns>
        public Tree MapTree(string revstr)
        {
            ObjectId id = Resolve(revstr);
            return (id != null) ? MapTree(id) : null;
        }

        /// <summary>
        /// Access a Tree by SHA'1 id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Tree or null</returns>
        public Tree MapTree(ObjectId id)
        {
            ObjectLoader or = OpenObject(id);
            if (or == null)
            {
                return null;
            }

            byte[] raw = or.Bytes;
            switch (((ObjectType)or.Type))
            {
                case ObjectType.Tree:
                    return new Tree(this, id, raw);

                case ObjectType.Commit:
                    return MapTree(ObjectId.FromString(raw, 5));
            }

            throw new IncorrectObjectTypeException(id, ObjectType.Tree);
        }

        private Tree MakeTree(ObjectId id, byte[] raw)
        {
            return new Tree(this, id, raw);
        }

        private Tag MakeTag(ObjectId id, string refName, byte[] raw)
        {
            return new Tag(this, id, refName, raw);
        }


        /// <summary>
        /// Access a tag by symbolic name.
        /// </summary>
        /// <param name="revstr"></param>
        /// <returns>Tag or null</returns>
        public Tag MapTag(string revstr)
        {
            ObjectId id = Resolve(revstr);
            return id != null ? MapTag(revstr, id) : null;
        }

        /// <summary>
        /// Access a Tag by SHA'1 id
        /// </summary>
        /// <param name="refName"></param>
        /// <param name="id"></param>
        /// <returns>Commit or null</returns>
        public Tag MapTag(string refName, ObjectId id)
        {
            ObjectLoader or = OpenObject(id);
            if (or == null) return null;

            byte[] raw = or.Bytes;

            if (ObjectType.Tag == (ObjectType)or.Type)
            {
                return new Tag(this, id, refName, raw);
            }

            return new Tag(this, id, refName, null);
        }

        /// <summary>
        /// Create a command to update (or create) a ref in this repository.
        /// </summary>
        /// <param name="refName">
        /// name of the ref the caller wants to modify.
        /// </param>
        /// <returns>
        /// An update command. The caller must finish populating this command
        /// and then invoke one of the update methods to actually make a
        /// change.
        /// </returns>
        public RefUpdate UpdateRef(string refName)
        {
            return UpdateRef(refName, false);
        }

        /// <summary>
        /// Create a command to update, create or delete a ref in this repository.
        /// </summary>
        /// <param name="refName">name of the ref the caller wants to modify.</param>
        /// <param name="detach">true to create a detached head</param>
        /// <returns>An update command. The caller must finish populating this command and then invoke one of the update methods to actually make a change.</returns>
        public RefUpdate UpdateRef(string refName, bool detach)
        {
            return _refDb.newUpdate(refName, detach);
        }

        ///	<summary>
        /// Create a command to rename a ref in this repository
        ///	</summary>
        ///	<param name="fromRef">Name of ref to rename from.</param>
        ///	<param name="toRef">Name of ref to rename to.</param>
        ///	<returns>
        /// An update command that knows how to rename a branch to another.
        /// </returns>
        ///	<exception cref="IOException">The rename could not be performed.</exception>
        public RefRename RenameRef(string fromRef, string toRef)
        {
            return _refDb.newRename(fromRef, toRef);
        }

        ///	<summary>
        /// Parse a git revision string and return an object id.
        ///	<para />
        ///	Currently supported is combinations of these.
        ///	<ul>
        ///	 <li>SHA-1 - a SHA-1</li>
        ///	 <li>refs/... - a ref name</li>
        ///	 <li>ref^n - nth parent reference</li>
        ///	 <li>ref~n - distance via parent reference</li>
        ///	 <li>ref@{n} - nth version of ref</li>
        ///	 <li>ref^{tree} - tree references by ref</li>
        ///	 <li>ref^{commit} - commit references by ref</li>
        ///	</ul>
        ///	<para />
        ///	Not supported is
        ///	<ul>
        ///	 <li>timestamps in reflogs, ref@{full or relative timestamp}</li>
        ///	 <li>abbreviated SHA-1's</li>
        ///	</ul>
        ///	</summary>
        ///	<param name="revision">A git object references expression.</param>
        ///	<returns>
        /// An <see cref="ObjectId"/> or null if revstr can't be resolved to any <see cref="ObjectId"/>.
        /// </returns>
        ///	<exception cref="IOException">On serious errors.</exception>
        public ObjectId Resolve(string revision)
        {
            object oref = null;
            ObjectId refId = null;

            // [ammachado] Avoid the loop if the reference is not a special one.
            if (revision.IndexOfAny(new[] { '^', '~', '@' }) == -1)
            {
                return ResolveSimple(revision);
            }

            for (int i = 0; i < revision.Length; ++i)
            {
                switch (revision[i])
                {
                    case '^':
                        if (refId == null)
                        {
                            var refstr = new string(revision.ToCharArray(0, i));
                            refId = ResolveSimple(refstr);
                            if (refId == null) return null;
                        }

                        if (i + 1 < revision.Length)
                        {
                            switch (revision[i + 1])
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

                                    int j;
                                    oref = MapObject(refId, null);

                                    while (oref is Tag)
                                    {
                                        var tag = (Tag)oref;
                                        refId = tag.Id;
                                        oref = MapObject(refId, null);
                                    }

                                    Commit oCom = (oref as Commit);
                                    if (oCom == null)
                                    {
                                        throw new IncorrectObjectTypeException(refId, ObjectType.Commit);
                                    }

                                    for (j = i + 1; j < revision.Length; ++j)
                                    {
                                        if (!Char.IsDigit(revision[j])) break;
                                    }

                                    var parentnum = new string(revision.ToCharArray(i + 1, j - i - 1));

                                    int pnum;

                                    try
                                    {
                                        pnum = Convert.ToInt32(parentnum);
                                    }
                                    catch (FormatException)
                                    {
                                        throw new RevisionSyntaxException(revision, "Invalid commit parent number");
                                    }
                                    if (pnum != 0)
                                    {
                                        ObjectId[] parents = oCom.ParentIds;
                                        if (pnum > parents.Length)
                                            refId = null;
                                        else
                                            refId = parents[pnum - 1];
                                    }

                                    i = j - 1;
                                    break;

                                case '{':
                                    int k;
                                    string item = null;
                                    for (k = i + 2; k < revision.Length; ++k)
                                    {
                                        if (revision[k] != '}') continue;
                                        item = new string(revision.ToCharArray(i + 2, k - i - 2));
                                        break;
                                    }

                                    i = k;
                                    if (item != null)
                                    {
                                        if (item.Equals("tree"))
                                        {
                                            oref = MapObject(refId, null);
                                            while (oref is Tag)
                                            {
                                                var t = (Tag)oref;
                                                refId = t.Id;
                                                oref = MapObject(refId, null);
                                            }
                                            Treeish oTree = (oref as Treeish);
                                            if (oTree != null)
                                            {
                                                refId = oTree.TreeId;
                                            }
                                            else
                                            {
                                                throw new IncorrectObjectTypeException(refId, ObjectType.Tree);
                                            }
                                        }
                                        else if (item.Equals("commit"))
                                        {
                                            oref = MapObject(refId, null);
                                            while (oref is Tag)
                                            {
                                                var t = (Tag)oref;
                                                refId = t.Id;
                                                oref = MapObject(refId, null);
                                            }
                                            if (!(oref is Commit))
                                            {
                                                throw new IncorrectObjectTypeException(refId, ObjectType.Commit);
                                            }
                                        }
                                        else if (item.Equals("blob"))
                                        {
                                            oref = MapObject(refId, null);
                                            while (oref is Tag)
                                            {
                                                var t = (Tag)oref;
                                                refId = t.Id;
                                                oref = MapObject(refId, null);
                                            }
                                            if (!(oref is byte[]))
                                            {
                                                throw new IncorrectObjectTypeException(refId, ObjectType.Commit);
                                            }
                                        }
                                        else if (string.Empty.Equals(item))
                                        {
                                            oref = MapObject(refId, null);
                                            while (oref is Tag)
                                            {
                                                var t = (Tag)oref;
                                                refId = t.Id;
                                                oref = MapObject(refId, null);
                                            }
                                        }
                                        else
                                        {
                                            throw new RevisionSyntaxException(revision);
                                        }
                                    }
                                    else
                                    {
                                        throw new RevisionSyntaxException(revision);
                                    }
                                    break;

                                default:
                                    oref = MapObject(refId, null);
                                    Commit oComm = (oref as Commit);
                                    if (oComm != null)
                                    {
                                        ObjectId[] parents = oComm.ParentIds;
                                        refId = parents.Length == 0 ? null : parents[0];
                                    }
                                    else
                                    {
                                        throw new IncorrectObjectTypeException(refId, ObjectType.Commit);
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            oref = MapObject(refId, null);
                            while (oref is Tag)
                            {
                                var tag = (Tag)oref;
                                refId = tag.Id;
                                oref = MapObject(refId, null);
                            }

                            Commit oCom = (oref as Commit);
                            if (oCom != null)
                            {
                                ObjectId[] parents = oCom.ParentIds;
                                refId = parents.Length == 0 ? null : parents[0];
                            }
                            else
                            {
                                throw new IncorrectObjectTypeException(refId, Constants.TYPE_COMMIT);
                            }
                        }
                        break;

                    case '~':
                        if (oref == null)
                        {
                            var refstr = new string(revision.ToCharArray(0, i));
                            refId = ResolveSimple(refstr);
                            if (refId == null) return null;
                            oref = MapObject(refId, null);
                        }

                        while (oref is Tag)
                        {
                            var tag = (Tag)oref;
                            refId = tag.Id;
                            oref = MapObject(refId, null);
                        }

                        if (!(oref is Commit))
                        {
                            throw new IncorrectObjectTypeException(refId, Constants.TYPE_COMMIT);
                        }

                        int l;
                        for (l = i + 1; l < revision.Length; ++l)
                        {
                            if (!Char.IsDigit(revision[l]))
                                break;
                        }

                        var distnum = new string(revision.ToCharArray(i + 1, l - i - 1));
                        int dist;

                        try
                        {
                            dist = Convert.ToInt32(distnum);
                        }
                        catch (FormatException)
                        {
                            throw new RevisionSyntaxException("Invalid ancestry length", revision);
                        }
                        while (dist > 0)
                        {

                            ObjectId[] parents = ((Commit)oref).ParentIds;
                            if (parents.Length == 0)
                            {
                                refId = null;
                                break;
                            }
                            refId = parents[0];
                            oref = MapCommit(refId);
                            --dist;
                        }
                        i = l - 1;
                        break;

                    case '@':
                        int m;
                        string time = null;
                        for (m = i + 2; m < revision.Length; ++m)
                        {
                            if (revision[m] != '}') continue;
                            time = new string(revision.ToCharArray(i + 2, m - i - 2));
                            break;
                        }

                        if (time != null)
                        {
                            throw new RevisionSyntaxException("reflogs not yet supported by revision parser yet", revision);
                        }
                        i = m - 1;
                        break;

                    default:
                        if (refId != null)
                        {
                            throw new RevisionSyntaxException(revision);
                        }
                        break;
                }
            }

            if (refId == null)
            {
                refId = ResolveSimple(revision);
            }

            return refId;
        }

        private ObjectId ResolveSimple(string revstr)
        {
            if (ObjectId.IsId(revstr))
            {
                return ObjectId.FromString(revstr);
            }
            Ref r = _refDb.getRef(revstr);
            return r != null ? r.ObjectId : null;
        }

        public void IncrementOpen()
        {
            _useCnt.incrementAndGet();
        }

        /// <summary>
        /// Close all resources used by this repository
        /// </summary>
        public void Close()
        {
            int usageCount = _useCnt.decrementAndGet();
            if (usageCount == 0)
            {
                if (_objectDatabase != null)
                {
                    _objectDatabase.Dispose();
                    _refDb.Dispose();
                }
#if DEBUG
                GC.SuppressFinalize(this); // Disarm lock-release checker
#endif
            }
        }

#if DEBUG
        // A debug mode warning if the type has not been disposed properly
        ~Repository()
        {
            Console.Error.WriteLine(GetType().Name + " has not been properly disposed: " + Directory);
        }
#endif

        public void OpenPack(FileInfo pack, FileInfo idx)
        {
            _objectDatabase.openPack(pack, idx);
        }

        public ObjectDirectory ObjectDatabase
        {
            get { return _objectDatabase; }
        }

        /// <summary>
        /// The reference database which stores the reference namespace.
        /// </summary>
        public RefDatabase RefDatabase
        {
            get { return _refDb; }
        }

        /// <summary>
        /// Gets a representation of the index associated with this repo
        /// </summary>
        public GitIndex Index
        {
            get
            {
                if (_index == null)
                {
                    _index = new GitIndex(this);
                    _index.Read();
                }
                else
                {
                    _index.RereadIfNecessary();
                }

                return _index;
            }
        }

        /// <returns>the index file location</returns>
        public FileInfo getIndexFile()
        {
            return indexFile;
        }

        /// <summary>
        /// Replaces any windows director separators (backslash) with /
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        internal static byte[] GitInternalSlash(byte[] bytes)
        {
            if (Path.DirectorySeparatorChar == '/') // [henon] DirectorySeparatorChar == \
            {
                return bytes;
            }

            for (int i = 0; i < bytes.Length; ++i)
            {
                if (bytes[i] == Path.DirectorySeparatorChar)
                {
                    bytes[i] = (byte)'/';
                }
            }

            return bytes;
        }

        /// <summary>
        /// Strip work dir and return normalized repository path
        /// </summary>
        /// <param name="workDir">Work directory</param>
        /// <param name="file">File whose path shall be stripp off it's workdir</param>
        /// <returns>Normalized repository relative path</returns>
        public static string StripWorkDir(FileSystemInfo workDir, FileSystemInfo file)
        {
            string filePath = file.DirectoryName();
            string workDirPath = workDir.DirectoryName();

            if (filePath.Length <= workDirPath.Length ||
                filePath[workDirPath.Length] != Path.DirectorySeparatorChar ||
                !filePath.StartsWith(workDirPath))
            {
                FileSystemInfo absWd = new DirectoryInfo(workDir.DirectoryName());
                FileSystemInfo absFile = new FileInfo(file.FullName);

                if (absWd.FullName == workDir.FullName && absFile.FullName == file.FullName)
                {
                    return string.Empty;
                }

                return StripWorkDir(absWd, absFile);
            }

            string relName = filePath.Substring(workDirPath.Length + 1);

            if (Path.DirectorySeparatorChar != '/')
            {
                relName = relName.Replace(Path.DirectorySeparatorChar, '/');
            }

            return relName;
        }

        /**
	     * Register a {@link RepositoryListener} which will be notified
	     * when ref changes are detected.
	     *
	     * @param l
	     */
        public void addRepositoryChangedListener(RepositoryListener l)
        {
            listeners.Add(l);
        }

        /**
         * Remove a registered {@link RepositoryListener}
         * @param l
         */
        public void removeRepositoryChangedListener(RepositoryListener l)
        {
            listeners.Remove(l);
        }

        /**
         * Register a global {@link RepositoryListener} which will be notified
         * when a ref changes in any repository are detected.
         *
         * @param l
         */
        public static void addAnyRepositoryChangedListener(RepositoryListener l)
        {
            allListeners.Add(l);
        }

        /**
         * Remove a globally registered {@link RepositoryListener}
         * @param l
         */
        public static void removeAnyRepositoryChangedListener(RepositoryListener l)
        {
            allListeners.Remove(l);
        }

        internal void fireRefsChanged()
        {
            var @event = new RefsChangedEventArgs(this);
            List<RepositoryListener> all;
            lock (listeners)
            {
                all = new List<RepositoryListener>(listeners);
            }
            lock (allListeners)
            {
                all.AddRange(allListeners);
            }
            foreach (RepositoryListener l in all)
            {
                l.refsChanged(@event);
            }
        }

        internal void fireIndexChanged()
        {
            var @event = new IndexChangedEventArgs(this);
            List<RepositoryListener> all;
            lock (listeners)
            {
                all = new List<RepositoryListener>(listeners);
            }
            lock (allListeners)
            {
                all.AddRange(allListeners);
            }
            foreach (RepositoryListener l in all)
            {
                l.indexChanged(@event);
            }
        }

        /**
 * Force a scan for changed refs.
 *
 * @throws IOException
 */
        public void scanForRepoChanges()
        {
            getAllRefs(); // This will look for changes to refs
            var index = Index; // This will detect changes in the index
        }
        /// <summary>
        /// Gets the <see cref="Repository"/> state
        /// </summary>
        public RepositoryState RespositoryState
        {
            get
            {
                // Pre Git-1.6 logic
                if (new FileInfo(Path.Combine(WorkingDirectory.FullName, ".dotest")).Exists)
                {
                    return RepositoryState.Rebasing;
                }

                if (new FileInfo(Path.Combine(WorkingDirectory.FullName, ".dotest-merge")).Exists)
                {
                    return RepositoryState.RebasingInteractive;
                }

                // From 1.6 onwards
                if (new FileInfo(Path.Combine(WorkingDirectory.FullName, "rebase-apply/rebasing")).Exists)
                {
                    return RepositoryState.RebasingRebasing;
                }

                if (new FileInfo(Path.Combine(WorkingDirectory.FullName, "rebase-apply/applying")).Exists)
                {
                    return RepositoryState.Apply;
                }

                if (new FileInfo(Path.Combine(WorkingDirectory.FullName, "rebase-apply")).Exists)
                {
                    return RepositoryState.Rebasing;
                }


                if (new FileInfo(Path.Combine(WorkingDirectory.FullName, "rebase-merge/interactive")).Exists)
                {
                    return RepositoryState.RebasingInteractive;
                }

                if (new FileInfo(Path.Combine(WorkingDirectory.FullName, "rebase-merge")).Exists)
                {
                    return RepositoryState.RebasingMerge;
                }

                // Both versions
                if (new FileInfo(Path.Combine(WorkingDirectory.FullName, "MERGE_HEAD")).Exists)
                {
                    return RepositoryState.Merging;
                }

                if (new FileInfo(Path.Combine(WorkingDirectory.FullName, "BISECT_LOG")).Exists)
                {
                    return RepositoryState.Bisecting;
                }

                return RepositoryState.Safe;
            }
        }

        /// <returns>mutable map of all known refs (heads, tags, remotes).</returns>
        public IDictionary<string, Ref> getAllRefs()
        {
            try
            {
                return _refDb.getRefs(RefDatabase.ALL);
            }
            catch (IOException)
            {
                return new Dictionary<string, Ref>();
            }
        }

        public Ref getRef(string name)
        {
            return _refDb.getRef(name);
        }

        /// <returns>
        /// mutable map of all tags; key is short tag name ("v1.0") and value
        /// of the entry contains the ref with the full tag name
        /// ("refs/tags/v1.0").
        /// </returns>
        public IDictionary<string, Ref> getTags()
        {
            try
            {
                return _refDb.getRefs(Constants.R_TAGS);
            }
            catch (IOException)
            {
                return new Dictionary<string, Ref>();
            }
        }

        public Ref Head
        {
            get { return getRef(Constants.HEAD); }
        }

        public Ref Peel(Ref pRef)
        {
            try
            {
                return _refDb.peel(pRef);
            }
            catch (IOException)
            {
                // Historical accident; if the reference cannot be peeled due
                // to some sort of repository access problem we claim that the
                // same as if the reference was not an annotated tag.
                return pRef;
            }
        }

        /**
	     * @return a map with all objects referenced by a peeled ref.
	     */
        public Dictionary<AnyObjectId, List<Ref>> getAllRefsByPeeledObjectId()
        {
            IDictionary<string, Ref> allRefs = getAllRefs();
            var ret = new Dictionary<AnyObjectId, List<Ref>>(allRefs.Count);
            foreach (Ref @ref in allRefs.Values)
            {
                Ref ref2 = @ref;
                ref2 = Peel(ref2);
                AnyObjectId target = ref2.PeeledObjectId;
                if (target == null)
                    target = ref2.ObjectId;
                // We assume most Sets here are singletons
                List<Ref> oset = ret.put(target, new List<Ref> { ref2 });
                if (oset != null)
                {
                    // that was not the case (rare)
                    if (oset.Count == 1)
                    {
                        // Was a read-only singleton, we must copy to a new Set
                        oset = new List<Ref>(oset);
                    }
                    ret.put(target, oset);
                    oset.Add(ref2);
                }
            }
            return ret;
        }

        public static Repository Open(string directory)
        {
            return Open(new DirectoryInfo(directory));
        }

        public static Repository Open(DirectoryInfo directory)
        {
            var name = directory.FullName;
            if (name.EndsWith(Constants.DOT_GIT_EXT))
            {
                return new Repository(directory);
            }

            var subDirectories = directory.GetDirectories(Constants.DOT_GIT);
            if (subDirectories.Length > 0)
            {
                return new Repository(subDirectories[0]);
            }

            if (directory.Parent == null) return null;

            return Open(directory.Parent);
        }

        /// <summary>
        /// Check validity of a ref name. It must not contain character that has
        /// a special meaning in a Git object reference expression. Some other
        /// dangerous characters are also excluded.
        /// </summary>
        /// <param name="refName"></param>
        /// <returns>
        /// Returns true if <paramref name="refName"/> is a valid ref name.
        /// </returns>
        public static bool IsValidRefName(string refName)
        {
            int len = refName.Length;

            if (len == 0) return false;

            if (refName.EndsWith(LockFile.SUFFIX)) return false;

            int components = 1;
            char p = '\0';
            for (int i = 0; i < len; i++)
            {
                char c = refName[i];
                if (c <= ' ') return false;

                switch (c)
                {
                    case '.':
                        switch (p)
                        {
                            case '\0':
                            case '/':
                            case '.':
                                return false;
                        }

                        if (i == len - 1) return false;
                        break;

                    case '/':
                        if (i == 0 || i == len - 1) return false;
                        components++;
                        break;

                    case '{':
                        if (p == '@') return false;
                        break;

                    case '~':
                    case '^':
                    case ':':
                    case '?':
                    case '[':
                    case '*':
                    case '\\':
                        return false;
                }
                p = c;
            }

            return components > 1;
        }

        public Commit OpenCommit(ObjectId id)
        {
            return MapCommit(id);
        }

        public override string ToString()
        {
            return "Repository[" + Directory + "]";
        }

        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Get the name of the reference that {@code HEAD} points to.
        /// Returns name of current branch (for example {@code refs/heads/master}) or
        /// an ObjectId in hex format if the current branch is detached.
        /// <para/>
        /// This is essentially the same as doing:
        /// 
        /// <code>
        /// return getRef(Constants.HEAD).getTarget().getName()
        /// </code>
        /// 
        /// Except when HEAD is detached, in which case this method returns the
        /// current ObjectId in hexadecimal string format.
        /// </summary>
        public string FullBranch
        {
            get
            {
                Ref head = getRef(Constants.HEAD);

                if (head == null)
                    return null;
                if (head.isSymbolic())
                    return head.getTarget().getName();
                if (head.getObjectId() != null)
                    return head.getObjectId().Name;
                return null;

            }
        }

        /// <summary>
        /// Get the short name of the current branch that {@code HEAD} points to.
        /// <para/>
        /// This is essentially the same as {@link #getFullBranch()}, except the
        /// leading prefix {@code refs/heads/} is removed from the reference before
        /// it is returned to the caller.
        /// </summary>
        /// <returns>
        /// name of current branch (for example {@code master}), or an
        /// ObjectId in hex format if the current branch is detached.
        /// </returns>
        public string getBranch()
        {
            string name = FullBranch;
            if (name != null)
                return ShortenRefName(name);
            return name;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="refName"></param>
        /// <returns>A more user friendly ref name</returns>
        public string ShortenRefName(string refName)
        {
            if (refName.StartsWith(Constants.R_HEADS))
            {
                return refName.Substring(Constants.R_HEADS.Length);
            }

            if (refName.StartsWith(Constants.R_TAGS))
            {
                return refName.Substring(Constants.R_TAGS.Length);
            }

            if (refName.StartsWith(Constants.R_REMOTES))
            {
                return refName.Substring(Constants.R_REMOTES.Length);
            }

            return refName;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="refName"></param>
        /// <returns>
        /// A <see cref="ReflogReader"/> for the supplied <paramref name="refName"/>,
        /// or null if the named ref does not exist.
        /// </returns>
        /// <exception cref="IOException">The <see cref="Ref"/> could not be accessed.</exception>
        public ReflogReader ReflogReader(string refName)
        {
            Ref gitRef = getRef(refName);
            if (gitRef != null)
            {
                return new ReflogReader(this, gitRef.Name);
            }

            return null;
        }
    }
}