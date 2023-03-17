using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AUI.DynamicItemsUI
{
	class Filter
	{
		public const int SortAZ = 0;
		public const int SortZA = 1;
		public const int SortNew = 2;
		public const int SortOld = 3;
		public const int SortAuthor = 4;
		public const int SortCount = 5;

		public delegate void Handler();
		public event Handler TagsChanged, AuthorsChanged;

		private readonly AtomUI parent_;

		private bool active_ = false;
		private string search_ = "";
		private int sort_ = SortNew;

		private readonly HashSet<string> tags_ = new HashSet<string>();
		private bool tagsAnd_ = true;

		private readonly HashSet<string> authors_ = new HashSet<string>();
		private readonly HashSet<string> authorsLc_ = new HashSet<string>();


		public Filter(AtomUI parent)
		{
			parent_ = parent;
		}

		public static string SortToString(int i)
		{
			switch (i)
			{
				case SortAZ: return "A to Z";
				case SortZA: return "Z to A";
				case SortNew: return "New to old";
				case SortOld: return "Old to new";
				case SortAuthor: return "Creator";
				default: return $"?{i}";
			}
		}

		public bool Active
		{
			get
			{
				return active_;
			}

			set
			{
				if (active_ != value)
				{
					active_ = value;
					parent_.CriteriaChangedInternal();
				}
			}
		}

		public string Search
		{
			get
			{
				return search_;
			}

			set
			{
				if (search_ != value)
				{
					search_ = value;
					parent_.CriteriaChangedInternal();
				}
			}
		}

		public int Sort
		{
			get
			{
				return sort_;
			}

			set
			{
				if (sort_ != value)
				{
					sort_ = value;
					parent_.CriteriaChangedInternal();
				}
			}
		}

		public HashSet<string> Tags
		{
			get { return tags_; }
		}

		public HashSet<string> Authors
		{
			get { return authors_; }
		}

		public bool TagsAnd
		{
			get
			{
				return tagsAnd_;
			}

			set
			{
				if (tagsAnd_ != value)
				{
					tagsAnd_ = value;
					parent_.CriteriaChangedInternal();
				}
			}
		}

		public void AddTag(string s)
		{
			tags_.Add(s);
			TagsChanged?.Invoke();
			parent_.CriteriaChangedInternal();
		}

		public void RemoveTag(string s)
		{
			tags_.Remove(s);
			TagsChanged?.Invoke();
			parent_.CriteriaChangedInternal();
		}

		public void ClearTags()
		{
			if (tags_.Count > 0)
			{
				tags_.Clear();
				TagsChanged?.Invoke();
				parent_.CriteriaChangedInternal();
			}
		}

		public void AddAuthor(string s)
		{
			authors_.Add(s);
			AuthorsChanged?.Invoke();
			parent_.CriteriaChangedInternal();
		}

		public void RemoveAuthor(string s)
		{
			authors_.Remove(s);
			AuthorsChanged?.Invoke();
			parent_.CriteriaChangedInternal();
		}

		public void ClearAuthors()
		{
			if (authors_.Count > 0)
			{
				authors_.Clear();
				AuthorsChanged?.Invoke();
				parent_.CriteriaChangedInternal();
			}
		}

		public DAZDynamicItem[] Filtered(DAZDynamicItem[] all)
		{
			var list = Culled(all);
			Sorted(list);
			return list.ToArray();
		}

		private List<DAZDynamicItem> Culled(DAZDynamicItem[] all)
		{
			if (!active_ && search_ == "" && tags_.Count == 0 && authors_.Count == 0)
				return new List<DAZDynamicItem>(all);

			authorsLc_.Clear();
			foreach (string a in authors_)
				authorsLc_.Add(a.ToLower());

			var list = new List<DAZDynamicItem>();
			var s = search_.ToLower().Trim();

			Regex re = null;
			if (s != "" && VUI.Utilities.IsRegex(s))
				re = VUI.Utilities.CreateRegex(s);

			for (int i = 0; i < all.Length; ++i)
			{
				var ci = all[i];

				if (active_ && !ci.active)
					continue;

				if (re == null)
				{
					if (!ci.displayName.ToLower().Contains(s))
						continue;
				}
				else
				{
					if (!re.IsMatch(ci.displayName))
						continue;
				}

				if (!TagsMatch(ci))
					continue;

				if (!AuthorsMatch(ci))
					continue;

				list.Add(ci);
			}

			return list;
		}

		public void Sorted(List<DAZDynamicItem> list)
		{
			switch (sort_)
			{
				case SortAZ:
				{
					list.Sort((a, b) => U.CompareNatural(a.displayName, b.displayName));
					break;
				}

				case SortZA:
				{
					list.Sort((a, b) => U.CompareNatural(b.displayName, a.displayName));
					break;
				}

				case SortNew:
				{
					list.Reverse();
					break;
				}

				case SortOld:
				{
					// no-op, default sort
					break;
				}

				case SortAuthor:
				{
					list.Sort((a, b) => U.CompareNatural(a.creatorName, b.creatorName));
					break;
				}
			}
		}

		private bool TagsMatch(DAZDynamicItem ci)
		{
			if (tags_.Count == 0)
				return true;

			bool matched;

			if (tagsAnd_)
			{
				matched = true;

				foreach (string t in tags_)
				{
					if (!ci.CheckMatchTag(t))
					{
						matched = false;
						break;
					}
				}
			}
			else
			{
				matched = false;

				foreach (string t in tags_)
				{
					if (ci.CheckMatchTag(t))
					{
						matched = true;
						break;
					}
				}
			}

			return matched;
		}

		private bool AuthorsMatch(DAZDynamicItem ci)
		{
			if (authors_.Count == 0)
				return true;

			return authorsLc_.Contains(ci.creatorName.ToLower());
		}
	}
}
