using MVR.FileManagementSecure;
using System.Collections.Generic;

namespace AUI.FileDialog
{
	interface IFileTreeItem
	{
		string Type { get; }
		string Path { get; }
		bool CanPin { get; }
		bool Virtual { get; }
		bool IsFlattened { get; }

		void SetFlags(int f);
		IFileContainer CreateContainer();
	}

	abstract class FileTreeItem : VUI.TreeView.Item, IFileTreeItem
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

			if (Virtual && Bits.IsSet(f, FileTree.Writeable))
				visible = false;
			else if (IsFlattened && Bits.IsSet(f, FileTree.FlattenDirectories))
				visible = false;

			Visible = visible;
		}

		public abstract IFileContainer CreateContainer();
	}


	abstract class BasicDirectoryItem : FileTreeItem
	{
		private readonly IFile file_;
		private bool checkedHasChildren_ = false;
		private bool hasChildren_ = false;

		protected BasicDirectoryItem(FileTree tree, IFile f)
			: this(tree, f, f.Name)
		{
		}

		protected BasicDirectoryItem(FileTree tree, IFile f, string display)
			: base(tree, display)
		{
			file_ = f;
			Icons.GetDirectoryIcon(t => Icon = t);
		}

		public IFile File
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

		protected virtual bool IncludeDir(IFile f)
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


	class DirectoryItem : BasicDirectoryItem
	{
		public DirectoryItem(FileTree tree, IFile f)
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


	class SavesRootItem : BasicDirectoryItem
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

		protected override bool IncludeDir(IFile f)
		{
			var lc = f.Name.ToLower();
			return (lc == "downloads" || lc == "scene");
		}
	}


	class PackageItem : BasicDirectoryItem
	{
		private readonly IFile p_;

		public PackageItem(FileTree tree, IFile p)
			: base(tree, new File(p.Path), p.Name)
		{
			p_ = p;

			Tooltip = $"path: {p_.Path}\npackage:{p_.Name}";
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
			return new PackageContainer(p_);
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
			foreach (Package p in Cache.GetPackages(Cache.ScenesRoot, null))
			{
				if (string.IsNullOrEmpty(p.ShortCut.package))
					continue;

				if (!string.IsNullOrEmpty(p.ShortCut.packageFilter))
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
			flat_.Visible = !Bits.IsSet(f, FileTree.FlattenDirectories);

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
}
