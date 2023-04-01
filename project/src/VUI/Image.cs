using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	public abstract class Icon
	{
		private static int currentCacheToken_ = 0;

		private Texture texture_ = null;
		private readonly List<Action<Texture>> callbacks_ = new List<Action<Texture>>();
		private readonly bool rerender_;
		private bool loading_ = false;
		private int cacheToken_ = currentCacheToken_;


		public static Icon FromTexture(Texture t)
		{
			return TextureIcon.FromCache(t);
		}

		public static Icon FromFile(string path, Texture def = null)
		{
			return FileIcon.FromCache(path, def);
		}

		public static Icon FromThumbnail(string path)
		{
			return ThumbnailIcon.FromCache(path);
		}


		protected Icon(bool rerender)
		{
			rerender_ = rerender;
		}

		public static void ClearAllCache()
		{
			++currentCacheToken_;
		}

		public Texture CachedTexture
		{
			get
			{
				if (cacheToken_ != currentCacheToken_)
					return null;

				return texture_;
			}
		}

		public void ClearCache()
		{
			cacheToken_ = -1;
		}

		public void GetTexture(Action<Texture> f)
		{
			Glue.LogInfo($"{this}: getting texture");

			if (cacheToken_ != currentCacheToken_)
			{
				Glue.LogInfo($"{this}: purging");

				cacheToken_ = currentCacheToken_;
				SetTexture(f, null);
				Purge();
			}

			if (texture_ == null)
			{
				Glue.LogInfo($"{this}: trying cache");
				SetTexture(f, GetFromCache());

				if (texture_ == null)
				{
					Glue.LogInfo($"{this}: cache failed, loading");

					callbacks_.Add(f);

					if (!loading_)
					{
						loading_ = true;
						Load();
					}
				}
				else
				{
					Glue.LogInfo($"{this}: got from cache {texture_}");
					SetTexture(f, texture_);
				}
			}
			else
			{
				Glue.LogInfo($"{this}: already loaded");
				SetTexture(f, texture_);
			}
		}

		protected abstract void Purge();
		protected abstract Texture GetFromCache();
		protected abstract void Load();

		protected void LoadFinished(Texture t)
		{
			Glue.LogInfo($"{this} load finished t={t}");
			loading_ = false;

			SetTexture(null, t);
			RunCallbacks();
		}

		private void SetTexture(Action<Texture> f, Texture t)
		{
			texture_ = t;

			if (texture_ != null)
			{
				texture_.wrapMode = TextureWrapMode.Clamp;

				if (rerender_)
				{
					texture_ = ScaleTexture(
						texture_, texture_.width, texture_.height);
				}
			}

			Glue.LogInfo($"{this} texture set {texture_}, invoking {f}");
			f?.Invoke(texture_);
		}

		protected void LoadFromImageLoader(string path, Texture def)
		{
			Glue.LogInfo($"{this} loading from loader {path}");

			ImageLoaderThreaded.QueuedImage q = new ImageLoaderThreaded.QueuedImage();
			q.imgPath = path;

			q.callback = (tt) =>
			{
				Texture tex = tt.tex ?? def;
				LoadFinished(tex);
			};

			ImageLoaderThreaded.singleton.QueueThumbnail(q);
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


	public class TextureIcon : Icon
	{
		private readonly Texture t_;

		public TextureIcon(Texture t)
			: base(false)
		{
			t_ = t;
		}

		public static TextureIcon FromCache(Texture t)
		{
			return new TextureIcon(t);
		}

		protected override void Purge()
		{
			// no-op
		}

		protected override Texture GetFromCache()
		{
			return t_;
		}

		protected override void Load()
		{
			LoadFinished(t_);
		}

		public override string ToString()
		{
			return $"TextureIcon({t_})";
		}
	}


	public class FileIcon : Icon
	{
		private readonly string path_ = null;
		private readonly Texture def_ = null;

		public FileIcon(string path, Texture def = null)
			: this(path, def, false)
		{
		}

		protected FileIcon(string path, Texture def, bool rerender)
			: base(rerender)
		{
			path_ = path;
			def_ = def;
		}

		public static FileIcon FromCache(string path, Texture def = null)
		{
			return new FileIcon(path, def);
		}

		protected override void Purge()
		{
			ImageLoaderThreaded.singleton.ClearCacheThumbnail(path_);
		}

		protected override Texture GetFromCache()
		{
			return ImageLoaderThreaded.singleton.GetCachedThumbnail(path_);
		}

		protected override void Load()
		{
			LoadFromImageLoader(path_, def_);
		}

		public override string ToString()
		{
			return $"FileIcon({path_})";
		}
	}


	public class ThumbnailIcon : Icon
	{
		private readonly string path_;
		private string thumbPath_;

		private static Dictionary<string, ThumbnailIcon> exts_ = new Dictionary<string, ThumbnailIcon>();
		private static Dictionary<string, ThumbnailIcon> thumbs_ = new Dictionary<string, ThumbnailIcon>();

		public ThumbnailIcon(string path)
			: base(false)
		{
			path_ = path;
			thumbPath_ = GetThumbnailPath(path);
		}

		public static ThumbnailIcon FromCache(string path)
		{
			ThumbnailIcon icon;
			var thumbPath = GetThumbnailPath(path);

			if (thumbPath == null)
			{
				string ext = Path.Extension(path);

				if (!exts_.TryGetValue(ext, out icon))
				{
					icon = new ThumbnailIcon(path);
					exts_.Add(ext, icon);
				}
			}
			else
			{
				if (!thumbs_.TryGetValue(thumbPath, out icon))
				{
					icon = new ThumbnailIcon(path);
					thumbs_.Add(thumbPath, icon);
				}
			}

			return icon;
		}

		protected override void Purge()
		{
			thumbPath_ = GetThumbnailPath(path_);
			if (thumbPath_ != null)
				ImageLoaderThreaded.singleton.ClearCacheThumbnail(thumbPath_);
		}

		protected override Texture GetFromCache()
		{
			if (thumbPath_ == null)
				return GetFileIcon();
			else
				return ImageLoaderThreaded.singleton.GetCachedThumbnail(thumbPath_);
		}

		protected override void Load()
		{
			if (thumbPath_ == null)
			{
				Glue.LogInfo($"{this}: loading file icon");
				LoadFinished(GetFileIcon());
			}
			else
			{
				Glue.LogInfo($"{this}: forwarding to loader {thumbPath_}");
				LoadFromImageLoader(thumbPath_, SuperController.singleton.fileBrowserUI.defaultIcon.texture);
			}
		}


		public override string ToString()
		{
			return $"ThumbnailIcon({thumbPath_ ?? path_})";
		}

		private static string GetThumbnailPath(string file)
		{
			var exts = new string[] { ".jpg", ".JPG" };

			foreach (var e in exts)
			{
				var relImgPath = Path.Parent(file) + "/" + Path.Stem(file) + e;
				var imgPath = FileManagerSecure.GetFullPath(relImgPath);

				if (FileManagerSecure.FileExists(imgPath))
					return imgPath;
			}

			return null;
		}

		private Texture GetFileIcon()
		{
			string ext = Path.Extension(path_);
			return SuperController.singleton.fileBrowserUI.GetFileIcon(path_)?.texture;
		}
	}


	// for whatever reason, cursor textures are corrupted unless ScaleTexture()
	// is called, even if the size is the same
	//
	public class Cursor : FileIcon
	{
		public Cursor(string path, Texture def = null)
			: base(path, def, true)
		{
		}
	}


	public class ImageObject
	{
		private static Material emptyMat_ = null;

		private readonly Widget parent_;
		private readonly GameObject o_;
		private readonly RectTransform rt_ = null;
		private readonly RawImage raw_ = null;
		private Texture tex_ = null;
		private Texture grey_ = null;
		private Size size_ = new Size(Widget.DontCare, Widget.DontCare);
		private int align_ = Image.AlignDefault;
		private bool enabled_ = true;

		public ImageObject(Widget parent, int align = Image.AlignDefault)
		{
			parent_ = parent;
			align_ = align;

			o_ = new GameObject();
			o_.transform.SetParent(parent.WidgetObject.transform, false);

			raw_ = o_.AddComponent<RawImage>();
			rt_ = o_.GetComponent<RectTransform>();

			rt_.anchorMin = new Vector2(0, 0);
			rt_.anchorMax = new Vector2(1, 1);
			rt_.offsetMin = new Vector2(0, 0);
			rt_.offsetMax = new Vector2(0, 0);

			if (emptyMat_ == null)
			{
				emptyMat_ = new Material(raw_.material);
				emptyMat_.mainTexture = Texture2D.blackTexture;
			}

			raw_.material = emptyMat_;
			UpdateTexture();
		}

		public GameObject GameObject
		{
			get { return o_; }
		}

		public Texture Texture
		{
			get
			{
				return tex_;
			}

			set
			{
				if (tex_ != value)
				{
					if (grey_ != null)
						grey_ = null;

					tex_ = value;
					UpdateTexture();
				}
			}
		}

		public Size Size
		{
			get { return size_; }
			set { size_ = value; UpdateAspect(); }
		}

		public int Alignment
		{
			get { return align_; }
			set { align_ = value; UpdateAspect(); }
		}

		public void SetRender(bool b)
		{
			if (raw_ != null)
				raw_.gameObject.SetActive(b);
		}

		public void SetEnabled(bool b)
		{
			enabled_ = b;
			UpdateTexture();
		}

		public static Size SGetPreferredSize(Texture t, float maxWidth, float maxHeight)
		{
			Size s;

			if (t == null)
			{
				s = new Size(
					Math.Max(maxWidth, maxHeight),
					Math.Max(maxWidth, maxHeight));
			}
			else
			{
				s = new Size(t.width, t.height);

				if (maxWidth != Widget.DontCare)
					s.Width = Math.Min(maxWidth, s.Width);

				if (maxHeight != Widget.DontCare)
					s.Height = Math.Min(maxHeight, s.Height);
			}

			s.Width = Math.Min(s.Width, s.Height);
			s.Height = Math.Min(s.Width, s.Height);

			return s;
		}

		public Size GetPreferredSize(float maxWidth, float maxHeight)
		{
			return SGetPreferredSize(tex_, maxWidth, maxHeight);
		}

		public void UpdateAspect()
		{
			Size scaled;

			var maxSize = parent_.ClientBounds.Size;
			if (size_.Width != Widget.DontCare)
				maxSize.Width = Math.Min(maxSize.Width, size_.Height);

			if (size_.Height != Widget.DontCare)
				maxSize.Height = Math.Min(maxSize.Height, size_.Height);

			if (tex_ == null)
			{
				scaled = maxSize;
			}
			else
			{
				maxSize.Width = Math.Min(maxSize.Width, tex_.width);
				maxSize.Height = Math.Min(maxSize.Height, tex_.height);

				scaled = Aspect(
					tex_.width, tex_.height,
					maxSize.Width, maxSize.Height);
			}

			Vector2 anchorMin;
			Vector2 anchorMax;
			Vector2 offsetMin;
			Vector2 offsetMax;

			if (Bits.IsSet(align_, Align.Right))
			{
				anchorMin.x = 1;
				anchorMax.x = 1;
				offsetMin.x = -scaled.Width;
				offsetMax.x = 0;
			}
			else if (Bits.IsSet(align_, Align.Center))
			{
				anchorMin.x = 0.5f;
				anchorMax.x = 0.5f;
				offsetMin.x = -scaled.Width / 2;
				offsetMax.x = scaled.Width / 2;
			}
			else  // left
			{
				anchorMin.x = 0;
				anchorMax.x = 0;
				offsetMin.x = 0;
				offsetMax.x = scaled.Width;
			}


			if (Bits.IsSet(align_, Align.Bottom))
			{
				anchorMin.y = 0;
				anchorMax.y = 0;
				offsetMin.y = 0;
				offsetMax.y = scaled.Height;
			}
			else if (Bits.IsSet(align_, Align.VCenter))
			{
				anchorMin.y = 0.5f;
				anchorMax.y = 0.5f;
				offsetMin.y = -scaled.Height / 2;
				offsetMax.y = scaled.Height / 2;
			}
			else  // top
			{
				anchorMin.y = 1;
				anchorMax.y = 1;
				offsetMin.y = -scaled.Height;
				offsetMax.y = 0;
			}


			rt_.anchorMin = anchorMin;
			rt_.anchorMax = anchorMax;
			rt_.offsetMin = offsetMin;
			rt_.offsetMax = offsetMax;
		}

		private void UpdateTexture()
		{
			if (raw_ != null)
			{
				if (enabled_ || tex_ == null)
				{
					raw_.texture = tex_;
				}
				else
				{
					if (grey_ == null)
						CreateGrey();

					raw_.texture = grey_;
				}

				UpdateAspect();
			}
		}

		private void CreateGrey()
		{
			var t = new Texture2D(tex_.width, tex_.height, TextureFormat.ARGB32, false);

			Color32[] pixels = (tex_ as Texture2D).GetPixels32();
			for (int x = 0; x < tex_.width; x++)
			{
				for (int y = 0; y < tex_.height; y++)
				{
					Color32 pixel = pixels[x + y * tex_.width];
					Color c;

					int p = ((256 * 256 + pixel.r) * 256 + pixel.b) * 256 + pixel.g;
					int b = p % 256;
					p = Mathf.FloorToInt(p / 256);
					int g = p % 256;
					p = Mathf.FloorToInt(p / 256);
					int r = p % 256;
					float l = (0.2126f * r / 255f) + 0.7152f * (g / 255f) + 0.0722f * (b / 255f);
					l = l / 3;
					c = new Color(l, l, l, (float)pixel.a / 255.0f);

					t.SetPixel(x, y, c);
				}
			}

			t.Apply(false);

			grey_ = t;
		}

		private Size Aspect(float width, float height, float maxWidth, float maxHeight)
		{
			double ratioX = (double)maxWidth / (double)width;
			double ratioY = (double)maxHeight / (double)height;
			double ratio = ratioX < ratioY ? ratioX : ratioY;

			int newHeight = Convert.ToInt32(Math.Round(height * ratio));
			int newWidth = Convert.ToInt32(Math.Round(width * ratio));

			return new Size(newWidth, newHeight);
		}
	}


	class Image : Panel
	{
		public const int AlignDefault = Align.VCenterCenter;

		public override string TypeName { get { return "Image"; } }

		private ImageObject image_ = null;
		private Texture tex_ = null;
		private int align_ = AlignDefault;


		public Image(int align = AlignDefault)
			: this(null, align)
		{
		}

		public Image(Texture t, int align = AlignDefault)
		{
			tex_ = t;
			align_ = align;
		}

		public Texture Texture
		{
			get { return tex_; }
			set { tex_ = value; TextureChanged();  }
		}

		public int Alignment
		{
			get
			{
				return align_;
			}

			set
			{
				if (align_ != value)
				{
					align_ = value;

					if (image_ != null)
						image_.Alignment = align_;
				}
			}
		}

		protected override void AfterUpdateBounds()
		{
			base.AfterUpdateBounds();

			if (image_ == null)
				image_ = new ImageObject(this, align_);

			TextureChanged();
		}

		protected override void DoSetRender(bool b)
		{
			if (image_ != null)
				image_.SetRender(b);
		}

		protected override void DoSetEnabled(bool b)
		{
			if (image_ != null)
				image_.SetEnabled(b);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return ImageObject.SGetPreferredSize(tex_, maxWidth, maxHeight);
		}

		private void CheckBounds()
		{
			if (image_ != null)
			{
				image_.SetEnabled(Enabled);
				image_.UpdateAspect();
			}
		}

		private void TextureChanged()
		{
			if (image_ != null)
			{
				image_.Texture = tex_;
				CheckBounds();
			}
		}
	}
}
