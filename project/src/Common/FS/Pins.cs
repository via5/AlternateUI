using SimpleJSON;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	public class PinnedRoot : BasicFilesystemContainer
	{
		private readonly List<IFilesystemContainer> pinned_ =
			new List<IFilesystemContainer>();

		public PinnedRoot(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Pinned")
		{
		}

		protected override string DoGetDebugName()
		{
			return "PinnedRoot";
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
					pinned_.Add(new PinnedObject(fs_, this, o.VirtualPath, display));
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
		private readonly string vpath_;

		public PinnedObject(Filesystem fs, PinnedRoot parent, string vpath, string displayName = null)
			: base(fs, parent, displayName)
		{
			vpath_ = vpath;
		}

		protected override string DoGetDebugName()
		{
			return "PinnedObject";
		}

		protected override string DoGetDebugInfo()
		{
			return Object.GetDebugString();
		}

		public override string Tooltip
		{
			get
			{
				string s = base.Tooltip;

				s += "\n\n";

				s += Object.Tooltip;

				return s;
			}
		}

		protected override string DoGetDisplayName(Context cx)
		{
			var p = Object.ParentPackage;

			if (p == null || (p == Object))
				return base.DoGetDisplayName(cx);
			else
				return p.GetDisplayName(cx) + ":" + base.DoGetDisplayName(cx);
		}

		public override string Name { get { return Object.Name; } }
		public override string VirtualPath { get { return Object.VirtualPath; } }
		public override bool CanPin { get { return true; } }
		public override bool Virtual { get { return Object.Virtual; } }
		public override bool ChildrenVirtual { get { return Object.ChildrenVirtual; } }
		public override bool IsFlattened { get { return Object.IsFlattened; } }
		public override IPackage ParentPackage { get { return Object.ParentPackage; } }
		public bool AlreadySorted { get { return Object.AlreadySorted; } }
		public override bool IsInternal { get { return Object.IsInternal; } }
		public override bool IsFile { get { return Object.IsFile; } }
		public override bool IsWritable { get { return Object.IsWritable; } }

		public IFilesystemContainer Object
		{
			get
			{
				return fs_.Resolve<IFilesystemContainer>(
					Context.None, vpath_, FS.Filesystem.ResolveDirsOnly);
			}
		}

		protected override VUI.Icon GetIcon()
		{
			return Object.Icon;
		}

		protected override DateTime GetDateCreated()
		{
			return Object.DateCreated;
		}

		protected override DateTime GetDateModified()
		{
			return Object.DateModified;
		}

		public override bool UnderlyingCanChange
		{
			get { return false; }
		}

		public override string MakeRealPath()
		{
			return Object.MakeRealPath();
		}

		public override string DeVirtualize()
		{
			return Object.DeVirtualize();
		}

		public override bool IsSameObject(IFilesystemObject o)
		{
			return Object.IsSameObject(o);
		}

		public override IFilesystemObject Resolve(
			Context cx, string path, int flags = Filesystem.ResolveDefault)
		{
			return Object.Resolve(cx, path, flags);
		}

		public ResolveResult ResolveInternal(
			Context cx, PathComponents cs, int flags, ResolveDebug debug)
		{
			return Object.ResolveInternal(cx, cs, flags, debug);
		}

		public override void ClearCache()
		{
			base.ClearCache();
			Object.ClearCache();
		}


		public bool HasDirectories(Context cx)
		{
			return Object.HasDirectories(cx);
		}

		public List<IFilesystemContainer> GetDirectories(Context cx)
		{
			return Object.GetDirectories(cx);
		}

		public List<IFilesystemObject> GetFiles(Context cx)
		{
			return Object.GetFiles(cx);
		}

		public void GetFilesRecursiveInternal(
			Context cx, Listing<IFilesystemObject> listing)
		{
			Object.GetFilesRecursiveInternal(cx, listing);
		}

		protected override void DisplayNameChanged()
		{
			fs_.SaveOptions();
		}
	}
}
