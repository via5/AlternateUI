using UnityEngine.EventSystems;
using UnityEngine;

namespace VUI
{

	class MouseCallbacks : MonoBehaviour,
		IPointerEnterHandler, IPointerExitHandler,
		IPointerDownHandler, IPointerUpHandler,
		IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private Widget widget_ = null;

		public Widget Widget
		{
			get { return widget_; }
			set { widget_ = value; }
		}

		public void OnPointerEnter(PointerEventData d)
		{
			Utilities.Handler(() =>
			{
				if (widget_ != null)
					widget_.OnPointerEnterInternal(d);
			});
		}

		public void OnPointerExit(PointerEventData d)
		{
			Utilities.Handler(() =>
			{
				if (widget_ != null)
					widget_.OnPointerExitInternal(d);
			});
		}

		public void OnPointerDown(PointerEventData d)
		{
			Utilities.Handler(() =>
			{
				if (widget_ != null)
					widget_.OnPointerDownInternal(d);
			});
		}

		public void OnPointerUp(PointerEventData d)
		{
			Utilities.Handler(() =>
			{
				if (widget_ != null)
					widget_.OnPointerUpInternal(d);
			});
		}

		public void OnScroll(PointerEventData d)
		{
			if (d.scrollDelta != Vector2.zero)
			{
				Utilities.Handler(() =>
				{
					if (widget_ != null)
						widget_.OnWheelInternal(d);
				});
			}
		}

		public void OnBeginDrag(PointerEventData d)
		{
			Utilities.Handler(() =>
			{
				if (widget_ != null)
					widget_.OnBeginDragInternal(d);
			});
		}

		public void OnDrag(PointerEventData d)
		{
			Utilities.Handler(() =>
			{
				if (widget_ != null)
					widget_.OnDragInternal(d);
			});
		}

		public void OnEndDrag(PointerEventData d)
		{
			Utilities.Handler(() =>
			{
				if (widget_ != null)
					widget_.OnEndDragInternal(d);
			});
		}
	}


	interface IWidget
	{
		void Remove();
	}


	interface IEvent
	{
	}

	abstract class MouseEvent : IEvent
	{
		private readonly Widget w_;
		private readonly PointerEventData d_;

		protected MouseEvent(Widget w, PointerEventData d)
		{
			w_ = w;
			d_ = d;
		}

		public Point Mouse
		{
			get { return w_?.GetRoot()?.ToLocal(d_.position) ?? Point.Zero; }
		}
	}

	class WheelEvent : MouseEvent
	{
		private readonly Point d_;

		public WheelEvent(Widget w, PointerEventData d)
			: base(w, d)
		{
			d_ = new Point(d.scrollDelta.x / 100.0f, d.scrollDelta.y / 100.0f);
		}

		public Point Delta
		{
			get { return d_; }
		}
	}

	class DragEvent : MouseEvent
	{
		public DragEvent(Widget w, PointerEventData d)
			: base(w, d)
		{
		}
	}

	class PointerEvent : MouseEvent
	{
		public PointerEvent(Widget w, PointerEventData d)
			: base(w, d)
		{
		}
	}


	class Events
	{
		public delegate bool DragHandler(DragEvent e);
		public event DragHandler DragStart, Drag, DragEnd;

		public bool? FireDragStart(DragEvent e) { return DragStart?.Invoke(e); }
		public bool? FireDrag(DragEvent e) { return Drag?.Invoke(e); }
		public bool? FireDragEnd(DragEvent e) { return DragEnd?.Invoke(e); }


		public delegate bool WheelHandler(WheelEvent e);
		public event WheelHandler Wheel;

		public bool? FireWheel(WheelEvent e) { return Wheel?.Invoke(e); }


		public delegate bool PointerHandler(PointerEvent e);
		public event PointerHandler PointerDown, PointerUp;
		public event PointerHandler PointerEnter, PointerExit;

		public bool? FirePointerDown(PointerEvent e) { return PointerDown?.Invoke(e); }
		public bool? FirePointerUp(PointerEvent e) { return PointerUp?.Invoke(e); }
		public bool? FirePointerEnter(PointerEvent e) { return PointerEnter?.Invoke(e); }
		public bool? FirePointerExit(PointerEvent e) { return PointerExit?.Invoke(e); }
	}
}
