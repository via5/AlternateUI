using MVR.FileManagementSecure;
using System.Collections.Generic;

namespace AUI.FileDialog
{
	class FileTree
	{
		public interface IFileTreeItem
		{
			string Type { get; }
			string Path { get; }
			bool CanPin { get; }
			bool Virtual { get; }
			bool IsFlattened { get; }

			void SetFlags(int f);
			IFileContainer CreateContainer();
		}

		private abstract class FileTreeItem : VUI.TreeView.Item, IFileTreeItem
		{
			private readonly FileTree tree_;

			protected FileTreeItem(FileTree tree, string text)
				: base(text)
			{
				tree_ = tree;
			}

			public FileTree FileTree
			{
				get { return tree_; }
			}

			public string FullPath
			{
				get
				{
					string s = Path;

					var parent = Parent as FileTreeItem;
					while (parent != null)
					{
						s = parent.Path + "/" + s;
						parent = parent.Parent as FileTreeItem;
					}

					return s;
				}
			}

			public abstract string Type { get; }
			public abstract string Path { get; }
			public abstract bool CanPin { get; }
			public abstract bool Virtual { get; }
			public abstract bool IsFlattened { get; }

			public virtual void SetFlags(int f)
			{
				bool visible = true;

				if (Virtual && Bits.IsSet(f, Writeable))
					visible = false;
				else if (IsFlattened && Bits.IsSet(f, FlattenDirectories))
					visible = false;

				Visible = visible;
			}

			public abstract IFileContainer CreateContainer();
		}


		private abstract class BasicDirectoryItem : FileTreeItem
		{
			private readonly File file_;
			private bool checkedHasChildren_ = false;
			private bool hasChildren_ = false;

			protected BasicDirectoryItem(FileTree tree, File f)
				: this(tree, f, f.Filename)
			{
			}

			protected BasicDirectoryItem(FileTree tree, File f, string display)
				: base(tree, display)
			{
				file_ = f;
				Icons.GetDirectoryIcon(t => Icon = t);
			}

			public File File
			{
				get { return file_; }
			}

			public override bool CanPin
			{
				get { return true; }
			}

			public override string Path
			{
				get { return File.Path; }
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

			public override IFileContainer CreateContainer()
			{
				return new DirectoryContainer(file_.Path);
			}

			protected override void GetChildren()
			{
				var dirs = Cache.GetDirectories(file_.Path, null);

				foreach (var d in dirs)
				{
					if (IncludeDir(d))
						Add(new DirectoryItem(FileTree, d));
				}
			}

			protected virtual bool IncludeDir(File f)
			{
				return true;
			}
		}


		class RootItem : FileTreeItem
		{
			public RootItem(FileTree tree)
				: base(tree, "VaM")
			{
				Icons.GetDirectoryIcon(t => Icon = t);
			}

			public override string Type
			{
				get { return "Root"; }
			}

			public override string Path
			{
				get { return ""; }
			}

			public override bool CanPin
			{
				get { return false; }
			}

			public override bool Virtual
			{
				get { return true; }
			}

			public override bool IsFlattened
			{
				get { return true; }
			}

			public override IFileContainer CreateContainer()
			{
				return new AllFlatContainer("", false);
			}
		}


		private class DirectoryItem : BasicDirectoryItem
		{
			public DirectoryItem(FileTree tree, File f)
				: base(tree, f)
			{
			}

			public static DirectoryItem FromPath(FileTree tree, string path)
			{
				if (!FileManagerSecure.DirectoryExists(path))
				{
					AlternateUI.Instance.Log.Error(
						$"directory item: path not found '{path}'");

					return null;
				}

				return new DirectoryItem(tree, new File(path));
			}

			public override string Type
			{
				get { return "Directory"; }
			}

			public override bool Virtual
			{
				get { return Cache.DirectoryInPackage(Path); }
			}

			public override bool IsFlattened
			{
				get { return false; }
			}
		}


		private class SavesRootItem : BasicDirectoryItem
		{
			public SavesRootItem(FileTree tree)
				: base(tree, new File(Cache.SavesRoot))
			{
			}

			public override string Type
			{
				get { return "SavesRoot"; }
			}

			public override bool Virtual
			{
				get { return false; }
			}

			public override bool IsFlattened
			{
				get { return false; }
			}

			protected override bool IncludeDir(File f)
			{
				var lc = f.Filename.ToLower();
				return (lc == "downloads" || lc == "scene");
			}
		}


		private class PackageItem : BasicDirectoryItem
		{
			private readonly ShortCut sc_;

			public PackageItem(FileTree tree, ShortCut sc)
				: base(tree, new File(sc.path), sc.package)
			{
				sc_ = sc;

				Tooltip = $"path: {sc.path}\npackage:{sc.package}\npackageFilter:{sc.packageFilter}";
				Icons.GetPackageIcon(t => Icon = t);
			}

			public static PackageItem FromPath(FileTree tree, string path)
			{
				var sc = Cache.GetPackage(path);
				if (sc == null)
				{
					AlternateUI.Instance.Log.Error(
						$"package item: path not found '{path}'");

					return null;
				}

				return new PackageItem(tree, sc);
			}

			public override string Type
			{
				get { return "Package"; }
			}

			public override bool Virtual
			{
				get { return true; }
			}

			public override bool IsFlattened
			{
				get { return false; }
			}

			public override IFileContainer CreateContainer()
			{
				return new PackageContainer(sc_);
			}
		}


		class AllFlatItem : FileTreeItem
		{
			public AllFlatItem(FileTree tree)
				: base(tree, "All flattened")
			{
				Icons.GetDirectoryIcon(t => Icon = t);
			}

			public override string Type
			{
				get { return "AllFlat"; }
			}

			public override string Path
			{
				get { return ""; }
			}

			public override bool CanPin
			{
				get { return true; }
			}

			public override bool Virtual
			{
				get { return true; }
			}

			public override bool IsFlattened
			{
				get { return true; }
			}

			public override IFileContainer CreateContainer()
			{
				return new AllFlatContainer("All flattened", true);
			}
		}


		class PackagesFlatItem : FileTreeItem
		{
			public PackagesFlatItem(FileTree tree)
				: base(tree, "Packages flattened")
			{
				Icons.GetPackageIcon(t => Icon = t);
			}

			public override string Type
			{
				get { return "PackagesFlat"; }
			}

			public override string Path
			{
				get { return ""; }
			}

			public override bool CanPin
			{
				get { return true; }
			}

			public override bool Virtual
			{
				get { return true; }
			}

			public override bool IsFlattened
			{
				get { return true; }
			}

			public override IFileContainer CreateContainer()
			{
				return new PackagesFlatContainer(true);
			}
		}


		class PackagesRootItem : FileTreeItem
		{
			private bool checkedHasChildren_ = false;
			private bool hasChildren_ = false;

			public PackagesRootItem(FileTree tree)
				: base(tree, "Packages")
			{
				Icons.GetPackageIcon(t => Icon = t);
			}

			public override string Type
			{
				get { return "Packages"; }
			}

			public override string Path
			{
				get { return ""; }
			}

			public override bool CanPin
			{
				get { return true; }
			}

			public override bool Virtual
			{
				get { return true; }
			}

			public override bool IsFlattened
			{
				get { return false; }
			}

			public override IFileContainer CreateContainer()
			{
				return new PackagesFlatContainer(false);
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

					Add(new PackageItem(FileTree, p));
				}
			}
		}


		class PinnedRootItem : FileTreeItem
		{
			private readonly PinnedFlatItem flat_;

			public PinnedRootItem(FileTree tree, List<PinInfo> pins)
				: base(tree, "Pinned")
			{
				Icons.GetPinnedIcon(t => Icon = t);
				flat_ = new PinnedFlatItem(tree, this);
				Add(flat_);

				foreach (var p in pins)
				{
					var pi = CreateItem(p.type, p.path);
					if (pi != null)
						Add(pi);
				}
			}

			public override string Type
			{
				get { return "PinnedRoot"; }
			}

			public override string Path
			{
				get { return ""; }
			}

			public override bool CanPin
			{
				get { return false; }
			}

			public override bool Virtual
			{
				get { return true; }
			}

			public override bool IsFlattened
			{
				get { return false; }
			}

			public void AddPinned(FileTreeItem item)
			{
				var newItem = CreateItem(item.Type, item.Path);
				if (newItem == null)
					return;

				Add(newItem);
			}

			public bool RemovePinned(FileTreeItem item)
			{
				var cs = Children;
				if (cs == null || cs.Count == 0)
					return false;

				if (cs.Contains(item))
				{
					Remove(item);
					return true;
				}

				foreach (IFileTreeItem c in cs)
				{
					if (c == null)
						continue;

					if (c.Type == item.Type && c.Path == item.Path)
					{
						Remove(c as VUI.TreeView.Item);
						return true;
					}
				}

				return false;
			}

			public bool HasPinned(IFileTreeItem item)
			{
				var cs = Children;
				if (cs == null || cs.Count == 0)
					return false;

				if (cs.Contains(item as VUI.TreeView.Item))
					return true;

				foreach (IFileTreeItem c in cs)
				{
					if (c == null)
						continue;

					if (c.Type == item.Type && c.Path == item.Path)
						return true;
				}

				return false;
			}

			public List<IFileTreeItem> GetPins()
			{
				var list = new List<IFileTreeItem>();

				var cs = Children;
				if (cs != null && cs.Count > 0)
				{
					foreach (IFileTreeItem c in cs)
					{
						if (c == null || c is PinnedFlatItem)
							continue;

						list.Add(c);
					}
				}

				return list;
			}

			public MergedContainer CreateMergedContainer(string text)
			{
				var mc = new MergedContainer(text);

				foreach (var p in GetPins())
				{
					var c = p.CreateContainer() as BasicFileContainer;
					if (c != null)
						mc.Add(c);
				}

				return mc;
			}

			public override void SetFlags(int f)
			{
				flat_.Visible = !Bits.IsSet(f, FlattenDirectories);

				var cs = Children;
				if (cs != null && cs.Count > 0)
				{
					foreach (IFileTreeItem c in cs)
					{
						if (c != null)
							c.SetFlags(f);
					}
				}
			}

			public override IFileContainer CreateContainer()
			{
				return CreateMergedContainer("Pinned");
			}

			private FileTreeItem CreateItem(string type, string path)
			{
				if (type == "Root")
					return new RootItem(FileTree);
				else if (type == "Directory")
					return DirectoryItem.FromPath(FileTree, path);
				else if (type == "SavesRoot")
					return new SavesRootItem(FileTree);
				else if (type == "Package")
					return PackageItem.FromPath(FileTree, path);
				else if (type == "AllFlat")
					return new AllFlatItem(FileTree);
				else if (type == "PackagesFlat")
					return new PackagesFlatItem(FileTree);
				else if (type == "Packages")
					return new PackagesRootItem(FileTree);

				AlternateUI.Instance.Log.Error($"bad file tree item type '{type}'");
				return null;
			}
		}


		class PinnedFlatItem : FileTreeItem
		{
			private readonly PinnedRootItem pinned_;

			public PinnedFlatItem(FileTree tree, PinnedRootItem pinned)
				: base(tree, "Pinned flattened")
			{
				pinned_ = pinned;
			}

			public override string Type
			{
				get { return "PinnedFlat"; }
			}

			public override string Path
			{
				get { return ""; }
			}

			public override bool CanPin
			{
				get { return false; }
			}

			public override bool Virtual
			{
				get { return true; }
			}

			public override bool IsFlattened
			{
				get { return true; }
			}

			public override IFileContainer CreateContainer()
			{
				return pinned_.CreateMergedContainer("Pinned flattened");
			}
		}


		public delegate void ItemHandler(IFileTreeItem item);
		public event ItemHandler SelectionChanged;

		public const int NoFlags = 0x00;
		public const int FlattenDirectories = 0x01;
		public const int Writeable = 0x02;


		private readonly FileDialog fd_;
		private readonly VUI.TreeView tree_;
		private readonly RootItem root_;
		private readonly AllFlatItem allFlat_ = null;
		private readonly PackagesFlatItem packagesFlat_ = null;
		private readonly PinnedRootItem pinned_ = null;
		private readonly SavesRootItem savesRoot_ = null;
		private readonly PackagesRootItem packagesRoot_ = null;

		public FileTree(FileDialog fd, int fontSize, List<PinInfo> pins)
		{
			fd_ = fd;
			tree_ = new VUI.TreeView();

			tree_.FontSize = fontSize;
			tree_.Icons = true;
			tree_.DoubleClickToggle = true;
			tree_.LabelWrap = VUI.Label.Clip;
			tree_.Borders = new VUI.Insets(0);

			root_ = new RootItem(this);
			tree_.RootItem.Add(root_);

			tree_.SelectionChanged += OnSelection;


			allFlat_ = root_.Add(new AllFlatItem(this));
			packagesFlat_ = root_.Add(new PackagesFlatItem(this));
			pinned_ = root_.Add(new PinnedRootItem(this, pins));
			savesRoot_ = root_.Add(new SavesRootItem(this));
			packagesRoot_ = root_.Add(new PackagesRootItem(this));

			root_.Expanded = true;
			pinned_.Expanded = true;
		}

		public FileDialog FileDialog
		{
			get { return fd_; }
		}

		public VUI.Widget Widget
		{
			get { return tree_; }
		}

		public void SetFlags(int f)
		{
			foreach (FileTreeItem item in root_.Children)
			{
				if (item != null)
					item.SetFlags(f);
			}
		}

		public List<IFileTreeItem> GetPins()
		{
			return pinned_.GetPins();
		}

		public void PinSelected(bool b)
		{
			var s = tree_.Selected as FileTreeItem;
			if (s == null)
				return;

			if (b)
				pinned_.AddPinned(s);
			else
				pinned_.RemovePinned(s);
		}

		public bool IsPinned(IFileTreeItem item)
		{
			if (item == null)
				return false;

			return pinned_.HasPinned(item);
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

		private void OnSelection(VUI.TreeView.Item item)
		{
			var fi = item as FileTreeItem;
			var c = fi?.CreateContainer();

			if (c == null)
				c = new EmptyContainer("");

			fd_.SetContainer(c);
			SelectionChanged?.Invoke(fi);
		}
	}
}

