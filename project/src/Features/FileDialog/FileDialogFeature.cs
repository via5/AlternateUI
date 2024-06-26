﻿using System.Collections.Generic;
using uFileBrowser;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AUI.FileDialog
{
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
			hook_ = CreateHook();
		}

		public override Availability GetAvailability()
		{
			if (hook_ == null)
				return Availability.No("Requires VaM 1.22 or higher.");
			else
				return Availability.Yes();
		}

		private IFileDialogHook CreateHook()
		{
#if VAM_GT_1_22
			return new VamHook(this);
#else
			//return new VamosHook(this);
			return null;
#endif
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

		public static ExtensionItem[] GetAnyExtensions(string fileFormat, bool includeAll)
		{
			var formats = fileFormat.Split('|');

			var list = new List<ExtensionItem>();

			foreach (var f in formats)
			{
				var tf = f?.Trim();
				if (string.IsNullOrEmpty(tf))
					continue;

				list.Add(new ExtensionItem($"{tf} (*.{tf})", new string[] { $".{tf}" }));
			}

			if (list.Count == 0 || includeAll)
				list.Add(GetAnyExtension());

			return list.ToArray();
		}


		private static ExtensionItem GetAnyExtension()
		{
			return new ExtensionItem("All files (*.*)", new string[] { "*.*" });
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
				list.Add(GetAnyExtension());

			return list.ToArray();
		}

		public override string Description
		{
			get { return "Replaces the file dialogs. Requires VaM 1.22 or higher."; }
		}

		protected override void DoEnable()
		{
			FS.Filesystem.Init();

			fd_.Enable();
			hook_.Enable();
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

			Log.Info(
				$"show filebrowser request fb={fb} " +
				$"title={fb.titleText?.text} ff={fb.fileFormat} " +
				$"path={fb.defaultPath} normPath={path}");

			var m = FindMode(fb, false);
			if (m != null)
			{
				Show(m, cb);
				return true;
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

			var m = FindMode(fb, true);
			if (m != null)
			{
				Show(m, cb);
				return true;
			}

			return false;
		}

		IFileDialogMode FindMode(FileBrowser fb, bool full)
		{
			string path = FS.Path.Normalize(fb.defaultPath);
			string t = fb.titleText?.text ?? "";
			var a = SuperController.singleton.GetSelectedAtom();

			if (fb == SuperController.singleton.fileBrowserUI)
			{
				if (!full)
				{
					if (t == "Select Scene For Merge")
					{
						return Modes.OpenScene("Merge scene", false);
					}
					else if (t == "Select Scene For Edit")
					{
						return Modes.OpenScene("Open scene for edit");
					}
					else if (t == "Select Scene To Load")
					{
						return Modes.OpenScene();
					}
					else if (t == "Select Save File")
					{
						return Modes.SaveScene();
					}
					else if (t == "Select Preset File")
					{
						if (fb.fileFormat == "json|vac|zip")
							return Modes.OpenPreset(path, fb.fileRemovePrefix, a);
					}
					else if (t == "Select Save Preset File")
					{
						if (fb.fileFormat == "json|vac|zip")
							return Modes.SavePreset(path, a);
					}
				}
				else
				{
					if (t == "Select Save File")
					{
						return Modes.SaveAny(path, fb.fileFormat);
					}
				}
			}
			else if (fb == SuperController.singleton.mediaFileBrowserUI)
			{
				if (t == "Select File")
				{
					if (fb.fileFormat == "mp3|ogg|wav")
					{
						return Modes.OpenSound();
					}
					else if (fb.fileFormat == "assetbundle|scene")
					{
						return Modes.OpenCUA();
					}
					else if (fb.fileFormat == "cs|cslist|dll")
					{
						return Modes.OpenPlugin();
					}
					else if (fb.fileFormat == "vap")
					{
						return Modes.OpenPreset(path, fb.fileRemovePrefix, a);
					}
					else if (fb.fileFormat == "jpg|jpeg|png|tif|tiff")
					{
						string root = null;

						foreach (var sc in fb.shortCuts)
						{
							var nsc = FS.Path.Normalize(sc.path);
							if (nsc.StartsWith("Custom/"))
							{
								root = nsc;
								break;
							}
						}

						return Modes.OpenTexture(path, root);
					}
					else
					{
						return Modes.OpenAny(path, fb.fileFormat);
					}
				}
				else if (t == "Select Save File")
				{
					return Modes.SaveAny(path, fb.fileFormat);
				}
			}

			return null;
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
				Log.Info($"show: using filename '{f}', cwd={cwd}");
			}

			fd_.Show(mode, (path) => cb?.Invoke(path), cwd, f);
		}

		private void Show(IFileDialogMode mode, FileBrowserFullCallback cb, string cwd = null)
		{
			var f = GetFilename();

			if (f != null)
			{
				cwd = FS.Path.Parent(f);
				Log.Info($"show: using filename '{f}', cwd={cwd}");
			}

			fd_.Show(mode, (path) => cb?.Invoke(path, true), cwd, f);
		}

		private UnityEngine.UI.Button GetClickedButton()
		{
			PointerEventData pointer = new PointerEventData(EventSystem.current);
			pointer.position = Input.mousePosition;

			List<RaycastResult> raycastResults = new List<RaycastResult>();
			EventSystem.current.RaycastAll(pointer, raycastResults);

			if (raycastResults.Count > 0)
			{
				foreach (var go in raycastResults)
				{
					if (go.gameObject != null)
					{
						var b = go.gameObject.GetComponent<UnityEngine.UI.Button>();
						if (b != null)
							return b;
					}
				}
			}

			return null;
		}

		private string GetFilename()
		{
			var button = GetClickedButton();
			if (button == null)
				return null;

			string f = GetFilenameFromSelect(button.gameObject);
			if (f != null)
				return f;

			f = GetFilenameForPlugin(button.gameObject);
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
				return null;

			var panel = parent.Find("Panel");
			if (panel == null)
				return null;

			var url = panel.Find("URL");
			if (url == null)
				return null;

			var text = url?.GetComponent<UnityEngine.UI.Text>()?.text;
			if (string.IsNullOrEmpty(text))
				return null;

			if (text == "NULL")
				return null;

			return FS.Path.MakeFSPathFromShort(text);
		}
	}
}
