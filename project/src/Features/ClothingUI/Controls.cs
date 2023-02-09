using System.Collections.Generic;
using System.Linq;

namespace AUI.ClothingUI
{
	class TagsControls
	{
		private readonly Controls controls_;
		private readonly ToggledPanel tagsPanel_ = new ToggledPanel("Tags");
		private readonly VUI.TreeView tags_ = new VUI.TreeView();
		private readonly SearchBox tagsSearch_;
		private readonly VUI.ComboBox<string> and_;
		private bool ignore_ = false;

		private bool one_ = true;

		public TagsControls(Controls controls)
		{
			controls_ = controls;
			controls_.ClothingAtomInfo.TagsChanged += OnTagsChanged;

			tags_.CheckBoxes = true;
			tags_.ItemClicked += OnTagClicked;

			tagsSearch_ = new SearchBox("Search tags");
			tagsSearch_.Changed += OnTagsSearch;

			var top = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.FlowLayout.AlignLeftVCenter));
			top.Padding = new VUI.Insets(5);
			top.Add(new VUI.CheckBox("Only one", OnOneChanged, one_));
			and_ = top.Add(new VUI.ComboBox<string>(new string[] { "And", "Or" }, OnAndChanged));

			var bottom = new VUI.Panel(new VUI.VerticalFlow(10));
			bottom.Add(tagsSearch_.Widget);

			tagsPanel_.RightClick += ClearTags;
			tagsPanel_.Panel.Add(top, VUI.BorderLayout.Top);
			tagsPanel_.Panel.Add(tags_, VUI.BorderLayout.Center);
			tagsPanel_.Panel.Add(bottom, VUI.BorderLayout.Bottom);

			var sui = controls_.ClothingAtomInfo.SelectorUI;

			tags_.RootItem.Add(MakeTagItem("Regions", sui.regionTags));
			tags_.RootItem.Add(MakeTagItem("Types", sui.typeTags));
			tags_.RootItem.Add(MakeTagItem("Others", sui.GetOtherTags()));

			UpdateAnd();
		}

		public VUI.Button Button
		{
			get { return tagsPanel_.Button; }
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
				UncheckAll(tags_.RootItem);
				controls_.ClothingAtomInfo.ClearTags();
			}
			finally
			{
				ignore_ = false;
			}
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

		private void UpdateAnd()
		{
			and_.Enabled = !one_;
		}

		private void UncheckAllExceptFirst()
		{
			bool found = false;
			UncheckAllExceptFirst(tags_.RootItem, ref found);
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
			UncheckAllExcept(item, tags_.RootItem);
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

		private void OnCheckedChanged(VUI.TreeView.Item i, bool b)
		{
			if (ignore_) return;

			if (b)
			{
				if (one_)
					UncheckAllExcept(i);

				controls_.ClothingAtomInfo.AddTag(i.Text);
			}
			else
			{
				controls_.ClothingAtomInfo.RemoveTag(i.Text);
			}

			if (one_)
				tagsPanel_.Hide();
		}

		private void OnTagsChanged()
		{
			var tags = controls_.ClothingAtomInfo.Tags;

			if (tags.Count == 0)
				tagsPanel_.Button.Text = "Tags";
			else if (tags.Count == 1)
				tagsPanel_.Button.Text = tags.First();
			else
				tagsPanel_.Button.Text = $"{tags.First()} +{tags.Count - 1}";
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
			tags_.Filter = s;
		}

		private void OnOneChanged(bool b)
		{
			OnlyOne = b;
			UpdateAnd();
		}

		private void OnAndChanged(int i)
		{
			controls_.ClothingAtomInfo.TagsAnd = (i == 0);
		}
	}


	class Controls : VUI.Panel
	{
		private readonly ClothingAtomInfo parent_;
		private readonly VUI.IntTextSlider pages_;
		private readonly VUI.Label pageCount_;
		private readonly SearchBox search_ = new SearchBox("Search");
		private readonly TagsControls tags_;
		private bool ignore_ = false;

		public Controls(ClothingAtomInfo parent)
		{
			parent_ = parent;
			tags_ = new TagsControls(this);

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
				parent_.Active = !parent_.Active;
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
			parent_.Search = s;
		}
	}
}
