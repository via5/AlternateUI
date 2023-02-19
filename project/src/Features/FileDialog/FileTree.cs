using MVR.FileManagementSecure;
using System.Collections.Generic;
using System.IO;

namespace AUI.FileDialog
{
	class FileTree
	{
		private abstract class FileTreeItem : VUI.TreeView.Item
		{
			protected FileTreeItem(string text)
				: base(text)
			{
			}

			public abstract void Activate(FileTree ft);
		}


		private class DirectoryItem : FileTreeItem
		{
			private readonly string path_;
			private bool checkedHasChildren_ = false;
			private bool hasChildren_ = false;

			public DirectoryItem(string path)
				: this(path, AUI.Path.Filename(path))
			{
			}

			protected DirectoryItem(string path, string display)
				: base(display)
			{
				path_ = path;
				Icons.GetDirectoryIcon(t => Icon = t);
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

			public override void Activate(FileTree ft)
			{
				ft.FirePathSelected(path_);
			}

			protected override void GetChildren()
			{
				var dirs = new List<string>(FileManagerSecure.GetDirectories(path_));
				U.NatSort(dirs);

				foreach (var d in dirs)
				{
					if (IncludeDir(d))
						Add(new DirectoryItem(d));
				}
			}

			protected virtual bool IncludeDir(string path)
			{
				return true;
			}
		}


		private class SavesRootItem : DirectoryItem
		{
			public SavesRootItem()
				: base("Saves")
			{
			}

			protected override bool IncludeDir(string path)
			{
				var lc = AUI.Path.Filename(path).ToLower();
				return (lc == "downloads" || lc == "scene");
			}
		}


		private class PackageItem : DirectoryItem
		{
			private readonly ShortCut sc_;

			public PackageItem(ShortCut sc)
				: base(sc.path, sc.package)
			{
				sc_ = sc;

				Tooltip = $"path: {sc.path}\npackage:{sc.package}\npackageFilter:{sc.packageFilter}";
				Icons.GetPackageIcon(t => Icon = t);
			}

			public override void Activate(FileTree ft)
			{
				ft.FirePackageSelected(sc_.path);
			}
		}


		class AllFlattenedItem : FileTreeItem
		{
			public AllFlattenedItem()
				: base("All flattened")
			{
				Icons.GetPackageIcon(t => Icon = t);
			}

			public override void Activate(FileTree ft)
			{
				ft.FireAllFlattened();
			}
		}


		class PackagesFlattenedItem : FileTreeItem
		{
			public PackagesFlattenedItem(string text)
				: base(text)
			{
				Icons.GetPackageIcon(t => Icon = t);
			}

			public override void Activate(FileTree ft)
			{
				ft.FirePackagesFlattened();
			}
		}


		class PackagesRootItem : FileTreeItem
		{
			private bool checkedHasChildren_ = false;
			private bool hasChildren_ = false;

			public PackagesRootItem()
				: base("Packages")
			{
				Icons.GetPackageIcon(t => Icon = t);
			}

			public override void Activate(FileTree ft)
			{
				ft.FirePackagesFlattened();
			}

			public override bool HasChildren
			{
				get
				{
					if (!checkedHasChildren_)
					{
						var scs = FileManagerSecure.GetShortCutsForDirectory("Saves/scene");
						hasChildren_ = (scs != null && scs.Count > 0);
						checkedHasChildren_ = true;
					}

					return hasChildren_;
				}
			}

			protected override void GetChildren()
			{
				foreach (var p in FileManagerSecure.GetShortCutsForDirectory("Saves/scene"))
				{
					if (string.IsNullOrEmpty(p.package))
						continue;

					if (!string.IsNullOrEmpty(p.packageFilter))
						continue;

					Add(new PackageItem(p));
				}
			}
		}


		public delegate void Handler();
		public delegate void PathHandler(string path);

		public event PathHandler PathSelected;
		public event PathHandler PackageSelected;
		public event Handler AllFlattened, PackagesFlattened;

		private readonly VUI.TreeView tree_;
		private readonly VUI.TreeView.Item root_;

		public FileTree(int fontSize)
		{
			tree_ = new VUI.TreeView();
			tree_.MinimumSize = new VUI.Size(500, 0);
			tree_.FontSize = fontSize;
			tree_.Icons = true;

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

			root_.Add(new AllFlattenedItem());
			root_.Add(new PackagesFlattenedItem("Packages flattened"));
			root_.Add(new SavesRootItem());
			root_.Add(new PackagesRootItem());

			root_.Expanded = true;
		}

		public void FirePathSelected(string path)
		{
			PathSelected?.Invoke(path);
		}

		public void FirePackageSelected(string path)
		{
			PackageSelected?.Invoke(path);
		}

		public void FireAllFlattened()
		{
			AllFlattened?.Invoke();
		}

		public void FirePackagesFlattened()
		{
			PackagesFlattened?.Invoke();
		}

		private void OnSelection(VUI.TreeView.Item item)
		{
			var fi = item as FileTreeItem;
			if (fi != null)
				fi.Activate(this);
		}
	}
}

