using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VUI
{
	class ScrollBarHandle : Button
	{
		public delegate void Handler();
		public event Handler Moved;

		private Point dragStart_;
		private Rectangle initialBounds_;
		private bool dragging_ = false;

		private Point MakePoint(PointerEventData d)
		{
			//return new Point(d.position.x, d.position.y);
			return GetRoot().ToLocal(d.position);
		}

		public override void OnBeginDragInternal(PointerEventData d)
		{
			dragging_ = true;
			dragStart_ = MakePoint(d);
			initialBounds_ = AbsoluteClientBounds;
		}

		public override void OnDragInternal(PointerEventData d)
		{
			if (!dragging_)
				return;

			var p = MakePoint(d);
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

		public override void OnEndDragInternal(PointerEventData d)
		{
			dragging_ = false;
		}
	}


	class ScrollBar : Panel
	{
		public delegate void ValueHandler(float v);
		public event ValueHandler ValueChanged;

		private ScrollBarHandle handle_ = new ScrollBarHandle();
		private float range_ = 0;
		private float value_ = 0;

		public ScrollBar()
		{
			Borders = new Insets(1);
			Layout = new AbsoluteLayout();
			Add(handle_);

			handle_.Moved += OnHandleMoved;
		}

		public float Range
		{
			get { return range_; }
			set { range_ = value; }
		}

		public float Value
		{
			get { return value_; }
			set { value_ = value; }
		}

		public override void UpdateBounds()
		{
			var r = AbsoluteClientBounds;
			//Glue.LogInfo($"sb: h={r.Height} r={range_} nh={r.Height - range_}");

			var h = r.Height - range_;

			var cb = ClientBounds;
			var avh = cb.Height - handle_.ClientBounds.Height;
			var p = (value_ / range_);
			r.Top += Borders.Top + p * avh;
			r.Bottom = r.Top + h;

			//Glue.LogInfo($"{r.Top} {p} {avh}");
			handle_.SetBounds(r);
			DoLayout();

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
			//Glue.LogInfo($"{top} {p} {value_}");
		}
	}



	class TreeView : Panel
	{
		public override string TypeName { get { return "TreeView"; } }

		public class Item
		{
			private Item parent_ = null;
			private string text_;
			private List<Item> children_ = null;
			private bool expanded_ = false;

			public Item(string text)
			{
				text_ = text;
			}

			public virtual TreeView TreeView
			{
				get
				{
					if (parent_ == null)
						return null;
					else
						return parent_.TreeView;
				}
			}

			public Item Parent
			{
				get { return parent_; }
				set { parent_ = value; }
			}

			public string Text
			{
				get { return text_; }
				set { text_ = value; }
			}

			public List<Item> Children
			{
				get{ return children_; }
			}

			public bool Expanded
			{
				get
				{
					return expanded_;
				}

				set
				{
					expanded_ = value;
					TreeView?.ItemExpanded(this);
				}
			}

			public void Toggle()
			{
				Expanded = !Expanded;
			}

			public virtual bool HasChildren
			{
				get { return children_ != null && children_.Count > 0; }
			}

			public void Add(Item child)
			{
				if (children_ == null)
					children_ = new List<Item>();

				children_.Add(child);
				child.Parent = this;
			}
		}


		class InternalRootItem : Item
		{
			private readonly TreeView tree_;

			public InternalRootItem(TreeView tree)
				: base("root")
			{
				tree_ = tree;
			}

			public override TreeView TreeView
			{
				get { return tree_; }
			}
		}


		class Node
		{
			private readonly TreeView tree_;
			private Item item_ = null;
			private ToolButton toggle_ = null;
			private Label label_ = null;

			public Node(TreeView t)
			{
				tree_ = t;
			}

			public void Clear()
			{
				Set(null, Rectangle.Zero);
			}

			public void Set(Item i, Rectangle r)
			{
				item_ = i;

				if (item_ == null)
				{
					if (toggle_ != null)
						toggle_.Render = false;

					if (label_ != null)
						label_.Render = false;
				}
				else
				{
					if (item_.HasChildren)
					{
						if (toggle_ == null)
						{
							toggle_ = new ToolButton("", OnToggle);
							tree_.Add(toggle_);
							toggle_.Create();
						}

						if (item_.Expanded)
							toggle_.Text = "-";
						else
							toggle_.Text = "+";

						var tr = r;
						tr.Width = 30;
						toggle_.SetBounds(tr);
						toggle_.Render = true;
					}
					else
					{
						if (toggle_ != null)
							toggle_.Render = false;
					}


					if (label_ == null)
					{
						label_ = new Label();
						//label_.BackgroundColor = UnityEngine.Color.red;
						tree_.Add(label_);
						label_.Create();
					}

					var lr = r;
					lr.Left += 35;
					label_.Text = item_.Text;
					label_.SetBounds(lr);
					label_.Render = true;
				}
			}

			private void OnToggle()
			{
				if (item_ != null)
					item_.Toggle();
			}
		}


		class NodeContext
		{
			public Rectangle av;
			public int nodeIndex;
			public int itemIndex;
			public float x, y;
			public int indent;
		}


		private const int InternalPadding = 5;
		private const int ItemHeight = 35;
		private const int ItemPadding = 2;
		private const int IndentSize = 50;
		private const int ScrollBarWidth = 50;

		private readonly InternalRootItem root_;
		private List<Node> nodes_ = new List<Node>();
		private ScrollBar hsb_ = new ScrollBar();
		private int topItemIndex_ = 0;

		public TreeView()
		{
			root_ = new InternalRootItem(this);

			Borders = new Insets(1);
			Layout = new AbsoluteLayout();

			Add(hsb_);

			hsb_.ValueChanged += OnHorizontalScroll;
		}

		private void OnHorizontalScroll(float v)
		{
			topItemIndex_ = (int)(v / (ItemHeight + ItemPadding));
			UpdateNodes();
			base.UpdateBounds();
		}

		public Item RootItem
		{
			get{ return root_; }
		}

		public void ItemExpanded(Item i)
		{
			UpdateNodes();
			base.UpdateBounds();
		}

		private void UpdateNodes()
		{
			var cx = new NodeContext();
			cx.av = AbsoluteClientBounds;
			cx.nodeIndex = 0;
			cx.itemIndex = 0;
			cx.x = cx.av.Left + InternalPadding;
			cx.y = cx.av.Top + InternalPadding;
			cx.indent = 0;

			int nodeCount = (int)(cx.av.Height / (ItemHeight + ItemPadding));

			while (nodes_.Count < nodeCount)
				nodes_.Add(new Node(this));

			while (nodes_.Count > nodeCount)
				nodes_.RemoveAt(nodes_.Count - 1);


			UpdateNode(root_, cx);

			for (int i = cx.nodeIndex; i < nodes_.Count; ++i)
				nodes_[i].Clear();

			DoLayout();

			float requiredHeight = (cx.itemIndex + 1) * (ItemHeight + ItemPadding);
			float missingHeight = requiredHeight - cx.av.Height;
			hsb_.Range = missingHeight;

			//Glue.LogInfo(
			//	$"{nodes_.Count} nodes, {root_.Children.Count} items, " +
			//	$"avh={cx.av.Height}, rh={requiredHeight}, mh={missingHeight}");
		}

		private void UpdateNode(Item item, NodeContext cx)
		{
			for (int i = 0; i < item.Children.Count; ++i)
			{
				var child = item.Children[i];

				if (cx.itemIndex >= topItemIndex_)
				{
					if (cx.nodeIndex < nodes_.Count)
					{
						var node = nodes_[cx.nodeIndex];

						var x = cx.x + cx.indent * IndentSize;
						var y = cx.y;

						var r = Rectangle.FromPoints(
							x, y,
							cx.av.Right - InternalPadding - ScrollBarWidth,
							y + ItemHeight);

						node.Set(child, r);

						cx.y += ItemHeight + ItemPadding;
						++cx.nodeIndex;
					}
				}

				++cx.itemIndex;

				if (child.Expanded)
				{
					++cx.indent;
					UpdateNode(child, cx);
					--cx.indent;
				}
			}
		}

		protected override void DoCreate()
		{
			base.DoCreate();
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			//Style.Polish(this);
		}

		public override void UpdateBounds()
		{
			var r = AbsoluteClientBounds;
			r.Left = r.Right - ScrollBarWidth;
			hsb_.SetBounds(r);
			//hsb_.Set(0, 0, 10);

			UpdateNodes();

			base.UpdateBounds();
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return base.DoGetPreferredSize(maxWidth, maxHeight);
		}

		protected override Size DoGetMinimumSize()
		{
			return base.DoGetMinimumSize();
		}

		protected override void DoSetRender(bool b)
		{
			base.DoSetRender(b);
		}

		public override string DebugLine
		{
			get { return base.DebugLine; }
		}
	}
}
