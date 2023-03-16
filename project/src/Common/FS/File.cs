using System;

namespace AUI.FS
{
	class FSFile : BasicFilesystemObject, IFile
	{
		private readonly string name_;
		private DateTime dateCreated_ = DateTime.MaxValue;
		private DateTime dateModified_ = DateTime.MaxValue;

		public FSFile(Filesystem fs, IFilesystemContainer parent, string name)
			: base(fs, parent)
		{
			name_ = name;
		}

		public override string ToString()
		{
			return $"File({VirtualPath})";
		}

		public override string Name
		{
			get { return name_; }
		}

		public override DateTime DateCreated
		{
			get
			{
				if (dateCreated_ == DateTime.MaxValue)
					dateCreated_ = Sys.FileCreationTime(this, MakeRealPath());

				return dateCreated_;
			}
		}

		public override DateTime DateModified
		{
			get
			{
				if (dateModified_ == DateTime.MaxValue)
					dateModified_ = Sys.FileLastWriteTime(this, MakeRealPath());

				return dateModified_;
			}
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

		public override void ClearCache()
		{
			base.ClearCache();
			dateCreated_ = DateTime.MaxValue;
			dateModified_ = DateTime.MaxValue;
		}
	}
}
