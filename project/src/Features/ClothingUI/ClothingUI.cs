﻿using Battlehub.RTHandles;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

namespace AUI.ClothingUI
{
	class ClothingPanel : VUI.Panel
	{
		private const float ThumbnailSize = 150;

		private readonly ClothingAtomInfo parent_;
		private readonly VUI.CheckBox active_ = null;
		private readonly VUI.Label author_ = null;
		private readonly VUI.Label name_ = null;
		private readonly VUI.Button customize_ = null;
		private readonly VUI.Image thumbnail_ = null;

		private DAZClothingItem ci_ = null;
		private bool ignore_ = false;

		public ClothingPanel(ClothingAtomInfo parent)
		{
			parent_ = parent;

			Padding = new VUI.Insets(5);
			Layout = new VUI.BorderLayout(5);

			Clickthrough = false;
			Events.PointerClick += (e) => ToggleActive();

			var top = new VUI.Panel(new VUI.HorizontalFlow(5));
			active_ = top.Add(new VUI.CheckBox("", (b) => ToggleActive()));
			author_ = top.Add(new VUI.Label());

			var center = new VUI.Panel(new VUI.VerticalFlow(5, false));
			center.Add(top);
			name_ = center.Add(new VUI.Label());
			customize_ = center.Add(new VUI.ToolButton("...", Customize));

			var right = new VUI.Panel(new VUI.VerticalFlow(5));
			thumbnail_ = right.Add(new VUI.Image());

			Add(center, VUI.BorderLayout.Center);
			Add(right, VUI.BorderLayout.Right);

			author_.FontSize = 24;
			author_.WrapMode = VUI.Label.ClipEllipsis;
			author_.Alignment = VUI.Label.AlignLeft | VUI.Label.AlignTop;
			author_.AutoTooltip = true;

			name_.FontSize = 24;
			name_.WrapMode = VUI.Label.ClipEllipsis;
			name_.Alignment = VUI.Label.AlignLeft | VUI.Label.AlignTop;
			name_.AutoTooltip = true;

			thumbnail_.MinimumSize = new VUI.Size(ThumbnailSize, ThumbnailSize);
			thumbnail_.MaximumSize = new VUI.Size(ThumbnailSize, ThumbnailSize);

			Update();
		}

		public void Set(DAZClothingItem ci)
		{
			ci_ = ci;
			Update();
		}

		public void Clear()
		{
			ci_ = null;
			Update();
		}

		private void Update()
		{
			try
			{
				ignore_ = true;

				if (ci_ == null)
				{
					active_.Checked = false;
					author_.Text = "";
					name_.Text = "";
					thumbnail_.Texture = null;
					Borders = new VUI.Insets(0);
				}
				else
				{
					active_.Checked = ci_.active;
					author_.Text = ci_.creatorName;
					name_.Text = ci_.displayName;
					thumbnail_.Texture = null;
					Borders = new VUI.Insets(1);

					DAZClothingItem forCi = ci_;

					ci_.GetThumbnail((Texture2D t) =>
					{
						if (ci_ == forCi)
							thumbnail_.Texture = t;
					});
				}

				active_.Render = (ci_ != null);
				author_.Render = (ci_ != null);
				name_.Render = (ci_ != null);
				thumbnail_.Render = (ci_ != null);
				customize_.Render = (ci_ != null);

				ActiveChanged();
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void ToggleActive()
		{
			if (ignore_) return;

			try
			{
				ignore_ = true;
				ci_.characterSelector.SetActiveClothingItem(ci_, !ci_.active);
				active_.Checked = ci_.active;
				ActiveChanged();
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void Customize()
		{
			ci_.OpenUI();
		}

		private void ActiveChanged()
		{
			if (ci_ != null && ci_.active)
			{
				BackgroundColor = new Color(0.12f, 0.12f, 0.20f);
				BorderColor = new Color(1, 1, 1);
				customize_.Enabled = true;
			}
			else
			{
				BackgroundColor = new Color(0, 0, 0, 0);
				BorderColor = VUI.Style.Theme.BorderColor;
				customize_.Enabled = false;
			}
		}
	}


	class Controls : VUI.Panel
	{
		private readonly ClothingAtomInfo parent_;
		private readonly VUI.IntTextSlider pages_;
		private readonly VUI.Label pageCount_;
		private readonly SearchBox search_ = new SearchBox("Search");
		private readonly VUI.TreeView tags_ = new VUI.TreeView();
		private bool ignore_ = false;

		public Controls(ClothingAtomInfo parent)
		{
			parent_ = parent;

			Padding = new VUI.Insets(5);
			Borders = new VUI.Insets(1);
			Layout = new VUI.VerticalFlow(10);

			var pages = new VUI.Panel(new VUI.HorizontalFlow(
				5, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter));

			pages.Add(new VUI.Label("Pages"));
			pages_ = pages.Add(new VUI.IntTextSlider(OnPageChanged));
			pages.Add(new VUI.ToolButton("<", () => parent_.PreviousPage()));
			pages.Add(new VUI.ToolButton(">", () => parent_.NextPage()));
			pageCount_ = pages.Add(new VUI.Label("", VUI.Label.AlignLeft | VUI.Label.AlignVCenter));

			var top = new VUI.Panel(new VUI.BorderLayout(10));
			top.Add(pages, VUI.BorderLayout.Left);
			top.Add(search_.Widget, VUI.BorderLayout.Center);


			var left = new VUI.Panel(new VUI.HorizontalFlow(
				10, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter));

			left.Add(new VUI.CheckBox("Active", (b) => ToggleActive()));


			var right = new VUI.Panel(new VUI.HorizontalFlow(
				10, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter));

			right.Add(new VUI.Button("Tags", ToggleTags));

			var bottom = new VUI.Panel(new VUI.BorderLayout(10));
			bottom.Add(left, VUI.BorderLayout.Left);
			bottom.Add(right, VUI.BorderLayout.Right);


			Add(top);
			Add(bottom);

			search_.Changed += OnSearchChanged;
		}

		public void Set(int current, int count)
		{
			try
			{
				ignore_ = true;
				pages_.Set(current + 1, 1, count);
				pageCount_.Text = $"/{count}";
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void ToggleActive()
		{
			if (ignore_) return;

			try
			{
				ignore_ = true;
				parent_.Active = !parent_.Active;
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void ToggleTags()
		{
		}

		private void OnPageChanged(int i)
		{
			if (ignore_) return;
			parent_.Page = i - 1;
		}

		private void OnSearchChanged(string s)
		{
			if (ignore_) return;
			parent_.Search = s;
		}
	}


	class ClothingAtomInfo : BasicAtomUIInfo
	{
		private const int Columns = 3;
		private const int Rows = 7;

		private readonly AtomClothingUIModifier uiMod_;
		private DAZCharacter char_ = null;
		private DAZCharacterSelector cs_ = null;
		private GenerateDAZClothingSelectorUI ui_ = null;

		private VUI.Root root_ = null;
		private VUI.Panel grid_ = null;
		private ClothingPanel[] panels_ = null;
		private Controls controls_ = null;

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
				root_.Update();
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
