namespace AUI.MorphUI
{
	class Controls : VUI.Panel
	{
		private readonly MorphUI ui_;
		private VUI.IntTextSlider page_ = new VUI.IntTextSlider();
		private VUI.IgnoreFlag ignore_ = new VUI.IgnoreFlag();

		public Controls(MorphUI ui)
		{
			ui_ = ui;

			var p = new VUI.Panel(new VUI.HorizontalFlow());

			p.Add(new VUI.Label("Page: "));
			p.Add(page_);
			p.Add(new VUI.ToolButton("<", () => ui_.PreviousPage()));
			p.Add(new VUI.ToolButton(">", () => ui_.NextPage()));

			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Center);

			page_.ValueChanged += OnPageChanged;
		}

		public void Update()
		{
			ignore_.Do(() =>
			{
				page_.Set(ui_.CurrentPage + 1, 1, ui_.PageCount);
			});
		}

		private void OnPageChanged(int p)
		{
			if (ignore_) return;
			ui_.CurrentPage = page_.Value - 1;
		}
	}
}
