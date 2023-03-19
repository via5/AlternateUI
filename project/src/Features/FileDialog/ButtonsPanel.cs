namespace AUI.FileDialog
{
	class ButtonsPanel : VUI.Panel
	{
		public delegate void Handler();
		public event Handler FilenameChanged;

		private readonly FileDialog fd_;
		private readonly VUI.Button action_;
		private readonly VUI.TextBox filename_;
		private readonly VUI.ComboBox<ExtensionItem> extensions_;

		private bool ignore_ = false;

		public ButtonsPanel(FileDialog fd)
		{
			fd_ = fd;

			Layout = new VUI.VerticalFlow(20);
			Padding = new VUI.Insets(20);
			Borders = new VUI.Insets(0, 1, 0, 0);

			var fn = new VUI.Panel(new VUI.BorderLayout(10));
			fn.Padding = new VUI.Insets(30, 0, 0, 0);
			fn.Add(new VUI.Label("File name:"), VUI.BorderLayout.Left);

			filename_ = fn.Add(new VUI.TextBox(), VUI.BorderLayout.Center);
			filename_.Changed += OnFilenameChanged;
			filename_.Submitted += (s) => fd_.ExecuteAction();

			extensions_ = fn.Add(new VUI.ComboBox<ExtensionItem>(), VUI.BorderLayout.Right);
			extensions_.MinimumSize = new VUI.Size(500, VUI.Widget.DontCare);
			extensions_.MaximumSize = new VUI.Size(500, VUI.Widget.DontCare);
			extensions_.PopupWidth = 500;
			extensions_.SelectionChanged += OnExtensionChanged;

			var buttons = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.FlowLayout.AlignRight | VUI.FlowLayout.AlignVCenter));
			action_ = buttons.Add(new VUI.Button("", () => fd_.ExecuteAction()));
			buttons.Add(new VUI.Button("Cancel", () => fd_.Cancel()));

			Add(fn);
			Add(buttons);
		}

		public string Filename
		{
			get
			{
				return filename_.Text;
			}

			set
			{
				try
				{
					ignore_ = true;
					filename_.Text = value;
				}
				finally
				{
					ignore_ = false;
				}
			}
		}

		public ExtensionItem SelectedExtension
		{
			get
			{
				return extensions_.Selected;
			}
		}

		public void Set(IFileDialogMode mode)
		{
			try
			{
				ignore_ = true;
				action_.Text = mode.ActionText;
				extensions_.SetItems(mode.Extensions);
				filename_.Text = "";
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void FocusFilename()
		{
			filename_.Focus();
		}

		public void SetActionButton(bool enabled)
		{
			action_.Enabled = enabled;
		}

		private void OnExtensionChanged(ExtensionItem e)
		{
			if (ignore_) return;
			fd_.RefreshFiles();
		}

		private void OnFilenameChanged(string s)
		{
			if (ignore_) return;
			FilenameChanged?.Invoke();
		}
	}
}
