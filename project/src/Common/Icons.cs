using MVR.FileManagementSecure;
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
		public const int Package = 1;
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

		public const int BeamCursor = 0;
		public const int ResizeWECursor = 1;

		private static Dictionary<int, VUI.Icon> icons_ = new Dictionary<int, VUI.Icon>();
		private static Dictionary<int, VUI.Cursor> cursors_ = new Dictionary<int, VUI.Cursor>();
		private static Dictionary<string, VUI.Icon> exts_ = new Dictionary<string, VUI.Icon>();
		private static Dictionary<string, VUI.Icon> thumbs_ = new Dictionary<string, VUI.Icon>();

		public static void LoadAll()
		{
			var pp = AlternateUI.Instance.PluginPath;
			var sc = SuperController.singleton;

			icons_ = new Dictionary<int, VUI.Icon>
			{
				{ Null, new VUI.Icon(Texture2D.blackTexture) },
				{ Package, new VUI.Icon(pp + "/res/icons/package.png") },
				{ Pinned, new VUI.Icon(pp + "/res/icons/pinned.png") },
				{ Unpinned, new VUI.Icon(pp + "/res/icons/unpinned.png") },
				{ UnpinnedDark, new VUI.Icon(pp + "/res/icons/unpinned-dark.png") },
				{ Back, new VUI.Icon(pp + "/res/icons/back.png") },
				{ Next, new VUI.Icon(pp + "/res/icons/next.png") },
				{ Up, new VUI.Icon(pp + "/res/icons/up.png") },
				{ Drop, new VUI.Icon(pp + "/res/icons/drop.png") },
				{ Directory, new VUI.Icon(sc.fileBrowserUI.folderIcon.texture) },
				{ Reload, new VUI.Icon(pp + "/res/icons/reload.png") },
				{ OpenExternal, new VUI.Icon(pp + "/res/icons/open-external.png") },
			};

			cursors_ = new Dictionary<int, VUI.Cursor>
			{
				{ BeamCursor, new VUI.Cursor(pp + "/res/cursors/beam.png") },
				{ ResizeWECursor, new VUI.Cursor(pp + "/res/cursors/resize_w_e.png") },
			};
		}

		public static VUI.Icon GetIcon(int type)
		{
			return icons_[type];
		}

		public static VUI.Cursor GetCursor(int type)
		{
			return cursors_[type];
		}

		public static VUI.Icon File(string path)
		{
			var thumbPath = GetThumbnailPath(path);

			if (thumbPath == null)
				return GetFileIcon(path);
			else
				return GetThumbnailIcon(thumbPath);
		}

		public static void ClearFileCache(string path)
		{
			var thumbPath = GetThumbnailPath(path);

			if (thumbPath == null)
			{
				string ext = Path.Extension(path);
				exts_.Remove(ext);
			}
			else
			{
				thumbs_.Remove(thumbPath);
				ImageLoaderThreaded.singleton.ClearCacheThumbnail(thumbPath);
			}
		}

		private static VUI.Icon GetFileIcon(string path)
		{
			string ext = Path.Extension(path);
			VUI.Icon i;

			if (!exts_.TryGetValue(ext, out i))
			{
				var t = SuperController.singleton.fileBrowserUI.GetFileIcon(path)?.texture;
				i = new VUI.Icon(t);
				exts_.Add(ext, i);
			}

			return i;
		}

		private static VUI.Icon GetThumbnailIcon(string path)
		{
			VUI.Icon i;

			if (!thumbs_.TryGetValue(path, out i))
			{
				var t = ImageLoaderThreaded.singleton.GetCachedThumbnail(path);

				if (t == null)
				{
					i = new VUI.Icon(path, SuperController.singleton.fileBrowserUI.defaultIcon.texture);
					thumbs_.Add(path, i);
				}
				else
				{
					i = new VUI.Icon(t);
					thumbs_.Add(path, i);
				}
			}

			return i;
		}

		private static string GetThumbnailPath(string file)
		{
			var exts = new string[] { ".jpg", ".JPG" };

			foreach (var e in exts)
			{
				var relImgPath = Path.Parent(file) + "\\" + Path.Stem(file) + e;
				var imgPath = FileManagerSecure.GetFullPath(relImgPath);

				if (FileManagerSecure.FileExists(imgPath))
					return imgPath;
			}

			return null;
		}
	}
}
