using System;

namespace AUI.FS
{
	class FSFile : BasicFilesystemObject, IFile
	{
		private readonly string name_;
		private string displayName_ = null;
		private string displayNamePrefix_ = null;

		public FSFile(Filesystem fs, IFilesystemContainer parent, string name)
			: base(fs, parent)
		{
			name_ = name;
		}

		public override void ClearCache()
		{
			base.ClearCache();
			Icon?.ClearCache();
		}

		protected override string DoGetDebugName()
		{
			return "File";
		}

		public override string Name
		{
			get { return name_; }
		}

		protected override DateTime GetDateCreated()
		{
			return SysWrappers.FileCreationTime(this, MakeRealPath());
		}

		protected override DateTime GetDateModified()
		{
			return SysWrappers.FileLastWriteTime(this, MakeRealPath());
		}

		protected override VUI.Icon GetIcon()
		{
			return VUI.Icon.FromThumbnail(MakeRealPath());
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

		public override bool IsFile
		{
			get { return true; }
		}

		public override bool IsWritable
		{
			get { return false; }
		}

		public override string MakeRealPath()
		{
			string s = Name;

			if (Parent != null)
				s = Parent.MakeRealPath() + s;

			return s;
		}

		protected override string DoGetDisplayName(Context cx)
		{
			if (displayName_ == null || displayNamePrefix_ != cx.RemovePrefix)
			{
				var name = new StringView(name_);

				if (cx != null && !string.IsNullOrEmpty(cx.RemovePrefix))
				{
					if (name.StartsWith(cx.RemovePrefix))
					{
						name = name.Substring(cx.RemovePrefix.Length);

						if (name.EndsWith(".vap"))
							name = name.Substring(0, name.Length - 4);
					}
				}

				displayName_ = name;
				displayNamePrefix_ = cx.RemovePrefix;
			}

			return displayName_;
		}
	}
}
