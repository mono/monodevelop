// /cvs/monodevelop/Extras/VersionControl/MonoDevelop.VersionControl.Subversion/MonoDevelop.VersionControl.Subversion/LibSvnClient.cs created with MonoDevelop
// User: fejj at 12:12 PM 5/31/2007
//

using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Runtime.InteropServices;

using MonoDevelop.Core;
using MonoDevelop.VersionControl.Subversion.Gui;


namespace MonoDevelop.VersionControl.Subversion {
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
		                                      svn_wc_status_func_t status_func, IntPtr status_baton,
		                                      int descend, int get_all, int update, int no_ignore,
		                                      IntPtr ctx, IntPtr pool);
		
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
		                                      int recurse, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_delete (ref IntPtr commit_info_p, IntPtr apr_array_header_t_targets, 
		                                      int force, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_add3 (string path, int recurse, int force, int no_ignore, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_commit (ref IntPtr svn_client_commit_info_t_commit_info,
		                                      IntPtr apr_array_header_t_targets, int nonrecursive,
		                                      IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_revert (IntPtr apr_array_header_t_targets, int recursive,
		                                      IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_move (ref IntPtr commit_info_p, string srcPath, ref Rev rev,
		                                    string destPath, int force, IntPtr ctx, IntPtr pool);
		
		public abstract IntPtr client_checkout (IntPtr result_rev, string url, string path, ref Rev rev, 
		                                        int recurse, IntPtr ctx, IntPtr pool);
		
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
				IsDirectory = (ent.kind.ToInt32 () == (int) NodeKind.Dir);
				Size = ent.size;
				HasProps = ent.has_props != 0;
				CreatedRevision = ent.created_rev;
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
 			
			public readonly VersionStatus TextStatus;
			public readonly VersionStatus PropsStatus;
			public readonly bool Locked;
			public readonly bool Switched;
			public readonly VersionStatus RemoteTextStatus;
			public readonly VersionStatus RemotePropsStatus;
			
			static readonly DateTime Epoch = new DateTime (1970, 1, 1);
			
			internal StatusEnt (svn_wc_status_t status, string localpath)
			{
				LocalFilePath = localpath;
				TextStatus = (VersionStatus) status.svn_wc_status_kind_text;
				PropsStatus = (VersionStatus) status.svn_wc_status_kind_props;
				Locked = status.locked != 0;
				Copied = status.copied != 0;
				Switched = status.switched != 0;
				RemoteTextStatus = (VersionStatus) status.svn_wc_status_kind_text_repo;
				RemotePropsStatus = (VersionStatus) status.svn_wc_status_kind_props_repo;
				
				if (status.to_svn_wc_entry_t == IntPtr.Zero)
					return;
				
				svn_wc_entry_t ent = (svn_wc_entry_t) Marshal.PtrToStructure (status.to_svn_wc_entry_t, typeof (svn_wc_entry_t));
				Name = ent.name;
				Revision = ent.revision;
	  			Url = ent.url;
	  			Repository = ent.repos;
	  			RepositoryUuid = ent.repos_uuid;
	  			IsDirectory = (ent.node_kind == (int) NodeKind.Dir);
	  			Schedule = (NodeSchedule) ent.schedule;
	  			Copied = ent.copied != 0;
	  			Deleted = ent.deleted != 0;
	  			Absent = ent.absent != 0;
	  			Incomplete = ent.incomplete != 0;
	  			CopiedFrom = ent.copied_from;
	  			CopiedFromRevision = ent.copied_rev;
	  			ConflictOld = ent.conflict_old;
	  			ConflictNew = ent.conflict_new;
	  			ConflictWorking = ent.conflict_working;
	  			PropertyRejectFile = ent.property_reject_file;
	  			TextTime = Epoch.AddTicks (ent.text_time * 10);
	  			PropTime = Epoch.AddTicks (ent.prop_time * 10);
	  			Checksum = ent.checksum;
	  			LastCommitRevision = ent.last_commit_rev;
	  			LastCommitDate = Epoch.AddTicks (ent.last_commit_date * 10);
	  			LastCommitAuthor = ent.last_commit_author;
			}
		}
		
		public class LogEnt {
			public readonly int Revision;
			public readonly string Author;
			public readonly DateTime Time;
			public readonly string Message;
			public readonly LogEntChangedPath[] ChangedPaths;
			
			internal LogEnt (int rev, string author, DateTime time, string msg, LogEntChangedPath[] changes)
			{
				Revision = rev;
				Author = author;
				Time = time;
				Message = msg;
				ChangedPaths = changes;
			}
		}
		
		public class LogEntChangedPath {
			public readonly string Path;
			public readonly RevisionAction Action;
			public readonly string ActionDesc;
			public readonly string CopyFromPath;
			public readonly int CopyFromRevision;
			
			internal LogEntChangedPath(string path, svn_log_changed_path_t info) {
				Path = path;
				CopyFromPath = info.copy_from_path;
				CopyFromRevision = info.copy_from_rev;
				
				switch (info.action) {
				case 'A': Action = RevisionAction.Add; break;
				case 'D': Action = RevisionAction.Delete; break;
				case 'R': Action = RevisionAction.Replace; break;
				default: Action = RevisionAction.Modify; break; // should be an 'M'
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
		}
		
		public struct svn_error_t {
			[MarshalAs (UnmanagedType.SysInt)] public int apr_err;
			[MarshalAs (UnmanagedType.LPStr)] public string message;
			public IntPtr svn_error_t_child;
			public IntPtr pool;
			[MarshalAs (UnmanagedType.LPStr)] public string file;
			[MarshalAs (UnmanagedType.SysInt)] public int line;
		}
		
		public struct svn_client_commit_info_t {
			public int revision;
			public IntPtr date;
			public IntPtr author;
		}
		
		public struct svn_version_t {
			public int major;
			public int minor;
			public int patch;
			public string tag;
		}
		
		public enum NodeKind {
  			None,
  			File,
  			Dir,
  			Unknown
  		}
		
		internal struct svn_dirent_t {
			public IntPtr kind;
			public long size;
			//HACK: [MarshalAs (UnmanagedType.SysInt)] doesn't work on mono, so hack around 32/64 issue
			public IntPtr _has_props;
			public int has_props {
				get {
					return _has_props.ToInt32 ();
				}
			}
			public int created_rev;
			public long time; // created
			[MarshalAs (UnmanagedType.LPStr)] public string last_author;
		}
		
		public enum NodeSchedule {
			Normal,
			Add,
			Delete,
			Replace
		}
		
		public enum VersionStatus {
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
		
		public struct svn_wc_entry_t {
			public string name;
			public int revision;
			public string url;
			public string repos;
			public string repos_uuid;
			public int node_kind;
			public int schedule;
			public int copied;
			public int deleted;
			public int absent;
			public int incomplete;
			public string copied_from;
			public int copied_rev;
			public string conflict_old;
			public string conflict_new;
			public string conflict_working;
			public string property_reject_file;
			public long text_time; // or zero
			public long prop_time;
			public string checksum; //(base64 for text-base, or NULL);
			public int last_commit_rev;
			public long last_commit_date;
			public string last_commit_author;
		}
		
		public struct svn_wc_status_t {
			public IntPtr to_svn_wc_entry_t;
			public int svn_wc_status_kind_text;
			public int svn_wc_status_kind_props;
			public int locked;
			public int copied;
			public int switched;
			public int svn_wc_status_kind_text_repo;
			public int svn_wc_status_kind_props_repo;
		}
		
		public struct Rev {
			public IntPtr kind;
			public svn_opt_revision_value_t value;
			
			Rev (int kind) {
				this.kind = (IntPtr) kind;
				value = new svn_opt_revision_value_t ();
			}
			
			public static Rev Number (int rev)
			{
				Rev r = new Rev(1);
				r.value.number = (IntPtr) rev;
				return r;
			}
			
			public readonly static Rev Blank = new Rev(0);
			public readonly static Rev First = new Rev(1);
			public readonly static Rev Committed = new Rev(3);
			public readonly static Rev Previous = new Rev(4);
			public readonly static Rev Base = new Rev(5);
			public readonly static Rev Working = new Rev(6);
			public readonly static Rev Head = new Rev(7);
		}
		
		public struct svn_opt_revision_value_t {
			public IntPtr number;
			public IntPtr date1;
			public IntPtr date2;
		}
		
		public struct svn_log_changed_path_t {
			public char action;
			public string copy_from_path;
			public int copy_from_rev;
		}
		
		public struct svn_auth_cred_simple_t {
			[MarshalAs (UnmanagedType.LPStr)] public string username;
			[MarshalAs (UnmanagedType.LPStr)] public string password;
			[MarshalAs (UnmanagedType.SysInt)] public int may_save;
		}
		
		public struct svn_auth_cred_username_t {
			[MarshalAs (UnmanagedType.LPStr)] public string username;
			[MarshalAs (UnmanagedType.SysInt)] public int may_save;
		}
		
		public struct svn_auth_cred_ssl_server_trust_t {
			[MarshalAs (UnmanagedType.SysInt)] public int may_save;
			public uint accepted_failures;
		}
		
		public struct svn_auth_cred_ssl_client_cert_t {
			[MarshalAs (UnmanagedType.LPStr)] public string cert_file;
			[MarshalAs (UnmanagedType.SysInt)] public int may_save;
		}
		
		public struct svn_auth_cred_ssl_client_cert_pw_t {
			[MarshalAs (UnmanagedType.LPStr)] public string password;
			[MarshalAs (UnmanagedType.SysInt)] public int may_save;
		}
		
		public struct svn_auth_ssl_server_cert_info_t {
			[MarshalAs (UnmanagedType.LPStr)] public string hostname;
			[MarshalAs (UnmanagedType.LPStr)] public string fingerprint;
			[MarshalAs (UnmanagedType.LPStr)] public string valid_from;
			[MarshalAs (UnmanagedType.LPStr)] public string valid_until;
			[MarshalAs (UnmanagedType.LPStr)] public string issuer_dname;
			[MarshalAs (UnmanagedType.LPStr)] public string ascii_cert;
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
			BlameRevision
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
		
		public delegate void svn_wc_status_func_t (IntPtr baton, IntPtr path, ref svn_wc_status_t status);
		
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
		
		public delegate IntPtr svn_auth_simple_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.LPStr)] string user_name, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool);
		public delegate IntPtr svn_auth_username_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool);
		public delegate IntPtr svn_auth_ssl_server_trust_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, uint failures, ref svn_auth_ssl_server_cert_info_t cert_info, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool);
		public delegate IntPtr svn_auth_ssl_client_cert_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool);
		public delegate IntPtr svn_auth_ssl_client_cert_pw_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool);
		
		public delegate IntPtr svn_log_message_receiver_t (IntPtr baton, IntPtr apr_hash_changed_paths,
		                                                   int revision, IntPtr author, IntPtr date,
		                                                   IntPtr message, IntPtr pool);
		
		public delegate IntPtr svn_readwrite_fn_t (IntPtr baton, IntPtr data, ref IntPtr len);
		
		public delegate void svn_wc_notify_func_t (IntPtr baton, IntPtr path, NotifyAction action, NodeKind kind,
		                                           IntPtr mime_type, NotifyState content_state, NotifyState prop_state,
		                                           long revision);
		
		public delegate IntPtr svn_client_get_commit_log_t (ref IntPtr log_msg, ref IntPtr tmp_file, IntPtr commit_items,
		                                                    IntPtr baton, IntPtr pool);
	}
	
	public class LibSvnClient0 : LibSvnClient {
		private const string svnclientlib = "libsvn_client-1.so.0";
		
		public override void config_ensure (string config_dir, IntPtr pool)
		{
			svn_config_ensure (config_dir, pool);
		}
		
		public override void config_get_config (ref IntPtr cfg_hash, string config_dir, IntPtr pool)
		{
			svn_config_get_config (ref cfg_hash, config_dir, pool);
		}
		
		public override void auth_open (out IntPtr auth_baton, IntPtr providers, IntPtr pool)	
		{
			svn_auth_open (out auth_baton, providers, pool);
		}
		
		public override void auth_set_parameter (IntPtr auth_baton, string name, IntPtr value)
		{
			svn_auth_set_parameter (auth_baton, name, value);
		}
		
		public override IntPtr auth_get_parameter (IntPtr auth_baton, string name)
		{
			return svn_auth_get_parameter (auth_baton, name);
		}
		
		public override void client_get_simple_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_simple_provider (item, pool);
		}
		
		public override void client_get_simple_prompt_provider (IntPtr item, svn_auth_simple_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool)
		{
			svn_client_get_simple_prompt_provider (item, prompt_func, prompt_batton, retry_limit, pool);
		}
		
		public override void client_get_username_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_username_provider (item, pool);
		}
		
		public override void client_get_username_prompt_provider (IntPtr item, svn_auth_username_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool)
		{
			svn_client_get_username_prompt_provider (item, prompt_func, prompt_batton, retry_limit, pool);
		}
		
		public override void client_get_ssl_server_trust_file_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_ssl_server_trust_file_provider (item, pool);
		}
		
		public override void client_get_ssl_client_cert_file_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_ssl_client_cert_file_provider (item, pool);
		}
		
		public override void client_get_ssl_client_cert_pw_file_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_ssl_client_cert_pw_file_provider (item, pool);
		}
		
		public override void client_get_ssl_server_trust_prompt_provider (IntPtr item, svn_auth_ssl_server_trust_prompt_func_t prompt_func, IntPtr prompt_batton, IntPtr pool)
		{
			svn_client_get_ssl_server_trust_prompt_provider (item, prompt_func, prompt_batton, pool);
		}
		
		public override void client_get_ssl_client_cert_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool)
		{
			svn_client_get_ssl_client_cert_prompt_provider (item, prompt_func, prompt_batton, retry_limit, pool);
		}
		
		public override void client_get_ssl_client_cert_pw_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_pw_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool)
		{
			svn_client_get_ssl_client_cert_pw_prompt_provider (item, prompt_func, prompt_batton, retry_limit, pool);
		}
		
		public override IntPtr client_version ()
		{
			return svn_client_version ();
		}
		
		public override IntPtr client_create_context (out IntPtr ctx, IntPtr pool)
		{
			return svn_client_create_context (out ctx, pool);
		}
		
		public override IntPtr client_ls (out IntPtr dirents, string path_or_url,
		                                  ref Rev revision, int recurse, IntPtr ctx,
		                                  IntPtr pool)
		{
			return svn_client_ls (out dirents, path_or_url, ref revision, recurse, ctx, pool);
		}
		
		public override IntPtr client_status (IntPtr result_rev, string path, ref Rev revision,
		                                      svn_wc_status_func_t status_func, IntPtr status_baton,
		                                      int descend, int get_all, int update, int no_ignore,
		                                      IntPtr ctx, IntPtr pool)
		{
			return svn_client_status (result_rev, path, ref revision, status_func, status_baton,
			                          descend, get_all, update, no_ignore, ctx, pool);
		}
		
		public override IntPtr client_log (IntPtr apr_array_header_t_targets,
		                                   ref Rev rev_start, ref Rev rev_end,
		                                   int discover_changed_paths,
		                                   int strict_node_history,
		                                   svn_log_message_receiver_t receiver,
		                                   IntPtr receiver_baton,
		                                   IntPtr ctx, IntPtr pool)
		{
			return svn_client_log (apr_array_header_t_targets, ref rev_start, ref rev_end,
			                       discover_changed_paths, strict_node_history, receiver,
			                       receiver_baton,ctx, pool);
		}
		
		public override IntPtr time_from_cstring (out long aprtime, string time, IntPtr pool)
		{
			return svn_time_from_cstring (out aprtime, time, pool);
		}
		
		public override IntPtr client_url_from_path (ref IntPtr url, string path_or_url, IntPtr pool)
		{
			return svn_client_url_from_path (ref url, path_or_url, pool);
		}
		
		public override IntPtr client_cat2 (IntPtr stream, string path_or_url,
		                                    ref Rev peg_revision,
		                                    ref Rev revision,
		                                    IntPtr ctx, IntPtr pool)
		{
			return svn_client_cat2 (stream, path_or_url, ref peg_revision, ref revision, ctx, pool);
		}
		
		public override IntPtr stream_create (IntPtr baton, IntPtr pool)
		{
			return svn_stream_create (baton, pool);
		}
		
		//public override IntPtr stream_set_read (IntPtr stream, svn_readwrite_fn_t reader);
		
		public override IntPtr stream_set_write (IntPtr stream, svn_readwrite_fn_t writer)
		{
			return svn_stream_set_write (stream, writer);
		}
		
		public override IntPtr client_update (IntPtr result_rev, string path, ref Rev revision,
		                                      int recurse, IntPtr ctx, IntPtr pool)
		{
			return svn_client_update (result_rev, path, ref revision, recurse, ctx, pool);
		}
		
		public override IntPtr client_delete (ref IntPtr commit_info_p, IntPtr apr_array_header_t_targets, 
		                                      int force, IntPtr ctx, IntPtr pool)
		{
			return svn_client_delete (ref commit_info_p, apr_array_header_t_targets, force, ctx, pool);
		}
		
		public override IntPtr client_add3 (string path, int recurse, int force, int no_ignore, IntPtr ctx, IntPtr pool)
		{
			return svn_client_add3 (path, recurse, force, no_ignore, ctx, pool);
		}
		
		public override IntPtr client_commit (ref IntPtr svn_client_commit_info_t_commit_info,
		                                      IntPtr apr_array_header_t_targets, int nonrecursive,
		                                      IntPtr ctx, IntPtr pool)
		{
			return svn_client_commit (ref svn_client_commit_info_t_commit_info, apr_array_header_t_targets,
			                          nonrecursive, ctx, pool);
		}
		
		public override IntPtr client_revert (IntPtr apr_array_header_t_targets, int recursive,
		                                      IntPtr ctx, IntPtr pool)
		{
			return svn_client_revert (apr_array_header_t_targets, recursive, ctx, pool);
		}
		
		public override IntPtr client_move (ref IntPtr commit_info_p, string srcPath, ref Rev rev,
		                                    string destPath, int force, IntPtr ctx, IntPtr pool)
		{
			return svn_client_move (ref commit_info_p, srcPath, ref rev, destPath, force, ctx, pool);
		}
		
		public override IntPtr client_checkout (IntPtr result_rev, string url, string path, ref Rev rev, 
		                                        int recurse, IntPtr ctx, IntPtr pool)
		{
			return svn_client_checkout (result_rev, url, path, ref rev, recurse, ctx, pool);
		}
		
		public override IntPtr client_mkdir2 (ref IntPtr commit_info, IntPtr apr_array_paths, IntPtr ctx, IntPtr pool)
		{
			return svn_client_mkdir2 (ref commit_info, apr_array_paths, ctx, pool);
		}
		
		public override IntPtr client_diff (IntPtr diff_options, string path1, ref Rev revision1,
		                                    string path2, ref Rev revision2, int recurse,
		                                    int ignore_ancestry, int no_diff_deleted,
		                                    IntPtr outfile, IntPtr errfile,
		                                    IntPtr ctx, IntPtr pool)
		{
			return svn_client_diff (diff_options, path1, ref revision1, path2, ref revision2, recurse, ignore_ancestry,
			                        no_diff_deleted, outfile, errfile, ctx, pool);
		}
		
		public override IntPtr client_merge_peg2 (
		                          string source,
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
		                          IntPtr pool)
		{
			// svn_boolean_t == int
			return svn_client_merge_peg2 (source, ref revision1, ref revision2, ref peg_revision, target_wcpath, 
			                              recurse ? 1: 0, ignore_ancestry ? 1 : 0, force ? 1 : 0, dry_run ? 1 : 0,
			                              merge_options, ctx, pool);
		}
		
		[DllImport(svnclientlib)] static extern void svn_config_ensure (string config_dir, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_config_get_config (ref IntPtr cfg_hash, string config_dir, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_auth_open (out IntPtr auth_baton, IntPtr providers, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_auth_set_parameter (IntPtr auth_baton, string name, IntPtr value);
		[DllImport(svnclientlib)] static extern IntPtr svn_auth_get_parameter (IntPtr auth_baton, string name);
		[DllImport(svnclientlib)] static extern void svn_client_get_simple_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_simple_prompt_provider (IntPtr item, svn_auth_simple_prompt_func_t prompt_func, IntPtr prompt_batton, [MarshalAs (UnmanagedType.SysInt)] int retry_limit, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_username_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_username_prompt_provider (IntPtr item, svn_auth_username_prompt_func_t prompt_func, IntPtr prompt_batton, [MarshalAs (UnmanagedType.SysInt)] int retry_limit, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_server_trust_file_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_file_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_pw_file_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_server_trust_prompt_provider (IntPtr item, svn_auth_ssl_server_trust_prompt_func_t prompt_func, IntPtr prompt_batton, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_prompt_func_t prompt_func, IntPtr prompt_batton, [MarshalAs (UnmanagedType.SysInt)] int retry_limit, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_pw_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_pw_prompt_func_t prompt_func, IntPtr prompt_batton, [MarshalAs (UnmanagedType.SysInt)] int retry_limit, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_version();
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_create_context(out IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_ls (out IntPtr dirents, string path_or_url,
		                                                              ref Rev revision, int recurse, IntPtr ctx,
		                                                              IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_status (IntPtr result_rev, string path, ref Rev revision,
		                                                                  svn_wc_status_func_t status_func, IntPtr status_baton,
		                                                                  int descend, int get_all, int update, int no_ignore,
		                                                                  IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_log (IntPtr apr_array_header_t_targets,
		                                                               ref Rev rev_start, ref Rev rev_end,
		                                                               int discover_changed_paths,
		                                                               int strict_node_history,
		                                                               svn_log_message_receiver_t receiver,
		                                                               IntPtr receiver_baton,
		                                                               IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_time_from_cstring (out long aprtime, string time, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_url_from_path (ref IntPtr url, string path_or_url, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_cat2 (IntPtr stream, string path_or_url,
		                                                                ref Rev peg_revision,
		                                                                ref Rev revision,
		                                                                IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_stream_create (IntPtr baton, IntPtr pool);
		
		//[DllImport(svnclientlib)] static extern IntPtr svn_stream_set_read (IntPtr stream, svn_readwrite_fn_t reader);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_stream_set_write (IntPtr stream, svn_readwrite_fn_t writer);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_update (IntPtr result_rev, string path, ref Rev revision,
		                                                                  int recurse, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_delete (ref IntPtr commit_info_p, IntPtr apr_array_header_t_targets, 
		                                                                  int force, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_add3 (string path, int recurse, int force, int no_ignore, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_commit (ref IntPtr svn_client_commit_info_t_commit_info,
		                                                                  IntPtr apr_array_header_t_targets, int nonrecursive,
		                                                                  IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_revert (IntPtr apr_array_header_t_targets, int recursive,
		                                                                  IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_move (ref IntPtr commit_info_p, string srcPath, ref Rev rev,
		                                                                string destPath, int force, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_checkout (IntPtr result_rev, string url, string path, ref Rev rev, 
		                                                                    int recurse, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_mkdir2 (ref IntPtr commit_info, IntPtr apr_array_paths, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_diff (IntPtr diff_options, string path1,
		                                                                ref Rev revision1, string path2,
		                                                                ref Rev revision2, int recurse,
		                                                                int ignore_ancestry,
		                                                                int no_diff_deleted,
		                                                                IntPtr outfile,
		                                                                IntPtr errfile,
		                                                                IntPtr ctx,
		                                                                IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_merge_peg2 (string source,
		                                                                      ref Rev revision1,
		                                                                      ref Rev revision2,
		                                                                      ref Rev peg_revision,
		                                                                      string target_wcpath,
		                                                                      int recurse,
		                                                                      int ignore_ancestry,
		                                                                      int force,
		                                                                      int dry_run,
		                                                                      IntPtr merge_options,
		                                                                      IntPtr ctx,
		                                                                      IntPtr pool);
	}
	
	public class LibSvnClient1 : LibSvnClient {
		private const string svnclientlib = "libsvn_client-1.so.1";
		
		public override void config_ensure (string config_dir, IntPtr pool)
		{
			svn_config_ensure (config_dir, pool);
		}
		
		public override void config_get_config (ref IntPtr cfg_hash, string config_dir, IntPtr pool)
		{
			svn_config_get_config (ref cfg_hash, config_dir, pool);
		}
		
		public override void auth_open (out IntPtr auth_baton, IntPtr providers, IntPtr pool)	
		{
			svn_auth_open (out auth_baton, providers, pool);
		}
		
		public override void auth_set_parameter (IntPtr auth_baton, string name, IntPtr value)
		{
			svn_auth_set_parameter (auth_baton, name, value);
		}
		
		public override IntPtr auth_get_parameter (IntPtr auth_baton, string name)
		{
			return svn_auth_get_parameter (auth_baton, name);
		}
		
		public override void client_get_simple_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_simple_provider (item, pool);
		}
		
		public override void client_get_simple_prompt_provider (IntPtr item, svn_auth_simple_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool)
		{
			svn_client_get_simple_prompt_provider (item, prompt_func, prompt_batton, retry_limit, pool);
		}
		
		public override void client_get_username_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_username_provider (item, pool);
		}
		
		public override void client_get_username_prompt_provider (IntPtr item, svn_auth_username_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool)
		{
			svn_client_get_username_prompt_provider (item, prompt_func, prompt_batton, retry_limit, pool);
		}
		
		public override void client_get_ssl_server_trust_file_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_ssl_server_trust_file_provider (item, pool);
		}
		
		public override void client_get_ssl_client_cert_file_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_ssl_client_cert_file_provider (item, pool);
		}
		
		public override void client_get_ssl_client_cert_pw_file_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_ssl_client_cert_pw_file_provider (item, pool);
		}
		
		public override void client_get_ssl_server_trust_prompt_provider (IntPtr item, svn_auth_ssl_server_trust_prompt_func_t prompt_func, IntPtr prompt_batton, IntPtr pool)
		{
			svn_client_get_ssl_server_trust_prompt_provider (item, prompt_func, prompt_batton, pool);
		}
		
		public override void client_get_ssl_client_cert_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool)
		{
			svn_client_get_ssl_client_cert_prompt_provider (item, prompt_func, prompt_batton, retry_limit, pool);
		}
		
		public override void client_get_ssl_client_cert_pw_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_pw_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool)
		{
			svn_client_get_ssl_client_cert_pw_prompt_provider (item, prompt_func, prompt_batton, retry_limit, pool);
		}
		
		public override IntPtr client_version ()
		{
			return svn_client_version ();
		}
		
		public override IntPtr client_create_context (out IntPtr ctx, IntPtr pool)
		{
			return svn_client_create_context (out ctx, pool);
		}
		
		public override IntPtr client_ls (out IntPtr dirents, string path_or_url,
		                                  ref Rev revision, int recurse, IntPtr ctx,
		                                  IntPtr pool)
		{
			return svn_client_ls (out dirents, path_or_url, ref revision, recurse, ctx, pool);
		}
		
		public override IntPtr client_status (IntPtr result_rev, string path, ref Rev revision,
		                                      svn_wc_status_func_t status_func, IntPtr status_baton,
		                                      int descend, int get_all, int update, int no_ignore,
		                                      IntPtr ctx, IntPtr pool)
		{
			return svn_client_status (result_rev, path, ref revision, status_func, status_baton,
			                          descend, get_all, update, no_ignore, ctx, pool);
		}
		
		public override IntPtr client_log (IntPtr apr_array_header_t_targets,
		                                   ref Rev rev_start, ref Rev rev_end,
		                                   int discover_changed_paths,
		                                   int strict_node_history,
		                                   svn_log_message_receiver_t receiver,
		                                   IntPtr receiver_baton,
		                                   IntPtr ctx, IntPtr pool)
		{
			return svn_client_log (apr_array_header_t_targets, ref rev_start, ref rev_end,
			                       discover_changed_paths, strict_node_history, receiver,
			                       receiver_baton,ctx, pool);
		}
		
		public override IntPtr time_from_cstring (out long aprtime, string time, IntPtr pool)
		{
			return svn_time_from_cstring (out aprtime, time, pool);
		}
		
		public override IntPtr client_url_from_path (ref IntPtr url, string path_or_url, IntPtr pool)
		{
			return svn_client_url_from_path (ref url, path_or_url, pool);
		}
		
		public override IntPtr client_cat2 (IntPtr stream, string path_or_url,
		                                    ref Rev peg_revision,
		                                    ref Rev revision,
		                                    IntPtr ctx, IntPtr pool)
		{
			return svn_client_cat2 (stream, path_or_url, ref peg_revision, ref revision, ctx, pool);
		}
		
		public override IntPtr stream_create (IntPtr baton, IntPtr pool)
		{
			return svn_stream_create (baton, pool);
		}
		
		//public override IntPtr stream_set_read (IntPtr stream, svn_readwrite_fn_t reader);
		
		public override IntPtr stream_set_write (IntPtr stream, svn_readwrite_fn_t writer)
		{
			return svn_stream_set_write (stream, writer);
		}
		
		public override IntPtr client_update (IntPtr result_rev, string path, ref Rev revision,
		                                      int recurse, IntPtr ctx, IntPtr pool)
		{
			return svn_client_update (result_rev, path, ref revision, recurse, ctx, pool);
		}
		
		public override IntPtr client_delete (ref IntPtr commit_info_p, IntPtr apr_array_header_t_targets, 
		                                      int force, IntPtr ctx, IntPtr pool)
		{
			return svn_client_delete (ref commit_info_p, apr_array_header_t_targets, force, ctx, pool);
		}
		
		public override IntPtr client_add3 (string path, int recurse, int force, int no_ignore, IntPtr ctx, IntPtr pool)
		{
			return svn_client_add3 (path, recurse, force, no_ignore, ctx, pool);
		}
		
		public override IntPtr client_commit (ref IntPtr svn_client_commit_info_t_commit_info,
		                                      IntPtr apr_array_header_t_targets, int nonrecursive,
		                                      IntPtr ctx, IntPtr pool)
		{
			return svn_client_commit (ref svn_client_commit_info_t_commit_info, apr_array_header_t_targets,
			                          nonrecursive, ctx, pool);
		}
		
		public override IntPtr client_revert (IntPtr apr_array_header_t_targets, int recursive,
		                                      IntPtr ctx, IntPtr pool)
		{
			return svn_client_revert (apr_array_header_t_targets, recursive, ctx, pool);
		}
		
		public override IntPtr client_move (ref IntPtr commit_info_p, string srcPath, ref Rev rev,
		                                    string destPath, int force, IntPtr ctx, IntPtr pool)
		{
			return svn_client_move (ref commit_info_p, srcPath, ref rev, destPath, force, ctx, pool);
		}
		
		public override IntPtr client_checkout (IntPtr result_rev, string url, string path, ref Rev rev, 
		                                        int recurse, IntPtr ctx, IntPtr pool)
		{
			return svn_client_checkout (result_rev, url, path, ref rev, recurse, ctx, pool);
		}
		
		public override IntPtr client_mkdir2 (ref IntPtr commit_info, IntPtr apr_array_paths, IntPtr ctx, IntPtr pool)
		{
			return svn_client_mkdir2 (ref commit_info, apr_array_paths, ctx, pool);
		}
		
		public override IntPtr client_diff (IntPtr diff_options, string path1, ref Rev revision1,
		                                    string path2, ref Rev revision2, int recurse,
		                                    int ignore_ancestry, int no_diff_deleted,
		                                    IntPtr outfile, IntPtr errfile,
		                                    IntPtr ctx, IntPtr pool)
		{
			return svn_client_diff (diff_options, path1, ref revision1, path2, ref revision2, recurse, ignore_ancestry,
			                        no_diff_deleted, outfile, errfile, ctx, pool);
		}
		
		public override IntPtr client_merge_peg2 (
		                          string source,
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
		                          IntPtr pool)
		{
			// svn_boolean_t == int
			return svn_client_merge_peg2 (source, ref revision1, ref revision2, ref peg_revision, target_wcpath, 
			                              recurse ? 1: 0, ignore_ancestry ? 1 : 0, force ? 1 : 0, dry_run ? 1 : 0,
			                              merge_options, ctx, pool);
		}

		[DllImport(svnclientlib)] static extern void svn_config_ensure (string config_dir, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_config_get_config (ref IntPtr cfg_hash, string config_dir, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_auth_open (out IntPtr auth_baton, IntPtr providers, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_auth_set_parameter (IntPtr auth_baton, string name, IntPtr value);
		[DllImport(svnclientlib)] static extern IntPtr svn_auth_get_parameter (IntPtr auth_baton, string name);
		[DllImport(svnclientlib)] static extern void svn_client_get_simple_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_simple_prompt_provider (IntPtr item, svn_auth_simple_prompt_func_t prompt_func, IntPtr prompt_batton, [MarshalAs (UnmanagedType.SysInt)] int retry_limit, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_username_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_username_prompt_provider (IntPtr item, svn_auth_username_prompt_func_t prompt_func, IntPtr prompt_batton, [MarshalAs (UnmanagedType.SysInt)] int retry_limit, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_server_trust_file_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_file_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_pw_file_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_server_trust_prompt_provider (IntPtr item, svn_auth_ssl_server_trust_prompt_func_t prompt_func, IntPtr prompt_batton, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_prompt_func_t prompt_func, IntPtr prompt_batton, [MarshalAs (UnmanagedType.SysInt)] int retry_limit, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_pw_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_pw_prompt_func_t prompt_func, IntPtr prompt_batton, [MarshalAs (UnmanagedType.SysInt)] int retry_limit, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_version();
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_create_context(out IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_ls (out IntPtr dirents, string path_or_url,
		                                                              ref Rev revision, int recurse, IntPtr ctx,
		                                                              IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_status (IntPtr result_rev, string path, ref Rev revision,
		                                                                  svn_wc_status_func_t status_func, IntPtr status_baton,
		                                                                  int descend, int get_all, int update, int no_ignore,
		                                                                  IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_log (IntPtr apr_array_header_t_targets,
		                                                               ref Rev rev_start, ref Rev rev_end,
		                                                               int discover_changed_paths,
		                                                               int strict_node_history,
		                                                               svn_log_message_receiver_t receiver,
		                                                               IntPtr receiver_baton,
		                                                               IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_time_from_cstring (out long aprtime, string time, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_url_from_path (ref IntPtr url, string path_or_url, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_cat2 (IntPtr stream, string path_or_url,
		                                                                ref Rev peg_revision,
		                                                                ref Rev revision,
		                                                                IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_stream_create (IntPtr baton, IntPtr pool);
		
		//[DllImport(svnclientlib)] static extern IntPtr svn_stream_set_read (IntPtr stream, svn_readwrite_fn_t reader);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_stream_set_write (IntPtr stream, svn_readwrite_fn_t writer);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_update (IntPtr result_rev, string path, ref Rev revision,
		                                                                  int recurse, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_delete (ref IntPtr commit_info_p, IntPtr apr_array_header_t_targets, 
		                                                                  int force, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_add3 (string path, int recurse, int force, int no_ignore, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_commit (ref IntPtr svn_client_commit_info_t_commit_info,
		                                                                  IntPtr apr_array_header_t_targets, int nonrecursive,
		                                                                  IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_revert (IntPtr apr_array_header_t_targets, int recursive,
		                                                                  IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_move (ref IntPtr commit_info_p, string srcPath, ref Rev rev,
		                                                                string destPath, int force, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_checkout (IntPtr result_rev, string url, string path, ref Rev rev, 
		                                                                    int recurse, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_mkdir2 (ref IntPtr commit_info, IntPtr apr_array_paths, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_diff (IntPtr diff_options, string path1,
		                                                                ref Rev revision1, string path2,
		                                                                ref Rev revision2, int recurse,
		                                                                int ignore_ancestry,
		                                                                int no_diff_deleted,
		                                                                IntPtr outfile,
		                                                                IntPtr errfile,
		                                                                IntPtr ctx,
		                                                                IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_merge_peg2 (string source,
		                                                                      ref Rev revision1,
		                                                                      ref Rev revision2,
		                                                                      ref Rev peg_revision,
		                                                                      string target_wcpath,
		                                                                      int recurse,
		                                                                      int ignore_ancestry,
		                                                                      int force,
		                                                                      int dry_run,
		                                                                      IntPtr merge_options,
		                                                                      IntPtr ctx,
		                                                                      IntPtr pool);
		
	}
}
