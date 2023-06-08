using System;
using System.Collections.Generic;
using System.Linq;

namespace AUI.FS
{
	class FSDirectory : BasicFilesystemContainer, IDirectory
	{
		public FSDirectory(Filesystem fs, IFilesystemContainer parent, string name)
			: base(fs, parent, name)
		{
		}

		public override string DebugInfo()
		{
			return $"FSDirectory({VirtualPath})";
		}

		protected override DateTime GetDateCreated()
		{
			return SysWrappers.DirectoryCreationTime(this, MakeRealPath());
		}

		protected override DateTime GetDateModified()
		{
			return SysWrappers.DirectoryLastWriteTime(this, MakeRealPath());
		}

		protected override VUI.Icon GetIcon()
		{
			return Icons.GetIcon(Icons.Directory);
		}

		public override bool CanPin
		{
			get { return true; }
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

		public override bool IsWritable
		{
			get { return true; }
		}

		public override string MakeRealPath()
		{
			string s = Name + "/";

			if (Parent != null)
				s = Parent.MakeRealPath() + s;

			return s;
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			var list = new List<IFilesystemContainer>();

			Instrumentation.Start(I.BasicDoGetDirectories);
			{
				var path = MakeRealPath();

				if (!string.IsNullOrEmpty(path))
				{
					var dirs = SysWrappers.GetDirectories(this, path);

					foreach (var dirPath in dirs)
					{
						var vd = new VirtualDirectory(fs_, this, Path.Filename(dirPath));

						vd.Add(new FSDirectory(fs_, this, Path.Filename(dirPath)));

						list.Add(vd);
					}
				}
			}
			Instrumentation.End();

			return list;
		}
	}
}
