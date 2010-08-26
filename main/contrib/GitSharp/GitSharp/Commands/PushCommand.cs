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

using System.Collections.Generic;
using GitSharp.Core;
using GitSharp.Core.Transport;

namespace GitSharp.Commands
{

    public class PushCommand : AbstractCommand
    {
        private bool shownUri;

        public ProgressMonitor ProgressMonitor { get; set; }
        public string Remote { get; set; }
        public List<RefSpec> RefSpecs { get; set; }
        public bool Force { get; set; }
        public string ReceivePack { get; set; }
        public bool DryRun { get; set; }
        public bool Thin { get; set; }
        public bool Verbose { get; set; }

        public PushResult Result
        {
            get; private set;
        }

        public PushCommand()
        {
            Remote = Constants.DEFAULT_REMOTE_NAME;
            ProgressMonitor = NullProgressMonitor.Instance;
        }

        public void AddAll()
        {
            RefSpecs.Add(Transport.REFSPEC_PUSH_ALL);
        }

        public void AddTags()
        {
            RefSpecs.Add(Transport.REFSPEC_TAGS);
        }

        public override void Execute()
        {
            if (Force)
            {
                List<RefSpec> orig = new List<RefSpec>(RefSpecs);
                RefSpecs.Clear();
                foreach (RefSpec spec in orig)
                    RefSpecs.Add(spec.SetForce(true));
            }

            List<Transport> transports = Transport.openAll(Repository._internal_repo, Remote);
            foreach (Transport transport in transports)
            {
                if (ReceivePack != null)
                {
                    transport.OptionReceivePack = ReceivePack;
                }
                transport.DryRun = DryRun;
                transport.PushThin = Thin;

                URIish uri = transport.Uri;
                var toPush = transport.findRemoteRefUpdatesFor(RefSpecs);
                try
                {
                    Result = transport.push(ProgressMonitor, toPush);
                }
                finally
                {
                    transport.Dispose();
                }
                printPushResult(uri, Result);
            }
        }

        private void printPushResult(URIish uri, PushResult result)
        {
            shownUri = false;
            bool everythingUpToDate = true;

            foreach (RemoteRefUpdate rru in result.RemoteUpdates)
            {
                if (rru.Status == RemoteRefUpdate.UpdateStatus.UP_TO_DATE)
                {
                    if (Verbose)
                        printRefUpdateResult(uri, result, rru);
                }
                else
                {
                    everythingUpToDate = false;
                }
            }

            foreach (RemoteRefUpdate rru in result.RemoteUpdates)
            {
                if (rru.Status == RemoteRefUpdate.UpdateStatus.OK)
                    printRefUpdateResult(uri, result, rru);
            }

            foreach (RemoteRefUpdate rru in result.RemoteUpdates)
            {
                if (rru.Status != RemoteRefUpdate.UpdateStatus.OK && rru.Status != RemoteRefUpdate.UpdateStatus.UP_TO_DATE)
                    printRefUpdateResult(uri, result, rru);
            }

            if (everythingUpToDate)
                OutputStream.WriteLine("Everything up-to-date");
        }

        private void printRefUpdateResult(URIish uri, OperationResult result, RemoteRefUpdate rru)
        {
            if (!shownUri)
            {
                shownUri = true;
                OutputStream.WriteLine("To " + uri);
            }

            string remoteName = rru.RemoteName;
            string srcRef = rru.IsDelete ? null : rru.SourceRef;

            switch (rru.Status)
            {
                case RemoteRefUpdate.UpdateStatus.OK:
                    {
                        if (rru.IsDelete)
                            printUpdateLine('-', "[deleted]", null, remoteName, null);
                        else
                        {
                            GitSharp.Core.Ref oldRef = result.GetAdvertisedRef(remoteName);
                            if (oldRef == null)
                            {
                                string summary = remoteName.StartsWith(Constants.R_TAGS) ? "[new tag]" : "[new branch]";
                                printUpdateLine('*', summary, srcRef, remoteName, null);
                            }
                            else
                            {
                                bool fastForward = rru.FastForward;
                                char flag = fastForward ? ' ' : '+';
                                string summary = oldRef.ObjectId.Abbreviate(Repository._internal_repo).name() +
                                                 (fastForward ? ".." : "...") +
                                                 rru.NewObjectId.Abbreviate(Repository._internal_repo).name();
                                string message = fastForward ? null : "forced update";
                                printUpdateLine(flag, summary, srcRef, remoteName, message);
                            }
                        }
                        break;
                    }

                case RemoteRefUpdate.UpdateStatus.NON_EXISTING:
                    printUpdateLine('X', "[no match]", null, remoteName, null);
                    break;

                case RemoteRefUpdate.UpdateStatus.REJECTED_NODELETE:
                    printUpdateLine('!', "[rejected]", null, remoteName, "remote side does not support deleting refs");
                    break;

                case RemoteRefUpdate.UpdateStatus.REJECTED_NONFASTFORWARD:
                    printUpdateLine('!', "[rejected]", srcRef, remoteName, "non-fast forward");
                    break;

                case RemoteRefUpdate.UpdateStatus.REJECTED_REMOTE_CHANGED:
                    {
                        string message = "remote ref object changed - is not expected one " +
                                         rru.ExpectedOldObjectId.Abbreviate(Repository._internal_repo).name();
                        printUpdateLine('!', "[rejected]", srcRef, remoteName, message);
                        break;
                    }

                case RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON:
                    printUpdateLine('!', "[rejected]", srcRef, remoteName, rru.Message);
                    break;

                case RemoteRefUpdate.UpdateStatus.UP_TO_DATE:
                    if (Verbose)
                        printUpdateLine('=', "[up to date]", srcRef, remoteName, null);
                    break;

                case RemoteRefUpdate.UpdateStatus.NOT_ATTEMPTED:
                case RemoteRefUpdate.UpdateStatus.AWAITING_REPORT:
                    printUpdateLine('?', "[unexpected push-process behavior]", srcRef, remoteName, rru.Message);
                    break;
            }
        }

        private void printUpdateLine(char flag, string summary, string srcRef, string destRef, string message)
        {
            OutputStream.Write(" " + flag + " " + summary);
            
            if (srcRef != null)
                OutputStream.Write(" " + AbbreviateRef(srcRef, true) + " -> ");
            OutputStream.Write(AbbreviateRef(destRef, true));

            if (message != null)
                OutputStream.Write(" (" + message + ")");

            OutputStream.WriteLine();

        }
    }

}