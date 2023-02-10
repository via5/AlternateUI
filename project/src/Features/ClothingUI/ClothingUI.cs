using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using System.Collections;
using MVR.FileManagementSecure;
using SimpleJSON;

namespace AUI.ClothingUI
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

		private readonly ClothingAtomInfo parent_;

		private bool active_ = false;
		private string search_ = "";
		private int sort_ = SortNew;

		private readonly HashSet<string> tags_ = new HashSet<string>();
		private bool tagsAnd_ = true;

		private readonly HashSet<string> authors_ = new HashSet<string>();
		private readonly HashSet<string> authorsLc_ = new HashSet<string>();


		public Filter(ClothingAtomInfo parent)
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

		public DAZClothingItem[] Filtered(DAZClothingItem[] all)
		{
			var list = Culled(all);
			Sorted(list);
			return list.ToArray();
		}

		private List<DAZClothingItem> Culled(DAZClothingItem[] all)
		{
			if (!active_ && search_ == "" && tags_.Count == 0 && authors_.Count == 0)
				return new List<DAZClothingItem>(all);

			authorsLc_.Clear();
			foreach (string a in authors_)
				authorsLc_.Add(a.ToLower());

			var list = new List<DAZClothingItem>();
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

		public void Sorted(List<DAZClothingItem> list)
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

		private bool TagsMatch(DAZClothingItem ci)
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

		private bool AuthorsMatch(DAZClothingItem ci)
		{
			if (authors_.Count == 0)
				return true;

			return authorsLc_.Contains(ci.creatorName.ToLower());
		}
	}


	class ClothingAtomInfo : BasicAtomUIInfo
	{
		public const int MinColumns = 1;
		public const int MaxColumns = 3;
		public const int DefaultColumns = 3;

		public const int MinRows = 2;
		public const int MaxRows = 7;
		public const int DefaultRows = 7;

		private const float DisableCheckInterval = 1;
		private const float DisableCheckTries = 5;

		private readonly AtomClothingUIModifier uiMod_;
		private DAZCharacter char_ = null;
		private DAZCharacterSelector cs_ = null;
		private GenerateDAZClothingSelectorUI ui_ = null;

		private VUI.Root root_ = null;
		private VUI.Panel grid_ = null;
		private int cols_ = DefaultColumns;
		private int rows_ = DefaultRows;
		private ClothingPanel[] panels_ = null;
		private Controls controls_ = null;

		private float disableElapsed_ = 0;
		private int disableTries_ = 0;

		private int page_ = 0;
		private readonly Filter filter_;
		private DAZClothingItem[] items_ = new DAZClothingItem[0];

		public ClothingAtomInfo(AtomClothingUIModifier uiMod, Atom a)
			: base(a, uiMod.Log.Prefix)
		{
			uiMod_ = uiMod;
			filter_ = new Filter(this);
			LoadOptions();
		}

		public string GetAutoCompleteFile()
		{
			string g;

			if (char_.isMale)
				g = "male";
			else
				g = "female";

			return AlternateUI.Instance.GetConfigFilePath(
				$"aui.clothing.{g}.autocomplete.json");
		}

		public string GetOptionsFile()
		{
			return AlternateUI.Instance.GetConfigFilePath(
				$"aui.clothing.json");
		}

		private void LoadOptions()
		{
			var f = GetOptionsFile();

			if (FileManagerSecure.FileExists(f))
			{
				var j = SuperController.singleton.LoadJSON(f)?.AsObject;
				if (j == null)
					return;

				if (j.HasKey("columns"))
					cols_ = U.Clamp(j["columns"].AsInt, MinColumns, MaxColumns);

				if (j.HasKey("rows"))
					rows_ = U.Clamp(j["rows"].AsInt, MinRows, MaxRows);
			}
		}

		private void SaveOptions()
		{
			var j = new JSONClass();

			j["columns"] = new JSONData(cols_);
			j["rows"] = new JSONData(rows_);

			SuperController.singleton.SaveJSON(j, GetOptionsFile());
		}

		public void NotifyAutoCompleteChanged()
		{
			foreach (ClothingAtomInfo a in uiMod_.Atoms)
			{
				if (a == this)
					continue;

				if (a.char_.isMale == char_.isMale)
					a.controls_.UpdateAutoComplete();
			}
		}

		public void NotifyGridChanged()
		{
			foreach (ClothingAtomInfo a in uiMod_.Atoms)
			{
				if (a == this)
					continue;

				a.LoadOptions();
				a.GridChanged(false);
			}
		}

		public DAZCharacterSelector CharacterSelector
		{
			get { return cs_; }
		}

		public GenerateDAZClothingSelectorUI SelectorUI
		{
			get { return ui_; }
		}

		public int Page
		{
			get { return page_; }
			set { SetPage(value); }
		}

		public Filter Filter
		{
			get { return filter_; }
		}

		public int Columns
		{
			get { return cols_; }
			set { cols_ = value; GridChanged(); }
		}

		public int Rows
		{
			get { return rows_; }
			set { rows_ = value; GridChanged(); }
		}

		public int PerPage
		{
			get { return cols_ * rows_; }
		}

		public int PageCount
		{
			get
			{
				if (items_.Length <= PerPage)
					return 1;
				else
					return items_.Length / PerPage + 1;
			}
		}

		public void NextPage()
		{
			if (page_ < (PageCount - 1))
				SetPage(page_ + 1);
		}

		public void PreviousPage()
		{
			if (page_ > 0)
				SetPage(page_ - 1);
		}

		public void UpdatePanels()
		{
			for (int i = 0; i < panels_.Length; ++i)
				panels_[i].Update(false);
		}

		private void SetPage(int newPage)
		{
			if (newPage < 0 || newPage >= PageCount)
				return;

			page_ = newPage;
			UpdatePage();
		}

		private void UpdatePage()
		{
			if (page_ < 0)
				page_ = 0;
			else if (page_ >= PageCount)
				page_ = PageCount - 1;

			int first = PerPage * page_;

			for (int i = 0; i < panels_.Length; ++i)
			{
				int ci = first + i;
				if (ci >= items_.Length)
					panels_[i].Clear();
				else
					panels_[i].Set(items_[ci]);
			}

			controls_.Set(page_, PageCount);
		}

		public void CriteriaChangedInternal()
		{
			Rebuild();
		}

		private void Rebuild()
		{
			items_ = filter_.Filtered(cs_.clothingItems);

			page_ = 0;
			controls_.Set(page_, PageCount);
			UpdatePage();
		}

		public override bool Enable()
		{
			if (!GetInfo())
				return false;

			if (root_ == null)
			{
				try
				{
					CreateUI();
				}
				catch (Exception)
				{
					if (root_ != null)
						root_.Visible = false;

					throw;
				}
			}


			if (root_ != null)
				root_.Visible = true;

			return true;
		}

		public override void Disable()
		{
			if (root_ != null)
				root_.Visible = false;
		}

		public override void Update(float s)
		{
			if (root_ != null)
			{
				root_.Update();

				// vam will sometimes enable the clothing ui after the root has
				// been created, make sure everything is off for the first few
				// seconds
				if (disableTries_ < DisableCheckTries)
				{
					disableElapsed_ += s;
					if (disableElapsed_ >= DisableCheckInterval)
					{
						disableElapsed_ = 0;
						++disableTries_;

						foreach (Transform t in ui_.transform.parent)
						{
							if (t != root_.RootSupport.RootParent)
								t.gameObject.SetActive(false);
						}
					}
				}
			}
		}

		private bool GetInfo()
		{
			if (char_ == null)
			{
				char_ = Atom.GetComponentInChildren<DAZCharacter>();
				if (char_ == null)
				{
					Log.Error("no DAZCharacter");
					return false;
				}
			}

			if (cs_ == null)
			{
				cs_ = Atom.GetComponentInChildren<DAZCharacterSelector>();
				if (cs_ == null)
				{
					Log.Error("no DAZCharacterSelector");
					return false;
				}
			}

			if (ui_ == null)
			{
				ui_ = cs_.clothingSelectorUI;
				if (ui_ == null)
				{
					Log.Error("atom has no clothingSelectorUI");
					return false;
				}
			}

			return true;
		}

		public override bool IsLike(BasicAtomUIInfo other)
		{
			return true;
		}

		private void CreateUI()
		{
			root_ = new VUI.Root(new VUI.TransformUIRootSupport(ui_.transform.parent));
			root_.ContentPanel.Layout = new VUI.BorderLayout(10);

			controls_ = new Controls(this);
			grid_ = new VUI.Panel();

			root_.ContentPanel.Add(controls_, VUI.BorderLayout.Top);
			root_.ContentPanel.Add(grid_, VUI.BorderLayout.Center);

			GridChanged(false);
		}

		private void GridChanged(bool notify=true)
		{
			grid_.RemoveAllChildren();

			var gl = new VUI.GridLayout(cols_);
			gl.UniformWidth = true;
			gl.Spacing = 5;

			grid_.Layout = gl;

			var panels = new List<ClothingPanel>();

			for (int i = 0; i < PerPage; ++i)
			{
				var p = new ClothingPanel(this);

				panels.Add(p);
				grid_.Add(p);
			}

			panels_ = panels.ToArray();
			Rebuild();

			SaveOptions();

			if (notify)
				NotifyGridChanged();
		}
	}


	class AtomClothingUIModifier : AtomUIModifier
	{
		private readonly ClothingUI parent_;

		public AtomClothingUIModifier(ClothingUI parent)
			: base("aui.clothing")
		{
			parent_ = parent;
		}

		protected override BasicAtomUIInfo CreateAtomInfo(Atom a)
		{
			return new ClothingAtomInfo(this, a);
		}

		protected override bool ValidAtom(Atom a)
		{
			return (a.category == "People");
		}
	}


	class ClothingUI : BasicFeature
	{
		private readonly AtomClothingUIModifier uiMod_;

		public ClothingUI()
			: base("clothing", "Clothing UI", true)
		{
			uiMod_ = new AtomClothingUIModifier(this);
		}

		public override string Description
		{
			get
			{
				return
					"Complete overhaul of the clothing panel.";
			}
		}

		public static void OpenClothingUI(DAZClothingItem ci, string tab = null)
		{
			if (ci == null)
				return;

			ci.OpenUI();

			if (!string.IsNullOrEmpty(tab))
				SetTab(ci, tab);
		}

		private static void SetTab(DAZClothingItem ci, string name)
		{
			DoSetTab(ci, name);
			AlternateUI.Instance.StartCoroutine(CoSetTab(ci, name));
		}

		private static IEnumerator CoSetTab(DAZClothingItem ci, string name)
		{
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			DoSetTab(ci, name);
		}

		private static void DoSetTab(DAZClothingItem ci, string name)
		{
			var ts = ci.customizationUI.GetComponentInChildren<UITabSelector>();
			if (ts == null)
				return;

			if (ts.HasTab(name))
				ts.SetActiveTab(name);
		}

		protected override void DoEnable()
		{
			uiMod_.Enable();
		}

		protected override void DoDisable()
		{
			uiMod_.Disable();
		}

		protected override void DoUpdate(float s)
		{
			base.DoUpdate(s);
			uiMod_.Update(s);
		}
	}
}
