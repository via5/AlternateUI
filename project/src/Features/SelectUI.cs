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
		private SelectUI ui_ = null;
		private UIPopupButton button_ = null;

		public void Set(SelectUI ui, UIPopupButton button)
		{
			ui_ = ui;
			button_ = button;
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
							ui_.RemoveAtom(uid);
						}
					}
				}
			}
			catch (Exception e)
			{
				ui_.Log.Error(e.ToString());
			}
		}
	}


	class SelectUI : TweakFeature
	{
		private const float Interval = 0.2f;

		private SuperController sc_;
		private float elapsed_ = 0;
		private ScrollRect sr_ = null;

		private string[] lastDisplays_ = null;
		private string[] lastValues_ = null;

		public SelectUI()
			: base("select", "Remove atom middle-click", true)
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

		public void RemoveAtom(string uid)
		{
			if (uid == "None")
				return;

			var atom = SuperController.singleton.GetAtomByUid(uid);
			if (atom == null)
			{
				Log.Error($"atom {uid} not found");
				return;
			}

			float newPos = GetAdjustedScrollBarPosition();

			SuperController.singleton.RemoveAtom(atom);

			if (newPos >= 0)
				AlternateUI.Instance.StartCoroutine(CoRestoreScroll(newPos));

			RefreshCallbacks();
		}

		private float GetAdjustedScrollBarPosition()
		{
			var panel = SuperController.singleton.selectAtomPopup?.popupPanel;

			if (sr_ == null)
				sr_ = panel?.GetComponentInChildren<ScrollRect>(true);

			if (sr_ == null)
				return -1;

			// current scrollbar position [0, 1]
			var sb = sr_.verticalNormalizedPosition;

			// any button in the list
			var button = panel.GetComponentInChildren<UIPopupButton>();
			if (button == null)
				return -1;

			// these pixels will disappear from the list
			var buttonHeight = button.GetComponent<RectTransform>().rect.height;

			// current height of the content that's outside the viewport
			var currentHeight = sr_.content.rect.height - sr_.viewport.rect.height;

			// new height of the content once the button is gone
			var newHeight = currentHeight - buttonHeight;

			// happens when there's not enough buttons for the scrollbar anymore
			if (currentHeight <= 0 || newHeight <= 0)
				return -1;

			// current position in pixel of the scrollbar
			var currentPos = sb * currentHeight;

			// the new position is the old minus the button height that's gone
			float newPos = (currentPos - buttonHeight) / newHeight;

			// clamp to [0, 1], anything < 0 is considered an error
			return Mathf.Clamp01(newPos);
		}

		private IEnumerator CoRestoreScroll(float newPos)
		{
			yield return new WaitForEndOfFrame();

			if (sr_ != null)
				sr_.verticalScrollbar.value = newPos;
		}

		protected override void DoInit()
		{
			sc_ = SuperController.singleton;
		}

		protected override void DoEnable()
		{
			sc_.onSceneLoadedHandlers += RefreshCallbacks;
			sc_.onAtomAddedHandlers += OnAtomAdded;
			sc_.onAtomRemovedHandlers += OnAtomRemoved;
			sc_.onAtomUIDRenameHandlers += OnAtomRenamed;

			AddCallbacks();
		}

		protected override void DoDisable()
		{
			sc_.onSceneLoadedHandlers -= RefreshCallbacks;
			sc_.onAtomAddedHandlers -= OnAtomAdded;
			sc_.onAtomRemovedHandlers -= OnAtomRemoved;
			sc_.onAtomUIDRenameHandlers -= OnAtomRenamed;

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

		private void OnAtomAdded(Atom a)
		{
			try
			{
				RefreshCallbacks();
			}
			catch (Exception e)
			{
				Log.Error($"exception in OnAtomAdded:");
				Log.Error(e.ToString());
			}
		}

		private void OnAtomRemoved(Atom a)
		{
			try
			{
				RefreshCallbacks();
			}
			catch (Exception e)
			{
				Log.Error($"exception in OnAtomRemoved:");
				Log.Error(e.ToString());
			}
		}

		private void OnAtomRenamed(string a, string b)
		{
			try
			{
				RefreshCallbacks();
			}
			catch (Exception e)
			{
				Log.Error($"exception in OnAtomRemoved:");
				Log.Error(e.ToString());
			}
		}

		private void RefreshCallbacks()
		{
			try
			{
				sc_.StartCoroutine(RefreshCallbacksCo());
			}
			catch (Exception e)
			{
				Log.Error($"exception in RefreshCallbacks:");
				Log.Error(e.ToString());
			}
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

				mc.Set(this, bt);
				mc.enabled = true;
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
