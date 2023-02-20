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
			private readonly File file_;
			private bool checkedHasChildren_ = false;
			private bool hasChildren_ = false;

			public DirectoryItem(File f)
				: this(f, f.Filename)
			{
			}

			protected DirectoryItem(File f, string display)
				: base(display)
			{
				file_ = f;
				Icons.GetDirectoryIcon(t => Icon = t);
			}

			public File File
			{
				get { return file_; }
			}

			public override bool HasChildren
			{
				get
				{
					if (!checkedHasChildren_)
					{
						hasChildren_ = Cache.HasDirectories(file_.Path, null);
						checkedHasChildren_ = true;
					}

					return hasChildren_;
				}
			}

			public override void Activate(FileTree ft)
			{
				ft.FirePathSelected(file_.Path);
			}

			protected override void GetChildren()
			{
				var dirs = Cache.GetDirectories(file_.Path, null);

				foreach (var d in dirs)
				{
					if (IncludeDir(d))
						Add(new DirectoryItem(d));
				}
			}

			protected virtual bool IncludeDir(File f)
			{
				return true;
			}
		}


		private class SavesRootItem : DirectoryItem
		{
			public SavesRootItem()
				: base(new File(Cache.SavesRoot))
			{
			}

			protected override bool IncludeDir(File f)
			{
				var lc = f.Filename.ToLower();
				return (lc == "downloads" || lc == "scene");
			}
		}


		private class PackageItem : DirectoryItem
		{
			private readonly ShortCut sc_;

			public PackageItem(ShortCut sc)
				: base(new File(sc.path), sc.package)
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
						hasChildren_ = Cache.HasPackages(Cache.ScenesRoot);
						checkedHasChildren_ = true;
					}

					return hasChildren_;
				}
			}

			protected override void GetChildren()
			{
				foreach (var p in Cache.GetPackages(Cache.ScenesRoot))
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
		private readonly AllFlatItem allFlat_ = null;
		private readonly PackagesFlatItem packagesFlat_ = null;
		private readonly SavesRootItem savesRoot_ = null;
		private readonly PackagesRootItem packagesRoot_ = null;
		private bool flatten_ = false;

		public FileTree(int fontSize)
		{
			tree_ = new VUI.TreeView();

			tree_.MinimumSize = new VUI.Size(500, 0);
			tree_.FontSize = fontSize;
			tree_.Icons = true;
			tree_.DoubleClickToggle = true;
			tree_.LabelWrap = VUI.Label.Clip;
			tree_.Borders = new VUI.Insets(0);

			root_ = new RootItem();
			tree_.RootItem.Add(root_);

			tree_.SelectionChanged += OnSelection;


			allFlat_ = root_.Add(new AllFlatItem("All flattened"));
			packagesFlat_ = root_.Add(new PackagesFlatItem("Packages flattened"));
			savesRoot_ = root_.Add(new SavesRootItem());
			packagesRoot_ = root_.Add(new PackagesRootItem());

			root_.Expanded = true;
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

		public bool Select(string path)
		{
			path = path.Replace('\\', '/');
			path = path.Trim();

			var cs = new List<string>(path.Split('/'));
			if (cs.Count == 0)
				return false;

			return Select(tree_.RootItem, cs);
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

		public void Set(bool write)
		{
			allFlat_.Visible = !write;
			packagesFlat_.Visible = !write;
			savesRoot_.Visible = true;
			packagesRoot_.Visible = !write;
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

