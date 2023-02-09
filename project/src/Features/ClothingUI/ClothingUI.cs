﻿using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

namespace AUI.ClothingUI
{
	class ClothingAtomInfo : BasicAtomUIInfo
	{
		private const int Columns = 3;
		private const int Rows = 7;

		private const float DisableCheckInterval = 1;
		private const float DisableCheckTries = 5;

		private readonly AtomClothingUIModifier uiMod_;
		private DAZCharacter char_ = null;
		private DAZCharacterSelector cs_ = null;
		private GenerateDAZClothingSelectorUI ui_ = null;

		private VUI.Root root_ = null;
		private VUI.Panel grid_ = null;
		private ClothingPanel[] panels_ = null;
		private Controls controls_ = null;

		private float disableElapsed_ = 0;
		private int disableTries_ = 0;

		private int page_ = 0;
		private bool active_ = false;
		private string search_ = "";
		private DAZClothingItem[] items_ = new DAZClothingItem[0];

		public ClothingAtomInfo(AtomClothingUIModifier uiMod, Atom a)
			: base(a, uiMod.Log.Prefix)
		{
			uiMod_ = uiMod;
		}

		public int Page
		{
			get { return page_; }
			set { SetPage(value); }
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
					Rebuild();
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
					Rebuild();
				}
			}
		}

		public int PerPage
		{
			get { return Columns * Rows; }
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

		private void Rebuild()
		{
			if (!active_ && search_ == "")
			{
				items_ = cs_.clothingItems;
			}
			else
			{
				var list = new List<DAZClothingItem>();
				var all = cs_.clothingItems;

				var s = search_.ToLower().Trim();
				Regex re = null;

				if (s != "" && U.IsRegex(s))
					re = U.CreateRegex(s);

				for (int i = 0; i < all.Length; ++i)
				{
					if (active_ && !all[i].active)
						continue;

					if (re == null)
					{
						if (!all[i].displayName.ToLower().Contains(s))
							continue;
					}
					else
					{
						if (!re.IsMatch(all[i].displayName))
							continue;
					}

					list.Add(all[i]);
				}

				items_ = list.ToArray();
			}

			controls_.Set(page_, PageCount);
			UpdatePage();
		}

		public override bool Enable()
		{
			if (!GetInfo())
				return false;

			if (root_ == null)
				CreateUI();

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
					if (disableElapsed_ >= 1)
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
			controls_ = new Controls(this);

			grid_ = new VUI.Panel();

			var gl = new VUI.GridLayout(Columns);
			gl.UniformWidth = true;
			gl.Spacing = 5;
			grid_.Layout = gl;

			root_.ContentPanel.Layout = new VUI.BorderLayout(10);
			root_.ContentPanel.Add(controls_, VUI.BorderLayout.Center);
			root_.ContentPanel.Add(grid_, VUI.BorderLayout.Bottom);

			var panels = new List<ClothingPanel>();

			for (int i = 0; i < Columns * Rows; ++i)
			{
				var p = new ClothingPanel(this);

				panels.Add(p);
				grid_.Add(p);
			}

			panels_ = panels.ToArray();
			Rebuild();
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
