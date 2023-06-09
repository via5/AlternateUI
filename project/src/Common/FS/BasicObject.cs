using System;
using System.Globalization;

namespace AUI.FS
{
	public abstract class BasicFilesystemObject : IFilesystemObject
	{
		static private int nextIdentity_ = 1;

		private readonly int identity_;
		protected readonly Filesystem fs_;
		private readonly IFilesystemContainer parent_;
		private string displayName_ = null;
		private VUI.Icon icon_ = null;
		private DateTime dateCreated_ = Sys.BadDateTime;
		private DateTime dateModified_ = Sys.BadDateTime;
		private string vp_ = null;

		public BasicFilesystemObject(
			Filesystem fs, IFilesystemContainer parent,
			string displayName = null)
		{
			identity_ = NextIdentity();
			fs_ = fs;
			parent_ = parent;
			displayName_ = displayName;
		}

		public static int NextIdentity()
		{
			return nextIdentity_++;
		}

		public Logger Log
		{
			get { return fs_.Log; }
		}

		public int DebugIdentity
		{
			get { return identity_; }
		}

		public IFilesystemContainer Parent
		{
			get { return parent_; }
		}

		public string GetDisplayName(Context cx)
		{
			return displayName_ ?? DoGetDisplayName(cx);
		}

		public void SetDisplayName(string name)
		{
			if (displayName_ != name)
			{
				displayName_ = name;
				DisplayNameChanged();
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
				if (vp_ == null)
				{
					string s = Name;

					var parent = parent_;
					while (parent != null)
					{
						s = parent.Name + "/" + s;
						parent = parent.Parent;
					}

					vp_ = s;
				}

				return vp_;
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
		public abstract bool CanPin { get; }
		public abstract bool Virtual { get; }
		public abstract bool ChildrenVirtual { get; }
		public abstract bool IsFlattened { get; }
		public abstract bool IsInternal { get; }
		public abstract bool IsFile { get; }
		public abstract bool IsWritable { get; }

		public VUI.Icon Icon
		{
			get
			{
				if (icon_ == null)
					icon_ = GetIcon();

				return icon_;
			}
		}

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
					if (dateCreated_ == Sys.BadDateTime)
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

				if (dateModified_ == Sys.BadDateTime)
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
						tt += $"\nPackage: {p.GetDisplayName(null)}";

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
						tt += $"\nPackage: {p.GetDisplayName(null)}";

					tt += $"\nCreated: {FormatDT(DateCreated)}";
					tt += $"\nLast modified: {FormatDT(DateModified)}";
				}

				return tt;
			}
		}

		private string FormatDT(DateTime dt)
		{
			if (dt == Sys.BadDateTime)
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

		public virtual string DeVirtualize()
		{
			return MakeRealPath();
		}


		public string GetDebugString()
		{
			return $"{DoGetDebugName()}({DoGetDebugInfo()}#{DebugIdentity})";
		}

		protected abstract string DoGetDebugName();

		protected virtual string DoGetDebugInfo()
		{
			return VirtualPath;
		}

		public override string ToString()
		{
			return GetDebugString();
		}


		public virtual void ClearCache()
		{
			icon_?.ClearCache();
			dateCreated_ = Sys.BadDateTime;
			dateModified_ = Sys.BadDateTime;
		}

		protected virtual string DoGetDisplayName(Context cx)
		{
			return Name;
		}

		protected virtual void DisplayNameChanged()
		{
			// no-op
		}

		protected virtual DateTime GetDateCreated()
		{
			return Sys.BadDateTime;
		}

		protected virtual DateTime GetDateModified()
		{
			return Sys.BadDateTime;
		}

		protected abstract VUI.Icon GetIcon();
	}
}
