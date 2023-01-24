using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AUI.SkinUI
{
	class MouseCallbacks : MonoBehaviour, IPointerDownHandler
	{
		private JSONStorableUrl url_ = null;

		public JSONStorableUrl Url
		{
			set { url_ = value; }
		}

		public void OnPointerDown(PointerEventData d)
		{
			try
			{
				if (d != null && d.button == PointerEventData.InputButton.Right)
				{
					if (url_ != null)
					{
						Log.Info($"reloading {url_.name}");
						url_.Reload();
					}
				}
			}
			catch (Exception e)
			{
				Log.Error(e.ToString());
			}
		}
	}

	class SkinUI : BasicAlternateUI
	{
		private SuperController sc_ = SuperController.singleton;

		public SkinUI()
			: base("skin", "Right-click skin texture reload", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Right-click the Select button in the Skin Textures " +
					"panel to reload the texture.";
			}
		}

		protected override void DoInit()
		{
			RefreshCallbacks();
		}

		private void RefreshCallbacks()
		{
			foreach (var a in SuperController.singleton.GetAtoms())
				RefreshCallbacks(a);
		}

		private void RefreshCallbacks(Atom a)
		{
			RemoveCallbacks(a);
			AddCallbacks(a);
		}

		protected override void DoEnable()
		{
			sc_.onAtomAddedHandlers += AtomAdded;
			sc_.onAtomRemovedHandlers += AtomRemoved;
			sc_.onSceneLoadedHandlers += OnSceneLoaded;
		}

		protected override void DoDisable()
		{
			sc_.onAtomAddedHandlers -= AtomAdded;
			sc_.onAtomRemovedHandlers -= AtomRemoved;
			sc_.onSceneLoadedHandlers -= OnSceneLoaded;
		}

		private void AtomAdded(Atom a)
		{
			RefreshCallbacks(a);
		}

		private void AtomRemoved(Atom a)
		{
			RemoveCallbacks(a);
		}

		private void OnSceneLoaded()
		{
			RefreshCallbacks();
		}

		private void AddCallbacks(Atom a)
		{
			var c = a.GetComponentInChildren<DAZCharacterSelector>();
			if (c == null)
				return;

			AddCallbacks(a,
				c.characterTextureUI?.faceDiffuseFileBrowseButton,
				"faceDiffuseUrl");

			AddCallbacks(a,
				c.characterTextureUI?.torsoDiffuseFileBrowseButton,
				"torsoDiffuseUrl");

			AddCallbacks(a,
				c.characterTextureUI?.limbsDiffuseFileBrowseButton,
				"limbsDiffuseUrl");

			AddCallbacks(a,
				c.characterTextureUI?.genitalsDiffuseFileBrowseButton,
				"genitalsDiffuseUrl");


			AddCallbacks(a,
				c.characterTextureUI?.faceDecalFileBrowseButton,
				"faceDecalUrl");

			AddCallbacks(a,
				c.characterTextureUI?.torsoDecalFileBrowseButton,
				"torsoDecalUrl");

			AddCallbacks(a,
				c.characterTextureUI?.limbsDecalFileBrowseButton,
				"limbsDecalUrl");

			AddCallbacks(a,
				c.characterTextureUI?.genitalsDecalFileBrowseButton,
				"genitalsDecalUrl");


			AddCallbacks(a,
				c.characterTextureUI?.faceSpecularFileBrowseButton,
				"faceSpecularUrl");

			AddCallbacks(a,
				c.characterTextureUI?.torsoSpecularFileBrowseButton,
				"torsoSpecularUrl");

			AddCallbacks(a,
				c.characterTextureUI?.limbsSpecularFileBrowseButton,
				"limbsSpecularUrl");

			AddCallbacks(a,
				c.characterTextureUI?.genitalsSpecularFileBrowseButton,
				"genitalsSpecularUrl");


			AddCallbacks(a,
				c.characterTextureUI?.faceNormalFileBrowseButton,
				"faceNormalUrl");

			AddCallbacks(a,
				c.characterTextureUI?.torsoNormalFileBrowseButton,
				"torsoNormalUrl");

			AddCallbacks(a,
				c.characterTextureUI?.limbsNormalFileBrowseButton,
				"limbsNormalUrl");

			AddCallbacks(a,
				c.characterTextureUI?.genitalsNormalFileBrowseButton,
				"genitalsNormalUrl");


			AddCallbacks(a,
				c.characterTextureUI?.faceGlossFileBrowseButton,
				"faceGlossUrl");

			AddCallbacks(a,
				c.characterTextureUI?.torsoGlossFileBrowseButton,
				"torsoGlossUrl");

			AddCallbacks(a,
				c.characterTextureUI?.limbsGlossFileBrowseButton,
				"limbsGlossUrl");

			AddCallbacks(a,
				c.characterTextureUI?.genitalsGlossFileBrowseButton,
				"genitalsGlossUrl");
		}

		private void AddCallbacks(Atom a, UnityEngine.UI.Button b, string param)
		{
			var url = a.GetStorableByID("textures")?.GetUrlJSONParam(param);

			if (b == null || url == null)
				return;

			RemoveCallbacks(b);
			AddCallbacks(b, url);
		}

		private void AddCallbacks(UnityEngine.UI.Button bt, JSONStorableUrl url)
		{
			var mc = bt.GetComponent<MouseCallbacks>();

			if (mc != null)
				UnityEngine.Object.Destroy(mc);

			mc = bt.gameObject.AddComponent<MouseCallbacks>();

			mc.enabled = true;
			mc.Url = url;
		}


		private void RemoveCallbacks(Atom a)
		{
			var c = a.GetComponentInChildren<DAZCharacterSelector>();
			if (c == null)
				return;

			var tui = c.characterTextureUI;
			if (tui == null)
				return;

			RemoveCallbacks(tui.faceDiffuseFileBrowseButton);
			RemoveCallbacks(tui.torsoDiffuseFileBrowseButton);
			RemoveCallbacks(tui.limbsDiffuseFileBrowseButton);
			RemoveCallbacks(tui.genitalsDiffuseFileBrowseButton);

			RemoveCallbacks(tui.faceDecalFileBrowseButton);
			RemoveCallbacks(tui.torsoDecalFileBrowseButton);
			RemoveCallbacks(tui.limbsDecalFileBrowseButton);
			RemoveCallbacks(tui.genitalsDecalFileBrowseButton);

			RemoveCallbacks(tui.faceSpecularFileBrowseButton);
			RemoveCallbacks(tui.torsoSpecularFileBrowseButton);
			RemoveCallbacks(tui.limbsSpecularFileBrowseButton);
			RemoveCallbacks(tui.genitalsSpecularFileBrowseButton);

			RemoveCallbacks(tui.faceNormalFileBrowseButton);
			RemoveCallbacks(tui.torsoNormalFileBrowseButton);
			RemoveCallbacks(tui.limbsNormalFileBrowseButton);
			RemoveCallbacks(tui.genitalsNormalFileBrowseButton);

			RemoveCallbacks(tui.faceGlossFileBrowseButton);
			RemoveCallbacks(tui.torsoGlossFileBrowseButton);
			RemoveCallbacks(tui.limbsGlossFileBrowseButton);
			RemoveCallbacks(tui.genitalsGlossFileBrowseButton);
		}

		private void RemoveCallbacks(UnityEngine.UI.Button bt)
		{
			if (bt == null)
				return;

			foreach (var c in bt.GetComponents<Component>())
			{
				if (c.GetType().ToString().Contains("SkinUI.MouseCallbacks"))
					UnityEngine.Object.Destroy(c);
			}
		}
	}
}
