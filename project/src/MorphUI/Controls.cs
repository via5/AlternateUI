namespace AUI.MorphUI
{
	class Controls : VUI.Panel
	{
		private readonly MorphUI ui_;
		private VUI.IgnoreFlag ignore_ = new VUI.IgnoreFlag();

		private VUI.IntTextSlider page_ = new VUI.IntTextSlider();
		private VUI.Label count_ = new VUI.Label();
		private VUI.TextBox search_ = new VUI.TextBox();

		public Controls(MorphUI ui)
		{
			ui_ = ui;

			var p = new VUI.Panel(new VUI.HorizontalFlow(5));

			p.Add(new VUI.Label("Page: "));
			p.Add(page_);
			p.Add(new VUI.ToolButton("<", () => ui_.PreviousPage()));
			p.Add(new VUI.ToolButton(">", () => ui_.NextPage()));
			p.Add(count_);
			p.Add(search_);

			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Center);

			page_.ValueChanged += OnPageChanged;
			search_.Changed += OnSearchChanged;
		}

		public void Update()
		{
			ignore_.Do(() =>
			{
				page_.Set(ui_.CurrentPage + 1, 1, ui_.PageCount);
				count_.Text = $"/{ui_.PageCount}";
			});
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
