namespace AUI.FileDialog
{
	interface IFileTreeItem
	{
		IFilesystemObject Object { get; }
		void SetFlags(int f);
	}


	class FileTreeItem : VUI.TreeView.Item, IFileTreeItem
	{
		private readonly FileTree tree_;
		private readonly IFilesystemObject o_;

		private bool checkedHasChildren_ = false;
		private bool hasChildren_ = false;

		public FileTreeItem(FileTree tree, IFilesystemObject o)
			: base(o.DisplayName)
		{
			tree_ = tree;
			o_ = o;
			o_.Icon.GetTexture(t => Icon = t);
		}

		public FileTree FileTree
		{
			get { return tree_; }
		}

		public IFilesystemObject Object
		{
			get { return o_; }
		}

		public virtual void SetFlags(int f)
		{
			bool visible = true;

			if (o_.Virtual && Bits.IsSet(f, FileTree.Writeable))
				visible = false;
			else if (o_.IsFlattened && Bits.IsSet(f, FileTree.FlattenDirectories))
				visible = false;

			Visible = visible;
		}

		public void Refresh()
		{
			if (!checkedHasChildren_)
				return;

			checkedHasChildren_ = false;
			if (!HasChildren)
			{
				Clear();
				return;
			}

			var dirs = (o_ as IFilesystemContainer).GetSubDirectories(CreateFilter());

			{
				int i = 0;
				while (i < Children.Count)
				{
					bool found = false;

					for (int j = 0; j < dirs.Count; ++j)
					{
						if (Children[i] == dirs[j])
						{
							found = true;
							break;
						}
					}

					if (!found)
						Remove(Children[i]);
					else
						++i;
				}
			}

			for (int i = 0; i < dirs.Count; ++i)
			{
				if (i >= Children.Count || Children[i] != dirs[i])
					Insert(i, new FileTreeItem(tree_, dirs[i]));
			}
		}

		public override bool HasChildren
		{
			get
			{
				if (!checkedHasChildren_)
				{
					var d = (o_ as IFilesystemContainer);
					hasChildren_ = (d != null && d.HasSubDirectories(null));
					checkedHasChildren_ = true;
				}

				return hasChildren_;
			}
		}

		protected override void GetChildren()
		{
			var d = (o_ as IFilesystemContainer);

			if (d != null)
			{
				foreach (var c in d.GetSubDirectories(CreateFilter()))
					Add(new FileTreeItem(tree_, c));
			}
		}

		private Filter CreateFilter()
		{
			return new Filter("", null, Filter.SortFilename, Filter.SortAscending);
		}
	}
}
