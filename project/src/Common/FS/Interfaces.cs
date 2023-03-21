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


	class Listing<EntriesType>
		where EntriesType : class, IFilesystemObject
	{
		private List<EntriesType> raw_ = null;
		private string currentExtensions_ = null;
		private List<EntriesType> perExtension_ = null;
		public string currentSearch_ = null;
		private List<EntriesType> searched_ = null;
		public int currentSort_ = Context.NoSort;
		public int currentSortDir_ = Context.NoSortDirection;
		private List<EntriesType> sorted_ = null;

		public void SetRaw(List<EntriesType> list)
		{
			raw_ = list;
			SetAllStale();
		}

		public void AddRaw(List<EntriesType> list)
		{
			if (raw_ == null)
				raw_ = new List<EntriesType>();

			raw_.AddRange(list);
			SetAllStale();
		}

		public List<EntriesType> Raw
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
			get { return sorted_ ?? searched_ ?? perExtension_ ?? raw_ ?? new List<EntriesType>(); }
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
		}


		private void SetAllStale()
		{
			SetPerExtensionStale();
			SetSearchStale();
			SetSortStale();
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
	}


	interface IFilesystemObject
	{
		IFilesystemContainer Parent { get; }

		Logger Log { get; }
		string Name { get; }
		string VirtualPath { get; }
		string DisplayName { get; set; }
		string Tooltip { get; }
		bool HasCustomDisplayName { get; }
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

		string MakeRealPath();
		string DeVirtualize();
		bool IsSameObject(IFilesystemObject o);
		void ClearCache();
	}


	struct ResolveDebug
	{
		public int indent;

		public ResolveDebug(int i)
		{
			indent = i;
		}

		public static ResolveDebug Null
		{
			get { return new ResolveDebug(-1); }
		}

		public bool Enabled
		{
			get { return (indent >= 0); }
		}

		public ResolveDebug Inc()
		{
			if (indent < 0)
				return Null;
			else
				return new ResolveDebug(indent + 1);
		}

		public void Info(object o, string s)
		{
			if (Enabled)
			{
				if (o == null)
				{
					AlternateUI.Instance.Log.Info(
						new string(' ', indent * 4) + $"{s}");
				}
				else
				{
					AlternateUI.Instance.Log.Info(
						new string(' ', indent * 4) + $"{o} {s}");
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
						new string(' ', indent * 4) + $"{s}");
				}
				else
				{
					AlternateUI.Instance.Log.Error(
						new string(' ', indent * 4) + $"{o} {s}");
				}
			}
		}
	}


	struct ResolveResult
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


	interface IFilesystemContainer : IFilesystemObject
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


	interface IDirectory : IFilesystemContainer
	{
	}


	interface IPackage : IFilesystemContainer
	{
	}


	interface IFile : IFilesystemObject
	{
	}
}
