using MVR.FileManagementSecure;
using System.Collections.Generic;

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
				ft.FirePackageSelected(sc_);
			}
		}


		class RootItem : FileTreeItem
		{
			public RootItem()
				: base("VaM")
			{
				Icons.GetDirectoryIcon(t => Icon = t);
			}

			public override void Activate(FileTree ft)
			{
				if (ft.FlattenDirectories)
					ft.FireAllFlat();
				else
					ft.FireNothingSelected();
			}
		}


		class AllFlatItem : FileTreeItem
		{
			public AllFlatItem(string text)
				: base(text)
			{
				Icons.GetDirectoryIcon(t => Icon = t);
			}

			public override void Activate(FileTree ft)
			{
				ft.FireAllFlat();
			}
		}


		class PackagesFlatItem : FileTreeItem
		{
			public PackagesFlatItem(string text)
				: base(text)
			{
				Icons.GetPackageIcon(t => Icon = t);
			}

			public override void Activate(FileTree ft)
			{
				ft.FirePackagesFlat();
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
				if (ft.FlattenDirectories)
					ft.FirePackagesFlat();
				else
					ft.FireNothingSelected();
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
		public delegate void PackageHandler(ShortCut sc);

		public event PathHandler PathSelected;
		public event PackageHandler PackageSelected;
		public event Handler AllFlatSelected, PackagesFlatSelected, NothingSelected;

		private readonly VUI.TreeView tree_;
		private readonly RootItem root_;
		private AllFlatItem allFlat_ = null;
		private PackagesFlatItem packagesFlat_ = null;
		private bool flatten_ = false;

		public FileTree(int fontSize)
		{
			tree_ = new VUI.TreeView();
			tree_.MinimumSize = new VUI.Size(500, 0);
			tree_.FontSize = fontSize;
			tree_.Icons = true;

			root_ = new RootItem();
			tree_.RootItem.Add(root_);

			tree_.SelectionChanged += OnSelection;

		}

		public VUI.Widget Widget
		{
			get { return tree_; }
		}

		public bool FlattenDirectories
		{
			get
			{
				return flatten_;
			}

			set
			{
				flatten_ = value;
				allFlat_.Visible = !value;
				packagesFlat_.Visible = !value;
			}
		}

		public void Select(string path)
		{
			path = path.Replace('\\', '/');
			path = path.Trim();

			var cs = new List<string>(path.Split('/'));
			if (cs.Count == 0)
			{
				FireNothingSelected();
				return;
			}

			Select(tree_.RootItem, cs);
		}

		private bool Select(VUI.TreeView.Item parent, List<string> cs)
		{
			if (parent.Children == null)
				return false;

			foreach (var i in parent.Children)
			{
				if (i.Text == cs[0])
				{
					var fi = i as FileTreeItem;
					if (fi != null)
					{
						if (cs.Count == 1)
						{
							tree_.Select(fi);
							return true;
						}
						else
						{
							cs.RemoveAt(0);
							if (Select(fi, cs))
								return true;
						}
					}

					break;
				}
			}

			return false;
		}

		public void Rebuild()
		{
			root_.Clear();

			allFlat_ = root_.Add(new AllFlatItem("All flattened"));
			packagesFlat_ = root_.Add(new PackagesFlatItem("Packages flattened"));
			root_.Add(new SavesRootItem());
			root_.Add(new PackagesRootItem());

			root_.Expanded = true;
		}

		public void FireNothingSelected()
		{
			NothingSelected?.Invoke();
		}

		public void FirePathSelected(string path)
		{
			PathSelected?.Invoke(path);
		}

		public void FirePackageSelected(ShortCut sc)
		{
			PackageSelected?.Invoke(sc);
		}

		public void FireAllFlat()
		{
			AllFlatSelected?.Invoke();
		}

		public void FirePackagesFlat()
		{
			PackagesFlatSelected?.Invoke();
		}

		private void OnSelection(VUI.TreeView.Item item)
		{
			var fi = item as FileTreeItem;
			if (fi != null)
				fi.Activate(this);
		}
	}
}

