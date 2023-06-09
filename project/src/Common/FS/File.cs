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
			var name = new StringView(Name);

			if (cx != null && !string.IsNullOrEmpty(cx.RemovePrefix))
			{
				if (name.StartsWith(cx.RemovePrefix))
				{
					name = name.Substring(cx.RemovePrefix.Length);

					Log.Info($"'{name}'");
					if (name.EndsWith(".vap"))
					{
						Log.Info($"{name}, has vap");
						name = name.Substring(0, name.Length - 4);
						Log.Info($"after: {name}");
					}
				}
			}

			return name;
		}
	}
}
