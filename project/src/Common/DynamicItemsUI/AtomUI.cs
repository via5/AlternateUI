using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AUI.DynamicItemsUI
{
	abstract class AtomUI : BasicAtomUIInfo
	{
		public const int MinColumns = 1;
		public const int MaxColumns = 3;
		public const int DefaultColumns = 3;

		public const int MinRows = 2;
		public const int MaxRows = 7;
		public const int DefaultRows = 7;

		private const float DisableCheckInterval = 1;
		private const float DisableCheckTries = 5;

		private readonly string name_;
		private readonly AtomUIModifier uiMod_;
		private DAZCharacter char_ = null;
		private DAZCharacterSelector cs_ = null;
		private GenerateDAZDynamicSelectorUI ui_ = null;

		private VUI.Root root_ = null;
		private VUI.Panel grid_ = null;
		private int cols_ = DefaultColumns;
		private int rows_ = DefaultRows;
		private ItemPanel[] panels_ = null;
		private Controls controls_ = null;

		private float disableElapsed_ = 0;
		private int disableTries_ = 0;

		private int page_ = 0;
		private readonly DynamicItemsUI.Filter filter_;
		private DAZDynamicItem[] items_ = new DAZDynamicItem[0];


		public AtomUI(string name, AtomUIModifier uiMod, Atom a)
			: base(a, uiMod.Log.Prefix)
		{
			name_ = name;
			uiMod_ = uiMod;
			filter_ = new Filter(this);
			LoadOptions();
		}


		public static void OpenUI(DAZDynamicItem ci, string tab = null)
		{
			if (ci == null)
				return;

			ci.OpenUI();

			if (!string.IsNullOrEmpty(tab))
				SetTab(ci, tab);
		}

		private static void SetTab(DAZDynamicItem ci, string name)
		{
			DoSetTab(ci, name);
			AlternateUI.Instance.StartCoroutine(CoSetTab(ci, name));
		}

		private static IEnumerator CoSetTab(DAZDynamicItem ci, string name)
		{
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			DoSetTab(ci, name);
		}

		private static void DoSetTab(DAZDynamicItem ci, string name)
		{
			var ts = ci.customizationUI.GetComponentInChildren<UITabSelector>();
			if (ts == null)
				return;

			if (ts.HasTab(name))
				ts.SetActiveTab(name);
		}


		public override bool IsLike(BasicAtomUIInfo other)
		{
			return true;
		}

		public string GetAutoCompleteFile()
		{
			string g;

			if (char_.isMale)
				g = "male";
			else
				g = "female";

			return AlternateUI.Instance.GetConfigFilePath(
				$"aui.{name_}.{g}.autocomplete.json");
		}

		public string GetOptionsFile()
		{
			return AlternateUI.Instance.GetConfigFilePath(
				$"aui.{name_}.json");
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
			foreach (AtomUI a in uiMod_.Atoms)
			{
				if (a == this)
					continue;

				if (a.char_.isMale == char_.isMale)
					a.controls_.UpdateAutoComplete();
			}
		}

		public void NotifyGridChanged()
		{
			foreach (AtomUI a in uiMod_.Atoms)
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

		public GenerateDAZDynamicSelectorUI SelectorUI
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
				else if ((items_.Length % PerPage) == 0)
					return items_.Length / PerPage;
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
			items_ = filter_.Filtered(GetItems());

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

		public void SetActive(DAZDynamicItem item, bool b)
		{
			DoSetActive(item, b);
		}

		public CurrentControls CreateCurrentControls(Controls c)
		{
			return DoCreateCurrentControls(c);
		}

		public CurrentControls.ItemPanel CreateCurrentItemPanel(Controls c)
		{
			return DoCreateCurrentItemPanel(c);
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

		private void GridChanged(bool notify = true)
		{
			grid_.RemoveAllChildren();

			var gl = new VUI.GridLayout(cols_);
			gl.UniformWidth = true;
			gl.Spacing = 5;

			grid_.Layout = gl;

			var panels = new List<ItemPanel>();

			for (int i = 0; i < PerPage; ++i)
			{
				var p = DoCreateItemPanel();

				panels.Add(p);
				grid_.Add(p);
			}

			panels_ = panels.ToArray();
			Rebuild();

			SaveOptions();

			if (notify)
				NotifyGridChanged();
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
				ui_ = DoGetSelectorUI();
				if (ui_ == null)
				{
					Log.Error("atom has no selector ui");
					return false;
				}
			}

			return true;
		}

		public DAZDynamicItem[] GetItems()
		{
			return DoGetItems();
		}


		protected abstract GenerateDAZDynamicSelectorUI DoGetSelectorUI();
		protected abstract ItemPanel DoCreateItemPanel();
		protected abstract CurrentControls DoCreateCurrentControls(Controls c);
		protected abstract CurrentControls.ItemPanel DoCreateCurrentItemPanel(Controls c);
		protected abstract void DoSetActive(DAZDynamicItem item, bool b);
		protected abstract DAZDynamicItem[] DoGetItems();
	}
}
