﻿namespace AUI.MorphUI
{
	class CategoriesWidget
	{
		private GenderMorphUI ui_;
		private VUI.Button button_;
		private VUI.Panel panel_;
		private VUI.TreeView tree_;
		private SearchBox search_;
		private Categories cats_ = null;
		private bool ignore_ = false;

		class CategoryItem : VUI.TreeView.Item
		{
			private readonly Categories.Node node_;

			public CategoryItem(Categories.Node n)
			{
				node_ = n;

				if (node_ == null)
					Text = "All";
				else if (node_.Name == "")
					Text = "(no category)";
				else
					Text = n.Name;
			}

			public Categories.Node Node
			{
				get { return node_; }
			}
		}


		public CategoriesWidget(GenderMorphUI ui)
		{
			ui_ = ui;

			var s = new VUI.Size(500, 800);

			button_ = new VUI.Button("Categories", Toggle);
			button_.Events.PointerClick += ToggleClick;

			panel_ = new VUI.Panel("CategoriesWidgetPanel");
			panel_.Layout = new VUI.BorderLayout();
			panel_.BackgroundColor = VUI.Style.Theme.BackgroundColor;
			panel_.BorderColor = VUI.Style.Theme.BorderColor;
			panel_.Borders = new VUI.Insets(1);
			panel_.Clickthrough = false;
			panel_.MinimumSize = s;
			panel_.Visible = false;
			panel_.SetBounds(VUI.Rectangle.FromSize(
				ui_.Root.FloatingPanel.RelativeBounds.Width - s.Width - 20,
				ui_.Root.FloatingPanel.RelativeBounds.Top + 50,
				s.Width, s.Height));

			tree_ = new VUI.TreeView();

			panel_.Add(tree_, VUI.BorderLayout.Center);

			search_ = new SearchBox("Search categories");
			search_.Changed += OnSearch;
			panel_.Add(search_.Widget, VUI.BorderLayout.Bottom);

			ui_.Root.FloatingPanel.Add(panel_);

			tree_.ItemClicked += OnSelection;
		}

		public VUI.Button Button
		{
			get { return button_; }
		}

		public void Set(Categories cats)
		{
			cats_ = cats;
			RebuildTree();
		}

		private void RebuildTree()
		{
			try
			{
				ignore_ = true;

				tree_.RootItem.Clear();

				if (cats_ != null)
				{
					tree_.RootItem.Add(new CategoryItem(null));
					AddCategories(tree_.RootItem, cats_.Root);
					tree_.RootItem.Children[0].Selected = true;
				}

				tree_.ItemsChanged();
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void AddCategories(
			VUI.TreeView.Item parentItem, Categories.Node parentCat)
		{
			if (parentCat.Children == null)
				return;

			foreach (var c in parentCat.Children)
			{
				var item = new CategoryItem(c);
				AddCategories(item, c);
				parentItem.Add(item);
			}
		}

		public void Toggle()
		{
			if (panel_.Visible)
				Hide();
			else
				Show();
		}

		public void Show()
		{
			panel_.Visible = true;
			panel_.BringToTop();
			panel_.GetRoot().FocusChanged += OnFocusChanged;
		}

		public void Hide()
		{
			panel_.Visible = false;
			panel_.GetRoot().FocusChanged -= OnFocusChanged;
		}

		private void OnFocusChanged(VUI.Widget blurred, VUI.Widget focused)
		{
			if (!focused.HasParent(panel_) && focused != button_)
				Hide();
		}

		private void ToggleClick(VUI.PointerEvent e)
		{
			if (e.Button == VUI.PointerEvent.RightButton)
				ResetCategory();

			e.Bubble = false;
		}

		private void ResetCategory()
		{
			ui_.Filter.Category = null;
			button_.Text = "Categories";
		}

		private void OnSelection(VUI.TreeView.Item item)
		{
			if (ignore_) return;

			var c = item as CategoryItem;

			if (c?.Node == null)
			{
				ResetCategory();
			}
			else
			{
				ui_.Filter.Category = c.Node;
				button_.Text = c.Node.Name;
			}

			panel_.Visible = false;
		}

		private void OnSearch(string s)
		{
			tree_.Filter = s;
		}
	}


	class Controls : VUI.Panel
	{
		private readonly GenderMorphUI ui_;
		private bool ignore_ = false;

		private VUI.IntTextSlider page_ = new VUI.IntTextSlider();
		private VUI.Label count_ = new VUI.Label();
		private SearchBox search_ = new SearchBox("Search");
		private CategoriesWidget cats_;

		public Controls(GenderMorphUI ui)
		{
			ui_ = ui;
			cats_ = new CategoriesWidget(ui);

			Layout = new VUI.VerticalFlow(5);

			var pagePanel = new VUI.Panel(new VUI.HorizontalFlow(
				5, VUI.FlowLayout.AlignLeft|VUI.FlowLayout.AlignVCenter, true));

			pagePanel.Add(new VUI.Label("Page: "));
			pagePanel.Add(page_);
			pagePanel.Add(new VUI.ToolButton("<", () => ui_.PreviousPage()));
			pagePanel.Add(new VUI.ToolButton(">", () => ui_.NextPage()));
			pagePanel.Add(count_);

			var searchPanel = new VUI.Panel(new VUI.BorderLayout());
			searchPanel.Add(search_.Widget, VUI.BorderLayout.Center);

			var catsPanel = new VUI.Panel(new VUI.HorizontalFlow(
				5, VUI.FlowLayout.AlignDefault, true));
			catsPanel.Add(cats_.Button);

			var firstRow = new VUI.Panel(new VUI.BorderLayout(5));
			firstRow.Add(pagePanel, VUI.BorderLayout.Left);
			firstRow.Add(searchPanel, VUI.BorderLayout.Center);
			firstRow.Add(catsPanel, VUI.BorderLayout.Right);
			Add(firstRow);

			var secondRow = new VUI.Panel(new VUI.BorderLayout(5));

			var center = new VUI.Panel(new VUI.HorizontalFlow(
				5, VUI.FlowLayout.AlignLeft|VUI.FlowLayout.AlignVCenter, true));

			center.Add(new VUI.CheckBox(
				"Favorites",
				(b) => ui_.Filter.OnlyFavorites = b,
				ui_.Filter.OnlyFavorites));

			center.Add(new VUI.CheckBox(
				"Latest",
				(b) => ui_.Filter.OnlyLatest = b,
				ui_.Filter.OnlyLatest));

			center.Add(new VUI.CheckBox(
				"Active",
				(b) => ui_.Filter.OnlyActive = b,
				ui_.Filter.OnlyActive));

			var right = new VUI.Panel(new VUI.HorizontalFlow(5));

			secondRow.Add(center, VUI.BorderLayout.Center);
			secondRow.Add(right, VUI.BorderLayout.Right);

			Add(secondRow);

			Borders = new VUI.Insets(1);
			Padding = new VUI.Insets(5);

			page_.ValueChanged += OnPageChanged;
			search_.Changed += OnSearchChanged;

			search_.TextBox.AutoComplete.Enabled = true;
			search_.TextBox.AutoComplete.File = GetAutoCompleteFile();
			search_.TextBox.AutoComplete.Changed += OnAutoCompleteChanged;
		}

		private string GetAutoCompleteFile()
		{
			return AlternateUI.Instance.GetConfigFilePath(
				$"aui.{ui_.PersonMorphUI.MorphUI.Name}.{ui_.GenderName}.autocomplete.json");
		}

		public void UpdatePage()
		{
			try
			{
				ignore_ = true;
				page_.Set(ui_.CurrentPage + 1, 1, ui_.PageCount);
				count_.Text = $"/{ui_.PageCount}";
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void UpdateCategories()
		{
			cats_.Set(ui_.Categories);
		}

		private void OnPageChanged(int p)
		{
			if (ignore_) return;
			ui_.CurrentPage = page_.Value - 1;
		}

		private void OnSearchChanged(string s)
		{
			ui_.Filter.Search = s;
		}

		private void OnAutoCompleteChanged()
		{
			foreach (var pm in ui_.PersonMorphUI.MorphUI.PersonUIs)
			{
				if (pm == ui_.PersonMorphUI)
					continue;

				foreach (var gm in pm.GenderMorphUIs)
				{
					if (ui_.GenderName == gm.GenderName)
					{
						gm.Controls?.search_?.TextBox?.AutoComplete?.Reload();
					}
				}
			}
		}
	}
}
