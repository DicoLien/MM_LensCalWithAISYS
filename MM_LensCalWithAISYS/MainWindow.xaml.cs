using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Threading;

using AxMMMARKLib;
using AxMMMOTIONLib;
using AxMMLENSCALLib;
using AxAxAltairUDrv;

using AxOvkBase;
using AxAxOvkBase;
using AxAxOvkPat;

using AxOvkMsr;
using AxAxOvkMsr;

using System.Timers;

using MM_Motion;
using System.IO;

namespace MM_LensCalWithAISYS
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        //
        //MarkingMate OCX
        //
        AxMMMark axMark = new AxMMMark();
        AxMMMotion axMotion = new AxMMMotion();
        AxMMLensCal axLensCal = new AxMMLensCal();

        System.Timers.Timer MotionTimer = new System.Timers.Timer();
        private double dX = -1, dY = -1, dZ = -1, dR = -1; //For axMMMotion1.GetCurPostion
        private double eX = -1, eY = -1, eZ = -1, eR = -1; //For AxMotion.GetCurPostint(Encoder)

        //
        // AI Sys
        //
        AxAxAltairU m_AxAxAltairU_UI = new AxAxAltairU();
        AxAxCanvas axAxCanvas1 = new AxAxCanvas();
        AxAxCanvas axAxCanvas2 = new AxAxCanvas();
        AxAxROIBW8 axAxROIBW81 = new AxAxROIBW8();
        AxAxROIBW8 axAxROIBW82 = new AxAxROIBW8();
        AxAxMatch axAxMatch1 = new AxAxMatch();
        AxAxCircleMsr axAxCircleMsr1 = new AxAxCircleMsr();
       
        int g_nActiveSurfaceHandle;
        TxAxHitHandle nLockHandle = new TxAxHitHandle();       
        TxAxCircleMsrDragHandle cnLockHandle = new TxAxCircleMsrDragHandle();

        bool bLockROI1Flag, bLockROI2Flag, bCircleMsrFlag, AisysIsCreate;
        float g_fZoomX, g_fZoomY, g_fZoomX2, g_fZoomY2, org_fZoomX, org_fZoomY;

        //
        //common
        //
        string tempstr, caltempstr;
        bool g_Tech, g_SetArea, have_setarea, c_Det, have_learn, mm_startcal, initmm, _IsMotion, _XIsMove, _YIsMove, _ZIsMove, _FollowEncoder,_FollowEncoderInCal;
        int sa_X, sa_Y, sa_W, sa_H, DT, Axis;
        float p_centerX, p_centerY, r_centerX, r_centerY, mm_x, mm_y;
        private FlowDocument doc = new FlowDocument();
        private delegate void UpdateSysMsgCallBack(System.Windows.Controls.RichTextBox ctl, string value, string color);
        private delegate void UpdateXYZCallBack(TextBlock ctl, string value);
        private delegate void UpdateTextBoxCallBack(System.Windows.Controls.TextBox ctl, string value);
        float resX, resY;
        double AxisField, sp;
        System.Windows.Controls.TextBox cal = new System.Windows.Controls.TextBox();
        List<XYCal> CalList = new List<XYCal>();


        int mm_opt, mm_sgmt, mm_sot, mm_SFT, mm_totalgridpoint;

        public MainWindow()
        {
            InitializeComponent();

            InitAisys();
            InitMM();
            InitLensCal();
            InitMotion();


        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            //InitAisys();
        }

        private void InitLensCal()
        {
            if (axLensCal.GetCorrectDim() == -1)
            {
                System.Windows.MessageBox.Show("Lens correct file error.");
                return;
            }
            else if(axLensCal.GetCorrectDim() == 0)
            {
                System.Windows.MessageBox.Show("No License key.");
            }
            else
            {
                mm_totalgridpoint = axLensCal.GetCorrectDim();
                GetCorrectDim.Text = Math.Pow(mm_totalgridpoint, 0.5).ToString();
            }
        }

        private void MotionTimer_Tick(object sender, ElapsedEventArgs e)
        {
            CheckPostion();
            CheckMotionStatus();
        }

        private void CheckPostion()
        {
                axMotion.GetCurPosition(ref dX, ref dY, ref dZ, ref dR, 1);
            UpdateXYStatus(Xloc, dX.ToString("0.000"));
            UpdateXYStatus(Yloc, dY.ToString("0.000"));
            UpdateXYStatus(Zloc, dZ.ToString("0.000"));
                axMotion.GetCurPosition(ref eX, ref eY, ref eZ, ref eR, 2);
            UpdateXYStatus(Xenc, eX.ToString("0.000"));
            UpdateXYStatus(Yenc, eY.ToString("0.000"));
            UpdateXYStatus(Zenc, eZ.ToString("0.000"));

        }

        private void CheckMotionStatus()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (axMotion.IsMotion(0) == 101)
                { _IsMotion = true; }
                else { _IsMotion = false; }
                long iStatus = axMotion.MotionStatus(0).GetHashCode();
                //
                //運動中
                //
                if ((iStatus & 2) != 0)
                {
                    MX.IsChecked = true;
                    _XIsMove = true;
                    //mo_MoveXYZ.IsEnabled = false;
                }
                else
                {
                    MX.IsChecked = false;
                    _XIsMove = false;
                    //mo_MoveXYZ.IsEnabled = true;
                }
                if ((iStatus & 4) != 0)
                {
                    MY.IsChecked = true;
                    _YIsMove = true;
                    //mo_MoveXYZ.IsEnabled = false;
                }
                else
                {
                    MY.IsChecked = false;
                    _YIsMove = false;
                    //mo_MoveXYZ.IsEnabled = true;
                }
                if ((iStatus & 8) != 0)
                {
                    MZ.IsChecked = true;
                    _ZIsMove = true;
                    //mo_MoveXYZ.IsEnabled = false;
                }
                else
                {
                    MZ.IsChecked = false;
                    _ZIsMove = false;
                    //mo_MoveXYZ.IsEnabled = true;
                }
                //
                //正極限
                //
                if ((iStatus & 32) != 0)
                { LX.IsChecked = true; }
                else { LX.IsChecked = false; }
                if ((iStatus & 64) != 0)
                { LY.IsChecked = true; }
                else { LY.IsChecked = false; }
                if ((iStatus & 128) != 0)
                { LZ.IsChecked = true; }
                else { LZ.IsChecked = false; }

                //
                //負極限
                //
                if ((iStatus & 512) != 0)
                { RX.IsChecked = true; }
                else { RX.IsChecked = false; }
                if ((iStatus & 1024) != 0)
                { RY.IsChecked = true; }
                else { RY.IsChecked = false; }
                if ((iStatus & 2048) != 0)
                { RZ.IsChecked = true; }
                else { RZ.IsChecked = false; }

                //
                //原點
                //
                if ((iStatus & 8192) != 0)
                { HX.IsChecked = true; }
                else { HX.IsChecked = false; }
                if ((iStatus & 16384) != 0)
                { HY.IsChecked = true; }
                else { HY.IsChecked = false; }
                if ((iStatus & 32768) != 0)
                { HZ.IsChecked = true; }
                else { HZ.IsChecked = false; }

                //
                //到位訊號
                //
                if ((iStatus & 131072) != 0)
                { PX.IsChecked = true; }
                else { PX.IsChecked = false; }
                if ((iStatus & 262144) != 0)
                { PY.IsChecked = true; }
                else { PY.IsChecked = false; }
                if ((iStatus & 524288) != 0)
                { PZ.IsChecked = true; }
                else { PZ.IsChecked = false; }
            }));

        }

        private void UpdateXYZ(TextBlock ctl, string value)
        {
            if(!Dispatcher.CheckAccess())
            {
                UpdateXYZCallBack method = UpdateXYZ;
                Dispatcher.Invoke(method, ctl, value);
            }
            else
            {
                ctl.Text = value;
            }
        }

        private void UpdateSysMsg(System.Windows.Controls.RichTextBox ctl, string value, string color)
        {
            if (!Dispatcher.CheckAccess())
            {
                UpdateSysMsgCallBack method = UpdateSysMsg;
                Dispatcher.Invoke(method, ctl, value, color);
            }
            else
            {
                //Brush brush;
                //doc.PageWidth = ctl.ActualWidth;
                switch (color)
                {
                    case "red":
                        var p = new Paragraph();
                        var r = new Run(value);

                        p.Inlines.Add(r);
                        p.Foreground = Brushes.Red;
                        doc.Blocks.Add(p);
                        break;

                }
                ctl.Document = doc;
                ctl.ScrollToEnd();
            }
        }

        private void UpdateSysMsg(System.Windows.Controls.RichTextBox ctl, string value)
        {
            if (!Dispatcher.CheckAccess())
            {
                UpdateSysMsgCallBack method = UpdateSysMsg;
                Dispatcher.Invoke(method, ctl, value);
            }
            else
            {
                var p = new Paragraph();
                
                var r = new Run(value);
                //p.LineHeight = 1;
                p.Inlines.Add(r);
                doc.Blocks.Add(p);
                ctl.Document = doc;
                ctl.ScrollToEnd();
            }
        }

        private void UpdateXYStatus(TextBlock ctl, string value)
        {
            if(!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action<TextBlock, string>(UpdateXYZ), ctl, value);
            }
            else
            {
                UpdateXYZ(ctl, value);
            }
        }

        private void UpdateSysMsgStatus()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action<System.Windows.Controls.RichTextBox, string>(UpdateSysMsg), SysMsg, tempstr);
            }
            else
            {
                UpdateSysMsg(SysMsg, tempstr);
            }
        }

        private void InitMotion()
        {
            MotionTimer.Elapsed += MotionTimer_Tick;
            MotionTimer.AutoReset = true;
            MotionTimer.Enabled = true;
            MotionTimer.Start();
        }

        private void InitMM()
        {
            axMark.BeginInit();       
            wfh_axmark.Child = axMark;
            axMark.EndInit();
            axMark.Initial();

            axMotion.BeginInit();
            wfh_axmotion.Child = axMotion;
            axMotion.EndInit();
            axMotion.Initial();

            axLensCal.BeginInit();
            wfh_axlenscal.Child = axLensCal;
            axLensCal.EndInit();
            axLensCal.Initial();
            initmm = true;
        }

        private void InitAisys()
        {
            m_AxAxAltairU_UI.BeginInit();
            wfh_AltairU.Child = m_AxAxAltairU_UI;
            m_AxAxAltairU_UI.EndInit();

            m_AxAxAltairU_UI.OnChannelCreated += new AxAxAltairUDrv.IAxAltairUEvents_OnChannelCreatedEventHandler(m_AxAxAltairU_UI_OnChannelCreated);   //利用 +=m 自動產生事件委派涵式(m_AxAxAltairU_UI_OnChannelCreated)
            m_AxAxAltairU_UI.OnSurfaceFilled += new AxAxAltairUDrv.IAxAltairUEvents_OnSurfaceFilledEventHandler(m_AxAxAltairU_UI_OnSurfaceFilled);      //利用 +=m 自動產生事件委派涵式(m_AxAxAltairU_UI_OnSurfaceFilled)
            m_AxAxAltairU_UI.OnSurfaceDraw += new AxAxAltairUDrv.IAxAltairUEvents_OnSurfaceDrawEventHandler(m_AxAxAltairU_UI_OnSurfaceDraw);            //利用 +=m 自動產生事件委派涵式(m_AxAxAltairU_UI_OnSurfaceDraw)
            
            axAxCanvas1.BeginInit();
            wfh_canvas1.Child = axAxCanvas1;
            axAxCanvas1.EndInit();

            axAxCanvas1.OnCanvasMouseDown += new AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseDownEventHandler(axAxCanvas1_OnCanvasMouseDown);
            axAxCanvas1.OnCanvasMouseMove += new AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseMoveEventHandler(axAxCanvas1_OnCanvasMouseMove);
            axAxCanvas1.OnCanvasMouseUp += new AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseUpEventHandler(axAxCanvas1_OnCanvasMouseUp);
                       
            axAxCanvas2.BeginInit();
            wfh_canvas2.Child = axAxCanvas2;
            axAxCanvas2.EndInit();

            axAxROIBW81.BeginInit();
            wfh_roib81.Child = axAxROIBW81;
            axAxROIBW81.EndInit();

            axAxROIBW82.BeginInit();
            wfh_roib82.Child = axAxROIBW82;
            axAxROIBW82.EndInit();

            axAxMatch1.BeginInit();
            wfh_match1.Child = axAxMatch1;
            axAxMatch1.EndInit();

            axAxCircleMsr1.BeginInit();
            wfh_circlemsr1.Child = axAxCircleMsr1;
            axAxCircleMsr1.EndInit();

            m_AxAxAltairU_UI.ShowControlPanel = true;
            m_AxAxAltairU_UI.ShowControlPanel = false;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.RadioButton rb = (sender as System.Windows.Controls.RadioButton);
            switch (rb.Name)
            {
                case "mo_opt1":
                    mm_opt = 1;
                    break;
                case "mo_opt0":
                    mm_opt = 0;
                    break;
                case "SGMT1":
                    mm_sgmt = 1;
                    SetfillStyle.IsEnabled = true;
                    break;
                case "SGMT2":
                    mm_sgmt = 2;
                    SetfillStyle.IsEnabled = false;
                    break;
                    //
                    //極限點設定
                    //
                case "LX":
                    if (axMotion.GetAxisLimitTriggerLevel(2) == 0)
                    { axMotion.SetAxisLimitTriggerLevel(2, 1); }
                    else
                    { axMotion.SetAxisLimitTriggerLevel(2, 0); }
                    break;
                case "RX":
                    if (axMotion.GetAxisLimitTriggerLevel(2) == 0)
                    { axMotion.SetAxisLimitTriggerLevel(2, 1); }
                    else
                    { axMotion.SetAxisLimitTriggerLevel(2, 0); }
                    break;
                case "LY":
                    if (axMotion.GetAxisLimitTriggerLevel(4) == 0)
                    { axMotion.SetAxisLimitTriggerLevel(4, 1); }
                    else
                    { axMotion.SetAxisLimitTriggerLevel(4, 0); }
                    break;
                case "RY":
                    if (axMotion.GetAxisLimitTriggerLevel(4) == 0)
                    { axMotion.SetAxisLimitTriggerLevel(4, 1); }
                    else
                    { axMotion.SetAxisLimitTriggerLevel(4, 0); }
                    break;
                case "LZ":
                    if (axMotion.GetAxisLimitTriggerLevel(8) == 0)
                    { axMotion.SetAxisLimitTriggerLevel(8, 1); }
                    else
                    { axMotion.SetAxisLimitTriggerLevel(8, 0); }
                    break;
                case "RZ":
                    if (axMotion.GetAxisLimitTriggerLevel(8) == 0)
                    { axMotion.SetAxisLimitTriggerLevel(8, 1); }
                    else
                    { axMotion.SetAxisLimitTriggerLevel(8, 0); }
                    break;
                //
                //原點設定
                //
                case "HX":
                    if (axMotion.GetAxisHomeTriggerLevel(2) == 0)
                    { axMotion.SetAxisHomeTriggerLevel(2, 1); }
                    else
                    { axMotion.SetAxisHomeTriggerLevel(2, 0); }
                    break;
                case "HY":
                    if (axMotion.GetAxisHomeTriggerLevel(4) == 0)
                    { axMotion.SetAxisHomeTriggerLevel(4, 1); }
                    else
                    { axMotion.SetAxisHomeTriggerLevel(4, 0); }
                    break;
                case "HZ":
                    if (axMotion.GetAxisHomeTriggerLevel(8) == 0)
                    { axMotion.SetAxisHomeTriggerLevel(8, 1); }
                    else
                    { axMotion.SetAxisHomeTriggerLevel(8, 0); }
                    break;
                //
                //到位訊號
                //
                case "PX":
                    if(axMotion.GetAxisInposTriggerLevel(2) == 0)
                    { axMotion.SetAxisInposTriggerLevel(2, 1); }
                    else
                    { axMotion.SetAxisInposTriggerLevel(2, 0); }
                    break;
                case "PY":
                    if (axMotion.GetAxisInposTriggerLevel(4) == 0)
                    { axMotion.SetAxisInposTriggerLevel(4, 1); }
                    else
                    { axMotion.SetAxisInposTriggerLevel(4, 0); }
                    break;
                case "PZ":
                    if (axMotion.GetAxisInposTriggerLevel(8) == 0)
                    { axMotion.SetAxisInposTriggerLevel(8, 1); }
                    else
                    { axMotion.SetAxisInposTriggerLevel(8, 0); }
                    break;

            }
        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //InitAisys();
            //AutoCreateChannel();

            mo_MoveXYZ.IsEnabled = true;

        }

        private void Mo_setXYzero_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                axMotion.SetCurPosition(0, 0, dZ, dR, 1);
                axMotion.SetCurPosition(0, 0, eZ, eR, 2);
            }
            catch(Exception ec)
            {
                System.Windows.MessageBox.Show(ec.ToString());
            }
        }

        private void Mo_setZzero_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                axMotion.SetCurPosition(dX, dY, 0, dR, 1);
                axMotion.SetCurPosition(eX, eY, 0, eR, 2);
            }
            catch (Exception ec)
            {
                System.Windows.MessageBox.Show(ec.ToString());
            }
        }

        private void Mo_XYup_btn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogBegin(4, 4096, 0);
        }

        private void Mo_XYup_btn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogEnd(4);
        }

        private void Mo_XYleft_btn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogBegin(2, 61439, 0);
        }

        private void Mo_XYleft_btn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogEnd(2);
        }

        private void Mo_XYright_btn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogBegin(2, 4096, 0);
        }

        private void Wfh_canvas1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            axAxCanvas1.CanvasWidth = Convert.ToInt32(wfh_canvas1.ActualWidth);
            axAxCanvas1.CanvasHeight = Convert.ToInt32(wfh_canvas1.ActualHeight);
            axAxCanvas1.Width = Convert.ToInt32(wfh_canvas1.ActualWidth);
            axAxCanvas1.Height = Convert.ToInt32(wfh_canvas1.ActualHeight);
        }



        private void Wfh_canvas2_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            axAxCanvas2.CanvasWidth = Convert.ToInt32(wfh_canvas2.ActualWidth);
            axAxCanvas2.CanvasHeight = Convert.ToInt32(wfh_canvas2.ActualHeight);
            axAxCanvas2.Width = Convert.ToInt32(wfh_canvas2.ActualWidth);
            axAxCanvas2.Height = Convert.ToInt32(wfh_canvas2.ActualHeight);
        }

        private void WFH_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            System.Windows.Forms.Integration.WindowsFormsHost wfh = (sender as System.Windows.Forms.Integration.WindowsFormsHost);
            
            switch(wfh.Name)
            {
                case "wfh_canvas1":
                    axAxCanvas1.CanvasWidth = Convert.ToInt32(wfh_canvas1.ActualWidth);
                    axAxCanvas1.CanvasHeight = Convert.ToInt32(wfh_canvas1.ActualHeight);
                    axAxCanvas1.Width = Convert.ToInt32(wfh_canvas1.ActualWidth);
                    axAxCanvas1.Height = Convert.ToInt32(wfh_canvas1.ActualHeight);
                    break;
                case "wfh_canvas2":
                    wfh2_border.Width = wfh2_border.ActualHeight;
                    wfh_canvas2.Width = wfh_canvas2.ActualHeight;
                    axAxCanvas2.CanvasWidth = Convert.ToInt32(wfh_canvas2.ActualWidth);
                    axAxCanvas2.CanvasHeight = Convert.ToInt32(wfh_canvas2.ActualHeight);
                    axAxCanvas2.Width = Convert.ToInt32(wfh_canvas2.ActualWidth);
                    axAxCanvas2.Height = Convert.ToInt32(wfh_canvas2.ActualHeight);
                    break;

            }

        }

        private void Mo_control_Click(object sender, RoutedEventArgs e)
        {
            axMark.XYTableConfig();
        }

        private void Mo_XYright_btn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogEnd(2);
        }

        private void Wfh1_border_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (wfh1_border.IsFocused)
            {
                float i = (float)e.Delta / 10000;

                if (i > 0)
                {
                    if (g_fZoomX != 0)
                    {
                        if (g_fZoomX < 1)
                        {
                            g_fZoomX += i;
                            axAxCanvas1.CanvasWidth = Convert.ToInt32(m_AxAxAltairU_UI.ImageWidth * g_fZoomX);
                        }
                        else
                        {
                            g_fZoomX = 1;
                            axAxCanvas1.CanvasWidth = Convert.ToInt32(m_AxAxAltairU_UI.ImageWidth * g_fZoomX);
                        }
                    }

                    if (g_fZoomY != 0)
                    {
                        if (g_fZoomY < 1)
                        {
                            g_fZoomY += i;
                            axAxCanvas1.CanvasHeight = Convert.ToInt32(m_AxAxAltairU_UI.ImageHeight * g_fZoomY);
                        }
                        else
                        {
                            g_fZoomY = 1;
                            axAxCanvas1.CanvasHeight = Convert.ToInt32(m_AxAxAltairU_UI.ImageHeight * g_fZoomY);
                        }
                    }

                }
                else if (i < 0)
                {
                    if (g_fZoomX > org_fZoomX)
                    {
                        g_fZoomX += i;
                        axAxCanvas1.CanvasWidth = Convert.ToInt32(m_AxAxAltairU_UI.ImageWidth * g_fZoomX);
                    }
                    else
                    {
                        g_fZoomX = org_fZoomX;
                        axAxCanvas1.CanvasWidth = Convert.ToInt32(m_AxAxAltairU_UI.ImageWidth * g_fZoomX);
                    }
                    if (g_fZoomY > org_fZoomY)
                    {
                        g_fZoomY += i;
                        axAxCanvas1.CanvasHeight = Convert.ToInt32(m_AxAxAltairU_UI.ImageHeight * g_fZoomY);
                    }
                    else
                    {
                        g_fZoomY = org_fZoomY;
                        axAxCanvas1.CanvasHeight = Convert.ToInt32(m_AxAxAltairU_UI.ImageHeight * g_fZoomY);
                    }
                }
            }
        }

        private void Wfh1_border_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (wfh1_border.IsFocused)
            {
                float i = (float)e.Delta / 10000;

                if (i > 0)
                {
                    if (g_fZoomX != 0)
                    {
                        if (g_fZoomX < 1)
                        {
                            g_fZoomX += i;
                            axAxCanvas1.CanvasWidth = Convert.ToInt32(m_AxAxAltairU_UI.ImageWidth * g_fZoomX);
                        }
                        else
                        {
                            g_fZoomX = 1;
                            axAxCanvas1.CanvasWidth = Convert.ToInt32(m_AxAxAltairU_UI.ImageWidth * g_fZoomX);
                        }
                    }

                    if (g_fZoomY != 0)
                    {
                        if (g_fZoomY < 1)
                        {
                            g_fZoomY += i;
                            axAxCanvas1.CanvasHeight = Convert.ToInt32(m_AxAxAltairU_UI.ImageHeight * g_fZoomY);
                        }
                        else
                        {
                            g_fZoomY = 1;
                            axAxCanvas1.CanvasHeight = Convert.ToInt32(m_AxAxAltairU_UI.ImageHeight * g_fZoomY);
                        }
                    }

                }
                else if (i < 0)
                {
                    if (g_fZoomX > org_fZoomX)
                    {
                        g_fZoomX += i;
                        axAxCanvas1.CanvasWidth = Convert.ToInt32(m_AxAxAltairU_UI.ImageWidth * g_fZoomX);
                    }
                    else
                    {
                        g_fZoomX = org_fZoomX;
                        axAxCanvas1.CanvasWidth = Convert.ToInt32(m_AxAxAltairU_UI.ImageWidth * g_fZoomX);
                    }
                    if (g_fZoomY > org_fZoomY)
                    {
                        g_fZoomY += i;
                        axAxCanvas1.CanvasHeight = Convert.ToInt32(m_AxAxAltairU_UI.ImageHeight * g_fZoomY);
                    }
                    else
                    {
                        g_fZoomY = org_fZoomY;
                        axAxCanvas1.CanvasHeight = Convert.ToInt32(m_AxAxAltairU_UI.ImageHeight * g_fZoomY);
                    }
                }
            }

        }

        private void Wfh1_border_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            wfh1_border.Focus();
        }

        private void UpdateTextBox(System.Windows.Controls.TextBox ctl, string value)
        {
            if (!Dispatcher.CheckAccess())
            {
                UpdateTextBoxCallBack method = UpdateTextBox;
                Dispatcher.Invoke(method, ctl, value);
            }
            else
            {
                ctl.AppendText(value);
            }
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            DT = Convert.ToInt32(float.Parse(DelayTime.Text) * 1000);
            if (AxisSel.SelectedIndex == 0) //X Axis
            {
                cal.Text = "";
                Axis = 1;
                AxisField = axMotion.GetAxisField(2);
                sp = Convert.ToInt32(MoveSpace.Text);
                if (sp > 0)
                {
                    AxisField = 0 - AxisField;
                }
                cal.AppendText("[1-Axis]" + Environment.NewLine);
                Thread t1 = new Thread(new ThreadStart(Cal_X));
                t1.Start();
            }
            else if (AxisSel.SelectedIndex == 1)
            {
                cal.Text = "";
                Axis = 2;
                AxisField = axMotion.GetAxisField(4);
                sp = Convert.ToInt32(MoveSpace.Text);
                if (sp > 0)
                {
                    AxisField = 0 - AxisField;
                }
                cal.AppendText("[2-Axis]" + Environment.NewLine);
                //Cal_Y();
                Thread t1 = new Thread(new ThreadStart(Cal_Y));
                t1.Start();
            }
            else if (AxisSel.SelectedIndex == 2)
            {
                cal.Text = "";
                Axis = 3;
                AxisField = axMotion.GetAxisField(4);
                sp = Convert.ToInt32(MoveSpace.Text);
                if (sp > 0)
                {
                    AxisField = 0 - AxisField;
                }
                cal.AppendText("[3-Axis]" + Environment.NewLine);
                Thread t1 = new Thread(new ThreadStart(Cal_Z));
                t1.Start();
            }

        }

        private void Cal_X()
        {
            while (AxisField != 0)
            {
                double Enctemp = dX - eX;
                caltempstr = dX.ToString("0.0000") + " " + Enctemp.ToString("0.0000") + Environment.NewLine;
                UpdateTextBox(cal, caltempstr);
                X_Motion();
                Thread.Sleep(DT);
                CheckPostion();
                AxisField = AxisField + sp;
                Console.WriteLine(AxisField.ToString());
            }
            double Enctemp1 = dX - eX;
            caltempstr = dX.ToString("0.0000") + " " + Enctemp1.ToString("0.0000") + Environment.NewLine;
            UpdateTextBox(cal, caltempstr);
        }
        private void X_Motion()
        {
            axMotion.MoveTo(sp, 0, 0, 0, 1);
            for (; ; )
            {
                if (axMotion.IsMotion(0) == 0)
                {
                    return;
                }
            }

        }

        private void Cal_Y()
        {
            while (AxisField != 0)
            {
                double Enctemp = dY - eY;
                caltempstr = dY.ToString("0.0000") + " " + Enctemp.ToString("0.0000") + Environment.NewLine;
                UpdateTextBox(cal, caltempstr);
                Y_Motion();
                Thread.Sleep(DT);
                CheckPostion();
                AxisField = AxisField + sp;
                Console.WriteLine(AxisField.ToString());
            }
            double Enctemp1 = dY - eY;
            caltempstr = dY.ToString("0.0000") + " " + Enctemp1.ToString("0.0000") + Environment.NewLine;
            UpdateTextBox(cal, caltempstr);
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (AxisSel.SelectedIndex == 0)
            {
                StreamWriter write = new StreamWriter(Environment.CurrentDirectory + "\\X.txt");
                write.WriteLine(cal.Text.ToString().TrimEnd());
                write.Dispose();

            }
            else if (AxisSel.SelectedIndex == 1)
            {
                StreamWriter write = new StreamWriter(Environment.CurrentDirectory + "\\Y.txt");
                write.WriteLine(cal.Text.ToString().TrimEnd());
                write.Dispose();
            }
            else if (AxisSel.SelectedIndex == 2)
            {
                StreamWriter write = new StreamWriter(Environment.CurrentDirectory + "\\Z.txt");
                write.WriteLine(cal.Text.ToString().TrimEnd());
                write.Dispose();
            }
        }



        private void Exit_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Y_Motion()
        {
            axMotion.MoveTo(0, sp, 0, 0, 1);
            for (; ; )
            {
                if (axMotion.IsMotion(0) == 0)
                {
                    return;
                }
            }
        }

        private void Cal_Z()
        {
            while (AxisField != 0)
            {
                double Enctemp = Convert.ToDouble(Zloc.Text) - Convert.ToDouble(Zenc.Text);
                cal.AppendText(Convert.ToDouble(Zloc.Text).ToString("0.0000") + " " + Enctemp.ToString("0.0000") + Environment.NewLine);
                Z_Motion();
                Thread.Sleep(DT);
                CheckPostion();

                AxisField = AxisField + sp;
                Console.WriteLine(AxisField.ToString());
            }
            double Enctemp1 = Convert.ToDouble(Zloc.Text) - Convert.ToDouble(Zenc.Text);
            cal.AppendText(Convert.ToDouble(Zloc.Text).ToString("0.0000") + " " + Enctemp1.ToString("0.0000") + Environment.NewLine);

        }

        private void FollowEncoderInCal_Click(object sender, RoutedEventArgs e)
        {
            if (FollowEncoderInCal.IsChecked == true)
            {
                _FollowEncoderInCal = true;
            }
            else
            {
                _FollowEncoderInCal = false;
            }
        }

        private void Follo_encoder_Click(object sender, RoutedEventArgs e)
        {
            if(Follo_encoder.IsChecked == true)
            {
                _FollowEncoder = true;
            }
            else { _FollowEncoder = false; }
        }

        private void Follo_encoder_Checked(object sender, RoutedEventArgs e)
        {


        }

        private void FollowEncoderInCal_Checked(object sender, RoutedEventArgs e)
        {


        }

        private void Z_Motion()
        {
            axMotion.MoveTo(0, 0, sp, 0, 1);
            for (; ; )
            {
                if (axMotion.IsMotion(0) == 0)
                {
                    return;
                }
            }
        }

        private void Mo_XYdown_btn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogBegin(4, 61439, 0);
        }



        private void Mo_XYdown_btn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogEnd(4);
        }

        private void Border_GotFocus(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Border bo = (sender as System.Windows.Controls.Border);
            if(bo != null)
            bo.BorderBrush = Brushes.Red;

        }



        private void Border_LostFocus(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Border bo = (sender as System.Windows.Controls.Border);
            if(bo != null)
            bo.BorderBrush = Brushes.Black;
        }

        private void Mo_Zup_btn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogBegin(8, 4096, 0);
        }

        private void Mo_Zup_btn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogEnd(8);
        }

        private void Mo_Zdown_btn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogBegin(8, 61439, 0);
        }

        private void Mo_Zdown_btn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogEnd(8);
        }

        private void Mo_toCCDdown_Click(object sender, RoutedEventArgs e)
        {
            if (axMotion.IsMotion(0) != 0)
            { return; }
            axMotion.GetCurPosition(ref dX, ref dY, ref dZ, ref dR, 1);
            double endX = dX + Convert.ToDouble(X_dis.Text);
            double endY = dY + Convert.ToDouble(Y_dis.Text);
            UpdateSysMsg(SysMsg, "Move To X:" + dX.ToString() + " -> " + endX.ToString() + " Y:"+ dY.ToString() + " -> " + endY.ToString());
            //axMotion.MoveTo(Convert.ToDouble(X_dis.Text),
            //    Convert.ToDouble(Y_dis.Text),
            //    0,
            //    0,
            //    1);
            ThreadPool.QueueUserWorkItem(o =>
            {
                mmMoveTo(endX, endY, 0);
            });
            
        }

        private void Cam_Control_Click(object sender, RoutedEventArgs e)
        {
            if(m_AxAxAltairU_UI.ShowControlPanel)
            {
                m_AxAxAltairU_UI.ShowControlPanel = false;
            }
            else
            {
                m_AxAxAltairU_UI.ShowControlPanel = true; 
            }
        }

        private void MarkGrid_Click(object sender, RoutedEventArgs e)
        {
            DoMarkGrid();
        }

        private void StartCal_Click(object sender, RoutedEventArgs e)
        {
            if (mm_totalgridpoint <= 0)
            {
                System.Windows.MessageBox.Show("Fail to Start Lens Calibration Function.");
                return;
            }
            else
            {

                DT = Convert.ToInt32(float.Parse(delaytime.Text) * 1000);
                mm_startcal = true;

                //ThreadPool.QueueUserWorkItem(DoStartCal);
                //DoStartCal();
                Thread t1 = new Thread(new ThreadStart(DoStartCal));
                t1.Start();
                
            }
            
        }

        private void DoStartCal()
        {

            //axMotion.SetCurPosition(0, 0, 0, 0, 0);
            //axMotion.SetCurPosition(0, 0, 0, 0, 1);
            double baseX = dX, baseY = dY;
            double moveX, moveY, CorX, CorY;

            if (mm_startcal)
            {
                

                for (int i = 1; i < mm_totalgridpoint +1; i++)
                {
                    tempstr = "Start Calibration No." + i;
                    //UpdateSysMsg(SysMsg, tempstr);
                    UpdateSysMsgStatus();
                    CorX = axLensCal.GetCorrectPointX(i);
                    CorY = axLensCal.GetCorrectPointY(i);
                    moveX = baseX + CorX;
                    moveY = baseY + CorY;
                    //axMotion.MoveTo(moveX, moveY, 0, 0, 1);
                    System.Windows.MessageBox.Show("BaseX:" + baseX
                        + " BaseY:" + baseY
                        + Environment.NewLine
                        + " CorX:" + CorX
                        + " CorY:" + CorY
                        + Environment.NewLine
                        + " moveX:" + moveX
                        + " moveY:" + moveY);
                    tempstr = "Move To X:" + moveY + " Y:" + moveX + "..";
                    UpdateSysMsgStatus();
                    mmMoveTo(moveY, moveX, 0);
                    Thread.Sleep(DT);
                    StartMatch();
                   while(mm_x > resX && mm_y > resY)
                    {
                        //axMotion.MoveTo(mm_x, mm_y, 0, 0, 1);
                        tempstr = "Move To X:" + moveY + " Y:" + moveX + "..";
                        UpdateSysMsgStatus();
                        mmMoveTo1(mm_x, mm_y, 0);
                        Thread.Sleep(DT);
                        StartMatch();
                    }
                    if (_FollowEncoderInCal)
                    {
                        //axMotion.GetCurPosition(ref eX, ref eY, ref eZ, ref dR, 1);
                        CalList.AddRange(new XYCal[] { new XYCal(i, eY, eX) });
                    }
                    else
                    {
                        //axMotion.GetCurPosition(ref dX, ref dY, ref dZ, ref dR, 0);
                        //Pxy.AddRange(new xylist[] { new xylist(0, 0, 0) });
                        CalList.AddRange(new XYCal[] { new XYCal(i, dY, dX) });
                    }
                }
            }
            mm_startcal = false;
            DoOutPut();
        }
        private void mmMoveTo(double X, double Y, double Z)
        {
            axMotion.MoveTo(X, Y, Z, 0, 0);
            for (; ; )
            {
                if (axMotion.IsMotion(0) == 0)
                {
                    if (_FollowEncoder)
                    {
                        if (eX != 0 || eY != 0 || eZ != 0)
                        {
                            double tempX = dX - eX, tempY = dY - eY, tempZ = dZ - eZ;
                            encMoveTo(tempX, tempY, tempZ);
                        }
                        else
                        {
                            tempstr = "Encoder not work!";
                            UpdateSysMsgStatus();
                        }
                    }
                    tempstr = "Done.";
                    UpdateSysMsgStatus();
                    return;
                }
            }
        }
        private void mmMoveTo0(double X, double Y, double Z)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                axMotion.MoveTo(X, Y, Z, 0, 0);
                //for (; ; )
                //{
                //    if (axMotion.IsMotion(0) == 0)
                //    {
                //        //if (_FollowEncoder)
                //        //{
                //        //    double tempX = dX - eX, tempY = dY - eY, tempZ = dZ - eZ;

                //        //}
                //        tempstr = "Done.";
                //        UpdateSysMsgStatus();
                //        return;
                //    }
                //}
                tempstr = "Done.";
                UpdateSysMsgStatus();
            }));
        }
        private void mmMoveTo1(double X, double Y, double Z)
        {
            axMotion.MoveTo(X, Y, Z, 0, 1);
            for (; ; )
            {
                if (axMotion.IsMotion(0) == 0)
                {
                    if(_FollowEncoder)
                    {
                        if (eX != 0 || eY != 0 || eZ != 0)
                        {
                            double tempX = dX - eX, tempY = dY - eY, tempZ = dZ - eZ;
                            encMoveTo(tempX, tempY, tempZ);
                        }
                        else
                        {
                            tempstr = "Encoder not work!";
                            UpdateSysMsgStatus();
                        }
                    }
                    tempstr = "done.";
                    UpdateSysMsgStatus();
                    return;
                }
            }

        }

        private void encMoveTo(double X, double Y, double Z)
        {
            axMotion.MoveTo(X, Y, Z, 0, 1);
            for (; ; )
            {
                if (axMotion.IsMotion(0) == 0)
                {
                    //tempstr = "Done.";
                    //UpdateSysMsgStatus();
                    return;
                }
            }
        }
        private void DoOutPut()
        {
            System.Windows.Forms.TextBox result = new System.Windows.Forms.TextBox();
            int C_number = mm_totalgridpoint / 2;
            double cp_X = CalList[C_number].X;
            double cp_Y = CalList[C_number].Y;
            //int j = 0;
            for(int i = 0; i < mm_totalgridpoint ; i++)
            {
                
                double temp_X = CalList[i].X;
                double temp_Y = CalList[i].Y;
                double result_X = temp_X - cp_X;
                double result_Y = temp_Y - cp_Y;
                result.AppendText(CalList[i].No.ToString() + " " +
                    result_X + " " +
                    result_Y + Environment.NewLine);
            }
            StreamWriter write = new StreamWriter(Environment.CurrentDirectory + "\\XYCal.txt");
            write.WriteLine(result.Text.ToString().TrimEnd());
            write.Dispose();
            System.Windows.MessageBox.Show("Calibration Finish.");
        }

        private void SetMo_MoveXYZStatus(bool value)
        {
            mo_MoveXYZ.IsEnabled = value;
        }

        private void Mo_MoveXYZ_Click(object sender, RoutedEventArgs e)
        {
            //SetMo_MoveXYZStatus(false);
            if (axMotion.IsMotion(0) != 0)
            {return;}
            double goX = Convert.ToDouble(mo_X.Text);
            double goY = Convert.ToDouble(mo_Y.Text);
            double goZ = Convert.ToDouble(mo_Z.Text);
            if (mm_opt == 0)
            {

                UpdateSysMsg(SysMsg, "Move To X:" + mo_X.Text + " Y:" + mo_Y.Text + " Z:" + mo_Z.Text);
                ThreadPool.QueueUserWorkItem(o =>
                {
                    mmMoveTo(goX, goY, goZ);
                });

                //axMotion.MoveTo(Convert.ToDouble(mo_X.Text),
                //    Convert.ToDouble(mo_Y.Text),
                //    Convert.ToDouble(mo_Z.Text),
                //    0,
                //    mm_opt);
            }
            else
            {

                axMotion.GetCurPosition(ref dX, ref dY, ref dZ, ref dR, 1);

                double endX = dX + Convert.ToDouble(mo_X.Text);
                double endY = dY + Convert.ToDouble(mo_Y.Text);
                double endZ = dZ + Convert.ToDouble(mo_Z.Text);

                UpdateSysMsg(SysMsg, "Move To X:" + dX.ToString() + "->" + endX.ToString() + 
                    " Y:" + dY.ToString() + "->" + endY.ToString() + 
                    " Z:" + dZ.ToString() + "->" + endZ.ToString());
                ThreadPool.QueueUserWorkItem(o =>
                {
                    mmMoveTo1(goX, goY, goZ);
                });
                //if(!Dispatcher.CheckAccess())
                //{
                //    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action<double, double, double>(mmMoveTo1), goX, goY, goZ);
                //}
                //else { mmMoveTo1(goX, goY, goZ); }

                //ThreadPool.QueueUserWorkItem(o =>
                //{
                //    mmMoveTo1(Convert.ToDouble(mo_X.Text), Convert.ToDouble(mo_Y.Text), Convert.ToDouble(mo_Z.Text));
                //    axMotion.MoveTo(Convert.ToDouble(mo_X.Text),
                //        Convert.ToDouble(mo_Y.Text),
                //        Convert.ToDouble(mo_Z.Text),
                //        0,
                //        mm_opt);
                //});

                //UpdateSysMsg(SysMsg, "Done.");
            }
            SetMo_MoveXYZStatus(true);
        }

        private void SOT_Click(object sender, RoutedEventArgs e)
        {
            if(SOT.IsChecked == true)
            {
                mm_sot = 1;
            }
            else
            {
                mm_sot = 0;
            }
        }

        private void DoMarkGrid()
        {
            axLensCal.SetGridDiameter(Convert.ToDouble(SetGriddiameter.Text));
            axLensCal.SetGridMarkType(mm_sgmt);
            axLensCal.SetOutputTexts(mm_sot);
            axLensCal.SetLensCorSpeed(Convert.ToDouble(SetLenscorPower.Text));
            axLensCal.SetLensCorPower(Convert.ToDouble(SetLenscorPower.Text));
            axLensCal.SetLensCorFrequency(Convert.ToDouble(SetLensCorFrequerncy.Text));
            axLensCal.SetLensCorPulseWidth(Convert.ToDouble(SetLenscorPulseWidth.Text));
            axLensCal.SetFillStyle(mm_SFT);
            axLensCal.SetLensCorFillPitch(Convert.ToDouble(SetLenscorfillPitch.Text));
            axLensCal.GridMarking();
        }

        private void SetfillStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mm_SFT = SetfillStyle.SelectedIndex;
        }        

        //private void Aisys_canvas1_On_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        //{
        //    if (wfh_canvas1.IsFocused)
        //    {
        //        float i = (float)e.Delta / 10000;

        //        if (i > 0)
        //        {
        //            if (g_fZoomX != 0)
        //            {
        //                if (g_fZoomX < 1)
        //                {
        //                    g_fZoomX += i;
        //                    axAxCanvas1.CanvasWidth = Convert.ToInt32(m_AxAxAltairU_UI.ImageWidth * g_fZoomX);
        //                }
        //                else
        //                {
        //                    g_fZoomX = 1;
        //                    axAxCanvas1.CanvasWidth = Convert.ToInt32(m_AxAxAltairU_UI.ImageWidth * g_fZoomX);
        //                }
        //            }

        //            if (g_fZoomY != 0)
        //            {
        //                if (g_fZoomY < 1)
        //                {
        //                    g_fZoomY += i;
        //                    axAxCanvas1.CanvasHeight = Convert.ToInt32(m_AxAxAltairU_UI.ImageHeight * g_fZoomY);
        //                }
        //                else
        //                {
        //                    g_fZoomY = 1;
        //                    axAxCanvas1.CanvasHeight = Convert.ToInt32(m_AxAxAltairU_UI.ImageHeight * g_fZoomY);
        //                }
        //            }
                    
        //        }
        //        else if (i <0)
        //        {
        //            if(g_fZoomX > org_fZoomX)
        //            {
        //                g_fZoomX += i;
        //                axAxCanvas1.CanvasWidth = Convert.ToInt32(m_AxAxAltairU_UI.ImageWidth * g_fZoomX);
        //            }
        //            else {
        //                g_fZoomX = org_fZoomX;
        //                axAxCanvas1.CanvasWidth = Convert.ToInt32(m_AxAxAltairU_UI.ImageWidth * g_fZoomX);
        //            }
        //            if (g_fZoomY > org_fZoomY)
        //            {
        //                g_fZoomY += i;
        //                axAxCanvas1.CanvasHeight = Convert.ToInt32(m_AxAxAltairU_UI.ImageHeight * g_fZoomY);
        //            }
        //            else {
        //                g_fZoomY = org_fZoomY;
        //                axAxCanvas1.CanvasHeight = Convert.ToInt32(m_AxAxAltairU_UI.ImageHeight * g_fZoomY);
        //            }
        //        }
        //    }
        //}


        private void axAxCanvas1_OnCanvasMouseDown(object sender, AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseDownEvent e)
        {
            if (m_AxAxAltairU_UI.IsPortCreated)
            {
                
                nLockHandle = axAxROIBW81.HitTest(e.x, e.y, g_fZoomX, g_fZoomY, 0, 0);
                if (nLockHandle != AxOvkBase.TxAxHitHandle.AX_HANDLE_NONE)
                {
                    bLockROI1Flag = true;
                }
                else
                {
                    bLockROI1Flag = false;
                    nLockHandle = axAxROIBW82.HitTest(e.x, e.y, g_fZoomX, g_fZoomY, 0, 0);
                    if (nLockHandle != AxOvkBase.TxAxHitHandle.AX_HANDLE_NONE)
                    {
                        bLockROI2Flag = true;
                    }
                    else
                    {
                        bLockROI2Flag = false;
                    }
                }

                cnLockHandle = axAxCircleMsr1.HitTest(e.x, e.y, g_fZoomX, g_fZoomY, 0, 0);
                if (cnLockHandle != AxOvkMsr.TxAxCircleMsrDragHandle.AX_CIRCLEMSR_NONE)
                    bCircleMsrFlag = true;
                else
                    bCircleMsrFlag = false;


            }
            
            

        }

        private void axAxCanvas1_OnCanvasMouseMove(object sender, AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseMoveEvent e)
        {
            //Drag the locked ROI and redraw all
            if (m_AxAxAltairU_UI.IsPortCreated)
            {
                if (bLockROI1Flag == true)
                {
                    axAxROIBW81.DragROI(nLockHandle, e.x, e.y, g_fZoomX, g_fZoomY, 0, 0);
                    RefreshImage();
                }
                else
                {
                    if (bLockROI2Flag == true)
                    {
                        axAxROIBW82.DragROI(nLockHandle, e.x, e.y, g_fZoomX, g_fZoomY, 0, 0);
                        RefreshImage();
                    }
                }

                if (bCircleMsrFlag == true)
                {
                    axAxCircleMsr1.DragFrame(cnLockHandle, e.x, e.y, g_fZoomX, g_fZoomY, 0, 0);
                    axAxCircleMsr1.DetectPrimitives();
                    RefreshImage();
                }
            }

        }

        private void axAxCanvas1_OnCanvasMouseUp(object sender, AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseUpEvent e)
        {
            //Clear lock ROI flags
            if (m_AxAxAltairU_UI.IsPortCreated)
            {
                bLockROI1Flag = false;
                bLockROI2Flag = false;
                bCircleMsrFlag = false;
            }
        }

        private void AutoCreateChannel()
        {
            m_AxAxAltairU_UI.WatchDogTimerState = AxAltairUDrv.TxAxauWatchDogTimerState.AXAU_WATCH_DOG_TIMER_STATE_ENABLED;
            m_AxAxAltairU_UI.VGain = 50;
            m_AxAxAltairU_UI.Language = AxAltairUDrv.TxAxauLanguage.AXAU_LANGUAGE_TRADITIONAL_CHINESE;
            m_AxAxAltairU_UI.DeviceIndex = 0;
            if (m_AxAxAltairU_UI.IsPortCreated)
            {
                m_AxAxAltairU_UI.ResetChannel();
            }
            else
            {

                if (m_AxAxAltairU_UI.CreateChannel())
                {
                    p_centerX = m_AxAxAltairU_UI.ImageWidth / 2;
                    p_centerY = m_AxAxAltairU_UI.ImageHeight / 2;
                    g_fZoomX = (float)axAxCanvas1.CanvasWidth / m_AxAxAltairU_UI.ImageWidth;
                    g_fZoomY = (float)axAxCanvas1.CanvasHeight / m_AxAxAltairU_UI.ImageHeight;
                    org_fZoomX = (float)axAxCanvas1.CanvasWidth / m_AxAxAltairU_UI.ImageWidth;
                    org_fZoomY = (float)axAxCanvas1.CanvasHeight / m_AxAxAltairU_UI.ImageHeight;
                    sa_X = m_AxAxAltairU_UI.ImageWidth /4 ; sa_Y = m_AxAxAltairU_UI.ImageHeight /4; sa_W = m_AxAxAltairU_UI.ImageWidth / 2; sa_H = m_AxAxAltairU_UI.ImageHeight / 2;
                    axAxCanvas1.CanvasWidth = Convert.ToInt32(m_AxAxAltairU_UI.ImageWidth * g_fZoomX);
                    axAxCanvas1.CanvasHeight = Convert.ToInt32(m_AxAxAltairU_UI.ImageHeight * g_fZoomY);
                    p_cx.Text = "Center X:" + p_centerX.ToString();
                    p_cy.Text = "Center Y:" + p_centerY.ToString();
                    m_AxAxAltairU_UI.Live();
                    AisysIsCreate = true;
                }
                else
                {
                    return;
                }
            }
            //Change AxCanvas geometry

        }


        private void m_AxAxAltairU_UI_OnChannelCreated(object sender, AxAxAltairUDrv.IAxAltairUEvents_OnChannelCreatedEvent e)
        {
            //指定畫布大小
            //m_AxAxCanvas.CanvasWidth = e.imageWidth;
            //m_AxAxCanvas.CanvasHeight = e.imageHeight;
        }

        private void m_AxAxAltairU_UI_OnSurfaceFilled(object sender, AxAxAltairUDrv.IAxAltairUEvents_OnSurfaceFilledEvent e)
        {
            //Set active image handle to this newly loaded image
            g_nActiveSurfaceHandle = e.surfaceHandle;
            axAxROIBW81.ParentHandle = g_nActiveSurfaceHandle;
            axAxROIBW82.ParentHandle = g_nActiveSurfaceHandle;
        }

        private void m_AxAxAltairU_UI_OnSurfaceDraw(object sender, AxAxAltairUDrv.IAxAltairUEvents_OnSurfaceDrawEvent e)
        {
            //Image Output
            //m_AxAxCanvas.DrawSurface(e.surfaceHandle, 1, 1, 0, 0);
            //m_AxAxCanvas.RefreshCanvas();
            RefreshImage();
        }



        private void Match_btn_Click(object sender, RoutedEventArgs e)
        {
            StartMatch();
        }

        private void StartMatch()
        {
            if (have_learn)
            {
                if (have_setarea)
                {
                    axAxMatch1.DstImageHandle = axAxROIBW82.VegaHandle;
                    axAxMatch1.AbsoluteCoord = true;
                    axAxMatch1.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
                    axAxMatch1.OperationMode = AxOvkPat.TxAxMatchOperationMode.AX_MATCH_OPERATION_MODE_NORMAL;
                    axAxMatch1.Match();
                    if (axAxMatch1.NumMatchedPos > 0)
                    {
                        axAxCircleMsr1.SrcImageHandle = g_nActiveSurfaceHandle;
                        axAxCircleMsr1.SetPlacement((int)axAxMatch1.FineMatchedX, (int)axAxMatch1.FineMatchedY, 20, axAxROIBW82.ROIWidth / 2, 0, 360);
                        //axAxCircleMsr1.DetectPrimitives();
                        axAxCircleMsr1.DetectPrimitives();
                        if (axAxCircleMsr1.CircleIsFitted)
                        {
                            r_centerX = (float)axAxCircleMsr1.MeasuredCenterX;
                            r_centerY = (float)axAxCircleMsr1.MeasuredCenterY;
                            p_rx.Text = "實際座標 X:" + r_centerX.ToString();
                            p_ry.Text = "實際座標 Y:" + r_centerY.ToString();
                            r_cx.Text = "相對座標 X:" + (r_centerX - p_centerX).ToString();
                            r_cy.Text = "相對座標 Y:" + (p_centerY - r_centerY).ToString();
                            mm_x = (r_centerX - p_centerX) * (float)Convert.ToDouble(X_res.Text);
                            mm_y = (p_centerY - r_centerY) * (float)Convert.ToDouble(Y_res.Text);
                            m_cx.Text = "轉換座標 X:" + mm_x.ToString() + " mm";
                            m_cy.Text = "轉換座標 Y:" + mm_y.ToString() + " mm";
                            c_Det = true;
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Search Fail-2.");
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Search Fail-1.");
                    }
                }
                else
                {
                    axAxROIBW82.ParentHandle = g_nActiveSurfaceHandle;
                    //axAxROIBW82.Title = "Matching Region";
                    axAxROIBW82.SetPlacement(0, 0, m_AxAxAltairU_UI.ImageWidth, m_AxAxAltairU_UI.ImageHeight);
                    axAxMatch1.DstImageHandle = axAxROIBW82.VegaHandle;
                    axAxMatch1.AbsoluteCoord = true;
                    axAxMatch1.PositionType = AxOvkPat.TxAxMatchPositionType.AX_MATCH_POSITION_TYPE_CENTER;
                    axAxMatch1.OperationMode = AxOvkPat.TxAxMatchOperationMode.AX_MATCH_OPERATION_MODE_NORMAL;
                    axAxMatch1.Match();
                    if (axAxMatch1.NumMatchedPos > 0)
                    {
                        axAxCircleMsr1.SetPlacement((int)axAxMatch1.FineMatchedX, (int)axAxMatch1.FineMatchedY, 20, m_AxAxAltairU_UI.ImageWidth / 2, 0, 360);
                        axAxCircleMsr1.DetectPrimitives();
                        if (axAxCircleMsr1.CircleIsFitted)
                        {
                            c_Det = true;
                        }
                    }
                }
            }
            else
            {
                System.Windows.MessageBox.Show("No Target.");
                return;
            }
        }

        private void SetMatchRec_Click(object sender, RoutedEventArgs e)
        {
            if(g_SetArea)
            {
                g_SetArea = false;
                SetMatchRec.Content = "Set Detect Area";
                have_setarea = true;
                sa_X = axAxROIBW82.OrgX;
                sa_Y = axAxROIBW82.OrgY;
                sa_W = axAxROIBW82.ROIWidth;
                sa_H = axAxROIBW82.ROIHeight;
            }
            else
            {

                have_setarea = false;
                g_SetArea = true;
                SetMatchRec.Content = "Confirm";
                axAxROIBW82.ParentHandle = g_nActiveSurfaceHandle;
                //axAxROIBW82.Title = "Matching Region";
                axAxROIBW82.SetPlacement(sa_X, sa_Y, sa_W, sa_H);
            }
        }

        private void TextBox_Click(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.TextBox tb = (sender as System.Windows.Controls.TextBox);
            tb.SelectAll();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e )
        {
            System.Windows.Controls.TextBox tb = (sender as System.Windows.Controls.TextBox);
            tb.SelectAll();

            
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            System.Windows.Controls.TextBox tb = (sender as System.Windows.Controls.TextBox);
            switch (tb.Name)
            {
                case "X_res":
                    resX = float.Parse(X_res.Text);
                    break;
                case "Y_res":
                    resY = float.Parse(Y_res.Text);
                    break;
            }

        }



        private void DoCloseAiSys()
        {
            if (AisysIsCreate)
            {
                m_AxAxAltairU_UI.Freeze();
                m_AxAxAltairU_UI.DestroyChannel();
                m_AxAxAltairU_UI = null;
                m_AxAxAltairU_UI.Dispose();

            }
        }

        private void DoCloseMM()
        {
            if (initmm)
            {
                axLensCal.Finish();
                axMotion.Finish();
                axMark.Finish();
            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MotionTimer.Stop();
            DoCloseAiSys();

            DoCloseMM();


            //m_AxAltairU_NoUI = null;
            //m_AxAxCanvas.Dispose();
            GC.Collect();
        }

        private void Tech_btn_Click(object sender, RoutedEventArgs e)
        {
            if(g_Tech)
            {
                axAxMatch1.SrcImageHandle = axAxROIBW81.VegaHandle;
                axAxMatch1.LearnPattern();
                have_learn = true;
                g_fZoomX2 = (float)axAxCanvas2.CanvasWidth / axAxMatch1.PatternWidth;
                g_fZoomY2 = (float)axAxCanvas2.CanvasHeight / axAxMatch1.PatternHeight;
                axAxMatch1.DrawLearnedPattern(axAxCanvas2.hDC, g_fZoomX2, g_fZoomY2, 0, 0);
                axAxCanvas2.RefreshCanvas();
                g_Tech = false;
                Tech_btn.Content = "Get Target";
            }
            else
            {
                axAxROIBW81.ParentHandle = g_nActiveSurfaceHandle;
                axAxROIBW81.Title = "Learn Patten";
                axAxROIBW81.SetPlacement(sa_X, sa_Y, sa_W, sa_H);

                g_Tech = true;
                have_learn = false;
                Tech_btn.Content = "Tech Target";
            }
        }

        private void RefreshImage()
        {
            //Draw image
            int sX = axAxCanvas1.CanvasWidth / 2;
            int sY = axAxCanvas1.CanvasHeight / 2;
            axAxCanvas1.DrawSurface(g_nActiveSurfaceHandle, g_fZoomX, g_fZoomY, 0, 0);
            axAxCanvas1.PenColor = AxOvkBase.TxAxColor.AX_COLOR_LIME;
            axAxCanvas1.DrawLine(0, sY, axAxCanvas1.CanvasWidth, sY, 1, 1, 0, 0); //X Line
            axAxCanvas1.DrawLine(sX, 0, sX, axAxCanvas1.CanvasHeight, 1, 1, 0, 0); //Y Line


            if (g_Tech)
            {
                axAxROIBW81.DrawFrame(axAxCanvas1.hDC, g_fZoomX, g_fZoomY, 0, 0, 16776960);
            }

            //Draw ROI2 : for match purpose in RED=0x0000FF=255
            //axAxROIBW82.DrawFrame(axAxCanvas1.hDC, g_fZoomX, g_fZoomY, 0, 0, 255);
            if (g_SetArea)
            {
                axAxROIBW82.DrawFrame(axAxCanvas1.hDC, g_fZoomX, g_fZoomY, 0, 0, 255);
            }
            if(have_setarea)
            {
                axAxROIBW82.DrawRect(axAxCanvas1.hDC, g_fZoomX, g_fZoomY, 0, 0, 255);
            }
            if (c_Det)
            {
                //axAxCircleMsr1.DrawFrame(axAxCanvas1.hDC, g_fZoomX, g_fZoomY, 0, 0);
                axAxCircleMsr1.DrawFittedPrimitives(axAxCanvas1.hDC, g_fZoomX, g_fZoomY, 0, 0);
                axAxCircleMsr1.DrawFittedCenter(axAxCanvas1.hDC, g_fZoomX, g_fZoomY, 0, 0, 255);
                //axAxCircleMsr1.DrawPoints(axAxCanvas1.hDC, g_fZoomX, g_fZoomY, 0, 0);
                //axAxCircleMsr1.DrawTolerances(axAxCanvas1.hDC, g_fZoomX, g_fZoomY, 0, 0);

            }

            //Draw matchedpattern
            //axAxMatch1.DrawMatchedPattern(axAxCanvas1.hDC, axAxMatch1.PatternIndex, g_fZoomX, g_fZoomY, 0, 0);
            //axAxMatch.DrawMatchedPattern(m_AxAxCanvas.hDC, axAxMatch.PatternIndex, g_fZoomX, g_fZoomY, 0, 0);

            //Refresh canvas
            //axAxCanvas1.RefreshCanvas();
            axAxCanvas1.RefreshCanvas();
            //axAxMatch
            //axAxMatch1.PatternIndex = Convert.ToInt16(txPatternIndex.Value);
            //labelMatchedPosX.Text = Convert.ToString(axAxMatch1.MatchedX);
            //labelMatchedPosY.Text = Convert.ToString(axAxMatch1.MatchedY);
            //labelMatchedAngle.Text = Convert.ToString(axAxMatch1.MatchedAngle);
            //labelMatchedScore.Text = Convert.ToString(axAxMatch1.MatchedScore);
        }
    }
}
