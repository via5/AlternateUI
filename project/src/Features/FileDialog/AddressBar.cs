using UnityEngine;

namespace AUI.FileDialog
{
	class AddressBar : VUI.Panel
	{
		private readonly FileDialog fd_;

		private readonly VUI.ToolButton back_, next_, up_, refresh_;
		private readonly VUI.MenuButton drop_;
		private readonly VUI.ToolButton pin_, gotoDir_, openInExplorer_;
		private readonly VUI.TextBox path_;
		private readonly VUI.SearchBox search_;
		private readonly VUI.Menu dropMenu_;
		private bool ignore_ = false;

		public AddressBar(FileDialog fd)
		{
			fd_ = fd;

			Layout = new VUI.BorderLayout();

			var left = new VUI.Panel(new VUI.BorderLayout(10));

			dropMenu_ = new VUI.Menu();
			drop_ = new VUI.MenuButton("v", true, true, dropMenu_);
			drop_.AboutToOpen += UpdateHistoryMenu;
			drop_.CloseOnMenuActivated = true;

			var buttons = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.Align.VCenterLeft, true));
			back_ = buttons.Add(new VUI.ToolButton("\x2190", () => fd_.Back(), "Back"));
			next_ = buttons.Add(new VUI.ToolButton("\x2192", () => fd_.Next(), "Next"));
			buttons.Add(drop_.Button);
			up_ = buttons.Add(new VUI.ToolButton("\x2191", () => fd_.Up(), "Up"));
			refresh_ = buttons.Add(new VUI.ToolButton("Refresh", () => fd_.Refresh(), "Refresh"));
			pin_ = buttons.Add(new VUI.ToolButton("Pin", OnTogglePin));
			gotoDir_ = buttons.Add(new VUI.ToolButton("Merged folder", GotoDir,
				"Go to the corresponding merged folder. Available when " +
				"'Merged packages into folders' is set and the current " +
				"folder is in a package"));
			openInExplorer_ = buttons.Add(new VUI.ToolButton("Explorer", OpenInExplorer, "Open in Explorer"));

			back_.Icon = Icons.GetIcon(Icons.Back);
			back_.SetBorderless();

			next_.Icon = Icons.GetIcon(Icons.Next);
			next_.SetBorderless();

			drop_.Button.Icon = Icons.GetIcon(Icons.Drop);
			drop_.Button.IconSize = new VUI.Size(16, 20);
			drop_.Button.SetBorderless();
			drop_.Button.Tooltip.Text = "Recent locations";

			up_.Icon = Icons.GetIcon(Icons.Up);
			up_.SetBorderless();

			refresh_.Icon = Icons.GetIcon(Icons.Reload);
			refresh_.SetBorderless();

			gotoDir_.Icon = Icons.GetIcon(Icons.GotoDirectory);
			gotoDir_.SetBorderless();

			openInExplorer_.Icon = Icons.GetIcon(Icons.OpenExternal);
			openInExplorer_.SetBorderless();

			pin_.Icon = Icons.GetIcon(Icons.Unpinned);
			pin_.SetBorderless();
			pin_.Borders = new VUI.Insets(1);
			pin_.BorderColor = new Color(0, 0, 0, 0);

			left.Add(buttons, VUI.BorderLayout.Left);
			path_ = left.Add(new VUI.TextBox(), VUI.BorderLayout.Center);
			path_.Submitted += OnPathSubmitted;
			path_.AutoComplete.Enabled = true;
			path_.AutoComplete.File = AlternateUI.Instance.GetConfigFilePath(
				"aui.filedialog.path.autocomplete.json");

			search_ = new VUI.SearchBox("Search");
			search_.MinimumSize = new VUI.Size(400, VUI.Widget.DontCare);
			search_.Changed += OnSearchChanged;
			search_.AutoComplete.Enabled = true;
			search_.AutoComplete.File = AlternateUI.Instance.GetConfigFilePath(
				"aui.filedialog.search.autocomplete.json");

			var sp = new VUI.Splitter(left, search_, VUI.Splitter.MinimumSecond);

			Add(sp, VUI.BorderLayout.Center);
		}

		public string Search
		{
			get { return search_.Text; }
			set { search_.Text = value; }
		}

		public string Path
		{
			get { return path_.Text; }
			set { path_.Text = value; }
		}

		public void SetDirectory(FS.IFilesystemContainer dir)
		{
			try
			{
				ignore_ = true;

				back_.Enabled = fd_.CanGoBack();
				next_.Enabled = fd_.CanGoNext();
				up_.Enabled = fd_.CanGoUp();
				drop_.Button.Enabled = (fd_.CanGoBack() || fd_.CanGoNext());
				gotoDir_.Enabled = CanGotoDir(dir);
				openInExplorer_.Enabled = CanOpenInExplorer(dir);

				UpdatePin();
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void ClearSearch()
		{
			try
			{
				ignore_ = true;
				search_.Text = "";
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void DropHistory()
		{
			drop_.Show();
		}

		public void UpdateHistoryMenu()
		{
			dropMenu_.Clear();

			var entries = fd_.History.Entries;
			var group = new VUI.RadioButton.Group("history");

			for (int ri = 0; ri < entries.Length; ++ri)
			{
				int i = entries.Length - ri - 1;
				bool check = (i == fd_.History.CurrentIndex);

				var mi = dropMenu_.AddMenuItem(new VUI.RadioMenuItem(entries[i], (b) =>
				{
					if (b)
						OnHistoryEntry(i);
				}, check, group));

				mi.RadioButton.FontSize = FileDialog.FontSize;
			}
		}

		private bool CanGotoDir(FS.IFilesystemContainer dir)
		{
			if (dir != null)
			{
				var cx = fd_.CreateTreeContext(false);
				if (cx.MergePackages)
					return (dir.VirtualPath != dir.RelativeVirtualPath);
			}

			return false;
		}

		public void GotoDir()
		{
			var dir = fd_.SelectedDirectory;

			if (CanGotoDir(dir))
				fd_.SelectDirectory(dir.RelativeVirtualPath);
		}

		public void OpenInExplorer()
		{
			var dir = fd_.SelectedDirectory;

			if (dir != null)
			{
				var rp = dir.DeVirtualize();
				if (rp != "")
					SuperController.singleton.OpenFolderInExplorer(rp);
			}
		}

		private bool CanOpenInExplorer(FS.IFilesystemContainer dir)
		{
			if (dir == null)
				return false;

			if (dir.ParentPackage != null)
				return false;

			return (dir.DeVirtualize() != "");
		}

		private void UpdatePin()
		{
			var dir = fd_.SelectedDirectory;

			bool enabled = (dir?.CanPin ?? false);
			bool pinned = (dir != null && FS.Filesystem.Instance.IsPinned(dir));

			pin_.Enabled = enabled;

			if (pinned)
			{
				pin_.Text = "Unpin";
				pin_.Tooltip.Text = "Unpin";
				pin_.Icon = Icons.GetIcon(Icons.Pinned);
				pin_.BorderColor = VUI.Style.Theme.BorderColor;
			}
			else
			{
				pin_.Text = "Pin";
				pin_.Tooltip.Text = "Pin";
				pin_.Icon = Icons.GetIcon(Icons.Unpinned);
				pin_.BorderColor = new Color(0, 0, 0, 0);
			}
		}

		private void OnHistoryEntry(int i)
		{
			fd_.GoHistory(i);
		}

		private void OnTogglePin()
		{
			if (ignore_) return;

			var s = fd_.SelectedDirectory;
			if (s != null && s.CanPin)
			{
				if (FS.Filesystem.Instance.IsPinned(s))
					FS.Filesystem.Instance.Unpin(s);
				else
					FS.Filesystem.Instance.Pin(s);

				UpdatePin();
			}
		}

		private void OnPathSubmitted(string s)
		{
			if (ignore_) return;
			fd_.SelectDirectory(s);
		}

		private void OnSearchChanged(string s)
		{
			if (ignore_) return;
			fd_.RefreshFiles();
		}
	}
}
