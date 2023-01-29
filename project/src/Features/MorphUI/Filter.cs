using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
					return U.CompareNatural(x.displayName, y.displayName);
				}
			}
		}
	}


	class Filter
	{
		class RefList<T>
		{
			private List<T> own_ = null;
			private List<T> refList_ = null;
			private RefList<T> refRef_ = null;

			public void Own(int capacity)
			{
				refList_ = null;
				refRef_ = null;

				if (own_ == null)
				{
					own_ = new List<T>(capacity);
				}
				else
				{
					own_.Clear();
					own_.Capacity = capacity;
				}
			}

			public void Own(List<T> copy)
			{
				refList_ = null;
				refRef_ = null;

				if (own_ == null)
				{
					own_ = new List<T>(copy);
				}
				else
				{
					own_.Clear();
					own_.AddRange(copy);
				}
			}

			public void Reference(List<T> other)
			{
				refList_ = other;
				refRef_ = null;
			}

			public void Reference(RefList<T> other)
			{
				refList_ = null;
				refRef_ = other;
			}

			public List<T> Get()
			{
				if (refList_ != null)
					return refList_;
				else if (refRef_ != null)
					return refRef_.Get();
				else
					return own_;
			}
		}


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
		private const int DirtySort = 0x04;
		private const int DirtyCategory = 0x08;
		private const int DirtySearch = 0x10;
		private const int DirtyAll = 0xff;

		private bool alwaysShowModified_ = true;
		private bool onlyFavorites_ = false;
		private bool onlyLatest_ = true;
		private bool onlyActive_ = false;
		private int dupes_ = AllowDupes;
		private string search_ = "";
		private int sort_ = NoSort;
		private int sortDir_ = SortAscending;
		private Categories.Node cat_ = null;

		private int dirty_ = Clean;

		private List<DAZMorph> all_ = new List<DAZMorph>();
		private readonly List<DAZMorph> checked_ = new List<DAZMorph>();
		private readonly List<DAZMorph> deduped_ = new List<DAZMorph>();
		private readonly RefList<DAZMorph> sorted_ = new RefList<DAZMorph>();
		private readonly RefList<DAZMorph> categorized_ = new RefList<DAZMorph>();
		private readonly RefList<DAZMorph> searched_ = new RefList<DAZMorph>();

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

		public bool OnlyFavorites
		{
			get { return onlyFavorites_; }
			set { onlyFavorites_ = value; ParamsChanged(); }
		}

		public bool OnlyLatest
		{
			get { return onlyLatest_; }
			set { onlyLatest_ = value; ParamsChanged(); }
		}

		public bool OnlyActive
		{
			get { return onlyActive_; }
			set { onlyActive_ = value; ParamsChanged(); }
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

		public Categories.Node Category
		{
			get { return cat_; }
			set { cat_ = value; CategoryChanged(); }
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
			dirty_ |= DirtyDupes | DirtySort | DirtyCategory | DirtySearch;
		}

		private void SortChanged()
		{
			dirty_ |= DirtySort | DirtyCategory | DirtySearch;
		}

		private void CategoryChanged()
		{
			dirty_ |= DirtyCategory | DirtySearch;
		}

		private void SearchChanged()
		{
			dirty_ |= DirtySearch;
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

			if (Bits.IsSet(dirty_, DirtySort))
				ProcessSort();

			if (Bits.IsAnySet(dirty_, DirtyCategory))
				ProcessCategory();

			if (Bits.IsAnySet(dirty_, DirtySearch))
				ProcessSearch();

			dirty_ = Clean;

			if (search_ == "")
				return categorized_.Get();
			else
				return searched_.Get();
		}

		private void ProcessParams()
		{
			var source = all_;

			checked_.Clear();
			checked_.Capacity = source.Count;

			for (int i = 0; i < source.Count; ++i)
			{
				var m = source[i];

				if (ShouldShowForParams(m))
					checked_.Add(m);
			}
		}

		private void ProcessDupes()
		{
			var source = checked_;

			deduped_.Clear();
			deduped_.Capacity = source.Count;

			paths_.Clear();
			filenames_.Clear();

			var keys = similar_.Keys.ToList();
			for (int i = 0; i < keys.Count; ++i)
				similar_[keys[i]].Clear();

			for (int i = 0; i < source.Count; ++i)
			{
				var m = source[i];

				if (ShouldShowForDupes(m))
					deduped_.Add(m);
			}
		}

		private void ProcessSort()
		{
			var source = deduped_;

			if (sort_ == NoSort)
			{
				sorted_.Reference(source);
			}
			else
			{
				sorted_.Own(source);
				sorted_.Get().Sort(new MorphComparer(sort_, sortDir_));
			}
		}

		private void ProcessCategory()
		{
			var source = sorted_;

			if (cat_ == null)
			{
				categorized_.Reference(source);
			}
			else
			{
				categorized_.Own(source.Get().Count);

				for (int i = 0; i < source.Get().Count; ++i)
				{
					if (cat_.ContainsRecursive(source.Get()[i]))
						categorized_.Get().Add(source.Get()[i]);
				}
			}
		}

		private bool IsRegex(string s)
		{
			return (s.Length >= 2 && s[0] == '/' && s[s.Length - 1] == '/');
		}

		private Regex CreateRegex(string s)
		{
			if (s.Length >= 2 && s[0] == '/' && s[s.Length - 1] == '/')
			{
				try
				{
					return new Regex(
						s.Substring(1, s.Length - 2), RegexOptions.IgnoreCase);
				}
				catch (Exception)
				{
					return null;
				}
			}

			return null;
		}

		private void ProcessSearch()
		{
			var source = categorized_;

			if (search_ == "")
			{
				searched_.Reference(source);
			}
			else
			{
				searched_.Own(source.Get().Count);

				if (IsRegex(search_))
				{
					var re = CreateRegex(search_);

					if (re != null)
					{
						for (int i = 0; i < source.Get().Count; ++i)
						{
							if (re.IsMatch(source.Get()[i].displayName))
								searched_.Get().Add(source.Get()[i]);
						}
					}
				}
				else
				{
					var searchLc = search_.ToLower();

					for (int i = 0; i < source.Get().Count; ++i)
					{
						if (source.Get()[i].displayName.ToLower().Contains(searchLc))
							searched_.Get().Add(source.Get()[i]);
					}
				}
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

			if (onlyActive_)
			{
				if (m.morphValue == m.startValue)
					return false;
			}

			if (onlyFavorites_)
			{
				if (!m.favorite)
					return false;
			}

			return true;
		}

		private bool ShouldShowForDupes(DAZMorph m)
		{
			if (dupes_ == AllowDupes)
				return true;

			if (alwaysShowModified_)
			{
				if (m.morphValue != m.startValue)
					return true;
			}

			var path = GetPath(m);
			var filename = GetFilename(m);

			if (Bits.IsSet(dupes_, SamePathDupes))
			{
				if (paths_.ContainsKey(path))
					return false;
				else
					paths_.Add(path, m);
			}

			if (Bits.IsSet(dupes_, SameFilenameDupes))
			{
				if (filenames_.ContainsKey(filename))
					return false;
				else
					filenames_.Add(filename, 0);
			}

			if (Bits.IsSet(dupes_, SimilarDupes))
			{
				List<DAZMorph> list;
				int hash = GetHash(m);

				if (similar_.TryGetValue(hash, out list) && list.Count > 0)
				{
					list.Add(m);
					return false;
				}
				else
				{
					if (list == null)
					{
						list = new List<DAZMorph>();
						similar_.Add(hash, list);
					}

					list.Add(m);
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

				AlternateUI.Instance.Log.Error($"{m.uid} has no :/");
			}

			return m.uid;
		}
	}
}
