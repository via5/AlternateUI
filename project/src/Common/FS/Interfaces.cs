using System;
using System.Collections.Generic;

namespace AUI.FS
{
	public struct PathComponents
	{
		private readonly string path_;
		private readonly string[] cs_;
		private int i_;

		public PathComponents(string path)
		{
			path_ = path.Replace('\\', '/');
			if (path_.StartsWith("/"))
				path_ = path_.Substring(1);

			cs_ = path_.Split('/');
			i_ = 0;
		}

		private PathComponents(string path, string[] cs, int i)
		{
			path_ = path;
			cs_ = cs;
			i_ = i;
		}

		public string Path
		{
			get { return path_; }
		}

		public bool Done
		{
			get { return (i_ >= cs_.Length); }
		}

		public bool Last
		{
			get { return (i_ + 1 >= cs_.Length); }
		}

		public bool NextIsLast
		{
			get { return (i_ + 2 >= cs_.Length); }
		}

		public string Current
		{
			get
			{
				if (i_ < 0 || i_ >= cs_.Length)
					return null;
				else
					return cs_[i_];
			}
		}

		public void Next()
		{
			++i_;
		}

		public PathComponents NextCopy()
		{
			return new PathComponents(path_, cs_, i_ + 1);
		}

		public override string ToString()
		{
			string s = "";

			for (int i = 0; i < cs_.Length; ++i)
			{
				if (s != "")
					s += ",";

				if (i == i_)
					s += "[" + cs_[i] + "]";
				else
					s += cs_[i];
			}

			if (i_ >= cs_.Length)
				s += "[]";

			return s;
		}
	}


	public class Listing<EntriesType>
		where EntriesType : class, IFilesystemObject
	{
		private List<EntriesType> raw_ = null;
		private List<EntriesType> filtered_ = null;
		private Dictionary<string, EntriesType> lookup_ = null;
		private string currentExtensions_ = null;
		private List<EntriesType> perExtension_ = null;
		public string currentSearch_ = null;
		private List<EntriesType> searched_ = null;
		public int currentSort_ = Context.NoSort;
		public int currentSortDir_ = Context.NoSortDirection;
		private List<EntriesType> sorted_ = null;

		public void SetRaw(List<EntriesType> all, List<EntriesType> filtered)
		{
			raw_ = all;
			filtered_ = filtered;
			SetAllStale();
		}

		public void UpdateLookup()
		{
			if (raw_ != null)
			{
				if (lookup_ == null)
					lookup_ = new Dictionary<string, EntriesType>();
				else
					lookup_.Clear();

				foreach (var e in raw_)
					lookup_.Add(e.Name, e);
			}
		}

		public void AddRaw(List<EntriesType> list)
		{
			if (raw_ == null)
				raw_ = new List<EntriesType>();

			raw_.AddRange(list);
			filtered_ = null;

			SetAllStale();
		}

		public List<EntriesType> Raw
		{
			get { return filtered_ ?? raw_; }
		}

		public List<EntriesType> RawUnfiltered
		{
			get { return raw_; }
		}

		public List<EntriesType> PerExtension
		{
			get { return perExtension_; }
		}

		public List<EntriesType> Searched
		{
			get { return searched_; }
		}

		public List<EntriesType> Sorted
		{
			get { return sorted_; }
		}

		public List<EntriesType> Last
		{
			get { return sorted_ ?? searched_ ?? perExtension_ ?? filtered_ ?? raw_ ?? new List<EntriesType>(); }
		}

		public Dictionary<string, EntriesType> Lookup
		{
			get { return lookup_; }
		}


		public bool ExtensionsStale(Context cx)
		{
			return (currentExtensions_ != cx.ExtensionsString);
		}

		public void SetExtensions(Context cx, List<EntriesType> list)
		{
			currentExtensions_ = cx.ExtensionsString;
			perExtension_ = list;

			SetSearchStale();
			SetSortStale();
			SetLookupStale();
		}


		public bool SearchStale(Context cx)
		{
			return (currentSearch_ != cx.Search.String);
		}

		public void SetSearched(Context cx, List<EntriesType> list)
		{
			currentSearch_ = cx.Search.String;
			searched_ = list;
			SetSortStale();
			SetLookupStale();
		}


		public bool SortStale(Context cx)
		{
			return (currentSort_ != cx.Sort || currentSortDir_ != cx.SortDirection);
		}

		public void SetSorted(Context cx, List<EntriesType> list)
		{
			currentSort_ = cx.Sort;
			currentSortDir_ = cx.SortDirection;
			sorted_ = list;
			SetLookupStale();
		}


		private void SetAllStale()
		{
			SetPerExtensionStale();
			SetSearchStale();
			SetSortStale();
			SetLookupStale();
		}

		private void SetPerExtensionStale()
		{
			currentExtensions_ = null;
			perExtension_ = null;
		}

		private void SetSearchStale()
		{
			currentSearch_ = null;
			searched_ = null;
		}

		private void SetSortStale()
		{
			currentSort_ = Context.NoSort;
			currentSortDir_ = Context.NoSortDirection;
			sorted_ = null;
		}

		private void SetLookupStale()
		{
			lookup_?.Clear();
		}
	}


	public interface IFilesystemObject
	{
		IFilesystemContainer Parent { get; }

		int DebugIdentity { get; }
		Logger Log { get; }
		string Name { get; }
		string VirtualPath { get; }
		string RelativeVirtualPath { get; }
		string Tooltip { get; }
		bool HasCustomDisplayName { get; }
		bool IsFile { get; }
		bool IsWritable{ get; }
		DateTime DateCreated { get; }
		DateTime DateModified { get; }
		VUI.Icon Icon { get; }

		bool CanPin { get; }
		bool Virtual { get; }
		bool ChildrenVirtual { get; }
		bool IsFlattened { get; }
		bool IsRedundant { get; }
		bool UnderlyingCanChange { get; }
		bool IsInternal { get; }
		IPackage ParentPackage { get; }

		string GetDisplayName(Context cx);
		void SetDisplayName(string name);
		string MakeRealPath();
		string DeVirtualize();
		bool IsSameObject(IFilesystemObject o);
		void ClearCache();
		string DebugInfo();
	}


	public struct ResolveDebug
	{
		private int indent_;
		private bool enabled_;

		public ResolveDebug(int i)
		{
			indent_ = i;
			enabled_ = (i >= 0);
		}

		public static ResolveDebug Null
		{
			get { return new ResolveDebug(-1); }
		}

		public bool Enabled
		{
			get { return enabled_; }
		}

		public ResolveDebug Inc()
		{
			if (Enabled)
				return new ResolveDebug(indent_ + 1);
			else
				return Null;
		}

		public void Info(object o, string s)
		{
			if (Enabled)
			{
				if (o == null)
				{
					AlternateUI.Instance.Log.Info(
						new string(' ', indent_ * 4) + $"{s}");
				}
				else
				{
					AlternateUI.Instance.Log.Info(
						new string(' ', indent_ * 4) + $"{o} {s}");
				}
			}
		}

		public void Error(object o, string s)
		{
			if (Enabled)
			{
				if (o == null)
				{
					AlternateUI.Instance.Log.Error(
						new string(' ', indent_ * 4) + $"{s}");
				}
				else
				{
					AlternateUI.Instance.Log.Error(
						new string(' ', indent_ * 4) + $"{o} {s}");
				}
			}
		}
	}


	public struct ResolveResult
	{
		public IFilesystemObject o;

		private ResolveResult(IFilesystemObject o, bool partial)
		{
			this.o = o;
		}

		public static ResolveResult NotFound()
		{
			return new ResolveResult(null, false);
		}

		public static ResolveResult Found(IFilesystemObject o)
		{
			return new ResolveResult(o, false);
		}
	}


	public interface IFilesystemContainer : IFilesystemObject
	{
		bool AlreadySorted { get; }
		bool HasDirectories(Context cx);
		List<IFilesystemContainer> GetDirectories(Context cx);
		List<IFilesystemObject> GetFiles(Context cx);

		IFilesystemObject Resolve(
			Context cx, string path, int flags = Filesystem.ResolveDefault);

		void GetFilesRecursiveInternal(
			Context cx, Listing<IFilesystemObject> listing);

		ResolveResult ResolveInternal(
			Context cx, PathComponents cs, int flags,
			ResolveDebug debug);
	}


	public interface IDirectory : IFilesystemContainer
	{
	}


	public interface IPackage : IFilesystemContainer
	{
		string GetRelativeVirtualPath(IFilesystemObject o);
	}


	public interface IFile : IFilesystemObject
	{
	}
}
