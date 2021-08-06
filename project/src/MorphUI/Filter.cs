using System.Collections.Generic;

namespace AUI.MorphUI
{
	class MorphComparer : IComparer<DAZMorph>
	{
		private NaturalStringComparer nat_ = new NaturalStringComparer();
		private readonly int sort_;
		private readonly int sortDir_;

		public MorphComparer(int sort, int sortDir)
		{
			sort_ = sort;
			sortDir_ = sortDir;
		}

		public int Compare(DAZMorph x, DAZMorph y)
		{
			if (sortDir_ == Filter.SortDescending)
			{
				var temp = x;
				x = y;
				y = temp;
			}

			switch (sort_)
			{
				case Filter.SortName:
				default:
				{
					return nat_.Compare(x.displayName, y.displayName);
				}
			}
		}
	}


	class Filter
	{
		public const int AllowDupes = 0x00;
		public const int SamePathDupes = 0x01;
		public const int SameFilenameDupes = 0x02;
		public const int SimilarDupes = 0x04;

		public const int NoSort = 0;
		public const int SortName = 1;

		public const int SortAscending = 0;
		public const int SortDescending = 1;

		private const int Clean = 0x00;
		private const int DirtyParams = 0x01;
		private const int DirtyDupes = 0x02;
		private const int DirtySearch = 0x04;
		private const int DirtyDupesSort = 0x08;
		private const int DirtySearchedSort = 0x10;
		private const int DirtyAll = 0xff;

		private bool alwaysShowModified_ = true;
		private bool onlyLatest_ = true;
		private int dupes_ = SamePathDupes | SimilarDupes;
		private string search_ = "";
		private int sort_ = NoSort;
		private int sortDir_ = SortAscending;

		private int dirty_ = Clean;

		private List<DAZMorph> all_ = new List<DAZMorph>();
		private List<DAZMorph> checked_ = new List<DAZMorph>();
		private List<DAZMorph> deduped_ = new List<DAZMorph>();
		private List<DAZMorph> searched_ = new List<DAZMorph>();
		private List<DAZMorph> dedupedSorted_ = new List<DAZMorph>();
		private List<DAZMorph> searchedSorted_ = new List<DAZMorph>();

		private Dictionary<string, DAZMorph> paths_ =
			new Dictionary<string, DAZMorph>();

		private Dictionary<string, int> filenames_ =
			new Dictionary<string, int>();

		private Dictionary<int, List<DAZMorph>> similar_ =
			new Dictionary<int, List<DAZMorph>>();

		private List<string> cats_ = new List<string>();


		public Filter()
		{
		}

		public bool AlwaysShowModified
		{
			get { return alwaysShowModified_; }
			set { alwaysShowModified_ = value; ParamsChanged(); }
		}

		public bool OnlyLatest
		{
			get { return onlyLatest_; }
			set { onlyLatest_ = value; ParamsChanged(); }
		}

		public int Dupes
		{
			get { return dupes_; }
			set { dupes_ = value; DupesChanged(); }
		}

		public string Search
		{
			get { return search_; }
			set { search_ = value; SearchChanged(); }
		}

		public int Sort
		{
			get { return sort_; }
			set { sort_ = value; SortChanged(); }
		}

		public int SortDirection
		{
			get { return sortDir_; }
			set { sortDir_ = value; SortChanged(); }
		}

		public bool IsDirty
		{
			get { return (dirty_ != Clean); }
		}

		private void ParamsChanged()
		{
			dirty_ |= DirtyAll;
		}

		private void DupesChanged()
		{
			dirty_ |=
				DirtyDupes | DirtySearch |
				DirtyDupesSort | DirtySearchedSort;
		}

		private void SearchChanged()
		{
			dirty_ |= DirtySearch| DirtySearchedSort;
		}

		private void SortChanged()
		{
			dirty_ |= DirtyDupesSort | DirtySearchedSort;
		}

		public void Set(List<DAZMorph> all)
		{
			all_ = all;
			dirty_ = DirtyAll;
		}

		public List<DAZMorph> Process()
		{
			if (Bits.IsAnySet(dirty_, DirtyParams))
				ProcessParams();

			if (Bits.IsAnySet(dirty_, DirtyDupes))
				ProcessDupes();

			if (Bits.IsAnySet(dirty_, DirtySearch))
				ProcessSearch();

			if (Bits.IsSet(dirty_, DirtyDupesSort))
				ProcessDupesSort();

			if (Bits.IsSet(dirty_, DirtySearchedSort) && search_ != "")
				ProcessSearchedSort();

			dirty_ = Clean;

			if (search_ == "")
				return dedupedSorted_;
			else
				return searchedSorted_;
		}

		private void ProcessParams()
		{
			Log.Info("process params");

			checked_.Clear();
			checked_.Capacity = all_.Count;

			for (int i = 0; i < all_.Count; ++i)
			{
				var m = all_[i];

				if (ShouldShowForParams(m))
					checked_.Add(m);
				else
					Log.Verbose($"params: filtered {m.uid}");
			}
		}

		private void ProcessDupes()
		{
			Log.Info("process dupes");

			deduped_.Clear();
			deduped_.Capacity = all_.Count;

			paths_.Clear();
			filenames_.Clear();
			similar_.Clear();

			for (int i = 0; i < checked_.Count; ++i)
			{
				var m = checked_[i];

				if (ShouldShowForDupes(m))
					deduped_.Add(m);
				else
					Log.Verbose($"dupes: filtered {m.uid}");
			}
		}

		private void ProcessSearch()
		{
			Log.Info($"process search '{search_}'");

			if (search_ == "")
			{
				searched_ = deduped_;
			}
			else
			{
				if (searched_ == deduped_)
				{
					searched_ = new List<DAZMorph>(deduped_.Count);
				}
				else
				{
					searched_.Clear();
					searched_.Capacity = deduped_.Count;
				}

				var searchLc = search_.ToLower();

				for (int i = 0; i < deduped_.Count; ++i)
				{
					if (deduped_[i].displayName.ToLower().Contains(searchLc))
						searched_.Add(deduped_[i]);
				}
			}
		}

		private void ProcessDupesSort()
		{
			Log.Info("process dupes sort");

			if (sort_ == NoSort)
			{
				dedupedSorted_ = deduped_;
			}
			else
			{
				dedupedSorted_ = new List<DAZMorph>(searched_);
				dedupedSorted_.Sort(new MorphComparer(sort_, sortDir_));
			}
		}

		private void ProcessSearchedSort()
		{
			Log.Info("process searched sort");

			if (sort_ == NoSort)
			{
				searchedSorted_ = searched_;
			}
			else
			{
				searchedSorted_ = new List<DAZMorph>(searched_);
				searchedSorted_.Sort(new MorphComparer(sort_, sortDir_));
			}
		}

		private bool ShouldShowForParams(DAZMorph m)
		{
			if (alwaysShowModified_)
			{
				if (m.morphValue != m.startValue)
					return true;
			}

			if (onlyLatest_)
			{
				if (!m.isLatestVersion)
					return false;
			}

			return true;
		}

		private bool ShouldShowForDupes(DAZMorph m)
		{
			if (dupes_ == AllowDupes)
				return true;

			var path = GetPath(m);
			var filename = GetFilename(m);

			if (Bits.IsSet(dupes_, SamePathDupes))
			{
				if (paths_.ContainsKey(path))
				{
					Log.Verbose($"{m.uid}: path dupe {path}");
					return false;
				}
				else
				{
					paths_.Add(path, m);
				}
			}

			if (Bits.IsSet(dupes_, SameFilenameDupes))
			{
				if (filenames_.ContainsKey(filename))
				{
					Log.Verbose($"{m.uid}: filename dupe {filename}");
					return false;
				}
				else
				{
					filenames_.Add(filename, 0);
				}
			}

			if (Bits.IsSet(dupes_, SimilarDupes))
			{
				List<DAZMorph> list;
				int hash = GetHash(m);

				if (similar_.TryGetValue(hash, out list))
				{
					Log.Verbose($"{m.morphName}: similar dupe");
					list.Add(m);
					return false;
				}
				else
				{
					list = new List<DAZMorph>();
					similar_.Add(hash, list);
				}
			}

			return true;
		}

		private int GetHash(DAZMorph m)
		{
			return HashHelper.GetHashCode(
				m.morphName, m.numDeltas,
				m.min, m.max, m.isPoseControl,
				m.group, m.region);
		}

		public static string GetFilename(DAZMorph m)
		{
			var p = GetPath(m);

			var sep = p.LastIndexOfAny("/\\".ToCharArray());
			if (sep != -1)
				return p.Substring(sep + 1);

			return p;
		}

		public static string GetPath(DAZMorph m)
		{
			if (m.isInPackage)
			{
				var pos = m.uid.IndexOf(":/");
				if (pos != -1)
					return m.uid.Substring(pos + 2);

				Log.Error($"{m.uid} has no :/");
			}

			return m.uid;
		}
	}
}
