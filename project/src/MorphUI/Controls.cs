namespace AUI.MorphUI
{
	class SearchBox
	{
		public delegate void Handler(string s);
		public event Handler Changed;

		private VUI.Panel panel_;
		private VUI.TextBox box_;
		private VUI.ToolButton clear_;

		public SearchBox(string placeholder, bool autoComplete)
		{
			panel_ = new VUI.Panel(new VUI.BorderLayout());

			box_ = new VUI.TextBox("", placeholder);
			box_.TextMargins = new VUI.Insets(0, 0, 44, 0);
			box_.Changed += (s) => Changed?.Invoke(s);

			var clearPanel = new VUI.Panel(new VUI.HorizontalFlow(
				0, VUI.FlowLayout.AlignRight | VUI.FlowLayout.AlignVCenter));

			clear_ = new VUI.ToolButton("X");
			clear_.Margins = new VUI.Insets(0, 2, 5, 2);
			clear_.MaximumSize = new VUI.Size(35, 35);
			clear_.Clicked += () => box_.Text = "";

			clearPanel.Add(clear_);

			panel_.Add(box_, VUI.BorderLayout.Center);
			panel_.Add(clearPanel, VUI.BorderLayout.Center);

			box_.AutoComplete.Enabled = autoComplete;
		}

		public VUI.Widget Widget
		{
			get { return panel_; }
		}

		public string Text
		{
			get { return box_?.Text ?? ""; }
		}
	}


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

			search_ = new SearchBox("Search categories", false);
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
			if (!focused.HasParent(panel_))
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
		private SearchBox search_ = new SearchBox("Search", true);
		private CategoriesWidget cats_;

		public Controls(GenderMorphUI ui)
		{
			ui_ = ui;
			cats_ = new CategoriesWidget(ui);

			Layout = new VUI.VerticalFlow(5);

			var pagePanel = new VUI.Panel(new VUI.HorizontalFlow(5));
			pagePanel.Add(new VUI.Label("Page: "));
			pagePanel.Add(page_);
			pagePanel.Add(new VUI.ToolButton("<", () => ui_.PreviousPage()));
			pagePanel.Add(new VUI.ToolButton(">", () => ui_.NextPage()));
			pagePanel.Add(count_);

			var searchPanel = new VUI.Panel(new VUI.BorderLayout());
			searchPanel.Add(search_.Widget, VUI.BorderLayout.Center);

			var catsPanel = new VUI.Panel(new VUI.HorizontalFlow(5));
			catsPanel.Add(cats_.Button);

			var row = new VUI.Panel(new VUI.BorderLayout(5));
			row.Add(pagePanel, VUI.BorderLayout.Left);
			row.Add(searchPanel, VUI.BorderLayout.Center);
			row.Add(catsPanel, VUI.BorderLayout.Right);
			Add(row);

			row = new VUI.Panel(new VUI.HorizontalFlow(5));

			row.Add(new VUI.CheckBox(
				"Favorites",
				(b) => ui_.Filter.OnlyFavorites = b,
				ui_.Filter.OnlyFavorites));

			row.Add(new VUI.CheckBox(
				"Latest",
				(b) => ui_.Filter.OnlyLatest = b,
				ui_.Filter.OnlyLatest));

			row.Add(new VUI.CheckBox(
				"Active",
				(b) => ui_.Filter.OnlyActive = b,
				ui_.Filter.OnlyActive));

			//row.Add(new VUI.Button(
			//	"Reload plugin", () => AlternateUI.Instance.ReloadPlugin()));

			Add(row);

			Borders = new VUI.Insets(1);
			Padding = new VUI.Insets(5);

			page_.ValueChanged += OnPageChanged;
			search_.Changed += OnSearchChanged;
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
	}
}
