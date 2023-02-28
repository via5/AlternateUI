using System.Collections.Generic;

namespace AUI.FileDialog
{
	class FileTree
	{
		public delegate void ItemHandler(IFileTreeItem item);
		public event ItemHandler SelectionChanged;

		public const int NoFlags = 0x00;
		public const int FlattenDirectories = 0x01;
		public const int Writeable = 0x02;

		private readonly FileDialog fd_;
		private readonly VUI.TreeView tree_;
		private readonly FileTreeItem root_;
		//private readonly AllFlatItem allFlat_ = null;
		//private readonly PackagesFlatItem packagesFlat_ = null;
		//private readonly PinnedRootItem pinned_ = null;
		//private readonly SavesRootItem savesRoot_ = null;
		//private readonly PackagesRootItem packagesRoot_ = null;

		public FileTree(FileDialog fd, int fontSize, List<PinInfo> pins)
		{
			fd_ = fd;
			tree_ = new VUI.TreeView();

			tree_.FontSize = fontSize;
			tree_.Icons = true;
			tree_.DoubleClickToggle = true;
			tree_.LabelWrap = VUI.Label.Clip;
			tree_.Borders = new VUI.Insets(0);

			root_ = new FileTreeItem(this, FS.Instance.GetRootDirectory());
			tree_.RootItem.Add(root_);

			tree_.SelectionChanged += OnSelection;

			//allFlat_ = root_.Add(new AllFlatItem(this));
			//packagesFlat_ = root_.Add(new PackagesFlatItem(this));
			//pinned_ = root_.Add(new PinnedRootItem(this, pins));
			//savesRoot_ = root_.Add(new SavesRootItem(this));
			//packagesRoot_ = root_.Add(new PackagesRootItem(this));
			//
			root_.Expanded = true;
			//pinned_.Expanded = true;
		}

		public FileDialog FileDialog
		{
			get { return fd_; }
		}

		public VUI.Widget Widget
		{
			get { return tree_; }
		}

		public IFilesystemObject Selected
		{
			get
			{
				var fi = tree_.Selected as FileTreeItem;
				return fi?.Object;
			}
		}

		public void Enable()
		{
			FS.Instance.ObjectChanged += OnObjectChanged;
		}

		public void Disable()
		{
			FS.Instance.ObjectChanged -= OnObjectChanged;
		}

		public void SetFlags(int f)
		{
			if (root_.Children != null)
			{
				foreach (FileTreeItem item in root_.Children)
				{
					if (item != null)
						item.SetFlags(f);
				}
			}
		}

		private void OnObjectChanged(IFilesystemObject o)
		{
			var item = FindItem(o);
			if (item == null)
				return;

			item.Refresh();
		}

		public FileTreeItem FindItem(IFilesystemObject o)
		{
			return FindItem(tree_.RootItem, o);
		}

		private FileTreeItem FindItem(VUI.TreeView.Item parent, IFilesystemObject o)
		{
			var cs = parent.Children;

			if (cs != null)
			{
				for (int i=0; i<cs.Count; ++i)
				{
					var c = cs[i] as FileTreeItem;
					if (c.Object == o)
						return c;

					var r = FindItem(c, o);
					if (r != null)
						return r;
				}
			}

			return null;
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
			var o = fi?.Object as IFilesystemContainer;

			fd_.SetContainer(o ?? new NullDirectory());
			SelectionChanged?.Invoke(fi);
		}
	}
}

