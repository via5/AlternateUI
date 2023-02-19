using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

namespace VUI
{
	class ScrollBarHandle : Button
	{
		public delegate void Handler();
		public event Handler DragStarted, Moved, DragEnded;

		private Point dragStart_;
		private Rectangle initialBounds_;
		private bool dragging_ = false;

		public ScrollBarHandle()
		{
			Events.DragStart += OnDragStart;
			Events.Drag += OnDrag;
			Events.DragEnd += OnDragEnd;
		}

		public void OnDragStart(DragEvent e)
		{
			dragging_ = true;
			dragStart_ = e.Pointer;
			initialBounds_ = AbsoluteClientBounds;

			SetCapture();
			DragStarted?.Invoke();
		}

		public void OnDrag(DragEvent e)
		{
			if (!dragging_)
				return;

			var p = e.Pointer;
			var delta = p - dragStart_;

			var r = Rectangle.FromSize(
				initialBounds_.Left,
				initialBounds_.Top + (delta.Y),
				initialBounds_.Width,
				initialBounds_.Height);

			var box = Parent.AbsoluteClientBounds;

			if (r.Top < box.Top)
				r.MoveTo(r.Left, box.Top);

			if (r.Bottom > box.Bottom)
				r.MoveTo(r.Left, box.Bottom - r.Height);

			SetBounds(r);
			UpdateBounds();

			Moved?.Invoke();
		}

		public void OnDragEnd(DragEvent e)
		{
			bool wasDragging_ = dragging_;

			dragging_ = false;
			ReleaseCapture();

			if (wasDragging_)
				DragEnded?.Invoke();
		}
	}


	class ScrollBar : Panel
	{
		public delegate void Handler();
		public delegate void ValueHandler(float v);

		public event Handler DragStarted, DragEnded;
		public event ValueHandler ValueChanged;

		private ScrollBarHandle handle_ = new ScrollBarHandle();
		private float range_ = 0;
		private float value_ = 0;

		public ScrollBar()
		{
			Margins = new Insets(0, 1, 1, 1);
			Layout = new AbsoluteLayout();
			Clickthrough = false;
			BackgroundColor = Style.Theme.ScrollBarBackgroundColor;

			Add(handle_);

			Events.PointerDown += OnPointerDown;
			handle_.Moved += OnHandleMoved;
			handle_.DragStarted += () => DragStarted?.Invoke();
			handle_.DragEnded += () => DragEnded?.Invoke();
		}

		public float Range
		{
			get
			{
				return range_;
			}

			set
			{
				if (range_ != value)
				{
					range_ = value;

					if (WidgetObject != null)
						UpdateBounds();
				}
			}
		}

		public float Value
		{
			get
			{
				return value_;
			}

			set
			{
				if (value_ != value)
				{
					value_ = value;

					if (WidgetObject != null)
						UpdateBounds();

					ValueChanged?.Invoke(value_);
				}
			}
		}

		protected override void DoSetEnabled(bool b)
		{
			base.DoSetEnabled(b);
			handle_.Visible = b;
		}

		protected override Size DoGetMinimumSize()
		{
			return new Size(Style.Metrics.ScrollBarWidth, DontCare);
		}

		public override void UpdateBounds()
		{
			var r = AbsoluteClientBounds;
			var h = Math.Max(r.Height - range_, 50);

			var cb = ClientBounds;
			var avh = cb.Height - handle_.ClientBounds.Height;
			var p = range_ == 0 ? 0 : (value_ / range_);
			r.Top += Borders.Top + p * avh;
			r.Bottom = r.Top + h;

			handle_.SetBounds(r);
			DoLayoutImpl();

			base.UpdateBounds();
		}

		private void OnHandleMoved()
		{
			var r = ClientBounds;
			var hr = handle_.RelativeBounds;
			var top = hr.Top - Borders.Top;
			var h = r.Height - hr.Height;
			var p = (top / h);
			value_ = p * range_;
			ValueChanged?.Invoke(value_);
		}

		private void OnPointerDown(PointerEvent e)
		{
			if (!Enabled)
				return;

			var r = AbsoluteClientBounds;
			var p = e.Pointer - r.TopLeft;
			var y = r.Top + p.Y - handle_.ClientBounds.Height / 2;

			if (y < r.Top)
				y = r.Top;
			else if (y + handle_.ClientBounds.Height > ClientBounds.Height)
				y = r.Bottom - handle_.ClientBounds.Height;

			var cb = handle_.Bounds;
			var h = cb.Height;
			cb.Top = y;
			cb.Bottom = y + h;

			handle_.SetBounds(cb);
			DoLayoutImpl();
			base.UpdateBounds();

			OnHandleMoved();

			var d = e.EventData as PointerEventData;
			SuperController.singleton.StartCoroutine(StartDrag(d));

			e.Bubble = false;
		}

		private IEnumerator StartDrag(PointerEventData d)
		{
			yield return new WaitForEndOfFrame();

			var o = handle_.WidgetObject.gameObject;

			d.pointerPress = o;
			d.pointerDrag = o;
			d.rawPointerPress = o;
			d.pointerEnter = o;
			d.selectedObject = o;
			d.hovered.Clear();

			List<RaycastResult> rc = new List<RaycastResult>();
			EventSystem.current.RaycastAll(d, rc);

			foreach (var r in rc)
			{
				d.hovered.Add(r.gameObject);

				if (r.gameObject == o)
				{
					d.pointerCurrentRaycast = r;
					d.pointerPressRaycast = r;
					break;
				}
			}

			ExecuteEvents.Execute(
				handle_.WidgetObject.gameObject, d, ExecuteEvents.pointerEnterHandler);

			ExecuteEvents.Execute(
				handle_.WidgetObject.gameObject, d, ExecuteEvents.pointerDownHandler);
		}
	}
}
