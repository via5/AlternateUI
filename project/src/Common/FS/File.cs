using System;

namespace AUI.FS
{
	class FSFile : BasicFilesystemObject, IFile
	{
		private readonly string name_;

		public FSFile(Filesystem fs, IFilesystemContainer parent, string name)
			: base(fs, parent)
		{
			name_ = name;
		}

		public override void ClearCache()
		{
			base.ClearCache();
			Icons.ClearFileCache(MakeRealPath());
		}

		public override string DebugInfo()
		{
			return $"File({VirtualPath})";
		}

		public override string Name
		{
			get { return name_; }
		}

		protected override DateTime GetDateCreated()
		{
			return Sys.FileCreationTime(this, MakeRealPath());
		}

		protected override DateTime GetDateModified()
		{
			return Sys.FileLastWriteTime(this, MakeRealPath());
		}

		public override bool CanPin
		{
			get { return false; }
		}

		public override bool Virtual
		{
			get { return false; }
		}

		public override bool ChildrenVirtual
		{
			get { return false; }
		}

		public override bool IsFlattened
		{
			get { return false; }
		}

		public override bool IsInternal
		{
			get { return false; }
		}

		public override VUI.Icon Icon
		{
			get { return Icons.File(MakeRealPath()); }
		}

		public override string MakeRealPath()
		{
			string s = Name;

			if (Parent != null)
				s = Parent.MakeRealPath() + s;

			return s;
		}
	}
}
