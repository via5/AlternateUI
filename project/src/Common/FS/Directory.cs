using System;
using System.Collections.Generic;

namespace AUI.FS
{
	class VirtualDirectory : BasicFilesystemContainer, IDirectory
	{
		private List<IFilesystemContainer> dirs_ = null;

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
				s += dirs_[0].DisplayName;

			return s + $" (VD {dirs_?.Count ?? 0})";
		}

		public override string Tooltip
		{
			get
			{
				var s = base.Tooltip;

				if (dirs_ == null || dirs_.Count == 0)
				{
					s += $"\nNo sources";
				}
				else
				{
					string dirSources = "";
					string packageSources = "";

					foreach (var d in dirs_)
					{
						var pp = d.ParentPackage;

						if (pp == null)
						{
							dirSources += $"\n   - {d} {d.VirtualPath} ({d.MakeRealPath()})";
						}
						else
						{
							packageSources += $"\n   -  {d} {pp.DisplayName}, {d.VirtualPath} ({d.MakeRealPath()})";
						}
					}

					s += $"\nMerged sources:{dirSources}{packageSources}";
				}

				return s;
			}
		}

		public void Add(IFilesystemContainer c)
		{
			AlternateUI.Assert(!(c is VirtualDirectory));

			if (dirs_ == null)
				dirs_ = new List<IFilesystemContainer>();

			dirs_.Add(c);
		}

		public void AddRange(IEnumerable<IFilesystemContainer> c)
		{
			if (dirs_ == null)
				dirs_ = new List<IFilesystemContainer>();

			dirs_.AddRange(c);
		}

		public List<IFilesystemContainer> Content
		{
			get { return dirs_; }
		}

		public override DateTime DateCreated
		{
			get { return DateTime.MaxValue; }
		}

		public override DateTime DateModified
		{
			get { return DateTime.MaxValue; }
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
				foreach (var d in dirs_)
				{
					var ds = d.GetDirectories(cx);
					if (ds != null)
						list.AddRange(ds);
				}
			}

			return list;
		}

		protected override List<IFilesystemObject> DoGetFiles(Context cx)
		{
			var list = new List<IFilesystemObject>();

			if (dirs_ != null)
			{
				foreach (var d in dirs_)
				{
					var fs = d.GetFiles(cx);
					if (fs != null)
						list.AddRange(fs);
				}
			}

			return list;
		}
	}


	class FSDirectory : BasicFilesystemContainer, IDirectory
	{
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
			get { return DateTime.MaxValue; }
		}

		public override DateTime DateModified
		{
			get { return DateTime.MaxValue; }
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
