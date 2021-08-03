using System.Collections.Generic;

namespace AUI.MorphUI
{
	class MorphComparer : IComparer<DAZMorph>
	{
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
					return (x.displayName.CompareTo(y.displayName));
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

		private int dupes_ = SamePathDupes | SimilarDupes;
		private bool showModified_ = true;
		private bool onlyLatest_ = true;
		private int sort_ = NoSort;
		private int sortDir_ = SortAscending;

		private Dictionary<string, DAZMorph> paths_ =
			new Dictionary<string, DAZMorph>();

		private Dictionary<string, int> filenames_ =
			new Dictionary<string, int>();

		private Dictionary<int, List<DAZMorph>> similar_ =
			new Dictionary<int, List<DAZMorph>>();

		public Filter()
		{
		}

		public int Dupes
		{
			get { return dupes_; }
			set { dupes_ = value; }
		}

		public bool ShowModified
		{
			get { return showModified_; }
			set { showModified_	= value; }
		}

		public bool OnlyLatest
		{
			get { return onlyLatest_; }
			set { onlyLatest_ = value; }
		}

		public int Sort
		{
			get { return sort_; }
			set { sort_ = value; }
		}

		public int SortDirection
		{
			get { return sortDir_; }
			set { sortDir_ = value; }
		}

		public List<DAZMorph> Process(List<DAZMorph> morphs)
		{
			paths_.Clear();
			filenames_.Clear();

			var list = new List<DAZMorph>();
			list.Capacity = morphs.Count;

			for (int i = 0; i < morphs.Count; ++i)
			{
				var m = morphs[i];

				if (ShouldShow(m))
					list.Add(m);
				else
					Log.Verbose($"filtered {m.uid}");
			}

			if (sort_ != NoSort)
				DoSort(list);

			return list;
		}

		private void DoSort(List<DAZMorph> list)
		{
			list.Sort(new MorphComparer(sort_, sortDir_));
		}

		private bool ShouldShow(DAZMorph m)
		{
			if (showModified_)
			{
				if (m.morphValue != m.startValue)
					return true;
			}

			if (onlyLatest_)
			{
				if (!m.isLatestVersion)
					return false;
			}

			if (dupes_ != AllowDupes)
			{
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
						Log.Info($"{m.morphName}: similar dupe");
						list.Add(m);
						return false;
					}
					else
					{
						list = new List<DAZMorph>();
						similar_.Add(hash, list);
					}
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
