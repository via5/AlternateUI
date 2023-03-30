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

			root_ = new FileTreeItem(this, FS.Filesystem.Instance.GetRoot());
			tree_.RootItem.Add(root_);
			tree_.SelectionChanged += OnSelection;
		}

		public VUI.TreeView TreeView
		{
			get { return tree_; }
		}

		public Logger Log
		{
			get { return fd_.Log; }
		}

		public void Init()
		{
			root_.Expanded = true;

			var fsRoot = FS.Filesystem.Instance.GetRoot();
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

		public FS.IFilesystemContainer SelectedRootInPinned
		{
			get
			{
				var s = tree_.Selected as FileTreeItem;
				var pinnedRoot = FS.Filesystem.Instance.GetPinnedRoot();

				while (s != null)
				{
					var parent = s.Parent as FileTreeItem;
					if (parent == null)
						break;

					var po = parent.Object;
					if (po != null && po.IsSameObject(pinnedRoot))
						return s.Object as FS.IFilesystemContainer;

					s = s.Parent as FileTreeItem;
				}

				return null;
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

		public void SearchPackages(string s)
		{
			FS.Filesystem.Instance.GetRoot().ClearImmediateChildrenCache();
			FS.Filesystem.Instance.GetPackagesRoot().ClearPackagesCache();

			if (!string.IsNullOrEmpty(s))
				Expand(FS.Filesystem.Instance.GetPackagesRoot());

			Refresh();
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
			string path, int flags,
			int scrollTo = VUI.TreeView.ScrollToNearest)
		{
			bool debug = Bits.IsSet(flags, FileDialog.SelectDirectoryDebug);
			bool expand = Bits.IsSet(flags, FileDialog.SelectDirectoryExpand);

			path = path.Replace('\\', '/');
			path = path.Trim();

			var cs = new List<string>(path.Split('/'));
			if (cs.Count == 0)
				return false;

			if (debug)
				Log.Info($"selecting '{path}': {string.Join(";", cs.ToArray())}");

			return Select(tree_.RootItem, cs, expand, scrollTo, debug);
		}

		public bool SelectInPinned(
			string path, string pinnedRoot, int flags,
			int scrollTo = VUI.TreeView.ScrollToNearest)
		{
			bool debug = Bits.IsSet(flags, FileDialog.SelectDirectoryDebug);
			bool expand = Bits.IsSet(flags, FileDialog.SelectDirectoryExpand);

			path = path.Replace('\\', '/');
			path = path.Trim();
			if (!path.EndsWith("/"))
				path += "/";

			pinnedRoot = pinnedRoot.Replace('\\', '/');
			pinnedRoot = pinnedRoot.Trim();
			if (!pinnedRoot.EndsWith("/"))
				pinnedRoot += "/";

			if (debug)
			{
				Log.Info(
					$"SelectInPinned: path={path} pinnedRoot={pinnedRoot}");
			}

			if (!path.StartsWith(pinnedRoot))
			{
				if (debug)
				{
					Log.Info(
						$"SelectInPinned: path '{path}' doesn't start with " +
						$"pinned root '{pinnedRoot}'");
				}

				return false;
			}

			var fs = FS.Filesystem.Instance;
			var rpath = path.Substring(pinnedRoot.Length);

			if (pinnedRoot.EndsWith("/"))
				pinnedRoot = pinnedRoot.Substring(0, pinnedRoot.Length - 1);

			if (rpath.EndsWith("/"))
				rpath = rpath.Substring(0, rpath.Length - 1);

			foreach (var o in fs.GetPinnedRoot().Pinned)
			{
				if (o.VirtualPath == pinnedRoot)
				{
					if (debug)
						Log.Info($"SelectInPinned: got root {o}, rpath={rpath}");

					var cs = new List<string>(rpath.Split('/'));

					if (debug)
						Log.Info($"SelectInPinned: cs={cs.Count}");

					while (cs.Count > 0 && cs[0].Trim() == "")
						cs.RemoveAt(0);

					var oi = FindItem(o);
					if (oi == null)
					{
						Log.Error(
							$"SelectInPinned: can't find object {o}");

						return false;
					}

					if (cs.Count == 0)
					{
						tree_.Select(oi, true, scrollTo);

						if (expand && tree_.Selected != null)
							tree_.Selected.Expanded = true;

						return true;
					}

					return Select(oi, cs, expand, scrollTo, debug);
				}
				else
				{
					if (debug)
					{
						Log.Info(
							$"SelectInPinned: o.vp {o.VirtualPath} " +
							$"is not {pinnedRoot}");
					}
				}
			}

			return false;
		}

		private bool Select(
			VUI.TreeView.Item parent, List<string> cs,
			bool expand, int scrollTo, bool debug)
		{
			while (cs.Count > 0 && cs[0] == "")
				cs.RemoveAt(0);

			if (cs.Count == 0)
			{
				if (debug)
					Log.Info($"{cs}: {parent} is final");

				tree_.Select(parent, true, scrollTo);

				if (expand && tree_.Selected != null)
					tree_.Selected.Expanded = true;

				return true;
			}

			if (parent.Children == null)
			{
				if (debug)
					Log.Info($"{cs}: {parent} has no children");

				return false;
			}

			if (debug)
				Log.Info($"{cs}: select parent={parent}");


			foreach (var i in parent.Children)
			{
				var fi = i as FileTreeItem;
				if (fi == null)
					continue;

				var o = fi.Object;
				if (o == null)
				{
					if (debug)
						Log.Info($"{cs}: {fi} has null object, so not '{cs[0]}'");
				}

				if (o.Name == cs[0])
				{
					if (debug)
						Log.Info($"{cs}: found {cs[0]} {o}");

					if (fi != null)
					{
						if (cs.Count == 1)
						{
							if (debug)
								Log.Info($"{cs}: {fi} is final");

							tree_.Select(fi, true, scrollTo);

							if (expand && tree_.Selected != null)
								tree_.Selected.Expanded = true;

							return true;
						}
						else
						{
							if (debug)
								Log.Info($"{cs}: going into {fi}");

							cs.RemoveAt(0);

							if (Select(fi, cs, expand, scrollTo, debug))
								return true;
						}
					}

					break;
				}
				else
				{
					if (debug)
						Log.Info($"{cs}: '{o.Name}' != '{cs[0]}'");
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

