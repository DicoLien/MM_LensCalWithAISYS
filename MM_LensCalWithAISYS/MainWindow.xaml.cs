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

using AxAxOvkBase;
using AxAxOvkPat;
using AxAxOvkMsr;
using System.Timers;

namespace MM_LensCalWithAISYS
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        AxMMMark axMark = new AxMMMark();
        AxMMMotion axMotion = new AxMMMotion();
        AxMMLensCal axLensCal = new AxMMLensCal();

        System.Timers.Timer MotionTimer = new System.Timers.Timer();
        


        AxAxAltairU m_AxAxAltairU_UI = new AxAxAltairU();

        AxAxCanvas axAxCanvas1 = new AxAxCanvas();
        AxAxCanvas axAxCanvas2 = new AxAxCanvas();

        AxAxROIBW8 axAxROIBW81 = new AxAxROIBW8();
        AxAxROIBW8 axAxROIBW82 = new AxAxROIBW8();


        AxAxMatch axAxMatch1 = new AxAxMatch();

        AxAxCircleMsr axAxCircleMsr1 = new AxAxCircleMsr();
       
        int g_nActiveSurfaceHandle;
        AxOvkBase.TxAxHitHandle nLockHandle = new AxOvkBase.TxAxHitHandle();

        AxOvkMsr.TxAxCircleMsrDragHandle cnLockHandle = new AxOvkMsr.TxAxCircleMsrDragHandle();

        private double dX = -1, dY = -1, dZ = -1, dR = -1; //For axMMMotion1.GetCurPostion
        private double eX = -1, eY = -1, eZ = -1, eR = -1; //For AxMotion.GetCurPostint(Encoder)
        float g_fZoomX, g_fZoomY, g_fZoomX2, g_fZoomY2, org_fZoomX, org_fZoomY;
        bool g_Tech, g_SetArea, have_setarea, c_Det, have_learn, mm_startcal, initwfh;
        bool bLockROI1Flag, bLockROI2Flag, bCircleMsrFlag, AisysIsCreate;
        int sa_X, sa_Y, sa_W, sa_H;
        float p_centerX, p_centerY, r_centerX, r_centerY, mm_x, mm_y;
        private FlowDocument doc = new FlowDocument();
        private delegate void UpdateSysMsgCallBack(System.Windows.Controls.RichTextBox ctl, string value, string color);
        private delegate void UpdateXYZCallBack(TextBlock ctl, string value);

        List<XYCal> CalList = new List<XYCal>();


        int mm_opt, mm_sgmt, mm_sot, mm_SFT, mm_totalgridpoint;

        public MainWindow()
        {
            InitializeComponent();

            //InitAisys();
            InitWFH();
            //InitLensCal();
            //InitMotion();


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
        }

        private void CheckPostion()
        {

                axMotion.GetCurPosition(ref dX, ref dY, ref dZ, ref dR, 1);
                UpdateXYZ(Xloc, dX.ToString("0.000"));
                UpdateXYZ(Yloc, dY.ToString("0.000"));
                UpdateXYZ(Zloc, dZ.ToString("0.000"));
                axMotion.GetCurPosition(ref eX, ref eY, ref eZ, ref eR, 2);
                UpdateXYZ(Xenc, eX.ToString("0.000"));
                UpdateXYZ(Yenc, eY.ToString("0.000"));
                UpdateXYZ(Zenc, eZ.ToString("0.000"));

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
            }
        }
        private void InitMotion()
        {
            MotionTimer.Elapsed += MotionTimer_Tick;
            MotionTimer.AutoReset = true;
            MotionTimer.Enabled = true;
            MotionTimer.Start();
        }

        private void InitWFH()
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
            initwfh = true;
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

            axAxCanvas1.CanvasWidth = Convert.ToInt32(wfh_canvas1.Width);
            axAxCanvas1.CanvasHeight = Convert.ToInt32(wfh_canvas1.Height);
            axAxCanvas1.Width = Convert.ToInt32(wfh_canvas1.Width);
            axAxCanvas1.Height = Convert.ToInt32(wfh_canvas1.Height);

            axAxCanvas1.OnCanvasMouseDown += new AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseDownEventHandler(axAxCanvas1_OnCanvasMouseDown);
            axAxCanvas1.OnCanvasMouseMove += new AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseMoveEventHandler(axAxCanvas1_OnCanvasMouseMove);
            axAxCanvas1.OnCanvasMouseUp += new AxAxOvkBase.IAxCanvasEvents_OnCanvasMouseUpEventHandler(axAxCanvas1_OnCanvasMouseUp);
            //aisys_canvas1.MouseWheel += new System.Windows.Forms.MouseEventHandler(Aisys_canvas1_On_MouseWheel);
            //aisys_canvas1.GotFocus += new EventHandler(Aisys_canvas1_On_GotFocus);
            //aisys_canvas1.LostFocus += new EventHandler(Aisys_canvas1_On_LostFocus);
            //aisys_canvas1.MouseDown += new System.Windows.Forms.MouseEventHandler(Aisys_canvas1_On_MouseDown);


            axAxCanvas2.BeginInit();
            wfh_canvas2.Child = axAxCanvas2;
            axAxCanvas2.EndInit();
            axAxCanvas2.CanvasWidth = Convert.ToInt32(wfh_canvas2.Width);
            axAxCanvas2.CanvasHeight = Convert.ToInt32(wfh_canvas2.Height);
            axAxCanvas2.Width = Convert.ToInt32(wfh_canvas2.Width);
            axAxCanvas2.Height = Convert.ToInt32(wfh_canvas2.Height);



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
            }
        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //InitAisys();
            //AutoCreateChannel();

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

        private void Mo_XYright_btn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogEnd(2);
        }

        private void Mo_XYdown_btn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogBegin(4, 61439, 0);
        }

        private void Mo_XYdown_btn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            axMotion.JogEnd(4);
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
            axMotion.GetCurPosition(ref dX, ref dY, ref dZ, ref dR, 1);
            double endX = dX + Convert.ToDouble(X_dis.Text);
            double endY = dY + Convert.ToDouble(Y_dis.Text);
            UpdateSysMsg(SysMsg, "Move To X:" + endX.ToString() + " Y:" + endY.ToString());
            axMotion.MoveTo(Convert.ToDouble(X_dis.Text),
                Convert.ToDouble(Y_dis.Text),
                0,
                0,
                1);
            
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
            mm_startcal = true;
            Thread t1 = new Thread(new ThreadStart(DoStartCal));
            //DoStartCal();

            t1.Start();
        }

        private void DoStartCal()
        {

            axMotion.SetCurPosition(0, 0, 0, 0, 0);
            axMotion.SetCurPosition(0, 0, 0, 0, 1);
            if (mm_startcal)
            {
                double moveX, moveY;
                for (int i = 1; i < mm_totalgridpoint; i++)
                {
                    moveX = axLensCal.GetCorrectPointX(i);
                    moveY = axLensCal.GetCorrectPointY(i);
                    axMotion.MoveTo(moveX, moveY, 0, 0, 1);
                    StartMatch();
                   while(mm_x > Convert.ToDouble(X_res.ToString()) &&
                        mm_y > Convert.ToDouble(Y_res.ToString()))
                    {
                        axMotion.MoveTo(mm_x, mm_y, 0, 0, 1);
                        StartMatch();
                    }
                    //Pxy.AddRange(new xylist[] { new xylist(0, 0, 0) });
                    CalList.AddRange(new XYCal[] { new XYCal(i, mm_x, mm_y) });
                }
            }
        }

        private void DoOutPut()
        {
            System.Windows.Forms.TextBox result = new System.Windows.Forms.TextBox();
            int C_number = mm_totalgridpoint / 2;
            double cp_X = CalList[C_number].X;
            double cp_Y = CalList[C_number].Y;
            int j = 0;
            for(int i = 0; i < mm_totalgridpoint -1; i++)
            {
                
                double temp_X = CalList[i].X;
                double temp_Y = CalList[i].Y;
                double result_X = temp_X - cp_X;
                double result_Y = temp_Y - cp_Y;
                result.AppendText(CalList[i].No.ToString() + " " +
                    result_X + " " +
                    result_Y + Environment.NewLine);
            }
                    
        }

        private void Mo_MoveXYZ_Click(object sender, RoutedEventArgs e)
        {
            if (mm_opt == 0)
            {

                UpdateSysMsg(SysMsg, "Move To X:" + mo_X.Text + " Y:" + mo_Y.Text + " Z:" + mo_Z.Text);

                axMotion.MoveTo(Convert.ToDouble(mo_X.Text),
                    Convert.ToDouble(mo_Y.Text),
                    Convert.ToDouble(mo_Z.Text),
                    0,
                    mm_opt);
            }
            else
            {
                axMotion.GetCurPosition(ref dX, ref dY, ref dZ, ref dR, 1);
                double endX = dX + Convert.ToDouble(mo_X.Text);
                double endY = dY + Convert.ToDouble(mo_Y.Text);
                double endZ = dZ + Convert.ToDouble(mo_Z.Text);

                UpdateSysMsg(SysMsg, "Move To X:" + endX.ToString() + " Y:" + endY.ToString() + " Z:" + endZ.ToString());

                axMotion.MoveTo(Convert.ToDouble(mo_X.Text),
                    Convert.ToDouble(mo_Y.Text),
                    Convert.ToDouble(mo_Z.Text),
                    0,
                    mm_opt);
            }
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
            if (initwfh)
            {
                axLensCal.Finish();
                axMotion.Finish();
                axMark.Finish();
            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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
