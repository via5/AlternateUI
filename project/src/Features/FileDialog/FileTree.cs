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

			var fsRoot = FS.Filesystem.Instance.GetRoot();

			root_ = new FileTreeItem(this, fsRoot);
			tree_.RootItem.Add(root_);

			tree_.SelectionChanged += OnSelection;
		}

		public VUI.TreeView TreeView
		{
			get { return tree_; }
		}

		public void Init()
		{
			var fsRoot = FS.Filesystem.Instance.GetRoot();

			root_.Expanded = true;
			Expand(fsRoot.PinnedRoot);
			Expand(fsRoot.Saves);
		}

		private void Expand(FS.IFilesystemContainer o)
		{
			var list = new List<FS.IFilesystemContainer>();

			var p = o;
			while (p != null)
			{
				list.Add(p);
				p = p.Parent;
			}

			list.Reverse();

			Expand(root_, list, 0);
		}

		private bool Expand(FileTreeItem parent, List<FS.IFilesystemContainer> list, int index)
		{
			if (index >= list.Count || parent.Object != list[index])
				return false;

			parent.Expanded = true;

			if (index + 1 == list.Count)
				return true;

			var cs = parent.GetInternalChildren();
			if (cs != null)
			{
				foreach (var c in cs)
				{
					if (Expand(c as FileTreeItem, list, index + 1))
						return true;
				}
			}

			return false;
		}

		public FileDialog FileDialog
		{
			get { return fd_; }
		}

		public VUI.Widget Widget
		{
			get { return tree_; }
		}

		public FS.Context CreateContext(bool recursive)
		{
			return fd_.CreateFileContext(recursive);
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
			root_.Refresh(true);
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

			FS.Instrumentation.Start(FS.I.FTRefreshOnObjectchanged);
			{
				item.Refresh(false);
			}
			FS.Instrumentation.End();
		}

		private FileTreeItem FindItem(FS.IFilesystemObject o)
		{
			FileTreeItem item;

			FS.Instrumentation.Start(FS.I.FTFindItem);
			{
				item = FindItem(tree_.RootItem, o);
			}
			FS.Instrumentation.End();

			return item;
		}

		private FileTreeItem FindItem(VUI.TreeView.Item parent, FS.IFilesystemObject o)
		{
			var cs = parent.GetInternalChildren();

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
				var fi = i as FileTreeItem;
				if (fi == null)
					continue;

				var o = fi.Object;

				if (o.Name == cs[0])
				{
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

