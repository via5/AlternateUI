using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AUI.SelectUI
{
	class MouseCallbacks : MonoBehaviour, IPointerDownHandler
	{
		private UIPopupButton button_ = null;

		public UIPopupButton Button
		{
			set { button_ = value; }
		}

		public void OnPointerDown(PointerEventData d)
		{
			try
			{
				if (d.button == PointerEventData.InputButton.Middle)
				{
					if (button_ != null)
					{
						var text = button_.GetComponentInChildren<Text>();
						if (text != null)
						{
							var uid = text.text;

							if (uid != "None")
							{
								var atom = SuperController.singleton.GetAtomByUid(uid);

								if (atom == null)
									Log.Error($"selectui: atom {uid} not found");
								else
									SuperController.singleton.RemoveAtom(atom);
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.Error(e.ToString());
			}
		}
	}


	class SelectUI : BasicAlternateUI
	{
		private const float Interval = 0.2f;

		private SuperController sc_;
		private float elapsed_ = 0;

		private string[] lastDisplays_ = null;
		private string[] lastValues_ = null;

		public SelectUI()
			: base("select", "Middle-click remove", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Middle-click atoms in the Select screen to remove them.";
			}
		}

		protected override void DoInit()
		{
			sc_ = SuperController.singleton;
		}

		protected override void DoEnable()
		{
			sc_.onSceneLoadedHandlers += OnSceneLoaded;
			AddCallbacks();
		}

		protected override void DoDisable()
		{
			sc_.onSceneLoadedHandlers -= OnSceneLoaded;
			RemoveCallbacks();
			lastDisplays_ = null;
			lastValues_ = null;
		}

		protected override void DoUpdate(float s)
		{
			elapsed_ += s;

			if (elapsed_ > Interval)
			{
				elapsed_ = 0;

				var popup = SuperController.singleton?.selectAtomPopup;

				if (popup != null && popup.visible)
				{
					if (popup.displayPopupValues != lastDisplays_ ||
						popup.popupValues != lastValues_)
					{
						lastDisplays_ = popup.displayPopupValues;
						lastValues_ = popup.popupValues;

						RefreshCallbacks();
					}
				}
			}
		}

		private void OnSceneLoaded()
		{
			RefreshCallbacks();
		}

		private void RefreshCallbacks()
		{
			sc_.StartCoroutine(RefreshCallbacksCo());
		}

		private IEnumerator RefreshCallbacksCo()
		{
			yield return new WaitForEndOfFrame();
			RemoveCallbacks();
			AddCallbacks();
		}

		private void AddCallbacks()
		{
			foreach (var bt in GetButtons())
			{
				var mc = bt.GetComponent<MouseCallbacks>();

				if (mc != null)
					UnityEngine.Object.Destroy(mc);

				mc = bt.gameObject.AddComponent<MouseCallbacks>();

				mc.enabled = true;
				mc.Button = bt;
			}
		}

		private void RemoveCallbacks()
		{
			foreach (var bt in GetButtons())
			{
				var mc = bt.GetComponent<MouseCallbacks>();

				if (mc != null)
					UnityEngine.Object.Destroy(mc);
			}
		}

		private List<UIPopupButton> GetButtons()
		{
			var list = new List<UIPopupButton>();

			var panel = SuperController.singleton?.selectAtomPopup?.popupPanel;
			if (panel != null)
				list.AddRange(panel.GetComponentsInChildren<UIPopupButton>());

			return list;
		}
	}
}
