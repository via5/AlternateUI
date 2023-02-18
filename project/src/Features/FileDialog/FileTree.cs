using MVR.FileManagementSecure;
using System.Collections.Generic;

namespace AUI.FileDialog
{
	class FileTree
	{
		private class FileItem : VUI.TreeView.Item
		{
			private readonly string path_;
			private bool checkedHasChildren_ = false;
			private bool hasChildren_ = false;

			public FileItem(string path, string display, bool childrenChecked=false)
				: base(display)
			{
				path_ = path;
				checkedHasChildren_ = childrenChecked;
				if (childrenChecked)
					hasChildren_ = true;
			}

			public string Path
			{
				get { return path_; }
			}

			public override bool HasChildren
			{
				get
				{
					if (!checkedHasChildren_)
					{
						var dirs = FileManagerSecure.GetDirectories(path_);
						hasChildren_ = (dirs != null && dirs.Length > 0);
						checkedHasChildren_ = true;
					}

					return hasChildren_;
				}
			}

			protected override void GetChildren()
			{
				foreach (var d in FileManagerSecure.GetDirectories(path_))
					Add(new FileItem(d, AUI.Path.Filename(d)));
			}
		}


		private class RootFileItem : FileItem
		{
			public RootFileItem(string path)
				: base(path, AUI.Path.Filename(path), true)
			{
				Text = Path;
			}
		}


		private class PackageItem : FileItem
		{
			public PackageItem(string path, string name)
				: base(path, name)
			{
			}
		}


		public delegate void Handler();
		public delegate void PathHandler(string path);

		public event PathHandler PathSelected;
		public event PathHandler PackageSelected;
		public event Handler PackagesFlattened;


		private readonly VUI.TreeView tree_;
		private readonly VUI.TreeView.Item root_;

		public FileTree(int fontSize)
		{
			tree_ = new VUI.TreeView();
			tree_.MinimumSize = new VUI.Size(500, 0);
			tree_.FontSize = fontSize;

			root_ = new VUI.TreeView.Item("VaM");
			tree_.RootItem.Add(root_);

			tree_.SelectionChanged += OnSelection;
		}

		public VUI.Widget Widget
		{
			get { return tree_; }
		}

		public void Update(string dir)
		{
			root_.Clear();

			if (string.IsNullOrEmpty(dir))
				return;

			var dirs = new List<string>(FileManagerSecure.GetDirectories(dir));
			U.NatSort(dirs);

			var sceneRoot = new RootFileItem("Saves/scene");
			foreach (var d in dirs)
				sceneRoot.Add(new FileItem(d, AUI.Path.Filename(d)));

			var varsRoot = new VUI.TreeView.Item("Packages");
			foreach (var p in FileManagerSecure.GetShortCutsForDirectory(dir))
			{
				if (string.IsNullOrEmpty(p.package))
					continue;

				if (!string.IsNullOrEmpty(p.packageFilter))
					continue;

				var item = new FileItem(p.path, p.package);
				item.Tooltip = $"path: {p.path}\npackage:{p.package}\npackageFilter:{p.packageFilter}";
				varsRoot.Add(item);
			}

			root_.Add(new VUI.TreeView.Item("All flattened"));
			root_.Add(new VUI.TreeView.Item("Packages flattened"));
			root_.Add(new RootFileItem("Saves/Downloads"));
			root_.Add(sceneRoot);
			root_.Add(varsRoot);

			root_.Expanded = true;
			root_.Children[4].Expanded = true;
		}

		private void OnSelection(VUI.TreeView.Item item)
		{
			var fi = item as FileItem;
			if (fi != null)
			{
				PathSelected?.Invoke(fi.Path);
			}
			else
			{
				var pi = item as PackageItem;
				if (pi != null)
				{
					PackageSelected?.Invoke(pi.Path);
				}
				else if (item.Text == "Packages")
				{
					PackagesFlattened?.Invoke();
				}
				else
				{
					PathSelected?.Invoke(null);
				}
			}
		}
	}
}
