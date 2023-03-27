using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AUI.LogUI
{
	class LogUI : TweakFeature
	{
		// changing the font breaks something in
		// InputFieldAutoSizing.SyncPreferredHeight(), the calculated
		// preferredHeight is incorrect by a couple of pixels, cutting off the
		// last line of the log
		//
		// there's nothing that can be hooked into, so this checks whether the
		// error/message count has changed to adjust the height of the content
		//
		abstract class LogPanel
		{
			private readonly LogUI ui_;
			private readonly Transform rootPanel_;
			private InputFieldAutoSizing field_ = null;
			private Text text_ = null;
			private LayoutElement content_ = null;

			private int oldFontSize_ = -1;
			private Font oldFont_ = null;
			private bool oldResizeTextForBestFit_ = false;

			private bool wasHidden_ = true;
			private int lastCount_ = -1;

			public LogPanel(LogUI ui, Transform rootPanel)
			{
				ui_ = ui;
				rootPanel_ = rootPanel;

				// viewport
				var viewport = VUI.Utilities.FindChildRecursive(rootPanel, "Viewport");
				if (viewport == null)
				{
					ui_.Log.Error($"no viewport in log panel {rootPanel.name}");
					return;
				}

				// input field
				field_ = VUI.Utilities.FindChildRecursive(viewport, "AutoSizingInputField")
					?.GetComponent<InputFieldAutoSizing>();

				if (field_ == null)
				{
					ui_.Log.Error($"no field in log panel {rootPanel.name}");
					return;
				}

				// text
				text_ = VUI.Utilities.FindChildRecursive(field_, "Text")
					?.GetComponent<Text>();

				if (text_ == null)
				{
					ui_.Log.Error($"no text in log panel {rootPanel.name}");
					return;
				}

				// content
				content_ = VUI.Utilities.FindChildRecursive(rootPanel_, "Content")
					?.GetComponent<LayoutElement>();

				if (content_ == null)
				{
					ui_.Log.Error($"no content in log panel {rootPanel.name}");
					return;
				}

				// reset the height to its (invalid) value so the adjustments
				// don't continually make the panel longer when reloading
				field_.onValueChanged?.Invoke(field_.text);
			}

			// the getter is different for the error and message logs
			//
			public abstract int EntryCount { get; }

			public void Update(float s)
			{
				if (wasHidden_ && rootPanel_.gameObject.activeInHierarchy)
				{
					// just shown on screen, fix the height
					wasHidden_ = false;
					lastCount_ = EntryCount;
					FixHeight();
				}
				else if (rootPanel_.gameObject.activeInHierarchy)
				{
					if (EntryCount != lastCount_)
					{
						// count has changed, fix the height
						lastCount_ = EntryCount;
						FixHeight();
					}
				}
			}

			public void FixHeight()
			{
				if (content_ != null && field_ != null)
					SuperController.singleton.StartCoroutine(CoFixHeight());
			}

			private IEnumerator CoFixHeight()
			{
				// this needs to wait until the next frame to adjust the height
				// because InputFieldAutoSizing.SyncPreferredHeight() could be
				// called after this, invalidating the changes
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				// after adjusting the height, the scrollbar won't be at the
				// bottom anymore, so scroll back down if necessary
				bool scrollToBottom =
					field_.scrollFollowIfAtBottom &&
					field_.scrollRect.verticalScrollbar.value < 0.01f;

				// hack
				content_.preferredHeight += 20;

				if (scrollToBottom)
				{
					// need to wait one more frame for the new preferred height
					// to get picked up and make the scrollbar longer
					yield return new WaitForEndOfFrame();

					// bottom
					field_.scrollRect.verticalNormalizedPosition = 0f;
				}
			}

			public void Enable()
			{
				if (text_ == null)
					return;

				var f = VUI.Style.Theme.MonospaceFont;

				// remember
				oldFontSize_ = text_.fontSize;
				oldFont_ = text_.font;
				oldResizeTextForBestFit_ = text_.resizeTextForBestFit;

				// set
				text_.resizeTextForBestFit = false;
				text_.font = f;
				text_.fontSize = f.fontSize;
			}

			public void Disable()
			{
				if (text_ == null)
					return;

				// restore
				text_.resizeTextForBestFit = oldResizeTextForBestFit_;
				text_.font = oldFont_;
				text_.fontSize = oldFontSize_;
			}
		}


		class ErrorLogPanel : LogPanel
		{
			public ErrorLogPanel(LogUI ui)
				: base(ui, SuperController.singleton.errorLogPanel)
			{
			}

			public override int EntryCount
			{
				get { return SuperController.singleton.errorCount; }
			}
		}


		class MessageLogPanel : LogPanel
		{
			public MessageLogPanel(LogUI ui)
				: base(ui, SuperController.singleton.msgLogPanel)
			{
			}

			public override int EntryCount
			{
				get { return SuperController.singleton.msgCount; }
			}
		}


		private LogPanel[] panels_ = null;


		public LogUI()
			: base("log", "Monospace log", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Changes the error log panel to use Consolas with a " +
					"smaller font size.";
			}
		}

		protected override void DoInit()
		{
			panels_ = new LogPanel[]
			{
				new ErrorLogPanel(this),
				new MessageLogPanel(this)
			};
		}

		protected override void DoUpdate(float s)
		{
			for (int i = 0; i < panels_.Length; ++i)
				panels_[i].Update(s);
		}

		protected override void DoEnable()
		{
			for (int i = 0; i < panels_.Length; ++i)
				panels_[i].Enable();
		}

		protected override void DoDisable()
		{
			for (int i = 0; i < panels_.Length; ++i)
				panels_[i].Disable();
		}
	}
}
