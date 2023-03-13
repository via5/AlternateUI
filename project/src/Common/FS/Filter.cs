using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AUI.FS
{
	class Context
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
		public const int DebugFlag = 0x08;

		private readonly string search_;
		private readonly string searchLc_;
		private readonly string[] exts_;
		private readonly Regex searchRe_;
		private readonly int sort_;
		private readonly int sortDir_;
		private int flags_;

		public Context(
			string search, string[] extensions,
			int sort, int sortDir, int flags)
		{
			search_ = search;
			exts_ = extensions;
			sort_ = sort;
			sortDir_ = sortDir;
			flags_ = flags;

			if (VUI.Utilities.IsRegex(search_))
			{
				searchLc_ = null;
				searchRe_ = VUI.Utilities.CreateRegex(search_);
			}
			else
			{
				searchLc_ = search.ToLower();
				searchRe_ = null;
			}
		}

		public override string ToString()
		{
			return
				$"search='{search_}' " +
				$"exts={(exts_ == null ? "(none)" : string.Join(";", exts_))} " +
				$"sort={sort_} " +
				$"sortDir={sortDir_} " +
				$"flags={flags_}";
		}

		public static Context None
		{
			get
			{
				return new Context(	"", null, NoSort, NoSortDirection, NoFlags);
			}
		}

		public bool Empty
		{
			get
			{
				return
					string.IsNullOrEmpty(search_) &&
					string.IsNullOrEmpty(ExtensionsString);
			}
		}

		public string Search
		{
			get { return search_; }
		}

		public string[] Extensions
		{
			get { return exts_; }
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

		public bool ExtensionMatches(string path)
		{
			if (exts_ != null)
			{
				foreach (var e in exts_)
				{
					if (e == "*.*" || path.EndsWith(e))
						return true;
				}

				return false;
			}

			return true;
		}

		public bool SearchMatches(string path)
		{
			var name = Path.Filename(path);

			if (searchLc_ != null)
			{
				if (name.ToLower().IndexOf(searchLc_) == -1)
					return false;
			}
			else if (searchRe_ != null)
			{
				if (!searchRe_.IsMatch(name))
					return false;
			}

			return true;
		}


		private class FilenameComparer<EntryType> : IComparer<EntryType>
			where EntryType : IFilesystemObject
		{
			private readonly int dir_;

			public FilenameComparer(int dir)
			{
				dir_ = dir;
			}

			public static int SCompare(EntryType a, EntryType b, int dir)
			{
				if (dir == SortAscending)
					return U.CompareNatural(a.DisplayName, b.DisplayName);
				else
					return U.CompareNatural(b.DisplayName, a.DisplayName);
			}

			public int Compare(EntryType a, EntryType b)
			{
				return SCompare(a, b, dir_);
			}
		}

		private class TypeComparer<EntryType> : IComparer<EntryType>
			where EntryType : IFilesystemObject
		{
			private readonly int dir_;

			public TypeComparer(int dir)
			{
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
					c = FilenameComparer<EntryType>.SCompare(a, b, dir_);

				return c;
			}
		}

		private class DateModifiedComparer<EntryType> : IComparer<EntryType>
			where EntryType : IFilesystemObject
		{
			private readonly int dir_;

			public DateModifiedComparer(int dir)
			{
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
					c = FilenameComparer<EntryType>.SCompare(a, b, dir_);

				return c;
			}
		}

		private class DateCreatedComparer<EntryType> : IComparer<EntryType>
			where EntryType : IFilesystemObject
		{
			private readonly int dir_;

			public DateCreatedComparer(int dir)
			{
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
					c = FilenameComparer<EntryType>.SCompare(a, b, dir_);

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
					list.Sort(new FilenameComparer<EntryType>(sortDir_));
					break;
				}

				case SortType:
				{
					list.Sort(new TypeComparer<EntryType>(sortDir_));
					break;
				}

				case SortDateModified:
				{
					list.Sort(new DateModifiedComparer<EntryType>(sortDir_));
					break;
				}

				case SortDateCreated:
				{
					list.Sort(new DateCreatedComparer<EntryType>(sortDir_));
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
