using System;
using System.Collections.Generic;
using System.Linq;
using OxyPlot.Series;

namespace userinterface.Models.Splines
{
    public class SplineMouseFollower
    {
        public SplineMouseFollower()
        {
            ControlPoints = new List<(double x, double y)>();
            IsSelected = false;
        }

        private List<(double x, double y)> ControlPoints { get; set; }

        public bool IsSelected { get; private set; }

        private int IndexOfHeldPoint { get; set; } = -1;

        private double PointClickThreshold = 5;

        public void Select(double x, double y)
        {
            if (!TryFindAndHoldPoint(x, y, out var heldPointIndex))
            {
                ControlPoints.Insert(heldPointIndex, (x, y));
            }
            else
            {
                ControlPoints[heldPointIndex] = (x, y);
            }

            IndexOfHeldPoint = heldPointIndex;
            IsSelected = true;
        }

        public void Drag(double x, double y)
        {
            ControlPoints[IndexOfHeldPoint] = (x, y);
        }

        public void LetGo()
        {
            IndexOfHeldPoint = -1;
            IsSelected = false;
        }

        public bool TryFindAndHoldPoint(double x, double y, out int index)
        {
            index = 0;

            foreach (var controlPoint in ControlPoints)
            {
                index++;
                var xDiff = controlPoint.x - x;

                if (Math.Abs(xDiff) < PointClickThreshold)
                {
                    var yDiff = controlPoint.y - y;

                    if (Math.Sqrt(Math.Pow(xDiff, 2) + Math.Pow(yDiff, 2)) < PointClickThreshold)
                    {
                        return true;
                    }    
                }

                if (xDiff > PointClickThreshold)
                {
                    break;
                }
            }

            return false;
        }

        public void FillSeries(LineSeries series)
        {
            series.Points.Clear();
            series.Points.AddRange(ControlPoints.Select(p => new OxyPlot.DataPoint(p.x, p.y)));
        }
    }
}
