using UnityEngine;

namespace VUI
{
	class SplitterHandle : Panel
	{
		public delegate void MovedHandler(float x);
		public event MovedHandler Moved;

		private readonly Splitter sp_;
		private Point dragStart_;
		private float initialPos_ = 0;
		private bool dragging_ = false;

		public SplitterHandle(Splitter sp)
		{
			sp_ = sp;

			Clickthrough = false;
			BackgroundColor = Style.Theme.SplitterHandleBackgroundColor;

			Events.PointerDown += OnPointerDown;
			Events.PointerUp += OnPointerUp;
			Events.DragStart += OnDragStart;
			Events.Drag += OnDrag;
			Events.DragEnd += OnDragEnd;
			Events.PointerEnter += OnPointerEnter;
			Events.PointerExit += OnPointerExit;
		}

		private void OnPointerEnter(PointerEvent e)
		{
			AUI.FileDialog.Icons.GetResizeWE((t) =>
			{
				Cursor.SetCursor(
					t as Texture2D,
					new Vector2(t.width/2, t.height/2), CursorMode.ForceSoftware);
			});
		}

		private void OnPointerExit(PointerEvent e)
		{
			Cursor.SetCursor(null, new Vector2(0, 0), CursorMode.Auto);
		}

		public void OnPointerDown(PointerEvent e)
		{
			dragStart_ = e.Pointer;
			SetCapture();
		}

		public void OnPointerUp(PointerEvent e)
		{
			ReleaseCapture();
		}

		public void OnDragStart(DragEvent e)
		{
			dragging_ = true;
			initialPos_ = sp_.HandlePosition;
		}

		public void OnDrag(DragEvent e)
		{
			if (!dragging_)
				return;

			var p = e.Pointer;
			var delta = p - dragStart_;

			Moved?.Invoke(initialPos_ + delta.X);
		}

		public void OnDragEnd(DragEvent e)
		{
			dragging_ = false;
			ReleaseCapture();
		}
	}


	class Splitter : Panel
	{
		public delegate void Handler();
		public event Handler Moved;

		private Widget first_ = null;
		private Widget second_  = null;
		private readonly SplitterHandle handle_;
		private float handlePos_ = 500;

		public Splitter()
		{
			Layout = new AbsoluteLayout();
			handle_ = new SplitterHandle(this);
			handle_.Moved += OnHandleMoved;
		}

		public Widget First
		{
			get { return first_; }
			set { first_ = value; Rebuild(); }
		}

		public Widget Second
		{
			get { return second_; }
			set { second_ = value; Rebuild(); }
		}

		public float HandlePosition
		{
			get { return handlePos_; }
		}

		private void Rebuild()
		{
			RemoveAllChildren();

			if (first_ != null)
				Add(first_);

			if (second_ != null)
				Add(second_);

			Add(handle_);
		}

		public override void UpdateBounds()
		{
			base.UpdateBounds();

			if (first_ == null || second_ == null)
				return;

			var r = new Rectangle(AbsoluteClientBounds);

			var leftRect = r;
			leftRect.Right = leftRect.Left + handlePos_;

			var handleRect = r;
			handleRect.Left = leftRect.Right;
			handleRect.Right = handleRect.Left + Style.Metrics.SplitterHandleSize;

			var rightRect = r;
			rightRect.Left = handleRect.Right;

			first_.SetBounds(leftRect);
			second_.SetBounds(rightRect);
			handle_.SetBounds(handleRect);

			first_.DoLayout();
			second_.DoLayout();
			handle_.DoLayout();
		}

		private void OnHandleMoved(float x)
		{
			float minFirst = 0;
			if (first_ != null)
				minFirst = first_.GetRealMinimumSize().Width;

			float minSecond = 0;
			if (second_ != null)
				minSecond = second_.GetRealMinimumSize().Width;

			handlePos_ = x;

			if (handlePos_ - ClientBounds.Left < minFirst)
				handlePos_ = ClientBounds.Left + minFirst;

			if (ClientBounds.Right - handlePos_ < minSecond)
				handlePos_ = ClientBounds.Right - minSecond;

			UpdateBounds();
			Moved?.Invoke();
		}
	}
}
