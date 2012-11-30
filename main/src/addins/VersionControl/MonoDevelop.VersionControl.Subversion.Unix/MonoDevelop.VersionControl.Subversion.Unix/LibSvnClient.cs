// /cvs/monodevelop/Extras/VersionControl/MonoDevelop.VersionControl.Subversion/MonoDevelop.VersionControl.Subversion/LibSvnClient.cs created with MonoDevelop
// User: fejj at 12:12 PM 5/31/2007
//

using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Runtime.InteropServices;

using MonoDevelop.Core;
using MonoDevelop.VersionControl;
using MonoDevelop.VersionControl.Subversion.Gui;

using svn_revnum_t = System.Int32;
using off_t = System.Int64;
using size_t = System.Int32;

namespace MonoDevelop.VersionControl.Subversion.Unix {
	public abstract class LibSvnClient {
		public LibSvnClient ()
		{
			client_version ();
		}
		
		public static LibSvnClient GetLib ()
		{
			try {
				return new LibSvnClient0 ();
			} catch {}
			
			try {
				return new LibSvnClient1 ();
			} catch {}
			
			return null;
		}
		
		public abstract void config_ensure (string config_dir, IntPtr pool);
		public abstract void config_get_config (ref IntPtr cfg_hash, string config_dir, IntPtr pool);
		public abstract void auth_open (out IntPtr auth_baton, IntPtr providers, IntPtr pool);
		public abstract void auth_set_parameter (IntPtr auth_baton, string name, IntPtr value);
		public abstract IntPtr auth_get_parameter (IntPtr auth_baton, string name);
		public abstract void client_get_simple_provider (IntPtr item, IntPtr pool);
		public abstract void client_get_simple_prompt_provider (IntPtr item, svn_auth_simple_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool);
		public abstract void client_get_username_provider (IntPtr item, IntPtr pool);
		public abstract void client_get_username_prompt_provider (IntPtr item, svn_auth_username_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool);
		public abstract void client_get_ssl_server_trust_file_provider (IntPtr item, IntPtr pool);
		public abstract void client_get_ssl_client_cert_file_provider (IntPtr item, IntPtr pool);
		public abstract void client_get_ssl_client_cert_pw_file_provider (IntPtr item, IntPtr pool);
		public abstract void client_get_ssl_server_trust_prompt_provider (IntPtr item, svn_auth_ssl_server_trust_prompt_func_t prompt_func, IntPtr prompt_batton, IntPtr pool);
		public abstract void client_get_ssl_client_cert_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool);
		public abstract void client_get_ssl_client_cert_pw_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_pw_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool);
		
		public abstract IntPtr client_version ();
		
		public abstract IntPtr client_create_context (out IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_ls (out IntPtr dirents, string path_or_url,
		                                  ref Rev revision, int recurse, IntPtr ctx,
		                                  IntPtr pool);
		
		public abstract IntPtr client_status (IntPtr result_rev, string path, ref Rev revision,
		                                      svn_wc_status_func2_t status_func, IntPtr status_baton,
		                                      bool recurse, bool get_all, bool update, bool no_ignore,
		                                      bool ignore_externals, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_root_url_from_path (ref IntPtr url, string path_or_url, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_log (IntPtr apr_array_header_t_targets,
		                                   ref Rev rev_start, ref Rev rev_end,
		                                   int discover_changed_paths,
		                                   int strict_node_history,
		                                   svn_log_message_receiver_t receiver,
		                                   IntPtr receiver_baton,
		                                   IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr time_from_cstring (out long aprtime, string time, IntPtr pool);
		
		public abstract IntPtr client_url_from_path (ref IntPtr url, string path_or_url, IntPtr pool);
		
		public abstract IntPtr client_cat2 (IntPtr stream, string path_or_url,
		                                    ref Rev peg_revision,
		                                    ref Rev revision,
		                                    IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr stream_create (IntPtr baton, IntPtr pool);
		
		//public abstract IntPtr stream_set_read (IntPtr stream, svn_readwrite_fn_t reader);
		
		public abstract IntPtr stream_set_write (IntPtr stream, svn_readwrite_fn_t writer);
		
		public abstract IntPtr client_update (IntPtr result_rev, string path, ref Rev revision,
		                                      bool recurse, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_delete (ref IntPtr commit_info_p, IntPtr apr_array_header_t_targets, 
		                                      int force, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_add3 (string path, int recurse, int force, int no_ignore, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_commit (ref IntPtr svn_client_commit_info_t_commit_info,
		                                      IntPtr apr_array_header_t_targets, int nonrecursive,
		                                      IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_revert (IntPtr apr_array_header_t_targets, int recursive,
		                                      IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_resolved (string path, int recursive, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_move (ref IntPtr commit_info_p, string srcPath, ref Rev rev,
		                                    string destPath, int force, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_checkout (IntPtr result_rev, string url, string path, ref Rev rev, 
		                                        bool recurse, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_mkdir2 (ref IntPtr commit_info, IntPtr apr_array_paths, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_diff (IntPtr diff_options, string path1, ref Rev revision1,
		                                    string path2, ref Rev revision2, int recurse,
		                                    int ignore_ancestry, int no_diff_deleted,
		                                    IntPtr outfile, IntPtr errfile,
		                                    IntPtr ctx, IntPtr pool);
		
		
		public abstract IntPtr client_merge_peg2 (string source,
		                                          ref Rev revision1,
		                                          ref Rev revision2,
		                                          ref Rev peg_revision,
		                                          string target_wcpath,
		                                          bool recurse,
		                                          bool ignore_ancestry,
		                                          bool force,
		                                          bool dry_run,
		                                          IntPtr merge_options,
		                                          IntPtr ctx,
		                                          IntPtr pool);
		
		public abstract IntPtr client_lock (IntPtr apr_array_header_t_targets, string comment, int steal_lock, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_unlock (IntPtr apr_array_header_t_targets, int break_lock, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_prop_get (out IntPtr value, string name, string target, ref Rev revision, int recurse, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_blame (string path, ref Rev rev_start, ref Rev rev_end, svn_client_blame_receiver_t receiver, IntPtr baton, IntPtr ctx, IntPtr pool);
		
		public abstract void strerror (int statcode, byte[] buf, int bufsize);
		
		public abstract IntPtr path_internal_style (string path, IntPtr pool);
		
		public class DirEnt {
			public readonly string Name;
			public readonly bool IsDirectory;
			public readonly long Size;
			public readonly bool HasProps;
			public readonly int CreatedRevision;
			public readonly DateTime Time;
			public readonly string LastAuthor;
			
			static readonly DateTime Epoch = new DateTime (1970, 1, 1);
			
			internal DirEnt (string name, svn_dirent_t ent)
			{
				Name = name;
				IsDirectory = ent.kind == svn_node_kind_t.Dir;
				Size = ent.size;
				HasProps = ent.has_props;
				CreatedRevision = (int) ent.created_rev;
				Time = Epoch.AddTicks(ent.time * 10);
				LastAuthor = ent.last_author;
			}
		}
		
		public class StatusEnt {
			public readonly string LocalFilePath;
			public readonly string Name;
			public readonly int Revision;
  			public readonly string Url;
  			public readonly string Repository;
  			public readonly string RepositoryUuid;
  			public readonly bool IsDirectory;
  			public readonly NodeSchedule Schedule;
  			public readonly bool Copied;
  			public readonly bool Deleted;
  			public readonly bool Absent;
  			public readonly bool Incomplete;
  			public readonly string CopiedFrom;
  			public readonly int CopiedFromRevision;
  			public readonly string ConflictOld;
  			public readonly string ConflictNew;
  			public readonly string ConflictWorking;
  			public readonly string PropertyRejectFile;
  			public readonly DateTime TextTime;
  			public readonly DateTime PropTime;
  			public readonly string Checksum; //(base64 for text-base, or NULL);
  			public readonly int LastCommitRevision;
  			public readonly DateTime LastCommitDate;
  			public readonly string LastCommitAuthor;
 			
			public readonly LibSvnClient.svn_wc_status_kind TextStatus;
			public readonly LibSvnClient.svn_wc_status_kind PropsStatus;
			public readonly bool Locked;
			public readonly bool Switched;
			public readonly LibSvnClient.svn_wc_status_kind RemoteTextStatus;
			public readonly LibSvnClient.svn_wc_status_kind RemotePropsStatus;
			public readonly bool RepoLocked;
			public readonly bool LockOwned;
			public readonly string LockToken;
			public readonly string LockOwner;
			public readonly string LockComment;
			
			static readonly DateTime Epoch = new DateTime (1970, 1, 1);
			
			internal StatusEnt (svn_wc_status2_t status, string localpath)
			{
				LocalFilePath = localpath;
				TextStatus = (svn_wc_status_kind) status.text_status;
				PropsStatus = (svn_wc_status_kind) status.prop_status;
				Locked = status.locked;
				Copied = status.copied;
				Switched = status.switched;
				RemoteTextStatus = (svn_wc_status_kind) status.repos_text_status;
				RemotePropsStatus = (svn_wc_status_kind) status.repos_prop_status;

				if (status.to_svn_wc_entry_t == IntPtr.Zero)
					return;
				
				svn_wc_entry_t ent = (svn_wc_entry_t) Marshal.PtrToStructure (status.to_svn_wc_entry_t, typeof (svn_wc_entry_t));
				Name = ent.name;
				Revision = (int) ent.revision;
				Url = ent.url;
				Repository = ent.repos;
				RepositoryUuid = ent.repos_uuid;
				IsDirectory = ent.node_kind ==  svn_node_kind_t.Dir;
				Schedule = (NodeSchedule) ent.schedule;
				Copied = ent.copied;
				Deleted = ent.deleted;
				Absent = ent.absent;
				Incomplete = ent.incomplete;
				CopiedFrom = ent.copiedfrom_url;
				CopiedFromRevision = (int)ent.copiedfrom_rev;
				ConflictOld = ent.conflict_old;
				ConflictNew = ent.conflict_new;
				ConflictWorking = ent.conflict_working;
				PropertyRejectFile = ent.property_reject_file;
				TextTime = Epoch.AddTicks (ent.text_time * 10);
				PropTime = Epoch.AddTicks (ent.prop_time * 10);
				Checksum = ent.checksum;
				LastCommitRevision = (int) ent.last_commit_rev;
				LastCommitDate = Epoch.AddTicks (ent.last_commit_date * 10);
				LastCommitAuthor = ent.last_commit_author;
				LockToken = ent.lock_token;
				LockOwner = ent.lock_owner;
				LockComment = ent.lock_comment;
				
				if (status.repos_lock != IntPtr.Zero) {
					svn_lock_t repoLock = (svn_lock_t) Marshal.PtrToStructure (status.repos_lock, typeof (svn_lock_t));
					LockToken = repoLock.token;
					LockOwner = repoLock.owner;
					LockComment = repoLock.comment;
					RepoLocked = true;
				}
				if (LockToken != null) {
					LockOwned = true;
					RepoLocked = true;
				}
			}
		}
		
		// Native Interop
		[StructLayout(LayoutKind.Sequential)]
		public struct svn_client_ctx_t {
			public IntPtr auth_baton;
			public svn_wc_notify_func_t NotifyFunc;
			public IntPtr notify_baton;
			public svn_client_get_commit_log_t LogMsgFunc;
			public IntPtr logmsg_baton;
			public IntPtr config;
			public IntPtr cancel_func;
			public IntPtr cancel_baton;
			public svn_wc_notify_func2_t NotifyFunc2;
			public IntPtr notify_baton2;
			public svn_client_get_commit_log2_t LogMsgFunc2;
			public IntPtr log_msg_baton2;
			public svn_ra_progress_notify_func_t progress_func;
			public IntPtr progress_baton;
			public svn_client_get_commit_log3_t LogMsgFunc3;
			public IntPtr log_msg_baton3;
			public IntPtr mimetypes_map;
			public svn_wc_conflict_resolver_func_t conflict_func;
			public IntPtr conflict_baton;
			public string client_name;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		public struct svn_wc_notify_t {
			public IntPtr path;
			public LibSvnClient.NotifyAction action;
			public LibSvnClient.svn_wc_status_kind kind;
			public IntPtr mime_type;
			public IntPtr repo_lock;
			public IntPtr err;
			public LibSvnClient.NotifyState content_state;
			public LibSvnClient.NotifyState prop_state;
			public LibSvnClient.NotifyLockState lock_state;
			public svn_revnum_t revision;
			public IntPtr changelist_name;
			public IntPtr merge_range;
			public IntPtr url;
			public IntPtr path_prefix;
			public IntPtr prop_name;
			public IntPtr rev_prop;

		}
		
		[StructLayout(LayoutKind.Sequential)]
		public struct svn_error_t {
			public int apr_err;
			[MarshalAs (UnmanagedType.LPStr)] public string message;
			public IntPtr svn_error_t_child;
			public IntPtr pool;
			[MarshalAs (UnmanagedType.LPStr)] public string file;
			// This is a native 'long' type which is the same size as IntPtr on *nix systems
			public IntPtr line;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		public struct svn_client_commit_info_t {
			public svn_revnum_t revision;
			public IntPtr date;
			public IntPtr author;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		public struct svn_version_t {
			public int major;
			public int minor;
			public int patch;
			public string tag;
		}
		
		public enum svn_node_kind_t {
  			None,
  			File,
  			Dir,
  			Unknown
  		}
		
		internal struct svn_dirent_t {
			public svn_node_kind_t kind;
			public long size;
			[MarshalAs (UnmanagedType.Bool)] public bool has_props;
			public svn_revnum_t created_rev;
			public long time; // created
			[MarshalAs (UnmanagedType.LPStr)] public string last_author;
		}
		
		public enum NodeSchedule {
			Normal,
			Add,
			Delete,
			Replace
		}
		
		public enum svn_wc_status_kind {
			EMPTY,
			None,
			Unversioned,
			Normal,
			Added,
			Missing,
			Deleted,
			Replaced,
			Modified,
			Merged,
			Conflicted,
			Ignored,
			Obstructed,
			External,
			Incomplete
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct svn_wc_entry_t {
			public string name;
			public svn_revnum_t revision;
			public string url;
			public string repos;
			public string repos_uuid;
			public svn_node_kind_t node_kind;
			public int schedule;
			[MarshalAs (UnmanagedType.Bool)] public bool copied;
			[MarshalAs (UnmanagedType.Bool)] public bool deleted;
			[MarshalAs (UnmanagedType.Bool)] public bool absent;
			[MarshalAs (UnmanagedType.Bool)] public bool incomplete;
			public string copiedfrom_url;
			public svn_revnum_t copiedfrom_rev;
			public string conflict_old;
			public string conflict_new;
			public string conflict_working;
			public string property_reject_file;
			public long text_time; // or zero
			public long prop_time;
			public string checksum; //(base64 for text-base, or NULL);
			public svn_revnum_t last_commit_rev;
			public long last_commit_date;
			public string last_commit_author;
			public string lock_token;
			public string lock_owner;
			public string lock_comment;
			public long lock_creation_date;
			[MarshalAs (UnmanagedType.Bool)] public bool has_props;
			[MarshalAs (UnmanagedType.Bool)] public bool has_prop_mods;
			public string cachable_props;
			public string present_props;
			public string changelist;
			public off_t working_size;
			[MarshalAs (UnmanagedType.Bool)] public bool keep_local;
			public SvnDepth depth;
			public string tree_conflict_data;
			public string file_external_path;
			public Rev file_external_peg_rev;
			public Rev file_external_rev;
		}

		public struct svn_wc_status2_t {
			public IntPtr to_svn_wc_entry_t;
			public svn_wc_status_kind text_status;      // Field not shown in the documentation
			public svn_wc_status_kind prop_status;     // Field not shown in the documentation
			[MarshalAs (UnmanagedType.Bool)] public bool locked;
			[MarshalAs (UnmanagedType.Bool)] public bool copied;
			[MarshalAs (UnmanagedType.Bool)] public bool switched;
			public svn_wc_status_kind repos_text_status;  // Field not shown in the documentation
			public svn_wc_status_kind repos_prop_status; // Field not shown in the documentation
			public IntPtr repos_lock;
			public IntPtr url;
			public svn_revnum_t ood_last_cmt_rev;
			public long ood_last_cmt_date;
			public svn_node_kind_t ood_kind;
			public string ood_last_cmt_author;
			public IntPtr tree_conflict;
			[MarshalAs (UnmanagedType.Bool)] public bool file_external;
			public svn_wc_status_kind pristine_text_status;
			public svn_wc_status_kind pristine_prop_status;
		}
		
		public struct svn_lock_t {
			public string path;
			public string token;
			public string owner;
			public string comment;
			[MarshalAs (UnmanagedType.Bool)] public bool is_dav_comment;
			public long creation_date;
			public long expiration_date;
		}
		
		public struct svn_string_t {
			public IntPtr data;
			public size_t len;
		}
		
		// struct svn_opt_revision_t
		[StructLayout (LayoutKind.Sequential)]
		public struct Rev {
			public svn_opt_revision_kind kind;
			public svn_opt_revision_value_t value;
			
			Rev (svn_opt_revision_kind kind) {
				this.kind = kind;
				value = new svn_opt_revision_value_t ();
			}
			
			public static Rev Number (long rev)
			{
				Rev r = new Rev(svn_opt_revision_kind.Number);
				r.value.number_or_date = rev;
				return r;
			}
			
			public static explicit operator Rev (SvnRevision rev)
			{
				Rev r = new Rev ();
				r.kind = (svn_opt_revision_kind) rev.Kind;
				if (r.kind == svn_opt_revision_kind.Number)
					r.value.number_or_date = rev.Rev;
				return r;
			}
			
			public static explicit operator Rev (Revision rev)
			{
				return (Rev) (SvnRevision) rev;
			}
			
			public readonly static Rev Blank = new Rev(svn_opt_revision_kind.Unspecified);
			public readonly static Rev First = new Rev(svn_opt_revision_kind.Number);
			public readonly static Rev Committed = new Rev(svn_opt_revision_kind.Committed);
			public readonly static Rev Previous = new Rev(svn_opt_revision_kind.Previous);
			public readonly static Rev Base = new Rev(svn_opt_revision_kind.Base);
			public readonly static Rev Working = new Rev(svn_opt_revision_kind.Working);
			public readonly static Rev Head = new Rev(svn_opt_revision_kind.Head);
		}

		public enum svn_opt_revision_kind {
			Unspecified,
			Number,
			Date,
			Committed,
			Previous,
		    Base,
			Working,
			Head
		}

		// This is a union between an svn_revnum_t size field and an int64 size field.
		[StructLayout (LayoutKind.Sequential)]
		public struct svn_opt_revision_value_t {
			public long number_or_date;
		}
		
		public struct svn_log_changed_path_t {
			public char action;
			public string copy_from_path;
			public svn_revnum_t copy_from_rev;
		}
		
		public struct svn_auth_cred_simple_t {
			[MarshalAs (UnmanagedType.LPStr)] public string username;
			[MarshalAs (UnmanagedType.LPStr)] public string password;
			[MarshalAs (UnmanagedType.Bool)] public bool may_save;
		}
		
		public struct svn_auth_cred_username_t {
			[MarshalAs (UnmanagedType.LPStr)] public string username;
			[MarshalAs (UnmanagedType.Bool)] public bool may_save;
		}
		
		public struct svn_auth_cred_ssl_server_trust_t {
			[MarshalAs (UnmanagedType.Bool)] public bool may_save;
			public uint accepted_failures;
		}
		
		public struct svn_auth_cred_ssl_client_cert_t {
			[MarshalAs (UnmanagedType.LPStr)] public string cert_file;
			[MarshalAs (UnmanagedType.Bool)] public bool may_save;
		}
		
		public struct svn_auth_cred_ssl_client_cert_pw_t {
			[MarshalAs (UnmanagedType.LPStr)] public string password;
			[MarshalAs (UnmanagedType.Bool)] public bool may_save;
		}
		
		public struct svn_auth_ssl_server_cert_info_t {
			[MarshalAs (UnmanagedType.LPStr)] public string hostname;
			[MarshalAs (UnmanagedType.LPStr)] public string fingerprint;
			[MarshalAs (UnmanagedType.LPStr)] public string valid_from;
			[MarshalAs (UnmanagedType.LPStr)] public string valid_until;
			[MarshalAs (UnmanagedType.LPStr)] public string issuer_dname;
			[MarshalAs (UnmanagedType.LPStr)] public string ascii_cert;
		}

		public enum SvnDepth {
			Unknown = - 2,
			Exclude = -1,
			Empty = 0,
			Files = 1,
			Immediates = 2,
			Infinity = 3
		}

		public enum NotifyAction {
			Add,
			Copy,
			Delete,
			Restore,
			Revert,
			FailedRevert,
			Resolved,
			Skip,
			UpdateDelete,
			UpdateAdd,
			UpdateUpdate,
			UpdateCompleted,
			UpdateExternal,
			StatusCompleted,
			StatusExternal,
			CommitModified,
			CommitAdded,
			CommitDeleted,
			CommitReplaced,
			CommitPostfixTxDelta,
			BlameRevision,
			Locked,
			Unlocked,
			FailedLock,
			FailedUnlock
		}
		
		public enum NotifyState {
			Inapplicable,
			Unknown,
			Unchanged,
			Missing,
			Obstructed,
			Changed,
			Merged,
			Conflicted
		}
		
		public enum NotifyLockState {
			Unchanged = 2,
			Locked = 3,
			Unlocked = 4
		}
		
		public delegate void svn_wc_status_func2_t (IntPtr baton, IntPtr path, IntPtr status);
		
		// Certificate is not yet valid.
		public const uint SVN_AUTH_SSL_NOTYETVALID = 0x00000001;
		// Certificate has expired.
		public const uint SVN_AUTH_SSL_EXPIRED = 0x00000002;
		// Certificate's CN (hostname) does not match the remote hostname.
		public const uint SVN_AUTH_SSL_CNMISMATCH = 0x00000004;
		// Certificate authority is unknown (i.e. not trusted)
		public const uint SVN_AUTH_SSL_UNKNOWNCA = 0x00000008;
		// Other failure. This can happen if neon has introduced a new failure bit that we do not handle yet. */
		public const uint SVN_AUTH_SSL_OTHER = 0x40000000;
		
		public delegate IntPtr svn_auth_simple_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.LPStr)] string user_name, [MarshalAs (UnmanagedType.Bool)] bool may_save, IntPtr pool);
		public delegate IntPtr svn_auth_username_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.Bool)] bool may_save, IntPtr pool);
		public delegate IntPtr svn_auth_ssl_server_trust_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, uint failures, ref svn_auth_ssl_server_cert_info_t cert_info, [MarshalAs (UnmanagedType.Bool)] bool may_save, IntPtr pool);
		public delegate IntPtr svn_auth_ssl_client_cert_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.Bool)] bool may_save, IntPtr pool);
		public delegate IntPtr svn_auth_ssl_client_cert_pw_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.Bool)] bool may_save, IntPtr pool);
		
		public delegate IntPtr svn_log_message_receiver_t (IntPtr baton, IntPtr apr_hash_changed_paths,
		                                                   svn_revnum_t revision, IntPtr author, IntPtr date,
		                                                   IntPtr message, IntPtr pool);
		
		public delegate IntPtr svn_readwrite_fn_t (IntPtr baton, IntPtr data, ref size_t len);
		
		public delegate void svn_wc_notify_func_t (IntPtr baton, IntPtr path, NotifyAction action, svn_node_kind_t kind,
		                                           IntPtr mime_type, NotifyState content_state, NotifyState prop_state,
		                                           svn_revnum_t revision);
		
		public delegate void svn_wc_notify_func2_t (IntPtr baton, ref svn_wc_notify_t notify, IntPtr pool);
		
		public delegate IntPtr svn_client_get_commit_log_t (ref IntPtr log_msg, ref IntPtr tmp_file, IntPtr commit_items,
		                                                    IntPtr baton, IntPtr pool);
		
		public delegate IntPtr svn_client_get_commit_log2_t (ref IntPtr log_msg, ref IntPtr tmp_file, IntPtr commit_items,
		                                                     IntPtr baton, IntPtr pool);

		public delegate IntPtr svn_client_get_commit_log3_t (ref IntPtr log_msg, ref IntPtr tmp_file, IntPtr commit_items,
		                                                     IntPtr baton, IntPtr pool);

		public delegate IntPtr svn_client_blame_receiver_t (IntPtr baton, long line_no, svn_revnum_t revision, string author, string date, string line, IntPtr pool);

		public delegate void svn_ra_progress_notify_func_t (off_t progress, off_t total, off_t baton, IntPtr pool);

		public delegate IntPtr svn_wc_conflict_resolver_func_t (out IntPtr result, out IntPtr description, IntPtr baton, IntPtr pool);
	}
}
