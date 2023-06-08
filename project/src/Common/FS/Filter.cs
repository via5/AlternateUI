using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AUI.FS
{
	public class SearchString
	{
		private string string_ = null;
		private string lc_ = null;
		private Regex re_ = null;

		public SearchString(string s)
		{
			Set(s);
		}

		public bool Empty
		{
			get { return string.IsNullOrEmpty(string_); }
		}

		public string String
		{
			get { return string_; }
		}

		public void Set(string s)
		{
			if (string_ != s)
			{
				string_ = s;

				if (VUI.Utilities.IsRegex(string_))
				{
					lc_ = null;
					re_ = VUI.Utilities.CreateRegex(string_);
				}
				else
				{
					lc_ = string_.ToLower();
					re_ = null;
				}
			}
		}

		public bool Matches(Context cx, IFilesystemObject o)
		{
			return DoMatches(o.Name) || DoMatches(o.GetDisplayName(cx));
		}

		private bool DoMatches(string s)
		{
			if (lc_ != null)
			{
				if (s.ToLower().IndexOf(lc_) == -1)
					return false;
			}
			else if (re_ != null)
			{
				if (!re_.IsMatch(s))
					return false;
			}

			return true;
		}

		public override string ToString()
		{
			return string_;
		}
	}


	public class Whitelist
	{
		private readonly PathComponents[] paths_;

		public Whitelist(string[] paths)
		{
			var list = new List<PathComponents>();

			for (int i = 0; i < paths.Length; ++i)
				list.Add(new PathComponents(paths[i]));

			paths_ = list.ToArray();
		}

		public PathComponents[] Paths
		{
			get { return paths_; }
		}

		public bool Matches(string path)
		{
			if (paths_ == null)
				return true;

			for (int i = 0; i < paths_.Length; ++i)
			{
				if (Matches(paths_[i], path))
					return true;
			}

			return false;
		}

		private bool Matches(PathComponents cs, string vp)
		{
			int start = 0;

			while (!cs.Done)
			{
				int sep = vp.IndexOf('/', start);
				if (sep != -1 && sep == vp.Length - 1)
					break;

				string vpc;
				if (sep == -1)
					vpc = vp.Substring(start);
				else
					vpc = vp.Substring(start, sep - start);

				if (cs.Current != vpc)
					return false;

				if (sep == -1)
					break;

				start = sep + 1;
				cs.Next();
			}

			return true;
		}

		public override string ToString()
		{
			string s = "";

			foreach (var c in paths_)
			{
				if (s != "")
					s += ";";

				s += c.ToString();
			}

			return s;
		}
	}


	public class Context
	{
		public const int NoSort = 0;
		public const int SortFilename = 1;
		public const int SortType = 2;
		public const int SortDateModified = 3;
		public const int SortDateCreated = 4;

		public const int NoSortDirection = 0;
		public const int SortAscending = 1;
		public const int SortDescending = 2;

		public const int SortCount = 4 * 2;

		public const int NoFlags = 0x00;
		public const int RecursiveFlag = 0x01;
		public const int ShowHiddenFoldersFlag = 0x02;
		public const int ShowHiddenFilesFlag = 0x04;
		public const int MergePackagesFlag = 0x08;
		public const int DebugFlag = 0x10;
		public const int LatestPackagesOnlyFlag = 0x20;

		private readonly string[] exts_;
		private string packagesRoot_;
		private readonly int sort_;
		private readonly int sortDir_;
		private readonly SearchString search_;
		private readonly SearchString packagesSearch_;
		private readonly string removePrefix_;
		private int flags_;
		private readonly Whitelist whitelist_;

		public Context(
			string search, string[] extensions, string packagesRoot,
			int sort, int sortDir, int flags, string packagesSearch,
			string removePrefix, Whitelist whitelist)
		{
			search_ = new SearchString(search);
			exts_ = extensions;
			packagesRoot_ = packagesRoot;
			sort_ = sort;
			sortDir_ = sortDir;
			flags_ = flags;
			packagesSearch_ = new SearchString(packagesSearch);
			removePrefix_ = removePrefix;
			whitelist_ = whitelist;
		}

		public static string SortToString(int s)
		{
			switch (s)
			{
				case SortFilename:
					return "Filename";

				case SortType:
					return "Type";

				case SortDateModified:
					return "Date modified";

				case SortDateCreated:
					return "Date created";

				default:
					return $"?{s}";
			}
		}

		public static string SortDirectionToString(int s)
		{
			switch (s)
			{
				case SortAscending:
					return "Ascending";

				case SortDescending:
					return "Descending";

				default:
					return $"?{s}";
			}
		}

		public static string SortDirectionToShortString(int s)
		{
			switch (s)
			{
				case SortAscending:
					return "\u2191";

				case SortDescending:
					return "\u2193";

				default:
					return $"?{s}";
			}
		}

		public override string ToString()
		{
			return
				$"exts={(exts_ == null ? "(none)" : string.Join(";", exts_))} " +
				$"pr='{packagesRoot_}' " +
				$"sort={sort_} sortDir={sortDir_} " +
				$"search='{search_}' psearch='{packagesSearch_}' " +
				$"flags={flags_}";
		}

		public static Context None
		{
			get
			{
				return new Context(
					"", null, "", NoSort, NoSortDirection, NoFlags,
					"", "", null);
			}
		}

		public bool Empty
		{
			get
			{
				return
					search_.Empty &&
					string.IsNullOrEmpty(ExtensionsString);
			}
		}

		public SearchString Search
		{
			get { return search_; }
		}

		public SearchString PackagesSearch
		{
			get { return packagesSearch_; }
		}

		public string RemovePrefix
		{
			get { return removePrefix_; }
		}

		public string[] Extensions
		{
			get { return exts_; }
		}

		public string PackagesRoot
		{
			get { return packagesRoot_; }
		}

		public string ExtensionsString
		{
			get
			{
				if (exts_ == null || exts_.Length == 0)
					return "";

				return string.Join(";", exts_);
			}
		}

		public int Sort
		{
			get { return sort_; }
		}

		public int SortDirection
		{
			get { return sortDir_; }
		}

		public int Flags
		{
			get { return flags_; }
		}

		public Whitelist Whitelist
		{
			get { return whitelist_; }
		}

		public bool Debug
		{
			get { return Bits.IsSet(flags_, DebugFlag); }
		}

		public bool Recursive
		{
			get
			{
				return Bits.IsSet(flags_, RecursiveFlag);
			}

			set
			{
				if (value)
					flags_ |= RecursiveFlag;
				else
					flags_ &= ~RecursiveFlag;
			}
		}

		public bool ShowHiddenFolders
		{
			get { return Bits.IsSet(flags_, ShowHiddenFoldersFlag); }
		}

		public bool ShowHiddenFiles
		{
			get { return Bits.IsSet(flags_, ShowHiddenFilesFlag); }
		}

		public bool MergePackages
		{
			get { return Bits.IsSet(flags_, MergePackagesFlag); }
		}

		public bool LatestPackagesOnly
		{
			get { return Bits.IsSet(flags_, LatestPackagesOnlyFlag); }
		}

		public bool ExtensionMatches(IFilesystemObject o)
		{
			if (exts_ != null)
			{
				foreach (var e in exts_)
				{
					if (e == "*.*" || o.Name.EndsWith(e))
						return true;
				}

				return false;
			}

			return true;
		}

		public bool SearchMatches(IFilesystemObject o)
		{
			return search_.Matches(this, o);
		}

		public bool WhitelistMatches(string path)
		{
			if (whitelist_ == null)
				return true;

			return whitelist_.Matches(path);
		}


		private class FilenameComparer<EntryType> : IComparer<EntryType>
			where EntryType : IFilesystemObject
		{
			private readonly Context cx_;
			private readonly int dir_;

			public FilenameComparer(Context cx, int dir)
			{
				cx_ = cx;
				dir_ = dir;
			}

			public static int SCompare(Context cx, EntryType a, EntryType b, int dir)
			{
				if (dir == SortAscending)
					return U.CompareNatural(a.GetDisplayName(cx), b.GetDisplayName(cx));
				else
					return U.CompareNatural(b.GetDisplayName(cx), a.GetDisplayName(cx));
			}

			public int Compare(EntryType a, EntryType b)
			{
				return SCompare(cx_, a, b, dir_);
			}
		}

		private class TypeComparer<EntryType> : IComparer<EntryType>
			where EntryType : IFilesystemObject
		{
			private readonly Context cx_;
			private readonly int dir_;

			public TypeComparer(Context cx, int dir)
			{
				cx_ = cx;
				dir_ = dir;
			}

			public int Compare(EntryType a, EntryType b)
			{
				int c;

				if (dir_ == SortAscending)
					c = U.CompareNatural(Path.Extension(a.Name), Path.Extension(b.Name));
				else
					c = U.CompareNatural(Path.Extension(b.Name), Path.Extension(a.Name));

				if (c == 0)
					c = FilenameComparer<EntryType>.SCompare(cx_, a, b, dir_);

				return c;
			}
		}

		private class DateModifiedComparer<EntryType> : IComparer<EntryType>
			where EntryType : IFilesystemObject
		{
			private readonly Context cx_;
			private readonly int dir_;

			public DateModifiedComparer(Context cx, int dir)
			{
				cx_ = cx;
				dir_ = dir;
			}

			public int Compare(EntryType a, EntryType b)
			{
				int c;

				if (dir_ == SortAscending)
					c = DateTime.Compare(a.DateModified, b.DateModified);
				else
					c = DateTime.Compare(b.DateModified, a.DateModified);

				if (c == 0)
					c = FilenameComparer<EntryType>.SCompare(cx_, a, b, dir_);

				return c;
			}
		}

		private class DateCreatedComparer<EntryType> : IComparer<EntryType>
			where EntryType : IFilesystemObject
		{
			private readonly Context cx_;
			private readonly int dir_;

			public DateCreatedComparer(Context cx, int dir)
			{
				cx_ = cx;
				dir_ = dir;
			}

			public int Compare(EntryType a, EntryType b)
			{
				int c;

				if (dir_ == SortAscending)
					c = DateTime.Compare(a.DateCreated, b.DateCreated);
				else
					c = DateTime.Compare(b.DateCreated, a.DateCreated);

				if (c == 0)
					c = FilenameComparer<EntryType>.SCompare(cx_, a, b, dir_);

				return c;
			}
		}

		public void SortList<EntryType>(List<EntryType> list)
			where EntryType : IFilesystemObject
		{
			switch (sort_)
			{
				case SortFilename:
				{
					list.Sort(new FilenameComparer<EntryType>(this, sortDir_));
					break;
				}

				case SortType:
				{
					list.Sort(new TypeComparer<EntryType>(this, sortDir_));
					break;
				}

				case SortDateModified:
				{
					list.Sort(new DateModifiedComparer<EntryType>(this, sortDir_));
					break;
				}

				case SortDateCreated:
				{
					list.Sort(new DateCreatedComparer<EntryType>(this, sortDir_));
					break;
				}

				case NoSort:
				{
					// no-op
					break;
				}
			}
		}
	}
}
