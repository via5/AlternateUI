using SimpleJSON;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	class PinnedRoot : BasicFilesystemContainer
	{
		private readonly List<IFilesystemContainer> pinned_ =
			new List<IFilesystemContainer>();

		public PinnedRoot(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Pinned")
		{
		}

		public override string DebugInfo()
		{
			return $"PinnedRoot";
		}

		public List<IFilesystemContainer> Pinned
		{
			get { return pinned_; }
		}

		public override bool AlreadySorted
		{
			get { return true; }
		}

		public void Pin(string s, string display = null)
		{
			var o = fs_.Resolve<IFilesystemContainer>(
				Context.None, s, Filesystem.ResolveDirsOnly);

			if (o == null)
			{
				AlternateUI.Instance.Log.Error($"cannot resolve pinned item '{s}'");
				return;
			}

			Pin(o, display);
		}

		public void Pin(IFilesystemContainer o, string display = null)
		{
			Instrumentation.Start(I.Pin);
			{
				if (!IsPinned(o))
				{
					pinned_.Add(new PinnedObject(fs_, this, o, display));
					Changed();
				}
			}
			Instrumentation.End();
		}

		public void Unpin(IFilesystemContainer c)
		{
			Instrumentation.Start(I.Unpin);
			{
				for (int i = 0; i < pinned_.Count; ++i)
				{
					if (pinned_[i].IsSameObject(c) || pinned_[i] == c)
					{
						pinned_.RemoveAt(i);
						Changed();
						break;
					}
				}
			}
			Instrumentation.End();
		}

		public bool IsPinned(IFilesystemContainer o)
		{
			bool b = false;

			Instrumentation.Start(I.IsPinned);
			{
				foreach (var p in pinned_)
				{
					if (o.IsSameObject(p) || p == o)
					{
						b = true;
						break;
					}
				}
			}
			Instrumentation.End();

			return b;
		}

		public bool HasPinnedParent(IFilesystemContainer o)
		{
			while (o != null)
			{
				if (IsPinned(o))
					return true;

				o = o.Parent;
			}

			return false;
		}

		private void Changed()
		{
			fs_.SaveOptions();
			ClearCache();
			fs_.FirePinsChanged();
			fs_.FireObjectChanged(this);
		}

		public override bool CanPin
		{
			get { return false; }
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

		public override bool IsRedundant
		{
			get { return true; }
		}

		public override bool IsInternal
		{
			get { return true; }
		}

		public override bool IsWritable
		{
			get { return false; }
		}

		protected override VUI.Icon GetIcon()
		{
			return Icons.GetIcon(Icons.UnpinnedDark);
		}

		public override string MakeRealPath()
		{
			return "";
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			return pinned_;
		}

		protected override bool DoHasDirectories(Context cx)
		{
			return (pinned_ != null && pinned_.Count > 0);
		}
	}


	class PinnedObject : BasicFilesystemObject, IFilesystemContainer
	{
		private readonly IFilesystemContainer c_;

		public PinnedObject(Filesystem fs, PinnedRoot parent, IFilesystemContainer c, string displayName = null)
			: base(fs, parent, displayName)
		{
			c_ = c;
		}

		public override string DebugInfo()
		{
			return $"PinnedObject({c_})";
		}

		public override string Tooltip
		{
			get
			{
				string s = base.Tooltip;

				s += "\n\n";

				s += c_.Tooltip;

				return s;
			}
		}

		protected override string DoGetDisplayName(Context cx)
		{
			var p = c_.ParentPackage;

			if (p == null || (p == c_))
				return base.DoGetDisplayName(cx);
			else
				return p.GetDisplayName(cx) + ":" + base.DoGetDisplayName(cx);
		}

		public override string Name { get { return c_.Name; } }
		public override string VirtualPath { get { return c_.VirtualPath; } }
		public override bool CanPin { get { return true; } }
		public override bool Virtual { get { return c_.Virtual; } }
		public override bool ChildrenVirtual { get { return c_.ChildrenVirtual; } }
		public override bool IsFlattened { get { return c_.IsFlattened; } }
		public override IPackage ParentPackage { get { return c_.ParentPackage; } }
		public bool AlreadySorted { get { return c_.AlreadySorted; } }
		public override bool IsInternal { get { return c_.IsInternal; } }
		public override bool IsFile { get { return c_.IsFile; } }
		public override bool IsWritable { get { return c_.IsWritable; } }

		protected override VUI.Icon GetIcon()
		{
			return c_.Icon;
		}

		protected override DateTime GetDateCreated()
		{
			return c_.DateCreated;
		}

		protected override DateTime GetDateModified()
		{
			return c_.DateModified;
		}

		public override bool UnderlyingCanChange
		{
			get { return false; }
		}

		public override string MakeRealPath()
		{
			return c_.MakeRealPath();
		}

		public override string DeVirtualize()
		{
			return c_.DeVirtualize();
		}

		public override bool IsSameObject(IFilesystemObject o)
		{
			return c_.IsSameObject(o);
		}

		public override IFilesystemObject Resolve(
			Context cx, string path, int flags = Filesystem.ResolveDefault)
		{
			return c_.Resolve(cx, path, flags);
		}

		public ResolveResult ResolveInternal(
			Context cx, PathComponents cs, int flags, ResolveDebug debug)
		{
			return c_.ResolveInternal(cx, cs, flags, debug);
		}

		public override void ClearCache()
		{
			base.ClearCache();
			c_.ClearCache();
		}


		public bool HasDirectories(Context cx)
		{
			return c_.HasDirectories(cx);
		}

		public List<IFilesystemContainer> GetDirectories(Context cx)
		{
			return c_.GetDirectories(cx);
		}

		public List<IFilesystemObject> GetFiles(Context cx)
		{
			return c_.GetFiles(cx);
		}

		public void GetFilesRecursiveInternal(
			Context cx, Listing<IFilesystemObject> listing)
		{
			c_.GetFilesRecursiveInternal(cx, listing);
		}

		protected override void DisplayNameChanged()
		{
			fs_.SaveOptions();
		}
	}
}
