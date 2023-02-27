using Leap;
using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AUI
{
	public class Icon
	{
		private Texture texture_ = null;
		private readonly string path_ = null;
		private readonly int w_ = 0, h_ = 0;
		private readonly Texture def_ = null;
		private readonly List<Action<Texture>> callbacks_ = new List<Action<Texture>>();
		private bool loading_ = false;

		public Icon(Texture t)
		{
			texture_ = t;
		}

		public Icon(string path, Texture def = null)
			: this(path, 0, 0, def)
		{
		}

		public Icon(string path, int w, int h, Texture def = null)
		{
			path_ = path;
			w_ = w;
			h_ = h;
			def_ = def;
		}

		public Texture CachedTexture
		{
			get { return texture_; }
		}

		public void GetTexture(Action<Texture> f)
		{
			if (texture_ != null)
			{
				f(texture_);
			}
			else
			{
				if (!loading_)
				{
					loading_ = true;
					Load();
				}

				callbacks_.Add(f);
			}
		}

		private void Load()
		{
			if (path_ == null)
			{
				texture_ = def_;
				RunCallbacks();
			}
			else
			{
				ImageLoaderThreaded.QueuedImage q = new ImageLoaderThreaded.QueuedImage();
				q.imgPath = path_;

				q.callback = (tt) =>
				{
					Texture tex = tt.tex;
					if (tex == null)
					{
						texture_ = def_;
					}
					else
					{
						texture_ = tex;

						if (w_ != 0 && h_ != 0)
							texture_ = ScaleTexture(texture_, w_, h_);

						texture_.wrapMode = TextureWrapMode.Clamp;
					}

					RunCallbacks();
				};

				//ImageLoaderThreaded.singleton.ClearCacheThumbnail(q.imgPath);
				ImageLoaderThreaded.singleton.QueueThumbnail(q);
			}
		}

		private void RunCallbacks()
		{
			foreach (var f in callbacks_)
				f(texture_);

			callbacks_.Clear();
		}

		private static Texture2D ScaleTexture(Texture src, int width, int height)
		{
			RenderTexture rt = RenderTexture.GetTemporary(width, height);
			Graphics.Blit(src, rt);

			RenderTexture currentActiveRT = RenderTexture.active;
			RenderTexture.active = rt;
			Texture2D tex = new Texture2D(rt.width, rt.height);

			tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
			tex.Apply();

			RenderTexture.ReleaseTemporary(rt);
			RenderTexture.active = currentActiveRT;

			return tex;
		}
	}


	public static class Icons
	{
		private static Icon null_ = null;
		private static Icon package_ = null;
		private static Icon pinned_ = null;
		private static Icon resizeWE_ = null;
		private static Icon dir_ = null;
		private static Dictionary<string, Icon> exts_ = new Dictionary<string, Icon>();
		private static Dictionary<string, Icon> thumbs_ = new Dictionary<string, Icon>();

		public static void LoadAll()
		{
			null_ = new Icon(Texture2D.blackTexture);
			package_ = new Icon(AlternateUI.Instance.PluginPath + "/res/icons/package.png");
			pinned_ = new Icon(AlternateUI.Instance.PluginPath + "/res/icons/pinned.png");
			resizeWE_ = new Icon(AlternateUI.Instance.PluginPath + "/res/cursors/resize_w_e.png", 40, 40);
			dir_ = new Icon(SuperController.singleton.fileBrowserUI.folderIcon.texture);
		}

		public static Icon Null
		{
			get { return null_; }
		}

		public static Icon Package
		{
			get { return package_; }
		}

		public static Icon Pinned
		{
			get { return pinned_; }
		}

		public static Icon ResizeWE
		{
			get { return resizeWE_; }
		}

		public static Icon Directory
		{
			get { return dir_; }
		}

		public static Icon File(string path)
		{
			var thumbPath = GetThumbnailPath(path);

			if (thumbPath == null)
				return GetFileIcon(path);
			else
				return GetThumbnailIcon(thumbPath);
		}

		private static Icon GetFileIcon(string path)
		{
			string ext = Path.Extension(path);
			Icon i;

			if (!exts_.TryGetValue(ext, out i))
			{
				var t = SuperController.singleton.fileBrowserUI.GetFileIcon(path)?.texture;
				i = new Icon(t);
				exts_.Add(ext, i);
			}

			return i;
		}

		private static Icon GetThumbnailIcon(string path)
		{
			Icon i;

			if (!thumbs_.TryGetValue(path, out i))
			{
				var t = ImageLoaderThreaded.singleton.GetCachedThumbnail(path);

				if (t == null)
				{
					i = new Icon(path, SuperController.singleton.fileBrowserUI.defaultIcon.texture);
					thumbs_.Add(path, i);
				}
				else
				{
					i = new Icon(t);
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
