﻿using System;
using System.Collections.Generic;

namespace AUI.FS
{
	public class FSDirectory : BasicFilesystemContainer, IDirectory
	{
		private string rp_ = null;

		public FSDirectory(Filesystem fs, IFilesystemContainer parent, string name)
			: base(fs, parent, name)
		{
		}

		protected override string DoGetDebugName()
		{
			return "FSDirectory";
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
			if (rp_ == null)
			{
				string s = Name + "/";

				if (Parent != null)
					s = Parent.MakeRealPath() + s;

				rp_ = s;
			}

			return rp_;
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
