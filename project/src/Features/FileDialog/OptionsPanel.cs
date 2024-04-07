using System.Collections.Generic;

namespace AUI.FileDialog
{
	class OptionsPanel : VUI.Panel
	{
		private readonly FileDialog fd_;
		private IFileDialogMode mode_ = null;

		private readonly VUI.MenuButton optionsPanel_;
		private readonly VUI.CheckBoxMenuItem flattenDirs_;
		private readonly VUI.CheckBoxMenuItem flattenPackages_;
		private readonly VUI.CheckBoxMenuItem mergePackages_;
		private readonly VUI.CheckBoxMenuItem showHiddenFolders_;
		private readonly VUI.CheckBoxMenuItem showHiddenFiles_;
		private readonly VUI.CheckBoxMenuItem latestPackagesOnly_;
		private readonly VUI.CheckBoxMenuItem usePackageTime_;
		private readonly VUI.MenuButton sortPanel_;
		private readonly VUI.Label stats_;

		private readonly Dictionary<int, VUI.RadioMenuItem> sortItems_ =
			new Dictionary<int, VUI.RadioMenuItem>();

		private readonly Dictionary<int, VUI.RadioMenuItem> sortDirItems_ =
			new Dictionary<int, VUI.RadioMenuItem>();

		private bool ignore_ = false;


		public OptionsPanel(FileDialog fd)
		{
			fd_ = fd;

			{
				var sortMenu = new VUI.Menu();
				var sortGroup = new VUI.RadioButton.Group("sort");
				var sortOrderGroup = new VUI.RadioButton.Group("sortOrder");

				AddSortItem(sortMenu, sortGroup, FS.Context.SortFilename, FS.Context.SortAscending);
				AddSortItem(sortMenu, sortGroup, FS.Context.SortType, FS.Context.SortAscending);
				AddSortItem(sortMenu, sortGroup, FS.Context.SortDateModified, FS.Context.SortDescending);
				AddSortItem(sortMenu, sortGroup, FS.Context.SortDateCreated, FS.Context.SortDescending);
				sortMenu.AddSeparator();
				AddSortDirItem(sortMenu, sortOrderGroup, FS.Context.SortAscending);
				AddSortDirItem(sortMenu, sortOrderGroup, FS.Context.SortDescending);

				sortPanel_ = new VUI.MenuButton("Sort", sortMenu);
				sortPanel_.CloseOnMenuActivated = true;
			}

			{
				var m = new VUI.Menu();

				flattenDirs_ = m.AddMenuItem(new VUI.CheckBoxMenuItem(
					"Flatten folders", SetFlattenDirectories, false,
					"Show files from subfolders when a folder is selected."));

				flattenPackages_ = m.AddMenuItem(new VUI.CheckBoxMenuItem(
					"Flatten package content", SetFlattenPackages, false,
					"Show files from subfolders when a package is selected."));

				mergePackages_ = m.AddMenuItem(new VUI.CheckBoxMenuItem(
					"Merge packages into folders", SetMergePackages, false,
					"Find all packages that contain the current folder and " +
					"merge their files in the current view."));

				m.AddSeparator();

				showHiddenFolders_ = m.AddMenuItem(new VUI.CheckBoxMenuItem(
					"Show all folders", SetShowHiddenFolders, false,
					"Show folders that are not usually relevant."));

				showHiddenFiles_ = m.AddMenuItem(new VUI.CheckBoxMenuItem(
					"Show all files", SetShowHiddenFiles, false,
					"Show internal files like meta.json."));

				latestPackagesOnly_ = m.AddMenuItem(new VUI.CheckBoxMenuItem(
					"Latest packages only", SetLatestPackagesOnly, false,
					"Only show the latest version of installed packages."));

				m.AddSeparator();

				usePackageTime_ = m.AddMenuItem(new VUI.CheckBoxMenuItem(
					"Use package time", SetUsePackageTime, fd_.UsePackageTime,
					"Files in packages will use the package modification " +
					"date/time instead of the files themselves."));

				optionsPanel_ = new VUI.MenuButton("Options", m);
			}

			stats_ = new VUI.Label("");


			var leftPanel = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.Align.VCenterLeft));
			leftPanel.Add(optionsPanel_.Button);
			leftPanel.Add(sortPanel_.Button);

			var rightPanel = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.Align.VCenterLeft));
			rightPanel.Add(stats_);


			Layout = new VUI.BorderLayout();
			Add(leftPanel, VUI.BorderLayout.Left);
			Add(rightPanel, VUI.BorderLayout.Right);
		}

		private void AddSortItem(VUI.Menu menu, VUI.RadioButton.Group g, int sort, int defaultDir)
		{
			var item = menu.AddMenuItem(MakeSortItem(FS.Context.SortToString(sort), sort, defaultDir, g));
			sortItems_.Add(sort, item);
		}

		private void AddSortDirItem(VUI.Menu menu, VUI.RadioButton.Group g, int dir)
		{
			var item = menu.AddMenuItem(MakeSortDirItem(FS.Context.SortDirectionToString(dir), dir, g));
			sortDirItems_.Add(dir, item);
		}

		public void SetMode(IFileDialogMode mode)
		{
			try
			{
				ignore_ = true;

				mode_ = mode;

				flattenDirs_.CheckBox.Enabled = !mode_.IsWritable;
				flattenPackages_.CheckBox.Enabled = !mode_.IsWritable;
				mergePackages_.CheckBox.Enabled = !mode_.IsWritable;
				latestPackagesOnly_.CheckBox.Enabled = !mode_.IsWritable;

				flattenDirs_.CheckBox.Checked = mode_.Options.FlattenDirectories;
				flattenPackages_.CheckBox.Checked = mode_.Options.FlattenPackages;
				mergePackages_.CheckBox.Checked = mode_.Options.MergePackages;
				showHiddenFolders_.CheckBox.Checked = mode_.Options.ShowHiddenFolders;
				showHiddenFiles_.CheckBox.Checked = mode_.Options.ShowHiddenFiles;
				latestPackagesOnly_.CheckBox.Checked = mode_.Options.LatestPackagesOnly;
				usePackageTime_.CheckBox.Checked = fd_.UsePackageTime;
			}
			finally
			{
				ignore_ = false;
			}

			UpdateSortButton();
		}

		public void SetFiles(List<FS.IFilesystemObject> files)
		{
			if (files.Count == 0)
				stats_.Text = $"0 files";
			else if (files.Count == 1)
				stats_.Text = $"1 file";
			else
				stats_.Text = $"{files.Count} files";
		}

		private VUI.RadioMenuItem MakeSortItem(string text, int sort, int defaultDir, VUI.RadioButton.Group g)
		{
			VUI.RadioButton.ChangedCallback cb = (bool b) =>
			{
				if (b)
					SetSort(sort, defaultDir);
			};

			return new VUI.RadioMenuItem(text, cb, false, g);
		}

		private VUI.RadioMenuItem MakeSortDirItem(string text, int sortDir, VUI.RadioButton.Group g)
		{
			VUI.RadioButton.ChangedCallback cb = (bool b) =>
			{
				if (b)
					SetSortDirection(sortDir);
			};

			return new VUI.RadioMenuItem(text, cb, false, g);
		}

		private void SetFlattenDirectories(bool b)
		{
			if (ignore_) return;

			mode_.Options.FlattenDirectories = b;
			fd_.RefreshBoth();
		}

		private void SetFlattenPackages(bool b)
		{
			if (ignore_) return;

			mode_.Options.FlattenPackages = b;
			fd_.RefreshBoth();
		}

		private void SetMergePackages(bool b)
		{
			if (ignore_) return;

			mode_.Options.MergePackages = b;
			fd_.RefreshBoth();
		}

		private void SetShowHiddenFolders(bool b)
		{
			if (ignore_) return;

			mode_.Options.ShowHiddenFolders = b;

			// this can also affect files for flattened folders where some
			// folders are hidden
			fd_.RefreshBoth();
		}

		private void SetShowHiddenFiles(bool b)
		{
			if (ignore_) return;

			mode_.Options.ShowHiddenFiles = b;
			fd_.RefreshFiles();
		}

		private void SetLatestPackagesOnly(bool b)
		{
			if (ignore_) return;

			mode_.Options.LatestPackagesOnly = b;
			fd_.RefreshBoth();
		}

		private void SetSort(int s, int d)
		{
			if (ignore_) return;

			mode_.Options.Sort = s;
			mode_.Options.SortDirection = d;

			fd_.RefreshFiles();
			UpdateSortButton();
		}

		private void SetSortDirection(int s)
		{
			if (ignore_) return;

			mode_.Options.SortDirection = s;
			fd_.RefreshFiles();
			UpdateSortButton();
		}

		private void SetUsePackageTime(bool b)
		{
			if (ignore_) return;
			fd_.UsePackageTime = b;
		}

		private void UpdateSortButton()
		{
			AlternateUI.Assert(!ignore_);

			try
			{
				ignore_ = true;

				VUI.RadioMenuItem item;

				if (sortItems_.TryGetValue(mode_.Options.Sort, out item))
					item.RadioButton.Checked = true;

				if (sortDirItems_.TryGetValue(mode_.Options.SortDirection, out item))
					item.RadioButton.Checked = true;

				string sort =
					FS.Context.SortToString(mode_.Options.Sort) + " " +
					FS.Context.SortDirectionToShortString(mode_.Options.SortDirection);

				sortPanel_.Button.Text = sort;
			}
			finally
			{
				ignore_ = false;
			}
		}
	}
}
