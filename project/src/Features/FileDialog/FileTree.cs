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

		public FileTree(FileDialog fd, int fontSize)
		{
			fd_ = fd;
			tree_ = new VUI.TreeView();

			tree_.FontSize = fontSize;
			tree_.Icons = true;
			tree_.DoubleClickToggle = true;
			tree_.LabelWrap = VUI.Label.Clip;
			tree_.Borders = new VUI.Insets(0);

			var fsRoot = FS.Filesystem.Instance.GetRootDirectory();

			root_ = new FileTreeItem(this, fsRoot);
			tree_.RootItem.Add(root_);

			tree_.SelectionChanged += OnSelection;

			root_.Expanded = true;
			Expand(fsRoot.PinnedRoot);
			Expand(fsRoot.Saves);
		}

		private void Expand(FS.IFilesystemContainer o, bool b = true)
		{
			var i = FindItem(o);
			if (i != null)
				i.Expanded = b;
		}

		public FileDialog FileDialog
		{
			get { return fd_; }
		}

		public VUI.Widget Widget
		{
			get { return tree_; }
		}

		public bool CanGoUp()
		{
			return (tree_.Selected != root_);
		}

		public void Up()
		{
			var p = tree_.Selected?.Parent;
			if (p != null && p != tree_.RootItem)
				tree_.Select(p, true, VUI.TreeView.ScrollToNearest);
		}

		public FS.IFilesystemObject Selected
		{
			get
			{
				var fi = tree_.Selected as FileTreeItem;
				return fi?.Object;
			}
		}

		public void Enable()
		{
			FS.Filesystem.Instance.ObjectChanged += OnObjectChanged;
		}

		public void Disable()
		{
			FS.Filesystem.Instance.ObjectChanged -= OnObjectChanged;
		}

		public void Refresh()
		{
			if (root_.Children != null)
			{
				foreach (FileTreeItem item in root_.Children)
					item.Refresh(true);
			}
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

		private void OnObjectChanged(FS.IFilesystemObject o)
		{
			var item = FindItem(o);
			if (item == null)
				return;

			item.Refresh(false);
		}

		public FileTreeItem FindItem(FS.IFilesystemObject o)
		{
			return FindItem(tree_.RootItem, o);
		}

		private FileTreeItem FindItem(VUI.TreeView.Item parent, FS.IFilesystemObject o)
		{
			var cs = parent.Children;

			if (cs != null)
			{
				for (int i=0; i<cs.Count; ++i)
				{
					var c = cs[i] as FileTreeItem;
					if (c.Object.IsSameObject(o))
						return c;

					var r = FindItem(c, o);
					if (r != null)
						return r;
				}
			}

			return null;
		}

		public bool Select(
			string path, bool expand = false,
			int scrollTo = VUI.TreeView.ScrollToNearest)
		{
			path = path.Replace('\\', '/');
			path = path.Trim();

			var cs = new List<string>(path.Split('/'));
			if (cs.Count == 0)
				return false;

			return Select(tree_.RootItem, cs, expand, scrollTo);
		}

		private bool Select(
			VUI.TreeView.Item parent, List<string> cs, bool expand, int scrollTo)
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
							tree_.Select(fi, true, scrollTo);

							if (expand && tree_.Selected != null)
								tree_.Selected.Expanded = true;

							return true;
						}
						else
						{
							cs.RemoveAt(0);
							if (Select(fi, cs, expand, scrollTo))
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
			SelectionChanged?.Invoke(item as FileTreeItem);
		}
	}
}

