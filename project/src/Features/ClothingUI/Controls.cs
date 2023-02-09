﻿using System.Collections.Generic;
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


	class Controls : VUI.Panel
	{
		private readonly ClothingAtomInfo parent_;
		private readonly VUI.IntTextSlider pages_;
		private readonly VUI.Label pageCount_;
		private readonly SearchBox search_ = new SearchBox("Search");
		private readonly TagsControls tags_;
		private readonly AuthorControls authors_;
		private bool ignore_ = false;

		public Controls(ClothingAtomInfo parent)
		{
			parent_ = parent;
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

			var top = new VUI.Panel(new VUI.BorderLayout(10));
			top.Add(pages, VUI.BorderLayout.Left);
			top.Add(search_.Widget, VUI.BorderLayout.Center);


			var left = new VUI.Panel(new VUI.HorizontalFlow(
				10, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter));

			left.Add(new VUI.CheckBox("Active", (b) => ToggleActive()));


			var right = new VUI.Panel(new VUI.HorizontalFlow(
				10, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter));

			right.Add(authors_.Button);
			right.Add(tags_.Button);

			var bottom = new VUI.Panel(new VUI.BorderLayout(10));
			bottom.Add(left, VUI.BorderLayout.Left);
			bottom.Add(right, VUI.BorderLayout.Right);


			Add(top);
			Add(bottom);

			search_.Changed += OnSearchChanged;
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
