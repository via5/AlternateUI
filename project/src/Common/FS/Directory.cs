﻿using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	using FMS = FileManagerSecure;

	class VirtualDirectory : BasicFilesystemContainer, IDirectory
	{
		private List<IFilesystemContainer> dirs_ = null;
		private List<IFilesystemContainer> sortedDirs_ = null;

		public VirtualDirectory(
			Filesystem fs, IFilesystemContainer parent,
			IFilesystemContainer content)
				: this(fs, parent, content.Name)
		{
			if (content != null)
				Add(content);
		}

		public VirtualDirectory(
			Filesystem fs, IFilesystemContainer parent, string name)
				: base(fs, parent, name)
		{
		}

		public override string ToString()
		{
			return $"VirtualDirectory({VirtualPath})";
		}

		protected override string GetDisplayName()
		{
			string s = "";

			if (dirs_ == null || dirs_.Count == 0)
				s += Name;
			else
				s += dirs_[0].Name;

			return s;
		}

		public override string Tooltip
		{
			get
			{
				var s = base.Tooltip;

				if (dirs_ != null && dirs_.Count > 0)
				{
					string dirSources = MakeTooltip(SortedDirs(), 0, U.DevMode);
					s += $"\nSources:{dirSources}";
				}

				return s;
			}
		}

		private List<IFilesystemContainer> SortedDirs()
		{
			if (sortedDirs_ == null)
			{
				sortedDirs_ = new List<IFilesystemContainer>(dirs_);
				sortedDirs_.Sort((a, b) =>
				{
					if (!a.Virtual && b.Virtual)
					{
						return -1;
					}
					else if (a.Virtual && !b.Virtual)
					{
						return 1;
					}
					else
					{
						var ap = a.ParentPackage;
						var bp = b.ParentPackage;

						if (ap == null && bp != null)
						{
							return -1;
						}
						else if (ap != null && bp == null)
						{
							return 1;
						}
						else
						{
							return U.CompareNatural(a.VirtualPath, b.VirtualPath);
						}
					}
				});
			}

			return sortedDirs_;
		}

		private string MakeTooltip(List<IFilesystemContainer> dirs, int indent, bool devMode)
		{

			string s = "";

			foreach (var d in dirs)
			{
				s += "\n" + new string(' ', indent * 2) + "- ";

				if (devMode)
				{
					s += d.ToString();
				}
				else
				{
					var p = d.ParentPackage;

					if (p == null)
						s += d.VirtualPath;
					else
						s += p.DisplayName;
				}

				if (d is VirtualDirectory)
					s += MakeTooltip((d as VirtualDirectory).SortedDirs(), indent + 1, devMode);
			}

			return s;
		}

		public void Add(IFilesystemContainer c)
		{
			if (dirs_ == null)
				dirs_ = new List<IFilesystemContainer>();

			if (!dirs_.Contains(c))
			{
				dirs_.Add(c);
				sortedDirs_ = null;
			}
		}

		public void AddRange(IEnumerable<IFilesystemContainer> c)
		{
			if (dirs_ == null)
				dirs_ = new List<IFilesystemContainer>();

			dirs_.AddRange(c);
			sortedDirs_ = null;
		}

		public List<IFilesystemContainer> Content
		{
			get { return dirs_; }
		}

		public override DateTime DateCreated
		{
			get
			{
				if (dirs_ != null && dirs_.Count == 1)
					return dirs_[0].DateCreated;

				return DateTime.MaxValue;
			}
		}

		public override DateTime DateModified
		{
			get
			{
				if (dirs_ != null && dirs_.Count == 1)
					return dirs_[0].DateModified;

				return DateTime.MaxValue;
			}
		}

		public override VUI.Icon Icon
		{
			get { return Icons.Get(Icons.Directory); }
		}

		public override bool CanPin
		{
			get { return true; }
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override bool ChildrenVirtual
		{
			get { return false; }
		}

		public override bool IsFlattened
		{
			get { return false; }
		}

		public override bool AlreadySorted
		{
			get { return false; }
		}


		public override string MakeRealPath()
		{
			return "";
		}

		public override string MakeRealPathForUser()
		{
			if (dirs_ != null)
			{
				for (int i = 0; i < dirs_.Count; ++i)
				{
					if (!dirs_[i].Virtual && dirs_[i].ParentPackage == null)
					{
						var rp = dirs_[i].MakeRealPathForUser();
						if (rp != "")
							return rp;
					}
				}
			}

			return base.MakeRealPathForUser();
		}

		public override bool IsSameObject(IFilesystemObject o)
		{
			if (o == null)
				return false;

			return (o.VirtualPath == VirtualPath);
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			var list = new List<IFilesystemContainer>();

			if (dirs_ != null)
			{
				// see DoGetFiles() below
				var cx2 = new Context(
					"", null, cx.PackagesRoot,
					Context.NoSort, Context.NoSortDirection, cx.Flags);

				foreach (var d in dirs_)
				{
					var ds = d.GetDirectories(cx2);
					if (ds != null)
						list.AddRange(ds);
				}
			}

			var map = new Dictionary<string, IFilesystemContainer>();
			Merge(list, map);


			var list2 = new List<IFilesystemContainer>();

			foreach (var ss in map)
				list2.Add(ss.Value);

			return list2;
		}

		private void Merge(
			List<IFilesystemContainer> list,
			Dictionary<string, IFilesystemContainer> map)
		{
			foreach (var d in list)
			{
				if (d is VirtualDirectory)
				{
					var vd = d as VirtualDirectory;
					if (vd.dirs_ != null)
						Merge(vd.dirs_, map);
				}
				else
				{
					IFilesystemContainer c;
					if (map.TryGetValue(d.Name, out c))
					{
						if (c is VirtualDirectory)
						{
							(c as VirtualDirectory).Add(d);
						}
						else
						{
							var vd = new VirtualDirectory(fs_, this, d.Name);
							vd.Add(d);
							vd.Add(c);

							map.Remove(d.Name);
							map.Add(d.Name, vd);
						}
					}
					else
					{
						map.Add(d.Name, d);
					}
				}
			}
		}

		protected override List<IFilesystemObject> DoGetFiles(Context cx)
		{
			var list = new List<IFilesystemObject>();

			if (dirs_ != null)
			{
				// this needs to get the raw files, not filtered, so get a new
				// context with the same flags only
				var cx2 = new Context(
					"", null, cx.PackagesRoot,
					Context.NoSort, Context.NoSortDirection, cx.Flags);

				foreach (var d in dirs_)
				{
					var fs = d.GetFiles(cx2);
					if (fs != null)
						list.AddRange(fs);
				}
			}

			return list;
		}
	}


	class FSDirectory : BasicFilesystemContainer, IDirectory
	{
		private DateTime dateCreated_ = DateTime.MaxValue;
		private DateTime dateModified_ = DateTime.MaxValue;

		public FSDirectory(Filesystem fs, IFilesystemContainer parent, string name)
			: base(fs, parent, name)
		{
		}

		public override string ToString()
		{
			return $"FSDirectory({VirtualPath})";
		}

		public override DateTime DateCreated
		{
			get
			{
#if VAM_GT_1_22
				if (dateCreated_ == DateTime.MaxValue)
					dateCreated_ = FMS.DirectoryCreationTime(MakeRealPath());
#endif

				return DateTime.MaxValue;
			}
		}

		public override DateTime DateModified
		{
			get
			{
#if VAM_GT_1_22
				if (dateModified_ == DateTime.MaxValue)
					dateModified_ = FMS.DirectoryLastWriteTime(MakeRealPath());
#endif

				return DateTime.MaxValue;
			}
		}

		public override VUI.Icon Icon
		{
			get { return Icons.Get(Icons.Directory); }
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

		public override string MakeRealPath()
		{
			string s = Name + "/";

			if (Parent != null)
				s = Parent.MakeRealPath() + s;

			return s;
		}
	}
}
