using System.Collections.Generic;

namespace AUI.FileDialog
{
	class OptionsPanel : VUI.Panel
	{
		private readonly FileDialog fd_;
		private IFileDialogMode mode_ = null;

		private readonly VUI.CheckBox flattenDirs_;
		private readonly VUI.CheckBox flattenPackages_;
		private readonly VUI.CheckBox mergePackages_;
		private readonly VUI.CheckBox showHiddenFolders_;
		private readonly VUI.CheckBox showHiddenFiles_;
		private readonly VUI.MenuButton sortPanel_;

		private readonly Dictionary<int, VUI.RadioMenuItem> sortItems_ =
			new Dictionary<int, VUI.RadioMenuItem>();

		private readonly Dictionary<int, VUI.RadioMenuItem> sortDirItems_ =
			new Dictionary<int, VUI.RadioMenuItem>();

		private bool ignore_ = false;


		public OptionsPanel(FileDialog fd)
		{
			fd_ = fd;

			var sortMenu = new VUI.Menu();
			var sortGroup = new VUI.RadioButton.Group("sort");
			var sortOrderGroup = new VUI.RadioButton.Group("sortOrder");

			AddSortItem(sortMenu, sortGroup, FS.Context.SortFilename);
			AddSortItem(sortMenu, sortGroup, FS.Context.SortType);
			AddSortItem(sortMenu, sortGroup, FS.Context.SortDateModified);
			AddSortItem(sortMenu, sortGroup, FS.Context.SortDateCreated);
			sortMenu.AddSeparator();
			AddSortDirItem(sortMenu, sortOrderGroup, FS.Context.SortAscending);
			AddSortDirItem(sortMenu, sortOrderGroup, FS.Context.SortDescending);

			sortPanel_ = new VUI.MenuButton("Sort", sortMenu);

			Layout = new VUI.HorizontalFlow(10, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter);

			flattenDirs_ = Add(new VUI.CheckBox("Flatten folders", SetFlattenDirectories));
			flattenPackages_ = Add(new VUI.CheckBox("Flatten package content", SetFlattenPackages));
			mergePackages_ = Add(new VUI.CheckBox("Merge packages into folders", SetMergePackages));
			showHiddenFolders_ = Add(new VUI.CheckBox("Show all folders", SetShowHiddenFolders));
			showHiddenFiles_ = Add(new VUI.CheckBox("Show all files", SetShowHiddenFiles));

			Add(sortPanel_.Button);
		}

		private void AddSortItem(VUI.Menu menu, VUI.RadioButton.Group g, int sort)
		{
			var item = menu.AddMenuItem(MakeSortItem(FS.Context.SortToString(sort), sort, g));
			sortItems_.Add(sort, item);
		}

		private void AddSortDirItem(VUI.Menu menu, VUI.RadioButton.Group g, int dir)
		{
			var item = menu.AddMenuItem(MakeSortDirItem(FS.Context.SortDirectionToString(dir), dir, g));
			sortDirItems_.Add(dir, item);
		}

		public void Set(IFileDialogMode mode)
		{
			try
			{
				ignore_ = true;

				mode_ = mode;

				flattenDirs_.Visible = !mode_.IsWritable;
				flattenPackages_.Visible = !mode_.IsWritable;

				flattenDirs_.Checked = mode_.Options.FlattenDirectories;
				flattenPackages_.Checked = mode_.Options.FlattenPackages;
				mergePackages_.Checked = mode_.Options.MergePackages;
				showHiddenFolders_.Checked = mode_.Options.ShowHiddenFolders;
				showHiddenFiles_.Checked = mode_.Options.ShowHiddenFiles;

				VUI.RadioMenuItem item;

				if (sortItems_.TryGetValue(mode_.Options.Sort, out item))
					item.RadioButton.Checked = true;

				if (sortDirItems_.TryGetValue(mode_.Options.SortDirection, out item))
					item.RadioButton.Checked = true;

				UpdateSortButton();
			}
			finally
			{
				ignore_ = false;
			}
		}

		private VUI.RadioMenuItem MakeSortItem(string text, int sort, VUI.RadioButton.Group g)
		{
			VUI.RadioButton.ChangedCallback cb = (bool b) =>
			{
				if (b)
					SetSort(sort);
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

		private void SetSort(int s)
		{
			if (ignore_) return;

			mode_.Options.Sort = s;
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

		private void UpdateSortButton()
		{
			string sort =
				FS.Context.SortToString(mode_.Options.Sort) + " " +
				FS.Context.SortDirectionToShortString(mode_.Options.SortDirection);

			sortPanel_.Button.Text = sort;
		}
	}
}
