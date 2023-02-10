using System;
using System.Collections.Generic;
using System.Linq;

namespace AUI.ClothingUI
{
	abstract class TreeFilter
	{
		public delegate void Handler();
		public event Handler Cleared;

		public delegate void ItemHandler(VUI.TreeView.Item item);
		public event ItemHandler ItemAdded, ItemRemoved;

		public delegate void AndHandler(bool b);
		public event AndHandler AndChanged;

		private readonly ToggledPanel panel_ = new ToggledPanel("");
		private readonly VUI.TreeView tree_ = new VUI.TreeView();
		private readonly SearchBox search_;
		private readonly VUI.ComboBox<string> and_ = null;
		private bool ignore_ = false;

		private bool firstShow_ = true;
		private bool one_ = true;

		public TreeFilter(string searchPlaceholder, bool supportsAnd)
		{
			panel_.Toggled += (b) =>
			{
				if (b && firstShow_)
				{
					firstShow_ = false;
					Rebuild();
				}
			};

			tree_.CheckBoxes = true;
			tree_.ItemClicked += OnTagClicked;

			search_ = new SearchBox(searchPlaceholder);
			search_.Changed += OnTagsSearch;

			var top = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.FlowLayout.AlignLeftVCenter));
			top.Padding = new VUI.Insets(5);
			top.Add(new VUI.CheckBox("Only one", OnOneChanged, one_));

			if (supportsAnd)
				and_ = top.Add(new VUI.ComboBox<string>(new string[] { "And", "Or" }, OnAndChanged));

			var bottom = new VUI.Panel(new VUI.VerticalFlow(10));
			bottom.Add(search_.Widget);

			panel_.RightClick += ClearTags;
			panel_.Panel.Add(top, VUI.BorderLayout.Top);
			panel_.Panel.Add(tree_, VUI.BorderLayout.Center);
			panel_.Panel.Add(bottom, VUI.BorderLayout.Bottom);

			UpdateAnd();
		}

		protected abstract void Rebuild();

		public VUI.Button Button
		{
			get { return panel_.Button; }
		}

		public VUI.TreeView Tree
		{
			get { return tree_; }
		}

		public VUI.TreeView.Item RootItem
		{
			get { return tree_.RootItem; }
		}

		public void SetButtonText(string s)
		{
			panel_.Button.Text = s;
		}

		public bool OnlyOne
		{
			get
			{
				return one_;
			}

			set
			{
				if (one_ != value)
				{
					// uncheck first or the checked handler will close the panel
					// because one_ is true
					if (value)
						UncheckAllExceptFirst();

					one_ = value;
				}
			}
		}

		public void ClearTags()
		{
			try
			{
				ignore_ = true;
				UncheckAll(tree_.RootItem);
				Cleared?.Invoke();
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void UpdateAnd()
		{
			if (and_ != null)
				and_.Enabled = !one_;
		}

		private void UncheckAllExceptFirst()
		{
			bool found = false;
			UncheckAllExceptFirst(tree_.RootItem, ref found);
		}

		private void UncheckAll(VUI.TreeView.Item item)
		{
			item.Checked = false;

			if (item.Children != null)
			{
				for (int i = 0; i < item.Children.Count; ++i)
					UncheckAll(item.Children[i]);
			}
		}

		private void UncheckAllExceptFirst(VUI.TreeView.Item item, ref bool found)
		{
			if (!found)
			{
				if (item.Checkable && item.Checked)
					found = true;
			}
			else
			{
				item.Checked = false;
			}

			if (item.Children != null)
			{
				for (int i = 0; i < item.Children.Count; ++i)
					UncheckAllExceptFirst(item.Children[i], ref found);
			}
		}

		private void UncheckAllExcept(VUI.TreeView.Item item)
		{
			UncheckAllExcept(item, tree_.RootItem);
		}

		private void UncheckAllExcept(VUI.TreeView.Item except, VUI.TreeView.Item item)
		{
			if (item != except && item.Checked)
				item.Checked = false;

			if (item.Children != null)
			{
				for (int i = 0; i < item.Children.Count; ++i)
					UncheckAllExcept(except, item.Children[i]);
			}
		}

		protected void OnCheckedChanged(VUI.TreeView.Item i, bool b)
		{
			if (ignore_) return;

			if (b)
			{
				if (one_)
					UncheckAllExcept(i);

				ItemAdded?.Invoke(i);
			}
			else
			{
				ItemRemoved?.Invoke(i);
			}

			if (one_)
				panel_.Hide();
		}

		private void OnTagClicked(VUI.TreeView.Item i)
		{
			if (ignore_) return;

			if (i.HasChildren)
				i.Toggle();
			else if (i.Checkable)
				i.Checked = !i.Checked;
		}

		private void OnTagsSearch(string s)
		{
			tree_.Filter = s;
		}

		private void OnOneChanged(bool b)
		{
			OnlyOne = b;
			UpdateAnd();
		}

		private void OnAndChanged(int i)
		{
			AndChanged?.Invoke(i == 0);
		}
	}


	class TagsControls : TreeFilter
	{
		private readonly Controls controls_;

		public TagsControls(Controls c)
			: base("Search tags", true)
		{
			controls_ = c;
			controls_.ClothingAtomInfo.Filter.TagsChanged += UpdateButton;

			ItemAdded += (i) => controls_.ClothingAtomInfo.Filter.AddTag(i.Text);
			ItemRemoved += (i) => controls_.ClothingAtomInfo.Filter.RemoveTag(i.Text);
			Cleared += () => controls_.ClothingAtomInfo.Filter.ClearTags();
			AndChanged += (b) => controls_.ClothingAtomInfo.Filter.TagsAnd = b;

			UpdateButton();
		}

		protected override void Rebuild()
		{
			var root = RootItem;

			root.Clear();

			var sui = controls_.ClothingAtomInfo.SelectorUI;
			root.Add(MakeTagItem("Regions", sui.regionTags));
			root.Add(MakeTagItem("Types", sui.typeTags));
			root.Add(MakeTagItem("Others", sui.GetOtherTags()));
		}

		private VUI.TreeView.Item MakeTagItem(string name, IEnumerable<string> e)
		{
			var list = new List<string>(e);
			U.NatSort(list);

			var item = new VUI.TreeView.Item(name);
			for (int i = 0; i < list.Count; ++i)
			{
				if (list[i].Trim() == "")
					continue;

				var c = new VUI.TreeView.Item(list[i]);

				c.Checkable = true;
				c.CheckedChanged += (b) => OnCheckedChanged(c, b);

				item.Add(c);
			}

			return item;
		}

		private void UpdateButton()
		{
			var tags = controls_.ClothingAtomInfo.Filter.Tags;

			if (tags.Count == 0)
				SetButtonText("Tags");
			else if (tags.Count == 1)
				SetButtonText(tags.First());
			else
				SetButtonText($"{tags.First()} +{tags.Count - 1}");
		}
	}


	class AuthorControls : TreeFilter
	{
		private readonly Controls controls_;

		public AuthorControls(Controls c)
			: base("Search creators", false)
		{
			controls_ = c;
			controls_.ClothingAtomInfo.Filter.AuthorsChanged += UpdateButton;

			ItemAdded += (i) => controls_.ClothingAtomInfo.Filter.AddAuthor(i.Text);
			ItemRemoved += (i) => controls_.ClothingAtomInfo.Filter.RemoveAuthor(i.Text);
			Cleared += () => controls_.ClothingAtomInfo.Filter.ClearAuthors();

			Tree.RootToggles = false;

			UpdateButton();
		}

		protected override void Rebuild()
		{
			var root = RootItem;

			root.Clear();

			var sui = controls_.ClothingAtomInfo.SelectorUI;

			var authors = new HashSet<string>();
			var items = controls_.ClothingAtomInfo.CharacterSelector.clothingItems;

			for (int i = 0; i < items.Length; ++i)
			{
				var s = items[i].creatorName.Trim();
				if (s == "")
					continue;

				authors.Add(s);
			}

			var list = new List<string>(authors);
			U.NatSort(list);

			for (int i = 0; i < list.Count; ++i)
			{
				var item = new VUI.TreeView.Item(list[i]);

				item.Checkable = true;
				item.CheckedChanged += (b) => OnCheckedChanged(item, b);

				root.Add(item);
			}
		}

		private void UpdateButton()
		{
			var authors = controls_.ClothingAtomInfo.Filter.Authors;

			if (authors.Count == 0)
				SetButtonText("Creators");
			else if (authors.Count == 1)
				SetButtonText(authors.First());
			else
				SetButtonText($"{authors.First()} +{authors.Count - 1}");
		}
	}


	class CurrentControls
	{
		class ItemPanel : VUI.Panel
		{
			private const int ThumbnailSize = 50;

			private readonly Controls c_;
			private readonly VUI.Image thumbnail_;
			private readonly VUI.CheckBox active_;
			private readonly VUI.Label name_;
			private readonly VUI.Button customize_;
			private readonly VUI.Button adjustments_;
			private readonly VUI.Button physics_;
			private readonly VUI.CheckBox visible_;
			private bool ignore_ = false;

			private DAZClothingItem ci_ = null;

			public ItemPanel(Controls c)
			{
				c_ = c;

				var left = new VUI.Panel(new VUI.HorizontalFlow(
					10, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter));

				active_ = left.Add(new VUI.CheckBox("", OnActive, false, "Active"));
				thumbnail_ = left.Add(new VUI.Image());

				var right = new VUI.Panel(new VUI.HorizontalFlow(
					10, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter));
				customize_ = right.Add(new VUI.ToolButton(
					"...", OpenCustomize, "Customize"));
				adjustments_ = right.Add(new VUI.ToolButton(
					"A", OpenAdjustments, "Adjustments"));
				physics_ = right.Add(new VUI.ToolButton(
					"P", OpenPhysics, "Physics"));
				visible_ = right.Add(new VUI.CheckBox(
					"V", OnVisible, false, "Hides all materials"));

				var center = new VUI.Panel(new VUI.BorderLayout());
				name_ = center.Add(new VUI.Label(), VUI.BorderLayout.Center);
				name_.MaximumSize = new VUI.Size(300, DontCare);
				name_.WrapMode = VUI.Label.ClipEllipsis;
				name_.AutoTooltip = true;

				Layout = new VUI.BorderLayout(10);
				Padding = new VUI.Insets(10, 5, 10, 5);

				Add(left, VUI.BorderLayout.Left);
				Add(center, VUI.BorderLayout.Center);
				Add(right, VUI.BorderLayout.Right);

				thumbnail_.MinimumSize = new VUI.Size(ThumbnailSize, ThumbnailSize);
				thumbnail_.MaximumSize = new VUI.Size(ThumbnailSize, ThumbnailSize);
			}

			public void Set(DAZClothingItem ci)
			{
				ci_ = ci;
				Update();
			}

			public void Update()
			{
				try
				{
					ignore_ = true;

					if (ci_ == null)
					{
						Visible = false;
					}
					else
					{
						Visible = true;

						active_.Checked = ci_.active;
						name_.Text = ci_.displayName;
						customize_.Enabled = true;
						adjustments_.Enabled = true;
						physics_.Enabled = HasSim();
						visible_.Checked = IsClothingVisible();

						var forCi = ci_;

						ci_.GetThumbnail((t) =>
						{
							if (ci_ == forCi)
								thumbnail_.Texture = t;
						});
					}
				}
				finally
				{
					ignore_ = false;
				}
			}

			private bool IsClothingVisible()
			{
				if (ci_ == null)
					return false;

				foreach (var mo in ci_.GetComponentsInChildren<MaterialOptions>())
				{
					var j = mo.GetBoolJSONParam("hideMaterial");
					if (j != null)
					{
						if (!j.val)
							return true;
					}
				}

				return false;
			}

			private bool HasSim()
			{
				var sim = ci_?.GetComponentInChildren<ClothSimControl>();
				return (sim != null);
			}

			private void OnActive(bool b)
			{
				if (ignore_) return;

				if (ci_ != null)
				{
					ci_.characterSelector.SetActiveClothingItem(ci_, b);
					c_.ClothingAtomInfo.UpdatePanels();
				}
			}

			private void OnVisible(bool b)
			{
				if (ignore_) return;

				if (ci_ != null)
				{
					foreach (var mo in ci_.GetComponentsInChildren<MaterialOptions>())
					{
						var j = mo.GetBoolJSONParam("hideMaterial");
						if (j != null)
							j.val = !b;
					}
				}
			}

			public void OpenCustomize()
			{
				ClothingUI.OpenClothingUI(ci_);
			}

			public void OpenPhysics()
			{
				ClothingUI.OpenClothingUI(ci_, "Physics");
			}

			public void OpenAdjustments()
			{
				ClothingUI.OpenClothingUI(ci_, "Adjustments");
			}
		}


		private readonly Controls controls_;
		private readonly ToggledPanel panel_;
		private readonly VUI.Panel itemsPanel_;
		private readonly List<ItemPanel> items_ = new List<ItemPanel>();

		public CurrentControls(Controls c)
		{
			controls_ = c;
			panel_ = new ToggledPanel("Current", false, true);
			itemsPanel_ = new VUI.Panel(new VUI.VerticalFlow(0));

			var p = new VUI.Panel(new VUI.VerticalFlow(0));

			AddButton(p, "Remove all", OnRemoveAll);
			AddButton(p, "Undress all", () => OnDressAll(false));
			AddButton(p, "Re-dress all", () => OnDressAll(true));
			p.Add(itemsPanel_);

			panel_.Panel.Add(p, VUI.BorderLayout.Top);
			panel_.Toggled += OnToggled;
		}

		private void OnToggled(bool b)
		{
			if (b)
			{
				var cs = controls_.ClothingAtomInfo.CharacterSelector;
				var items = cs.clothingItems;

				var list = new List<DAZClothingItem>();

				for (int i = 0; i < items.Length; ++i)
				{
					if (items[i].active)
						list.Add(items[i]);
				}

				controls_.ClothingAtomInfo.Filter.Sorted(list);

				int ii = 0;
				for (int i=0; i<list.Count; ++i)
				{
					while (ii >= items_.Count)
					{
						var p = new ItemPanel(controls_);
						items_.Add(p);
						itemsPanel_.Add(p);
					}

					items_[ii].Set(list[i]);
					++ii;
				}

				for (int i = ii; i < items_.Count; ++i)
					items_[i].Set(null);
			}
		}

		private void UpdateItems()
		{
			for (int i = 0; i < items_.Count; ++i)
				items_[i].Update();
		}

		public VUI.Widget Widget
		{
			get { return panel_.Button; }
		}

		private void AddButton(VUI.Panel p, string t, Action f)
		{
			var b = new VUI.ToolButton(t);

			b.BackgroundColor = new UnityEngine.Color(0, 0, 0, 0);
			b.Alignment = VUI.Label.AlignLeft | VUI.Label.AlignVCenter;
			b.Padding = new VUI.Insets(10, 5, 60, 5);

			b.Clicked += () =>
			{
				f();
			};

			p.Add(b);
		}

		private void OnRemoveAll()
		{
			var cs = controls_.ClothingAtomInfo.CharacterSelector;
			var items = cs.clothingItems;

			for (int i = 0; i < items.Length; ++i)
			{
				if (items[i].active)
					cs.SetActiveClothingItem(items[i], false);
			}

			UpdateItems();
			controls_.ClothingAtomInfo.UpdatePanels();
		}

		private void OnDressAll(bool b)
		{
			var cs = controls_.ClothingAtomInfo.CharacterSelector;
			var items = cs.clothingItems;

			for (int i = 0; i < items.Length; ++i)
			{
				if (items[i].active)
				{
					var csc = items[i].GetComponentsInChildren<ClothSimControl>();

					for (int j = 0; j < csc.Length; ++j)
					{
						var allowDetach = csc[j].GetBoolJSONParam("allowDetach");
						if (allowDetach != null)
							allowDetach.val = !b;

						if (b)
						{
							var r = csc[j].GetAction("Reset");
							r?.actionCallback?.Invoke();
						}
					}
				}
			}
		}
	}


	class Sorter
	{
		private readonly Controls c_;
		private readonly VUI.ComboBox<string> cb_;

		public Sorter(Controls c)
		{
			c_ = c;
			cb_ = new VUI.ComboBox<string>();

			for (int i = 0; i < Filter.SortCount; ++i)
				cb_.AddItem(Filter.SortToString(i));

			cb_.Select(c_.ClothingAtomInfo.Filter.Sort);
			cb_.SelectionIndexChanged += (i) => c_.ClothingAtomInfo.Filter.Sort = i;
		}

		public VUI.Widget Widget
		{
			get { return cb_; }
		}
	}


	class Controls : VUI.Panel
	{
		private readonly ClothingAtomInfo parent_;
		private readonly VUI.IntTextSlider pages_;
		private readonly VUI.Label pageCount_;
		private readonly SearchBox search_ = new SearchBox("Search");
		private readonly CurrentControls cc_;
		private readonly Sorter sorter_;
		private readonly TagsControls tags_;
		private readonly AuthorControls authors_;
		private bool ignore_ = false;

		public Controls(ClothingAtomInfo parent)
		{
			parent_ = parent;
			cc_ = new CurrentControls(this);
			sorter_ = new Sorter(this);
			tags_ = new TagsControls(this);
			authors_ = new AuthorControls(this);

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
			pageCount_.MinimumSize = new VUI.Size(80, DontCare);

			var top = new VUI.Panel(new VUI.BorderLayout(10));
			top.Add(pages, VUI.BorderLayout.Left);
			top.Add(search_.Widget, VUI.BorderLayout.Center);


			var left = new VUI.Panel(new VUI.HorizontalFlow(
				10, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter));

			left.Add(new VUI.CheckBox("Active", (b) => ToggleActive()));


			var right = new VUI.Panel(new VUI.HorizontalFlow(
				10, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter));

			right.Add(sorter_.Widget);
			right.Add(cc_.Widget);
			right.Add(authors_.Button);
			right.Add(tags_.Button);

			var bottom = new VUI.Panel(new VUI.BorderLayout(10));
			bottom.Add(left, VUI.BorderLayout.Left);
			bottom.Add(right, VUI.BorderLayout.Right);


			Add(top);
			Add(bottom);

			search_.Changed += OnSearchChanged;

			search_.TextBox.AutoComplete.Enabled = true;
			search_.TextBox.AutoComplete.File = parent_.GetAutoCompleteFile();
			search_.TextBox.AutoComplete.Changed += OnAutoCompleteChanged;
		}

		private void OnAutoCompleteChanged()
		{
			parent_.NotifyAutoCompleteChanged();
		}

		public void UpdateAutoComplete()
		{
			search_?.TextBox?.AutoComplete?.Reload();
		}

		public ClothingAtomInfo ClothingAtomInfo
		{
			get { return parent_; }
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
				parent_.Filter.Active = !parent_.Filter.Active;
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void OnPageChanged(int i)
		{
			if (ignore_) return;
			parent_.Page = i - 1;
		}

		private void OnSearchChanged(string s)
		{
			if (ignore_) return;
			parent_.Filter.Search = s;
		}
	}
}
