using System.Collections.Generic;
using UnityEngine;

namespace AUI
{
	public class CursorProvider : VUI.ICursorProvider
	{
		private static CursorProvider instance_ = new CursorProvider();

		public static CursorProvider Instance
		{
			get { return instance_; }
		}

		public VUI.Cursor ResizeWE
		{
			get { return Icons.GetCursor(Icons.ResizeWECursor); }
		}

		public VUI.Cursor Beam
		{
			get { return Icons.GetCursor(Icons.BeamCursor); }
		}
	}


	public static class Icons
	{
		public const int Null = 0;
		public const int PackageDark = 1;
		public const int Pinned = 2;
		public const int Unpinned = 3;
		public const int UnpinnedDark = 4;
		public const int Back = 5;
		public const int Next = 6;
		public const int Up = 7;
		public const int Drop = 8;
		public const int Directory = 9;
		public const int Reload = 10;
		public const int OpenExternal = 11;
		public const int GotoDirectory = 12;
		public const int Package = 13;

		public const int BeamCursor = 0;
		public const int ResizeWECursor = 1;

		private static Dictionary<int, VUI.Icon> icons_ = new Dictionary<int, VUI.Icon>();
		private static Dictionary<int, VUI.Cursor> cursors_ = new Dictionary<int, VUI.Cursor>();

		public static void LoadAll()
		{
			var pp = AlternateUI.Instance.PluginPath;
			var sc = SuperController.singleton;

			icons_ = new Dictionary<int, VUI.Icon>
			{
				{ Null, VUI.Icon.FromTexture(Texture2D.blackTexture) },
				{ PackageDark, VUI.Icon.FromFile(pp + "/res/icons/package-dark.png") },
				{ Pinned, VUI.Icon.FromFile(pp + "/res/icons/pinned.png") },
				{ Unpinned, VUI.Icon.FromFile(pp + "/res/icons/unpinned.png") },
				{ UnpinnedDark, VUI.Icon.FromFile(pp + "/res/icons/unpinned-dark.png") },
				{ Back, VUI.Icon.FromFile(pp + "/res/icons/back.png") },
				{ Next, VUI.Icon.FromFile(pp + "/res/icons/next.png") },
				{ Up, VUI.Icon.FromFile(pp + "/res/icons/up.png") },
				{ Drop, VUI.Icon.FromFile(pp + "/res/icons/drop.png") },
				{ Directory, VUI.Icon.FromTexture(sc.fileBrowserUI.folderIcon.texture) },
				{ Reload, VUI.Icon.FromFile(pp + "/res/icons/reload.png") },
				{ OpenExternal, VUI.Icon.FromFile(pp + "/res/icons/open-external.png") },
				{ GotoDirectory, VUI.Icon.FromFile(pp + "/res/icons/goto-directory.png") },
				{ Package, VUI.Icon.FromFile(pp + "/res/icons/package.png") },
			};

			cursors_ = new Dictionary<int, VUI.Cursor>
			{
				{ BeamCursor, new VUI.Cursor(pp + "/res/cursors/beam.png") },
				{ ResizeWECursor, new VUI.Cursor(pp + "/res/cursors/resize_w_e.png") },
			};
		}

		public static VUI.Icon GetIcon(int type)
		{
			if (type < 0 || type >= icons_.Count)
			{
				AlternateUI.Instance.Log.Error($"bad icon {type}");
				return null;
			}

			return icons_[type];
		}

		public static VUI.Cursor GetCursor(int type)
		{
			if (type < 0 || type >= cursors_.Count)
			{
				AlternateUI.Instance.Log.Error($"bad cursor {type}");
				return null;
			}

			return cursors_[type];
		}

		public static void ClearCache()
		{
			VUI.Icon.ClearAllCache();
		}
	}
}
