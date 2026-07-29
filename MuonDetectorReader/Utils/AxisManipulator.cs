using OxyPlot;

namespace MuonDetectorReader.Utils
{
    internal class AxisManipulator : MouseManipulator
    {
        private ScreenPoint _previousPosition;
        private readonly PlotModel _model;

        public AxisManipulator(IPlotView view, ScreenPoint startPosition, PlotModel model) : base(view)
        {
            _previousPosition = startPosition;
            _model = model;
        }

        public override void Delta(OxyMouseEventArgs e)
        {
            base.Delta(e);
            if (_model == null) return;

            foreach (var axis in _model.Axes)
            {
                if (axis.IsPanEnabled)
                {
                    axis.Pan(_previousPosition, e.Position);
                }
            }

            PlotView.InvalidatePlot(false);
            _previousPosition = e.Position;
            e.Handled = true;
        }
    }
}
