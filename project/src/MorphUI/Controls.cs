namespace AUI.MorphUI
{
	class CategoriesWidget
	{
		private VUI.Button button_;
		private VUI.Panel panel_;
		private VUI.TreeView tree_;

		public CategoriesWidget(VUI.Root root)
		{
			var s = new VUI.Size(500, 800);

			button_ = new VUI.Button("Categories", Toggle);

			panel_ = new VUI.Panel();
			panel_.Layout = new VUI.BorderLayout();
			panel_.BackgroundColor = VUI.Style.Theme.BackgroundColor;
			panel_.BorderColor = VUI.Style.Theme.BorderColor;
			panel_.Borders = new VUI.Insets(1);
			panel_.Clickthrough = false;
			panel_.MinimumSize = s;
			panel_.Visible = false;
			panel_.SetBounds(VUI.Rectangle.FromSize(
				root.FloatingPanel.RelativeBounds.Width - s.Width - 20,
				root.FloatingPanel.RelativeBounds.Top + 50,
				s.Width, s.Height));

			tree_ = new VUI.TreeView();
			panel_.Add(tree_, VUI.BorderLayout.Center);

			root.FloatingPanel.Add(panel_);
		}

		public VUI.Button Button
		{
			get { return button_; }
		}

		public void Set(Categories cats)
		{
			tree_.RootItem.Clear();
			AddCategories(tree_.RootItem, cats.Root);
		}

		private void AddCategories(
			VUI.TreeView.Item parentItem, Categories.Node parentCat)
		{
			if (parentCat.Children != null)
			{
				foreach (var c in parentCat.Children)
				{
					var item = new VUI.TreeView.Item(c.Name);
					parentItem.Add(item);
					AddCategories(item, c);
				}
			}

			//foreach (var c in cats.All)
			//	tree_.RootItem.Add(new VUI.TreeView.Item(c));

			//if (tree_.WidgetObject != null)
			//	tree_.UpdateBounds();
			//Log.Info($"{cats.All.Count}");
		}

		public void Toggle()
		{
			panel_.Visible = !panel_.Visible;
		}
	}


	class Controls : VUI.Panel
	{
		private readonly MorphUI ui_;
		private VUI.IgnoreFlag ignore_ = new VUI.IgnoreFlag();

		private VUI.IntTextSlider page_ = new VUI.IntTextSlider();
		private VUI.Label count_ = new VUI.Label();
		private VUI.TextBox search_ = new VUI.TextBox();
		private CategoriesWidget cats_;

		public Controls(MorphUI ui)
		{
			ui_ = ui;
			cats_ = new CategoriesWidget(ui.Root);
			search_.Placeholder = "Search";

			var pagePanel = new VUI.Panel(new VUI.HorizontalFlow(5));
			pagePanel.Add(new VUI.Label("Page: "));
			pagePanel.Add(page_);
			pagePanel.Add(new VUI.ToolButton("<", () => ui_.PreviousPage()));
			pagePanel.Add(new VUI.ToolButton(">", () => ui_.NextPage()));
			pagePanel.Add(count_);

			var searchPanel = new VUI.Panel(new VUI.BorderLayout());
			searchPanel.Add(search_, VUI.BorderLayout.Center);

			var catsPanel = new VUI.Panel(new VUI.HorizontalFlow(5));
			catsPanel.Add(cats_.Button);

			Layout = new VUI.BorderLayout(5);
			Add(pagePanel, VUI.BorderLayout.Left);
			Add(searchPanel, VUI.BorderLayout.Center);
			Add(catsPanel, VUI.BorderLayout.Right);

			page_.ValueChanged += OnPageChanged;
			search_.Changed += OnSearchChanged;
		}

		public void UpdatePage()
		{
			ignore_.Do(() =>
			{
				page_.Set(ui_.CurrentPage + 1, 1, ui_.PageCount);
				count_.Text = $"/{ui_.PageCount}";
			});
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
