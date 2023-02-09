using System.Collections.Generic;

namespace AUI.ClothingUI
{
	class Controls : VUI.Panel
	{
		private readonly ClothingAtomInfo parent_;
		private readonly VUI.IntTextSlider pages_;
		private readonly VUI.Label pageCount_;
		private readonly SearchBox search_ = new SearchBox("Search");
		private readonly ToggledPanel tagsPanel_ = new ToggledPanel("Tags");
		private readonly VUI.TreeView tags_ = new VUI.TreeView();
		private readonly SearchBox tagsSearch_;
		private bool ignore_ = false;

		public Controls(ClothingAtomInfo parent)
		{
			parent_ = parent;

			tags_.CheckBoxes = true;
			tags_.ItemClicked += OnTagClicked;

			tagsSearch_ = new SearchBox("Search tags");
			tagsSearch_.Changed += OnTagsSearch;

			tagsPanel_.RightClick += ClearTags;
			tagsPanel_.Panel.Add(tags_, VUI.BorderLayout.Center);
			tagsPanel_.Panel.Add(tagsSearch_.Widget, VUI.BorderLayout.Bottom);

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

			right.Add(tagsPanel_.Button);

			var bottom = new VUI.Panel(new VUI.BorderLayout(10));
			bottom.Add(left, VUI.BorderLayout.Left);
			bottom.Add(right, VUI.BorderLayout.Right);


			Add(top);
			Add(bottom);

			search_.Changed += OnSearchChanged;

			tags_.RootItem.Add(MakeTagItem("Regions", parent_.SelectorUI.regionTags));
			tags_.RootItem.Add(MakeTagItem("Types", parent_.SelectorUI.typeTags));
			tags_.RootItem.Add(MakeTagItem("Others", parent_.SelectorUI.GetOtherTags()));
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

		public void ClearTags()
		{
			try
			{
				ignore_ = true;
				UncheckAll(tags_.RootItem);
				parent_.ClearTags();
			}
			finally
			{
				ignore_ = false;
			}
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

		private void OnCheckedChanged(VUI.TreeView.Item i, bool b)
		{
			if (ignore_) return;

			if (b)
				parent_.AddTag(i.Text);
			else
				parent_.RemoveTag(i.Text);
		}

		private void OnTagClicked(VUI.TreeView.Item i)
		{
			if (ignore_) return;
			i.Checked = !i.Checked;
		}

		private void OnTagsSearch(string s)
		{
			tags_.Filter = s;
		}
	}
}
