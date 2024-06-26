﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AUI.DynamicItemsUI
{
	abstract class TreeFilter
	{
		public delegate void Handler();
		public event Handler Cleared;

		public delegate void ItemHandler(VUI.TreeView.Item item);
		public event ItemHandler ItemAdded, ItemRemoved;

		public delegate void AndHandler(bool b);
		public event AndHandler AndChanged;

		private readonly VUI.ToggledPanel panel_ = new VUI.ToggledPanel("");
		private readonly VUI.TreeView tree_ = new VUI.TreeView();
		private readonly VUI.SearchBox search_;
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

			search_ = new VUI.SearchBox(searchPlaceholder);
			search_.Changed += OnTagsSearch;

			var top = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.Align.VCenterLeft));
			top.Padding = new VUI.Insets(5);
			top.Add(new VUI.CheckBox("Only one", OnOneChanged, one_));

			if (supportsAnd)
				and_ = top.Add(new VUI.ComboBox<string>(new string[] { "And", "Or" }, OnAndChanged));

			var bottom = new VUI.Panel(new VUI.VerticalFlow(10));
			bottom.Add(search_);

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
			controls_.AtomUI.Filter.TagsChanged += UpdateButton;

			ItemAdded += (i) => controls_.AtomUI.Filter.AddTag(i.Text);
			ItemRemoved += (i) => controls_.AtomUI.Filter.RemoveTag(i.Text);
			Cleared += () => controls_.AtomUI.Filter.ClearTags();
			AndChanged += (b) => controls_.AtomUI.Filter.TagsAnd = b;

			UpdateButton();
		}

		protected override void Rebuild()
		{
			var root = RootItem;

			root.Clear();

			var sui = controls_.AtomUI.SelectorUI;
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
			var tags = controls_.AtomUI.Filter.Tags;

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
			controls_.AtomUI.Filter.AuthorsChanged += UpdateButton;

			ItemAdded += (i) => controls_.AtomUI.Filter.AddAuthor(i.Text);
			ItemRemoved += (i) => controls_.AtomUI.Filter.RemoveAuthor(i.Text);
			Cleared += () => controls_.AtomUI.Filter.ClearAuthors();

			Tree.RootToggles = false;

			UpdateButton();
		}

		protected override void Rebuild()
		{
			var root = RootItem;

			root.Clear();

			var sui = controls_.AtomUI.SelectorUI;

			var authors = new HashSet<string>();
			var items = controls_.AtomUI.GetItems();

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
			var authors = controls_.AtomUI.Filter.Authors;

			if (authors.Count == 0)
				SetButtonText("Creators");
			else if (authors.Count == 1)
				SetButtonText(authors.First());
			else
				SetButtonText($"{authors.First()} +{authors.Count - 1}");
		}
	}


	class OptionsControls
	{
		private readonly Controls controls_;
		private readonly VUI.ToggledPanel panel_;
		private readonly VUI.IntTextSlider cols_, rows_;
		private bool ignore_ = false;

		public OptionsControls(Controls c)
		{
			controls_ = c;
			panel_ = new VUI.ToggledPanel("...", true, true);

			var gridPanel = new VUI.Panel(new VUI.GridLayout(3, 10));

			gridPanel.Add(new VUI.Label("Columns"));
			cols_ = gridPanel.Add(new VUI.IntTextSlider(
				controls_.AtomUI.Columns,
				AtomUI.MinColumns,
				AtomUI.MaxColumns,
				OnColumns));
			gridPanel.Add(new VUI.ToolButton("R", () => OnColumns(AtomUI.DefaultColumns)));

			gridPanel.Add(new VUI.Label("Rows"));
			rows_ = gridPanel.Add(new VUI.IntTextSlider(
				controls_.AtomUI.Rows,
				AtomUI.MinRows,
				AtomUI.MaxRows,
				OnRows));
			gridPanel.Add(new VUI.ToolButton("R", () => OnRows(AtomUI.DefaultRows)));


			var p = new VUI.Panel(new VUI.VerticalFlow(10));

			p.Add(new VUI.Button("Rescan loose files", OnRescan));

			p.Add(gridPanel);


			panel_.Panel.Add(p, VUI.BorderLayout.Top);
			panel_.Panel.Padding = new VUI.Insets(20);

			panel_.Toggled += OnToggled;
		}

		public VUI.Button Button
		{
			get { return panel_.Button; }
		}

		private void OnColumns(int i)
		{
			if (ignore_) return;

			try
			{
				ignore_ = true;

				cols_.Value = i;
				controls_.AtomUI.Columns = i;
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void OnRows(int i)
		{
			if (ignore_) return;

			try
			{
				ignore_ = true;

				rows_.Value = i;
				controls_.AtomUI.Rows = i;
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void OnToggled(bool b)
		{
			if (b)
			{
				try
				{
					ignore_ = true;
					cols_.Value = controls_.AtomUI.Columns;
					rows_.Value = controls_.AtomUI.Rows;
				}
				finally
				{
					ignore_ = false;
				}
			}
		}

		private void OnRescan()
		{
			panel_.Hide();
			controls_.AtomUI.Rescan();
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

			for (int i = 0; i < DynamicItemsUI.Filter.SortCount; ++i)
				cb_.AddItem(DynamicItemsUI.Filter.SortToString(i));

			cb_.Select(c_.AtomUI.Filter.Sort);
			cb_.SelectionIndexChanged += (i) => c_.AtomUI.Filter.Sort = i;
		}

		public VUI.Widget Widget
		{
			get { return cb_; }
		}
	}


	abstract class CurrentControls
	{
		public abstract class ItemPanel : VUI.Panel
		{
			private const int ThumbnailSize = 50;

			private readonly Controls c_;
			private readonly VUI.Image thumbnail_;
			private readonly VUI.Panel buttons_;
			private readonly VUI.CheckBox active_;
			private readonly VUI.Label name_;
			private readonly VUI.Button customize_;
			private bool ignore_ = false;

			private DAZDynamicItem item_ = null;


			public ItemPanel(Controls c)
			{
				c_ = c;

				var left = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.Align.VCenterLeft));

				active_ = left.Add(new VUI.CheckBox("", (b) => ToggleActive(), false, "Active"));
				thumbnail_ = left.Add(new VUI.Image());
				thumbnail_.Tooltip.TextFunc = () => c_.AtomUI.MakeTooltip(item_);
				thumbnail_.Tooltip.FontSize = c.AtomUI.FontSize;

				buttons_ = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.Align.VCenterLeft));

				customize_ = AddWidget(new VUI.ToolButton("...", OpenCustomize, "Customize"));

				var center = new VUI.Panel(new VUI.BorderLayout());
				name_ = center.Add(new VUI.Label(), VUI.BorderLayout.Center);
				name_.MinimumSize = new VUI.Size(300, DontCare);
				name_.MaximumSize = new VUI.Size(300, DontCare);
				name_.WrapMode = VUI.Label.ClipEllipsis;
				name_.AutoTooltip = true;
				name_.FontSize = c.AtomUI.FontSize;

				Layout = new VUI.BorderLayout(10);
				Padding = new VUI.Insets(10, 5, 10, 5);

				Add(left, VUI.BorderLayout.Left);
				Add(center, VUI.BorderLayout.Center);
				Add(buttons_, VUI.BorderLayout.Right);

				Clickthrough = false;
				Events.PointerClick += (e) => ToggleActive();

				thumbnail_.MinimumSize = new VUI.Size(ThumbnailSize, ThumbnailSize);
				thumbnail_.MaximumSize = new VUI.Size(ThumbnailSize, ThumbnailSize);
			}

			public T AddWidget<T>(T w) where T : VUI.Widget
			{
				return buttons_.Add(w);
			}

			public DAZDynamicItem Item
			{
				get { return item_; }
			}

			public void Set(DAZDynamicItem item)
			{
				item_ = item;
				Update();
			}

			public void ToggleActive()
			{
				if (ignore_) return;

				try
				{
					ignore_ = true;

					if (item_ != null)
					{
						active_.Checked = !item_.active;
						c_.AtomUI.SetActive(item_, !item_.active);
						c_.AtomUI.UpdatePanels();
						ActiveChanged();
					}
				}
				finally
				{
					ignore_ = false;
				}
			}

			public void Update()
			{
				try
				{
					ignore_ = true;

					if (item_ == null)
					{
						Visible = false;
					}
					else
					{
						Visible = true;

						active_.Checked = item_.active;
						name_.Text = item_.displayName;
						customize_.Enabled = true;

						DoUpdate();

						var forItem = item_;
						item_.GetThumbnail((t) =>
						{
							if (item_ == forItem)
								thumbnail_.Texture = t;
						});
					}
				}
				finally
				{
					ignore_ = false;
				}
			}

			public void OpenCustomize()
			{
				item_?.OpenUI();
			}

			private void ActiveChanged()
			{
				bool b = (item_ != null && item_.active);

				customize_.Enabled = b;

				DoActiveChanged(b);
			}

			protected abstract void DoUpdate();
			protected abstract void DoActiveChanged(bool b);
		}


		private readonly Controls controls_;
		private readonly VUI.ToggledPanel panel_;
		private readonly VUI.Panel buttons_;
		private readonly VUI.Panel itemsPanel_;
		private readonly List<ItemPanel> items_ = new List<ItemPanel>();

		public CurrentControls(Controls c)
		{
			controls_ = c;
			panel_ = new VUI.ToggledPanel("Current", false, true);
			itemsPanel_ = new VUI.Panel(new VUI.VerticalFlow(0));

			var p = new VUI.Panel(new VUI.VerticalFlow(10));
			buttons_ = new VUI.Panel(new VUI.HorizontalFlow(10));

			p.Add(buttons_);
			p.Add(itemsPanel_);

			panel_.Panel.Padding = new VUI.Insets(5);
			panel_.Panel.Add(p, VUI.BorderLayout.Top);
			panel_.Toggled += OnToggled;

			AddWidget(new VUI.ToolButton("Remove all", OnRemoveAll));
		}

		public Controls Controls
		{
			get { return controls_; }
		}

		public T AddWidget<T>(T w) where T : VUI.Widget
		{
			return buttons_.Add(w);
		}

		private void OnToggled(bool b)
		{
			if (b)
			{
				var items = controls_.AtomUI.GetItems();
				var list = new List<DAZDynamicItem>();

				for (int i = 0; i < items.Length; ++i)
				{
					if (items[i].active)
						list.Add(items[i]);
				}

				controls_.AtomUI.Filter.Sorted(list);

				int ii = 0;
				for (int i = 0; i < list.Count; ++i)
				{
					while (ii >= items_.Count)
					{
						var p = controls_.AtomUI.CreateCurrentItemPanel(controls_);
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

		private void OnRemoveAll()
		{
			var items = controls_.AtomUI.GetItems();

			for (int i = 0; i < items.Length; ++i)
			{
				if (items[i].active)
					Controls.AtomUI.SetActive(items[i], false);
			}

			UpdateItems();
			controls_.AtomUI.UpdatePanels();
		}
	}


	class Controls : VUI.Panel
	{
		private readonly AtomUI parent_;
		private readonly VUI.IntTextSlider pages_;
		private readonly VUI.Label pageCount_;
		private readonly VUI.SearchBox search_;
		private readonly CurrentControls cc_;
		private readonly Sorter sorter_;
		private readonly TagsControls tags_;
		private readonly AuthorControls authors_;
		private readonly OptionsControls options_;
		private bool ignore_ = false;

		public Controls(AtomUI parent)
		{
			parent_ = parent;
			cc_ = parent_.CreateCurrentControls(this);
			sorter_ = new Sorter(this);
			tags_ = new TagsControls(this);
			authors_ = new AuthorControls(this);
			options_ = new OptionsControls(this);

			Padding = new VUI.Insets(5);
			Borders = new VUI.Insets(1);
			Layout = new VUI.VerticalFlow(10);

			var pages = new VUI.Panel(new VUI.HorizontalFlow(5, VUI.Align.VCenterLeft));

			pages.Add(new VUI.Label("Pages"));
			pages_ = pages.Add(new VUI.IntTextSlider(OnPageChanged));
			pages.Add(new VUI.ToolButton("<", () => parent_.PreviousPage()));
			pages.Add(new VUI.ToolButton(">", () => parent_.NextPage()));
			pageCount_ = pages.Add(new VUI.Label("", VUI.Align.VCenterLeft));
			pageCount_.MinimumSize = new VUI.Size(80, DontCare);

			search_ = new VUI.SearchBox("Search");

			var top = new VUI.Panel(new VUI.BorderLayout(10));
			top.Add(pages, VUI.BorderLayout.Left);
			top.Add(search_, VUI.BorderLayout.Center);


			var left = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.Align.VCenterLeft));
			left.Add(new VUI.CheckBox("Active", (b) => ToggleActive()));

			var right = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.Align.VCenterLeft));
			right.Add(sorter_.Widget);
			right.Add(cc_.Widget);
			right.Add(authors_.Button);
			right.Add(tags_.Button);
			right.Add(options_.Button);

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

		public AtomUI AtomUI
		{
			get { return parent_; }
		}

		public void ForwardPageWheel(VUI.WheelEvent e)
		{
			pages_.Slider.HandleWheelInternal(e);
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
