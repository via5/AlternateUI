using System;
using System.Globalization;

namespace AUI.FS
{
	abstract class BasicFilesystemObject : IFilesystemObject
	{
		protected readonly Filesystem fs_;
		private readonly IFilesystemContainer parent_;
		private string displayName_ = null;
		private DateTime dateCreated_ = DateTime.MaxValue;
		private DateTime dateModified_ = DateTime.MaxValue;

		public BasicFilesystemObject(
			Filesystem fs, IFilesystemContainer parent,
			string displayName = null)
		{
			fs_ = fs;
			parent_ = parent;
			displayName_ = displayName;
		}

		public Logger Log
		{
			get { return fs_.Log; }
		}

		public IFilesystemContainer Parent
		{
			get { return parent_; }
		}

		public string DisplayName
		{
			get
			{
				return displayName_ ?? GetDisplayName();
			}

			set
			{
				if (displayName_ != value)
				{
					displayName_ = value;
					DisplayNameChanged();
				}
			}
		}

		public bool HasCustomDisplayName
		{
			get { return (displayName_ != null); }
		}

		public virtual string VirtualPath
		{
			get
			{
				string s = Name;

				var parent = parent_;
				while (parent != null)
				{
					s = parent.Name + "/" + s;
					parent = parent.Parent;
				}

				return s;
			}
		}

		public virtual string RelativeVirtualPath
		{
			get
			{
				var pp = ParentPackage;

				if (pp == null)
					return VirtualPath;
				else
					return pp.GetRelativeVirtualPath(this);
			}
		}

		public virtual IPackage ParentPackage
		{
			get { return parent_?.ParentPackage ?? null; }
		}

		public abstract string Name { get; }
		public abstract VUI.Icon Icon { get; }
		public abstract bool CanPin { get; }
		public abstract bool Virtual { get; }
		public abstract bool ChildrenVirtual { get; }
		public abstract bool IsFlattened { get; }
		public abstract bool IsInternal { get; }

		public DateTime DateCreated
		{
			get
			{
				if (fs_.UsePackageTime)
				{
					var pp = ParentPackage;

					if (pp != null && pp != this)
						return ParentPackage.DateCreated;
				}

				{
					if (dateCreated_ == DateTime.MaxValue)
						dateCreated_ = GetDateCreated();

					return dateCreated_;
				}
			}
		}

		public DateTime DateModified
		{
			get
			{
				if (fs_.UsePackageTime)
				{
					var pp = ParentPackage;

					if (pp != null && pp != this)
						return ParentPackage.DateModified;
				}

				if (dateModified_ == DateTime.MaxValue)
					dateModified_ = GetDateModified();

				return dateModified_;
			}
		}

		public virtual bool IsRedundant
		{
			get { return false; }
		}

		public virtual bool UnderlyingCanChange
		{
			get { return true; }
		}


		public virtual string Tooltip
		{
			get
			{
				string tt;

				if (U.DevMode)
				{
					tt =
						$"{ToString()}\n" +
						$"Virtual path: {VirtualPath}\n" +
						$"Real path: {MakeRealPath()}\n" +
						$"Devirt: {DeVirtualize()}\n" +
						$"RVP: {RelativeVirtualPath}";

					var p = ParentPackage;

					if (p != null)
						tt += $"\nPackage: {p.DisplayName}";

					tt += $"\nCreated: {FormatDT(DateCreated)}";
					tt += $"\nLast modified: {FormatDT(DateModified)}";
				}
				else
				{
					var rp = DeVirtualize();
					if (string.IsNullOrEmpty(rp))
						rp = "(virtual folder)";

					tt =
						$"Virtual path: {VirtualPath}" +
						$"\nReal path: {rp}";

					var p = ParentPackage;
					if (p != null)
						tt += $"\nPackage: {p.DisplayName}";

					tt += $"\nCreated: {FormatDT(DateCreated)}";
					tt += $"\nLast modified: {FormatDT(DateModified)}";
				}

				return tt;
			}
		}

		private string FormatDT(DateTime dt)
		{
			if (dt == DateTime.MaxValue)
				return "(none)";
			else
				return dt.ToString(CultureInfo.CurrentCulture);
		}

		public virtual bool IsSameObject(IFilesystemObject o)
		{
			return fs_.IsSameObject(this, o);
		}

		public virtual IFilesystemObject Resolve(
			Context cx, string path, int flags = Filesystem.ResolveDefault)
		{
			if (path == Name)
				return this;

			return null;
		}

		public abstract string MakeRealPath();
		public abstract string DebugInfo();

		public virtual string DeVirtualize()
		{
			return MakeRealPath();
		}


		public override string ToString()
		{
			return DebugInfo();
		}


		public virtual void ClearCache()
		{
			dateCreated_ = DateTime.MaxValue;
			dateModified_ = DateTime.MaxValue;
		}

		protected virtual string GetDisplayName()
		{
			return Name;
		}

		protected virtual void DisplayNameChanged()
		{
			// no-op
		}

		protected virtual DateTime GetDateCreated()
		{
			return DateTime.MaxValue;
		}

		protected virtual DateTime GetDateModified()
		{
			return DateTime.MaxValue;
		}
	}
}
