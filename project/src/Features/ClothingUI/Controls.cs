namespace AUI.ClothingUI
{
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
}
