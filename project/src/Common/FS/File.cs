using MVR.FileManagementSecure;
using System;

namespace AUI.FS
{
	using FMS = FileManagerSecure;

	class FSFile : IFile
	{
		private readonly Filesystem fs_;
		private readonly IFilesystemContainer parent_;
		private readonly string name_;
		private string displayName_ = null;

		public FSFile(Filesystem fs, IFilesystemContainer parent, string name)
		{
			fs_ = fs;
			parent_ = parent;
			name_ = name;
		}

		public override string ToString()
		{
			return $"File({VirtualPath})";
		}

		public IFilesystemContainer Parent
		{
			get { return parent_; }
		}

		public string Name
		{
			get { return name_; }
		}

		public string VirtualPath
		{
			get
			{
				string s = name_;

				var parent = Parent;
				while (parent != null)
				{
					s = parent.Name + "/" + s;
					parent = parent.Parent;
				}

				return s;
			}
		}

		public virtual string DisplayName
		{
			get
			{
				return displayName_ ?? Name;
			}

			set
			{
				displayName_ = value;
			}
		}

		public bool HasCustomDisplayName
		{
			get { return (displayName_ != null); }
		}

		public DateTime DateCreated
		{
			get { return FMS.FileCreationTime(VirtualPath); }
		}

		public DateTime DateModified
		{
			get { return FMS.FileLastWriteTime(VirtualPath); }
		}

		public bool CanPin
		{
			get { return false; }
		}

		public bool Virtual
		{
			get { return false; }
		}

		public bool IsFlattened
		{
			get { return false; }
		}

		public IPackage ParentPackage
		{
			get
			{
				IFilesystemObject o = this;

				while (o != null)
				{
					if (o is IPackage)
						return o as IPackage;

					o = o.Parent;
				}

				return null;
			}
		}

		public Icon Icon
		{
			get { return Icons.File(MakeRealPath()); }
		}

		public string MakeRealPath()
		{
			string s = Name;

			if (Parent != null)
				s = Parent.MakeRealPath() + s;

			return s;
		}

		public bool IsSameObject(IFilesystemObject o)
		{
			return fs_.IsSameObject(this, o);
		}
	}
}
