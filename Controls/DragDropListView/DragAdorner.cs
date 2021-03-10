using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Dofus.Retro.Supertools.Controls.DragDropListView
{
	public class DragAdorner : Adorner
	{
		#region Data

		private readonly Rectangle child;
		private double offsetLeft;
		private double offsetTop;

		#endregion // Data

		#region Constructor

		/// <summary>
		///     Initializes a new instance of DragVisualAdorner.
		/// </summary>
		/// <param name="adornedElement">The element being adorned.</param>
		/// <param name="size">The size of the adorner.</param>
		/// <param name="brush">A brush to with which to paint the adorner.</param>
		public DragAdorner(UIElement adornedElement, Size size, Brush brush)
			: base(adornedElement)
		{
			var rect = new Rectangle { Fill = brush, Width = size.Width, Height = size.Height, IsHitTestVisible = false };
			child = rect;
		}

		#endregion // Constructor

		#region Public Interface

		#region GetDesiredTransform

		/// <summary>
		///     Override.
		/// </summary>
		/// <param name="transform"></param>
		/// <returns></returns>
		public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
		{
			var result = new GeneralTransformGroup();
			result.Children.Add(base.GetDesiredTransform(transform));
			result.Children.Add(new TranslateTransform(offsetLeft, offsetTop));
			return result;
		}

		#endregion // GetDesiredTransform

		#region OffsetLeft

		/// <summary>
		///     Gets/sets the horizontal offset of the adorner.
		/// </summary>
		public double OffsetLeft
		{
			get => offsetLeft;
			set
			{
				offsetLeft = value;
				UpdateLocation();
			}
		}

		#endregion // OffsetLeft

		#region SetOffsets

		/// <summary>
		///     Updates the location of the adorner in one atomic operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="top"></param>
		public void SetOffsets(double left, double top)
		{
			offsetLeft = left;
			offsetTop = top;
			UpdateLocation();
		}

		#endregion // SetOffsets

		#region OffsetTop

		/// <summary>
		///     Gets/sets the vertical offset of the adorner.
		/// </summary>
		public double OffsetTop
		{
			get => offsetTop;
			set
			{
				offsetTop = value;
				UpdateLocation();
			}
		}

		#endregion // OffsetTop

		#endregion // Public Interface

		#region Protected Overrides

		/// <summary>
		///     Override.
		/// </summary>
		/// <param name="constraint"></param>
		/// <returns></returns>
		protected override Size MeasureOverride(Size constraint)
		{
			child.Measure(constraint);
			return child.DesiredSize;
		}

		/// <summary>
		///     Override.
		/// </summary>
		/// <param name="finalSize"></param>
		/// <returns></returns>
		protected override Size ArrangeOverride(Size finalSize)
		{
			child.Arrange(new Rect(finalSize));
			return finalSize;
		}

		/// <summary>
		///     Override.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		protected override Visual GetVisualChild(int index)
		{
			return child;
		}

		/// <summary>
		///     Override.  Always returns 1.
		/// </summary>
		protected override int VisualChildrenCount => 1;

		#endregion // Protected Overrides

		#region Private Helpers

		private void UpdateLocation()
		{
			if (Parent is AdornerLayer adornerLayer)
				adornerLayer.Update(AdornedElement);
		}

		#endregion // Private Helpers
	}
}
