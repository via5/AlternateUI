using System.Collections.Generic;

namespace AUI.FileDialog
{
	interface IFileTreeItem
	{
		FS.IFilesystemObject Object { get; }
		void SetFlags(int f);
	}


	class FileTreeItem : VUI.TreeView.Item, IFileTreeItem
	{
		private readonly FileTree tree_;
		private readonly string path_;

		private bool checkedHasChildren_ = false;
		private bool hasChildren_ = false;
		private FS.IFilesystemContainer o_ = null;

		public FileTreeItem(FileTree tree, FS.IFilesystemObject o)
			: base(o.DisplayName)
		{
			tree_ = tree;
			path_ = o.VirtualPath;
			o.Icon.GetTexture(t => Icon = t);
		}

		public override string ToString()
		{
			return $"FileTreeItem({path_})";
		}

		protected override string GetTooltip()
		{
			return Object?.Tooltip ?? "";
		}

		public FileTree FileTree
		{
			get { return tree_; }
		}

		public FS.IFilesystemObject Object
		{
			get { return GetFSObject(); }
		}

		public string Path
		{
			get { return path_; }
		}

		public FS.IFilesystemContainer GetFSObject(int moreFlags = 0)
		{
			if (o_ == null)
			{
				o_ = FS.Filesystem.Instance.Resolve<FS.IFilesystemContainer>(
					CreateContext(), path_, FS.Filesystem.ResolveDefault | moreFlags);
			}

			return o_;
		}

		public virtual void SetFlags(int f)
		{
			bool visible = true;

			var o = GetFSObject();
			if (o == null)
				return;

			if (o.ChildrenVirtual && Bits.IsSet(f, FileTree.Writeable))
				visible = false;
			else if (o.IsFlattened && Bits.IsAnySet(f, FileTree.FlattenDirectories | FileTree.Writeable))
				visible = false;

			Visible = visible;
		}

		public void Refresh(bool recursive)
		{
			o_ = null;

			if (!checkedHasChildren_)
				return;

			checkedHasChildren_ = false;

			if (!Expanded)
			{
				Clear();
				UpdateToggle();
				return;
			}

			var o = GetFSObject();
			if (o == null || !HasChildren)
			{
				Clear();
				return;
			}

			var dirs = GetSubDirectories(o);

			{
				var cs = GetInternalChildren();

				if (cs != null)
				{
					int i = 0;
					while (i < cs.Count)
					{
						bool found = false;
						var c = cs[i] as FileTreeItem;
						var co = c.GetFSObject();

						if (co != null)
						{
							for (int j = 0; j < dirs.Count; ++j)
							{
								if (co.IsSameObject(dirs[j]))
								{
									found = true;
									break;
								}
							}
						}

						if (!found)
						{
							Remove(c);
						}
						else
						{
							if (recursive)
								c.Refresh(true);

							++i;
						}
					}
				}
			}

			{
				var cs = GetInternalChildren();

				for (int i = 0; i < dirs.Count; ++i)
				{
					if (i >= cs.Count || !(cs[i] as FileTreeItem).GetFSObject().IsSameObject(dirs[i]))
						Insert(i, new FileTreeItem(tree_, dirs[i]));
				}
			}
		}

		public override bool HasChildren
		{
			get
			{
				if (!checkedHasChildren_)
				{
					var d = GetFSObject();
					hasChildren_ = (d != null && !d.IsFlattened && HasSubDirectories(d));
					checkedHasChildren_ = true;
				}

				return hasChildren_;
			}
		}

		protected override void GetChildren()
		{
			var d = GetFSObject();

			if (d != null && !d.IsFlattened)
			{
				foreach (var c in GetSubDirectories(d))
					Add(new FileTreeItem(tree_, c));
			}
		}

		private List<FS.IFilesystemContainer> GetSubDirectories(FS.IFilesystemContainer o)
		{
			return o.GetDirectories(CreateContext());
		}

		private bool HasSubDirectories(FS.IFilesystemContainer o)
		{
			return o.HasDirectories(CreateContext());
		}

		private FS.Context CreateContext()
		{
			return FileTree.FileDialog.CreateTreeContext(false);
		}
	}
}
