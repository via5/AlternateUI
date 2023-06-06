using System.Collections.Generic;
using uFileBrowser;
using UnityEngine;

namespace AUI.FileDialog
{
	interface IFileDialogHook
	{
		void Enable();
		void Disable();
	}


	class VamosFileDialogHook : IFileDialogHook
	{
		private readonly FileDialogFeature fd_;

		public VamosFileDialogHook(FileDialogFeature fd)
		{
			fd_ = fd;
		}

		public Logger Log
		{
			get { return fd_.Log; }
		}

		public void Enable()
		{
			Vamos.API.Instance.EnableAPI("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool");
			Vamos.API.Instance.uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool += (fb, cb, cd) =>
			{
				if (fd_.IgnoreHandler() || !fd_.ShowHandler(fb, cb, cd))
				{
					Log.Error($"unknown show filebrowser request");
					Log.Error($"fb={fb} title={fb.titleText?.text} ff={fb.fileFormat} path={fb.defaultPath}");

					Vamos.API.Instance.InhibitNext("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserCallback_bool", () =>
					{
						fb.gameObject.SetActive(true);
						fb.transform.parent.gameObject.SetActive(true);
						fb.Show(cb, cd);
					});
				}
			};

			Vamos.API.Instance.EnableAPI("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool");
			Vamos.API.Instance.uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool += (fb, cb, cd) =>
			{
				if (fd_.IgnoreHandler() || !fd_.ShowHandler(fb, cb, cd))
				{
					Log.Error($"unknown show filebrowser request (full)");
					Log.Error($"fb={fb} title={fb.titleText?.text} ff={fb.fileFormat} path={fb.defaultPath}");

					Vamos.API.Instance.InhibitNext("uFileBrowser_FileBrowser_Show__FileBrowser_FileBrowserFullCallback_bool", () =>
					{
						fb.gameObject.SetActive(true);
						fb.transform.parent.gameObject.SetActive(true);
						fb.Show(cb, cd);
					});
				}
			};

			Vamos.API.Instance.EnableAPI("uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool");
			Vamos.API.Instance.uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool += (fb, path, pkgFilter, flatten, includeRegularDirs) =>
			{
				if (fd_.IgnoreHandler() || !fd_.GotoDirectoryHandler(fb, path, pkgFilter, flatten, includeRegularDirs))
				{
					Vamos.API.Instance.InhibitNext("uFileBrowser_FileBrowser_GotoDirectory__FileBrowser_FileBrowserCallback_string_string_bool_bool", () =>
					{
						fb.GotoDirectory(path, pkgFilter, flatten, includeRegularDirs);
					});
				}
			};
		}

		public void Disable()
		{
		}
	}


	class VamFileDialogHook : IFileDialogHook
	{
		private readonly FileDialogFeature fd_;
		private readonly FileBrowser[] browsers_;

		public VamFileDialogHook(FileDialogFeature fd)
		{
			fd_ = fd;

			browsers_ = new FileBrowser[]
			{
				SuperController.singleton.fileBrowserUI,
				SuperController.singleton.fileBrowserWorldUI,
				SuperController.singleton.templatesFileBrowserWorldUI,
				SuperController.singleton.mediaFileBrowserUI,
				SuperController.singleton.directoryBrowserUI,
			};
		}

		public Logger Log
		{
			get { return fd_.Log; }
		}

		public void Enable()
		{
			foreach (var b in browsers_)
				SetHandlers(b, false);
		}

		public void Disable()
		{
			foreach (var b in browsers_)
				SetHandlers(b, true);
		}

		private void SetHandlers(FileBrowser fb, bool clear)
		{
			SetShowHandler(fb, clear);
			SetShowFullHandler(fb, clear);
			SetGotoDirectoryHandler(fb, clear);
		}

		private void SetShowHandler(FileBrowser fb, bool clear)
		{
			if (clear)
				fb.showHandler = null;
			else
				fb.showHandler = (cb, cd) => ShowHandler(fb, cb, cd);
		}

		private void SetShowFullHandler(FileBrowser fb, bool clear)
		{
			if (clear)
				fb.showFullHandler = null;
			else
				fb.showFullHandler = (cb, cd) => ShowFullHandler(fb, cb, cd);
		}

		private void SetGotoDirectoryHandler(FileBrowser fb, bool clear)
		{
			if (clear)
				fb.gotoDirectoryHandler = null;
			else
				fb.gotoDirectoryHandler = (p, pk, f, i) => GotoDirectoryHandler(fb, p, pk, f, i);
		}

		private void ShowHandler(FileBrowser fb, FileBrowserCallback cb, bool cd)
		{
			if (fd_.IgnoreHandler() || !fd_.ShowHandler(fb, cb, cd))
			{
				Log.Error($"unknown show filebrowser request");
				Log.Error($"fb={fb} title={fb.titleText?.text} ff={fb.fileFormat} path={fb.defaultPath}");

				try
				{
					SetShowHandler(fb, true);

					fb.gameObject.SetActive(true);
					fb.transform.parent.gameObject.SetActive(true);
					fb.Show(cb, cd);
				}
				finally
				{
					SetShowHandler(fb, false);
				}
			}
		}

		private void ShowFullHandler(FileBrowser fb, FileBrowserFullCallback cb, bool cd)
		{
			if (fd_.IgnoreHandler() || !fd_.ShowHandler(fb, cb, cd))
			{
				Log.Error($"unknown show filebrowser request (full)");
				Log.Error($"fb={fb} title={fb.titleText?.text} ff={fb.fileFormat} path={fb.defaultPath}");

				try
				{
					SetShowFullHandler(fb, true);

					fb.gameObject.SetActive(true);
					fb.transform.parent.gameObject.SetActive(true);
					fb.Show(cb, cd);
				}
				finally
				{
					SetShowFullHandler(fb, false);
				}
			}
		}

		private void GotoDirectoryHandler(FileBrowser fb, string path, string pkgFilter, bool flatten, bool includeRegularDirs)
		{
			if (fd_.IgnoreHandler() || !fd_.GotoDirectoryHandler(fb, path, pkgFilter, flatten, includeRegularDirs))
			{
				try
				{
					SetGotoDirectoryHandler(fb, true);
					fb.GotoDirectory(path, pkgFilter, flatten, includeRegularDirs);
				}
				finally
				{
					SetGotoDirectoryHandler(fb, false);
				}
			}
		}
	}


	class FileDialogFeature : UIReplacementFeature
	{
		private FileDialog fd_ = new FileDialog();
		private IFileDialogHook hook_;

		public static string DefaultSceneExtension = ".json";
		public static FS.Extension[] SceneExtensions = new FS.Extension[]
		{
			new FS.Extension("JSON files", ".json"),
			new FS.Extension("VAC files", ".vac"),
			new FS.Extension("Zip files", ".zip"),
		};

		public static string DefaultCUAExtension = ".assetbundle";
		public static FS.Extension[] CUAExtensions = new FS.Extension[]
		{
			new FS.Extension("Asset bundles", ".assetbundle")
		};

		public static string DefaultPluginExtension = ".cslist";
		public static FS.Extension[] PluginExtensions = new FS.Extension[]
		{
			new FS.Extension("cslist files", ".cslist"),
			new FS.Extension("C# files", ".cs"),
			new FS.Extension("DLL files", ".dll"),
		};

		public static string DefaultPresetExtension = ".vap";
		public static FS.Extension[] PresetExtensions = new FS.Extension[]
		{
			new FS.Extension("VAP files", ".vap")
		};

		public static string DefaultLegacyPresetExtension = ".json";
		public static FS.Extension[] LegacyPresetExtensions = new FS.Extension[]
		{
			new FS.Extension("JSON files", ".json"),
			new FS.Extension("VAC files", ".vac"),
			new FS.Extension("Zip files", ".zip")
		};

		public static FS.Extension[] TextureExtensions = new FS.Extension[]
		{
			new FS.Extension("JPG files", new string[]{ ".jpg", ".jpeg" }),
			new FS.Extension("PNG files", ".png"),
			new FS.Extension("TIF files", new string[]{ ".tif", ".tiff" })
		};

		public static FS.Extension[] SoundExtensions = new FS.Extension[]
		{
			new FS.Extension("MP3 files", ".mp3"),
			new FS.Extension("OGG files", ".ogg"),
			new FS.Extension("WAV files", ".wav")
		};


		public FileDialogFeature()
			: base("fileDialog", "File dialog", false)
		{
			hook_ = new VamFileDialogHook(this);
		}

		public static ExtensionItem[] GetSceneExtensions(bool includeAll)
		{
			return GetExtensions(SceneExtensions, "All scene files", includeAll);
		}

		public static ExtensionItem[] GetCUAExtensions(bool includeAll)
		{
			return GetExtensions(CUAExtensions, "All CUA files", includeAll);
		}

		public static ExtensionItem[] GetPluginExtensions(bool includeAll)
		{
			return GetExtensions(PluginExtensions, "All plugin files", includeAll);
		}

		public static ExtensionItem[] GetPresetExtensions(bool includeAll)
		{
			return GetExtensions(PresetExtensions, "All preset files", includeAll);
		}

		public static ExtensionItem[] GetLegacyPresetExtensions(bool includeAll)
		{
			return GetExtensions(LegacyPresetExtensions, "All preset files", includeAll);
		}

		public static ExtensionItem[] GetTextureExtensions(bool includeAll)
		{
			return GetExtensions(TextureExtensions, "All texture files", includeAll);
		}

		public static ExtensionItem[] GetSoundExtensions(bool includeAll)
		{
			return GetExtensions(SoundExtensions, "All sound files", includeAll);
		}


		public static ExtensionItem[] GetExtensions(
			FS.Extension[] exts, string allText, bool includeAll)
		{
			var list = new List<ExtensionItem>();

			if (includeAll && exts.Length > 1)
			{
				string all = "";
				var allExts = new List<string>();

				foreach (var e in exts)
				{
					if (all != "")
						all += "; ";

					all += e.ExtensionString;

					foreach (var ee in e.Extensions)
						allExts.Add(ee);
				}

				all = $"{allText} ({all})";
				list.Add(new ExtensionItem(all, allExts.ToArray()));
			}

			foreach (var e in exts)
				list.Add(new ExtensionItem(e.Name + " (" + e.ExtensionString + ")", e.Extensions));

			if (includeAll)
			{
				list.Add(new ExtensionItem("All files (*.*)", new string[] { "*.*" }));
			}

			return list.ToArray();
		}

		public override string Description
		{
			get { return "Replaces the file dialogs."; }
		}

		protected override void DoEnable()
		{
			FS.Filesystem.Init();

			fd_.Enable();
			hook_.Enable();

			//fd_.Show(Modes.OpenPreset("Custom/Atom/Person/Plugins"), null, "VaM/Custom/Atom/Person/Plugins");
			//fd_.Show(Modes.OpenScene(), null);
		}

		protected override void DoDisable()
		{
			hook_.Disable();
			fd_.Disable();
		}

		protected override void DoUpdate(float s)
		{
			fd_.Update(s);
		}

		public bool IgnoreHandler()
		{
			return
				UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftShift) ||
				UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightShift);
		}

		public bool ShowHandler(FileBrowser fb, FileBrowserCallback cb, bool cd)
		{
			string path = FS.Path.Normalize(fb.defaultPath);

			Log.Verbose(
				$"show filebrowser request fb={fb} " +
				$"title={fb.titleText?.text} ff={fb.fileFormat} " +
				$"path={fb.defaultPath} normPath={path}");

			var t = fb.titleText?.text ?? "";

			if (fb == SuperController.singleton.fileBrowserUI)
			{
				if (t == "Select Scene For Merge")
				{
					Show(Modes.OpenScene("Merge scene"), cb);
					return true;
				}
				else if (t == "Select Scene For Edit")
				{
					Show(Modes.OpenScene("Open scene for edit"), cb);
					return true;
				}
				else if (t == "Select Scene To Load")
				{
					Show(Modes.OpenScene(), cb);
					return true;
				}
				else if (t == "Select Save File")
				{
					Show(Modes.SaveScene(), cb);
					return true;
				}
				else if (t == "Select Preset File")
				{
					if (fb.fileFormat == "json|vac|zip")
					{
						Show(Modes.OpenPreset(path, fb.fileRemovePrefix), cb, null);
						return true;
					}
				}
				else if (t == "Select Save Preset File")
				{
					if (fb.fileFormat == "json|vac|zip")
					{
						Show(Modes.SavePreset(path), cb, null);
						return true;
					}
				}
			}
			else if (fb == SuperController.singleton.mediaFileBrowserUI)
			{
				if (t == "Select File")
				{
					if (fb.fileFormat == "mp3|ogg|wav")
					{
						Show(Modes.OpenSound(), cb, null);
						return true;
					}
				}
			}

			return false;
		}

		public bool ShowHandler(FileBrowser fb, FileBrowserFullCallback cb, bool cd)
		{
			string path = FS.Path.Normalize(fb.defaultPath);

			Log.Info(
				$"show filebrowser request (full) fb={fb} " +
				$"title={fb.titleText?.text} ff={fb.fileFormat} " +
				$"path={fb.defaultPath} normPath={path} " +
				$"remove={fb.fileRemovePrefix}");

			if (fb == SuperController.singleton.mediaFileBrowserUI)
			{
				var t = fb.titleText?.text ?? "";

				if (t == "Select File")
				{
					if (fb.fileFormat == "assetbundle|scene")
					{
						Show(Modes.OpenCUA(), cb);
						return true;
					}
					else if (fb.fileFormat == "cs|cslist|dll")
					{
						Show(Modes.OpenPlugin(), cb);
						return true;
					}
					else if (fb.fileFormat == "vap")
					{
						Show(Modes.OpenPreset(path, fb.fileRemovePrefix), cb, null);
						return true;
					}
					else if (fb.fileFormat == "jpg|jpeg|png|tif|tiff")
					{
						Show(Modes.OpenTexture(path), cb, null);
						return true;
					}
				}
			}

			return false;
		}

		public bool GotoDirectoryHandler(
			FileBrowser fb, string originalPath, string pkgFilter,
			bool flatten, bool includeRegularDirs)
		{
			if (!fd_.Visible)
			{
				return false;
			}

			string path = FS.Path.MakeFSPath(originalPath);

			Log.Info(
				$"goto directory fb={fb} " +
				$"title={fb.titleText?.text} ff={fb.fileFormat} " +
				$"path={originalPath} normPath={path}");

			fd_.SelectDirectory(
				path, FileDialog.SelectDirectoryNoFlags,
				VUI.TreeView.ScrollToCenter);

			return true;
		}


		private void Show(IFileDialogMode mode, FileBrowserCallback cb, string cwd = null)
		{
			var f = GetFilename();

			if (f != null)
			{
				cwd = FS.Path.Parent(f);
				Log.Verbose($"show: using filename '{f}', cwd={cwd}");
			}

			fd_.Show(mode, (path) => cb?.Invoke(path), cwd, f);
		}

		private void Show(IFileDialogMode mode, FileBrowserFullCallback cb, string cwd = null)
		{
			var f = GetFilename();

			if (f != null)
			{
				cwd = FS.Path.Parent(f);
				Log.Verbose($"show: using filename '{f}', cwd={cwd}");
			}

			fd_.Show(mode, (path) => cb?.Invoke(path, true), cwd, f);
		}

		private string GetFilename()
		{
			var button = UnityEngine.EventSystems.EventSystem
				.current.currentSelectedGameObject;

			Log.Verbose($"sel={button}");

			if (button == null || button.GetComponent<UnityEngine.UI.Button>() == null)
				return null;

			string f = GetFilenameFromSelect(button);
			if (f != null)
				return f;

			f = GetFilenameForPlugin(button);
			if (f != null)
				return f;

			return null;
		}

		private string GetFilenameFromSelect(GameObject button)
		{
			var parent = button.transform.parent;
			if (parent == null)
				return null;

			foreach (Transform child in parent)
			{
				if (!child.name.Contains("Url"))
					continue;

				var s = child.GetComponent<UnityEngine.UI.Text>()?.text;
				if (string.IsNullOrEmpty(s))
					continue;

				if (s == "NULL")
					return null;

				return FS.Path.MakeFSPathFromShort(s);
			}

			return null;
		}

		private string GetFilenameForPlugin(GameObject button)
		{
			var parent = button.transform.parent;
			if (parent == null)
			{
				Log.Verbose($"button {button} has no parent");
				return null;
			}

			var panel = parent.Find("Panel");
			if (panel == null)
			{
				Log.Verbose($"button {button} parent {parent} has no Panel child");
				return null;
			}

			var url = panel.Find("URL");
			if (url == null)
			{
				Log.Verbose($"panel {panel} has no URL child");
				return null;
			}

			var text = url?.GetComponent<UnityEngine.UI.Text>()?.text;
			if (string.IsNullOrEmpty(text))
			{
				Log.Verbose($"url {url} has text");
				return null;
			}

			if (text == "NULL")
				return null;

			return FS.Path.MakeFSPathFromShort(text);
		}
	}
}
