using MVR.FileManagementSecure;
using System;

namespace AUI.FS
{
	using FMS = FileManagerSecure;

	class FSFile : BasicFilesystemObject, IFile
	{
		private readonly string name_;

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
			get { return FMS.FileCreationTime(MakeRealPath()); }
		}

		public override DateTime DateModified
		{
			get { return FMS.FileLastWriteTime(MakeRealPath()); }
		}

		public override bool CanPin
		{
			get { return false; }
		}

		public override bool Virtual
		{
			get { return false; }
		}

		public override bool IsFlattened
		{
			get { return false; }
		}

		public override Icon Icon
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
