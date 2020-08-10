using System;
//using Bridge;
using Newtonsoft.Json;

namespace MM_LensCalWithAISYS
{
    class CalTable
    {

    }

    public struct XYCal
    {
        private int _no;
        private double _x;
        private double _y;
        public XYCal(int No, double X, double Y)
        {
            this._no = No;  this._x = X; this._y = Y;
        }

        public int No
        {
            get { return _no; }
            set { if(_no == value) { return; }  _no = value; }
        }

        public double X
        {
            get { return _x; }
            set { if(_x == value) { return; } _x = value; }
        }
        public double Y
        {
            get { return _y; }
            set { if(_y == value) { return; } _y = value; }
        }
            

    }
}
