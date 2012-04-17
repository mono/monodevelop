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

using NGit.Internal;
using NGit.Nls;
using Sharpen;

namespace NGit.Internal
{
	/// <summary>Translation bundle for JGit core</summary>
	public class JGitText : TranslationBundle
	{
		/// <returns>an instance of this translation bundle</returns>
		public static JGitText Get()
		{
			return NLS.GetBundleFor<JGitText>();
		}

		public string abbreviationLengthMustBeNonNegative;

		public string abortingRebase;

		public string abortingRebaseFailed;

		public string advertisementCameBefore;

		public string advertisementOfCameBefore;

		public string amazonS3ActionFailed;

		public string amazonS3ActionFailedGivingUp;

		public string ambiguousObjectAbbreviation;

		public string aNewObjectIdIsRequired;

		public string anExceptionOccurredWhileTryingToAddTheIdOfHEAD;

		public string anSSHSessionHasBeenAlreadyCreated;

		public string applyingCommit;

		public string atLeastOnePathIsRequired;

		public string atLeastOnePatternIsRequired;

		public string atLeastTwoFiltersNeeded;

		public string authenticationNotSupported;

		public string badBase64InputCharacterAt;

		public string badEntryDelimiter;

		public string badEntryName;

		public string badEscape;

		public string badGroupHeader;

		public string badObjectType;

		public string badSectionEntry;

		public string bareRepositoryNoWorkdirAndIndex;

		public string base64InputNotProperlyPadded;

		public string baseLengthIncorrect;

		public string blameNotCommittedYet;

		public string blobNotFound;

		public string blobNotFoundForPath;

		public string branchNameInvalid;

		public string cachedPacksPreventsIndexCreation;

		public string cannotBeCombined;

		public string cannotBeRecursiveWhenTreesAreIncluded;

		public string cannotCombineTreeFilterWithRevFilter;

		public string cannotCommitOnARepoWithState;

		public string cannotCommitWriteTo;

		public string cannotConnectPipes;

		public string cannotConvertScriptToText;

		public string cannotCreateConfig;

		public string cannotCreateDirectory;

		public string cannotCreateHEAD;

		public string cannotDeleteCheckedOutBranch;

		public string cannotDeleteFile;

		public string cannotDeleteStaleTrackingRef;

		public string cannotDeleteStaleTrackingRef2;

		public string cannotDetermineProxyFor;

		public string cannotDownload;

		public string cannotExecute;

		public string cannotGet;

		public string cannotListRefs;

		public string cannotLock;

		public string cannotLockPackIn;

		public string cannotMatchOnEmptyString;

		public string cannotMoveIndexTo;

		public string cannotMovePackTo;

		public string cannotOpenService;

		public string cannotParseGitURIish;

		public string cannotPullOnARepoWithState;

		public string cannotRead;

		public string cannotReadBlob;

		public string cannotReadCommit;

		public string cannotReadFile;

		public string cannotReadHEAD;

		public string cannotReadObject;

		public string cannotReadTree;

		public string cannotRebaseWithoutCurrentHead;

		public string cannotResolveLocalTrackingRefForUpdating;

		public string cannotStoreObjects;

		public string cannotUnloadAModifiedTree;

		public string cannotWorkWithOtherStagesThanZeroRightNow;

		public string canOnlyCherryPickCommitsWithOneParent;

		public string canOnlyRevertCommitsWithOneParent;

		public string cantFindObjectInReversePackIndexForTheSpecifiedOffset;

		public string cantPassMeATree;

		public string channelMustBeInRange0_255;

		public string characterClassIsNotSupported;

		public string checkoutConflictWithFile;

		public string checkoutConflictWithFiles;

		public string checkoutUnexpectedResult;

		public string classCastNotA;

		public string cloneNonEmptyDirectory;

		public string collisionOn;

		public string commandWasCalledInTheWrongState;

		public string commitAlreadyExists;

		public string commitMessageNotSpecified;

		public string commitOnRepoWithoutHEADCurrentlyNotSupported;

		public string compressingObjects;

		public string connectionFailed;

		public string connectionTimeOut;

		public string contextMustBeNonNegative;

		public string corruptionDetectedReReadingAt;

		public string corruptObjectBadStream;

		public string corruptObjectBadStreamCorruptHeader;

		public string corruptObjectGarbageAfterSize;

		public string corruptObjectIncorrectLength;

		public string corruptObjectInvalidEntryMode;

		public string corruptObjectInvalidMode;

		public string corruptObjectInvalidMode2;

		public string corruptObjectInvalidMode3;

		public string corruptObjectInvalidType;

		public string corruptObjectInvalidType2;

		public string corruptObjectMalformedHeader;

		public string corruptObjectNegativeSize;

		public string corruptObjectNoAuthor;

		public string corruptObjectNoCommitter;

		public string corruptObjectNoHeader;

		public string corruptObjectNoObject;

		public string corruptObjectNoTaggerBadHeader;

		public string corruptObjectNoTaggerHeader;

		public string corruptObjectNoTagName;

		public string corruptObjectNotree;

		public string corruptObjectNoType;

		public string corruptObjectPackfileChecksumIncorrect;

		public string couldNotCheckOutBecauseOfConflicts;

		public string couldNotDeleteLockFileShouldNotHappen;

		public string couldNotDeleteTemporaryIndexFileShouldNotHappen;

		public string couldNotGetAdvertisedRef;

		public string couldNotLockHEAD;

		public string couldNotReadIndexInOneGo;

		public string couldNotReadObjectWhileParsingCommit;

		public string couldNotRenameDeleteOldIndex;

		public string couldNotRenameTemporaryFile;

		public string couldNotRenameTemporaryIndexFileToIndex;

		public string couldNotURLEncodeToUTF8;

		public string couldNotWriteFile;

		public string countingObjects;

		public string createBranchFailedUnknownReason;

		public string createBranchUnexpectedResult;

		public string createNewFileFailed;

		public string credentialPassword;

		public string credentialUsername;

		public string daemonAlreadyRunning;

		public string daysAgo;

		public string deleteBranchUnexpectedResult;

		public string deleteFileFailed;

		public string deleteTagUnexpectedResult;

		public string deletingNotSupported;

		public string destinationIsNotAWildcard;

		public string detachedHeadDetected;

		public string dirCacheDoesNotHaveABackingFile;

		public string dirCacheFileIsNotLocked;

		public string dirCacheIsNotLocked;

		public string DIRCChecksumMismatch;

		public string DIRCExtensionIsTooLargeAt;

		public string DIRCExtensionNotSupportedByThisVersion;

		public string DIRCHasTooManyEntries;

		public string DIRCUnrecognizedExtendedFlags;

		public string dirtyFilesExist;

		public string doesNotHandleMode;

		public string downloadCancelled;

		public string downloadCancelledDuringIndexing;

		public string duplicateAdvertisementsOf;

		public string duplicateRef;

		public string duplicateRemoteRefUpdateIsIllegal;

		public string duplicateStagesNotAllowed;

		public string eitherGitDirOrWorkTreeRequired;

		public string emptyCommit;

		public string emptyPathNotPermitted;

		public string encryptionError;

		public string endOfFileInEscape;

		public string entryNotFoundByPath;

		public string enumValueNotSupported2;

		public string enumValueNotSupported3;

		public string enumValuesNotAvailable;

		public string errorDecodingFromFile;

		public string errorEncodingFromFile;

		public string errorInBase64CodeReadingStream;

		public string errorInPackedRefs;

		public string errorInvalidProtocolWantedOldNewRef;

		public string errorListing;

		public string errorOccurredDuringUnpackingOnTheRemoteEnd;

		public string errorReadingInfoRefs;

		public string exceptionCaughtDuringExecutionOfAddCommand;

		public string exceptionCaughtDuringExecutionOfCherryPickCommand;

		public string exceptionCaughtDuringExecutionOfCommitCommand;

		public string exceptionCaughtDuringExecutionOfFetchCommand;

		public string exceptionCaughtDuringExecutionOfLsRemoteCommand;

		public string exceptionCaughtDuringExecutionOfMergeCommand;

		public string exceptionCaughtDuringExecutionOfPullCommand;

		public string exceptionCaughtDuringExecutionOfPushCommand;

		public string exceptionCaughtDuringExecutionOfResetCommand;

		public string exceptionCaughtDuringExecutionOfRevertCommand;

		public string exceptionCaughtDuringExecutionOfRmCommand;

		public string exceptionCaughtDuringExecutionOfTagCommand;

		public string exceptionOccurredDuringAddingOfOptionToALogCommand;

		public string exceptionOccurredDuringReadingOfGIT_DIR;

		public string expectedACKNAKFoundEOF;

		public string expectedACKNAKGot;

		public string expectedBooleanStringValue;

		public string expectedCharacterEncodingGuesses;

		public string expectedEOFReceived;

		public string expectedGot;

		public string expectedPktLineWithService;

		public string expectedReceivedContentType;

		public string expectedReportForRefNotReceived;

		public string failedUpdatingRefs;

		public string failureDueToOneOfTheFollowing;

		public string failureUpdatingFETCH_HEAD;

		public string failureUpdatingTrackingRef;

		public string fileCannotBeDeleted;

		public string fileIsTooBigForThisConvenienceMethod;

		public string fileIsTooLarge;

		public string fileModeNotSetForPath;

		public string flagIsDisposed;

		public string flagNotFromThis;

		public string flagsAlreadyCreated;

		public string funnyRefname;

		public string headRequiredToStash;

		public string hoursAgo;

		public string hugeIndexesAreNotSupportedByJgitYet;

		public string hunkBelongsToAnotherFile;

		public string hunkDisconnectedFromFile;

		public string hunkHeaderDoesNotMatchBodyLineCountOf;

		public string illegalArgumentNotA;

		public string illegalCombinationOfArguments;

		public string illegalPackingPhase;

		public string illegalStateExists;

		public string improperlyPaddedBase64Input;

		public string incorrectHashFor;

		public string incorrectOBJECT_ID_LENGTH;

		public string indexFileIsInUse;

		public string indexFileIsTooLargeForJgit;

		public string indexSignatureIsInvalid;

		public string indexWriteException;

		public string inMemoryBufferLimitExceeded;

		public string integerValueOutOfRange;

		public string internalRevisionError;

		public string internalServerError;

		public string interruptedWriting;

		public string inTheFuture;

		public string invalidAdvertisementOf;

		public string invalidAncestryLength;

		public string invalidBooleanValue;

		public string invalidChannel;

		public string invalidCharacterInBase64Data;

		public string invalidCommitParentNumber;

		public string invalidEncryption;

		public string invalidGitdirRef;

		public string invalidGitType;

		public string invalidId;

		public string invalidIdLength;

		public string invalidIntegerValue;

		public string invalidKey;

		public string invalidLineInConfigFile;

		public string invalidModeFor;

		public string invalidModeForPath;

		public string invalidObject;

		public string invalidOldIdSent;

		public string invalidPacketLineHeader;

		public string invalidPath;

		public string invalidReflogRevision;

		public string invalidRefName;

		public string invalidRemote;

		public string invalidStageForPath;

		public string invalidTagOption;

		public string invalidTimeout;

		public string invalidURL;

		public string invalidWildcards;

		public string invalidWindowSize;

		public string isAStaticFlagAndHasNorevWalkInstance;

		public string JRELacksMD5Implementation;

		public string kNotInRange;

		public string largeObjectExceedsByteArray;

		public string largeObjectExceedsLimit;

		public string largeObjectException;

		public string largeObjectOutOfMemory;

		public string lengthExceedsMaximumArraySize;

		public string listingAlternates;

		public string localObjectsIncomplete;

		public string localRefIsMissingObjects;

		public string lockCountMustBeGreaterOrEqual1;

		public string lockError;

		public string lockOnNotClosed;

		public string lockOnNotHeld;

		public string malformedpersonIdentString;

		public string maxCountMustBeNonNegative;

		public string mergeConflictOnNonNoteEntries;

		public string mergeConflictOnNotes;

		public string mergeStrategyAlreadyExistsAsDefault;

		public string mergeStrategyDoesNotSupportHeads;

		public string mergeUsingStrategyResultedInDescription;

		public string minutesAgo;

		public string missingAccesskey;

		public string missingConfigurationForKey;

		public string missingDeltaBase;

		public string missingForwardImageInGITBinaryPatch;

		public string missingObject;

		public string missingPrerequisiteCommits;

		public string missingRequiredParameter;

		public string missingSecretkey;

		public string mixedStagesNotAllowed;

		public string mkDirFailed;

		public string mkDirsFailed;

		public string month;

		public string months;

		public string monthsAgo;

		public string multipleMergeBasesFor;

		public string need2Arguments;

		public string needPackOut;

		public string needsAtLeastOneEntry;

		public string needsWorkdir;

		public string newlineInQuotesNotAllowed;

		public string noApplyInDelete;

		public string noClosingBracket;

		public string noHEADExistsAndNoExplicitStartingRevisionWasSpecified;

		public string noHMACsupport;

		public string noMergeHeadSpecified;

		public string noSuchRef;

		public string notABoolean;

		public string notABundle;

		public string notADIRCFile;

		public string notAGitDirectory;

		public string notAPACKFile;

		public string notARef;

		public string notASCIIString;

		public string notAuthorized;

		public string notAValidPack;

		public string notFound;

		public string nothingToFetch;

		public string nothingToPush;

		public string notMergedExceptionMessage;

		public string noXMLParserAvailable;

		public string objectAtHasBadZlibStream;

		public string objectAtPathDoesNotHaveId;

		public string objectIsCorrupt;

		public string objectIsNotA;

		public string objectNotFoundIn;

		public string obtainingCommitsForCherryPick;

		public string offsetWrittenDeltaBaseForObjectNotFoundInAPack;

		public string onlyAlreadyUpToDateAndFastForwardMergesAreAvailable;

		public string onlyOneFetchSupported;

		public string onlyOneOperationCallPerConnectionIsSupported;

		public string openFilesMustBeAtLeast1;

		public string openingConnection;

		public string operationCanceled;

		public string outputHasAlreadyBeenStarted;

		public string packChecksumMismatch;

		public string packCorruptedWhileWritingToFilesystem;

		public string packDoesNotMatchIndex;

		public string packetSizeMustBeAtLeast;

		public string packetSizeMustBeAtMost;

		public string packfileCorruptionDetected;

		public string packFileInvalid;

		public string packfileIsTruncated;

		public string packHasUnresolvedDeltas;

		public string packingCancelledDuringObjectsWriting;

		public string packObjectCountMismatch;

		public string packTooLargeForIndexVersion1;

		public string packWriterStatistics;

		public string patchApplyException;

		public string patchFormatException;

		public string pathIsNotInWorkingDir;

		public string pathNotConfigured;

		public string peeledLineBeforeRef;

		public string peerDidNotSupplyACompleteObjectGraph;

		public string prefixRemote;

		public string problemWithResolvingPushRefSpecsLocally;

		public string progressMonUploading;

		public string propertyIsAlreadyNonNull;

		public string pullOnRepoWithoutHEADCurrentlyNotSupported;

		public string pullTaskName;

		public string pushCancelled;

		public string pushIsNotSupportedForBundleTransport;

		public string pushNotPermitted;

		public string rawLogMessageDoesNotParseAsLogEntry;

		public string readingObjectsFromLocalRepositoryFailed;

		public string readTimedOut;

		public string receivePackObjectTooLarge1;

		public string receivePackObjectTooLarge2;

		public string receivingObjects;

		public string refAlreadyExists;

		public string reflogEntryNotFound;

		public string refNotResolved;

		public string refUpdateReturnCodeWas;

		public string remoteConfigHasNoURIAssociated;

		public string remoteDoesNotHaveSpec;

		public string remoteDoesNotSupportSmartHTTPPush;

		public string remoteHungUpUnexpectedly;

		public string remoteNameCantBeNull;

		public string renameBranchFailedBecauseTag;

		public string renameBranchFailedUnknownReason;

		public string renameBranchUnexpectedResult;

		public string renameFileFailed;

		public string renamesAlreadyFound;

		public string renamesBreakingModifies;

		public string renamesFindingByContent;

		public string renamesFindingExact;

		public string renamesRejoiningModifies;

		public string repositoryAlreadyExists;

		public string repositoryConfigFileInvalid;

		public string repositoryIsRequired;

		public string repositoryNotFound;

		public string repositoryState_applyMailbox;

		public string repositoryState_bisecting;

		public string repositoryState_conflicts;

		public string repositoryState_merged;

		public string repositoryState_normal;

		public string repositoryState_rebase;

		public string repositoryState_rebaseInteractive;

		public string repositoryState_rebaseOrApplyMailbox;

		public string repositoryState_rebaseWithMerge;

		public string requiredHashFunctionNotAvailable;

		public string resettingHead;

		public string resolvingDeltas;

		public string resultLengthIncorrect;

		public string rewinding;

		public string searchForReuse;

		public string searchForSizes;

		public string secondsAgo;

		public string sequenceTooLargeForDiffAlgorithm;

		public string serviceNotEnabledNoName;

		public string serviceNotPermitted;

		public string serviceNotPermittedNoName;

		public string shortCompressedStreamAt;

		public string shortReadOfBlock;

		public string shortReadOfOptionalDIRCExtensionExpectedAnotherBytes;

		public string shortSkipOfBlock;

		public string signingNotSupportedOnTag;

		public string similarityScoreMustBeWithinBounds;

		public string sizeExceeds2GB;

		public string skipMustBeNonNegative;

		public string smartHTTPPushDisabled;

		public string sourceDestinationMustMatch;

		public string sourceIsNotAWildcard;

		public string sourceRefDoesntResolveToAnyObject;

		public string sourceRefNotSpecifiedForRefspec;

		public string staleRevFlagsOn;

		public string startingReadStageWithoutWrittenRequestDataPendingIsNotSupported;

		public string stashApplyFailed;

		public string stashApplyOnUnsafeRepository;

		public string stashCommitMissingTwoParents;

		public string stashDropDeleteRefFailed;

		public string stashDropFailed;

		public string stashDropMissingReflog;

		public string stashFailed;

		public string stashResolveFailed;

		public string statelessRPCRequiresOptionToBeEnabled;

		public string submoduleExists;

		public string submodulesNotSupported;

		public string submoduleParentRemoteUrlInvalid;

		public string symlinkCannotBeWrittenAsTheLinkTarget;

		public string systemConfigFileInvalid;

		public string tagNameInvalid;

		public string tagOnRepoWithoutHEADCurrentlyNotSupported;

		public string theFactoryMustNotBeNull;

		public string timerAlreadyTerminated;

		public string topologicalSortRequired;

		public string transportExceptionBadRef;

		public string transportExceptionEmptyRef;

		public string transportExceptionInvalid;

		public string transportExceptionMissingAssumed;

		public string transportExceptionReadRef;

		public string transportProtoAmazonS3;

		public string transportProtoBundleFile;

		public string transportProtoFTP;

		public string transportProtoGitAnon;

		public string transportProtoHTTP;

		public string transportProtoLocal;

		public string transportProtoSFTP;

		public string transportProtoSSH;

		public string treeEntryAlreadyExists;

		public string treeIteratorDoesNotSupportRemove;

		public string treeWalkMustHaveExactlyTwoTrees;

		public string truncatedHunkLinesMissingForAncestor;

		public string truncatedHunkNewLinesMissing;

		public string truncatedHunkOldLinesMissing;

		public string tSizeMustBeGreaterOrEqual1;

		public string unableToCheckConnectivity;

		public string unableToStore;

		public string unableToWrite;

		public string unencodeableFile;

		public string unexpectedCompareResult;

		public string unexpectedEndOfConfigFile;

		public string unexpectedHunkTrailer;

		public string unexpectedOddResult;

		public string unexpectedRefReport;

		public string unexpectedReportLine;

		public string unexpectedReportLine2;

		public string unknownOrUnsupportedCommand;

		public string unknownDIRCVersion;

		public string unknownHost;

		public string unknownIndexVersionOrCorruptIndex;

		public string unknownObject;

		public string unknownObjectType;

		public string unknownRepositoryFormat;

		public string unknownRepositoryFormat2;

		public string unknownZlibError;

		public string unmergedPath;

		public string unmergedPaths;

		public string unpackException;

		public string unreadablePackIndex;

		public string unrecognizedRef;

		public string unsupportedCommand0;

		public string unsupportedEncryptionAlgorithm;

		public string unsupportedEncryptionVersion;

		public string unsupportedOperationNotAddAtEnd;

		public string unsupportedPackIndexVersion;

		public string unsupportedPackVersion;

		public string updatingReferences;

		public string updatingRefFailed;

		public string uriNotConfigured;

		public string uriNotFound;

		public string URINotSupported;

		public string URLNotFound;

		public string userConfigFileInvalid;

		public string walkFailure;

		public string wantNotValid;

		public string weeksAgo;

		public string windowSizeMustBeLesserThanLimit;

		public string windowSizeMustBePowerOf2;

		public string writerAlreadyInitialized;

		public string writeTimedOut;

		public string writingNotPermitted;

		public string writingNotSupported;

		public string writingObjects;

		public string wrongDecompressedLength;

		public string wrongRepositoryState;

		public string year;

		public string years;

		public string years0MonthsAgo;

		public string yearsAgo;

		public string yearsMonthsAgo;
	}
}
