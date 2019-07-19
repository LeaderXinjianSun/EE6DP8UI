using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using HalconDotNet;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
//using BingLibrary.hjb;
using Leader.DeltaAS300ModbusTCP;
using System.Windows.Threading;
using System.Drawing;
using OfficeOpenXml;
using Microsoft.Win32;
using System.Windows.Forms;
using System.IO.Ports;
using SxjLibrary;
using 臻鼎科技OraDB;
using System.Data;

namespace FQAPEE6AutoBreakPasteDeviceUI
{
    public delegate void DisplayResultsDelegate();
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        string MessageStr = "";
        HDevelopExport hdev_export;
        List<HTuple> drawing_objects;
        HTuple ShuXian, HengXian;
        DisplayResultsDelegate display_results_delegate;
        HDrawingObject.HDrawingObjectCallback cb;
        HObject ho_EdgeAmplitude;
        HObject background_image = null;
        //HObject background_image2 = null;
        object image_lock = new object();
        private HImage image;
        private HRegion Rectangle, ModelRegion;
        HWindow Window = null;
        //HWindow Window2 = null;
        private HShapeModel ShapeModel;
        private double Row, Column;
        DataAxisCoor CoorPar = new DataAxisCoor();
        AS300ModbusTCP aS300ModbusTCP;
        HTuple RowCheck, ColumnCheck, AngleCheck, ScaleCheck, Score;
        HTuple homMat2D;
        Bitmap ImgBitmap;
        MySQLClass mySQLClass;
        int CX, CY;
        //bool Window2Init = false;
        double Line1Angle, Line1Dist, Line2Angle, Line2Dist;

        object modbustcp = new object();
        bool[] PLC_In;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        DispatcherTimer dispatcherTimer1 = new DispatcherTimer();
        Double[] CrossPoint;
        string[] ImageFiles;
        int ImageIndex;
        private dialog mydialog = new dialog();
        bool ShutdownFlag = false;
        bool Loadin = false;
        bool PlcStatus = false;
        int EE6Mode;
        int LoadinCount = 0;
        string maintainIniFilename = "D:\\maintain\\" + "maintain.ini";
        string Paramaterini = System.Environment.CurrentDirectory + "\\Parameter.ini";
        long opendoorsec;
        long leisuresec;
        bool opendoorflag = false, leisureflag = false;
        int tumotimes;
        int fanzhuanFailTimes, scanFailTimes;
        string BaoYangHour = "";
        //delegate void DeviceLostRouteEventHandler(object sender, DeviceLostEventArgs e);
        //public class DeviceLostEventArgs : RoutedEventArgs
        //{
        //    public DeviceLostEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source) { }

        //}

        //private void ICImagingControl_DeviceLost(object sender, TIS.Imaging.ICImagingControl.DeviceLostEventArgs e)
        //{
        //    //throw new NotImplementedException();

        //    //DeviceLostEventArgs args = new DeviceLostEventArgs(DeviceLostEvent, this);
        //    //this.RaiseEvent(args);
        //    System.Windows.Forms.MessageBox.Show("Device Lost");
        //}
        public MainWindow()
        {
            InitializeComponent();
            #region 判断系统是否已启动

            System.Diagnostics.Process[] myProcesses = System.Diagnostics.Process.GetProcessesByName("FQAPEE6AutoBreakPasteDeviceUI");//获取指定的进程名   
            if (myProcesses.Length > 1) //如果可以获取到知道的进程名则说明已经启动
            {
                System.Windows.MessageBox.Show("不允许重复打开软件");
                System.Windows.Application.Current.Shutdown();
                ShutdownFlag = true;
            }
            else
            {
                hdev_export = new HDevelopExport();
                drawing_objects = new List<HTuple>();
                mySQLClass = new MySQLClass();
                //mySQLClass.test();
                //iCImagingControl.DeviceLost += ICImagingControl_DeviceLost;
            }


            #endregion



        }
        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            LoadinCount = 0;
            var validComNames = SerialPort.GetPortNames();
            foreach (var comName in validComNames)
            {
                if (!Com.Items.Contains(comName))
                    Com.Items.Add(comName);
            }
            List<string> toRemove = new List<string>();
            foreach (string addedName in Com.Items)
            {
                if (!validComNames.Contains(addedName))
                    toRemove.Add(addedName);
            }
            foreach (string remove in toRemove)
            {
                Com.Items.Remove(remove);
            }
        }
        private void GrapButton_Click(object sender, RoutedEventArgs e)
        {
            //dispatcherTimer.Stop();
            LoadinCount = 0;
            grapAction();
        }
        private bool grapAction()
        {
            bool r = false;
            OnClearAllObjects();
            r = hdev_export.GrapCamera();
            if (image != null)
            {
                image.Dispose();
            }

            image = new HImage(hdev_export.ho_Image);
            hSmartWindowControlWPF1.HalconWindow.DispObj(image);
            return r;
        }
        private void grapAction1()
        {
            OnClearAllObjects();
            //hdev_export.GrapCamera();
            hdev_export.ReadImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
            if (image != null)
            {
                image.Dispose();
            }

            image = new HImage(hdev_export.ho_Image);
            hSmartWindowControlWPF1.HalconWindow.DispObj(image);

        }
        private void grapAction2()
        {
            OnClearAllObjects();
            //hdev_export.GrapCamera();
            hdev_export.ReadImage(ImageFiles[ImageIndex]);
            ImageIndex++;
            if (ImageIndex >= ImageFiles.Length)
            {
                ImageIndex = 0;
            }
            ImageIndexNum.Text = ImageIndex.ToString();
            if (image != null)
            {
                image.Dispose();
            }

            image = new HImage(hdev_export.ho_Image);
            hSmartWindowControlWPF1.HalconWindow.DispObj(image);

        }
        private void OnClearAllObjects()
        {
            lock (image_lock)
            {
                foreach (HTuple dobj in drawing_objects)
                {
                    HOperatorSet.ClearDrawingObject(dobj);
                }
                drawing_objects.Clear();
            }
            hSmartWindowControlWPF1.HalconWindow.ClearWindow();
        }
        private void DrawButton_Click(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            HTuple draw_id;
            if (drawing_objects.Count >= 2)
            {
                OnClearAllObjects();
            }


            if ((bool)ImageCheckBox.IsChecked)
            {
                hdev_export.ReadImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
            }
            else
            {
                hdev_export.GrapCamera();
            }
            background_image = hdev_export.ho_Image;
            hSmartWindowControlWPF1.HalconWindow.AttachBackgroundToWindow(new HImage(background_image));
            hdev_export.add_new_drawing_object("rectangle2", hSmartWindowControlWPF1.HalconID, out draw_id);
            SetCallbacks(draw_id, 0);
        }
        private void SetCallbacks(HTuple draw_id, int option)
        {
            // Set callbacks for all relevant interactions
            switch (option)
            {
                case 0:
                    drawing_objects.Add(draw_id);
                    break;
                case 1:
                    ShuXian = draw_id;
                    break;
                case 2:
                    HengXian = draw_id;
                    break;
                default:
                    break;
            }

            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(cb);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_resize", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_drag", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_attach", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_select", ptr);
            lock (image_lock)
            {
                HOperatorSet.AttachDrawingObjectToWindow(hSmartWindowControlWPF1.HalconID, draw_id);
            }
        }
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            HTuple hv_ParamValues1, hv_ParamValues2;
            HObject Rec1, Rec2, Rec3;
            HImage ImgReduced;

            if (drawing_objects.Count >= 2)
            {
                HOperatorSet.GetDrawingObjectParams(drawing_objects[0], (new HTuple("row")).TupleConcat(new HTuple("column")
                ).TupleConcat(new HTuple("phi")).TupleConcat(new HTuple("length1")).TupleConcat(new HTuple("length2")), out hv_ParamValues1);
                HOperatorSet.GetDrawingObjectParams(drawing_objects[1], (new HTuple("row")).TupleConcat(new HTuple("column")
                ).TupleConcat(new HTuple("phi")).TupleConcat(new HTuple("length1")).TupleConcat(new HTuple("length2")), out hv_ParamValues2);
                HOperatorSet.GenEmptyObj(out Rec1);
                HOperatorSet.GenEmptyObj(out Rec2);
                HOperatorSet.GenEmptyObj(out Rec3);
                HOperatorSet.GenRectangle2(out Rec1, hv_ParamValues1.DArr[0], hv_ParamValues1.DArr[1], hv_ParamValues1.DArr[2], hv_ParamValues1.DArr[3], hv_ParamValues1.DArr[4]);
                HOperatorSet.GenRectangle2(out Rec2, hv_ParamValues2.DArr[0], hv_ParamValues2.DArr[1], hv_ParamValues2.DArr[2], hv_ParamValues2.DArr[3], hv_ParamValues2.DArr[4]);
                HOperatorSet.SymmDifference(Rec1, Rec2, out Rec3);
                Rectangle = new HRegion(Rec3);

                Rectangle.AreaCenter(out Row, out Column);
                //                //hdev_export.GrapCamera();
                if ((bool)ImageCheckBox.IsChecked)
                {
                    grapAction1();
                }
                image.Dispose();
                image = new HImage(hdev_export.ho_Image);
                image.DispObj(Window);
                ImgReduced = image.ReduceDomain(Rectangle);
                ImgReduced.InspectShapeModel(out ModelRegion, 1, 15);
                //                ShapeModel = new HShapeModel(ImgReduced, 6, 0, new HTuple(360.0).TupleRad().D,
                //new HTuple(0.4391).TupleRad().D, "no_pregeneration", "use_polarity", 16, 4);
                ShapeModel = new HShapeModel(ImgReduced, new HTuple(6), -0.52, 1.05, new HTuple(0.435).TupleRad(),
                    new HTuple("none").TupleConcat("no_pregeneration"), "use_polarity", new HTuple(10).TupleConcat(15).TupleConcat(4), new HTuple(5)
                    );






                //        (ho_TemplateImage, 6, (new HTuple(0)).TupleRad()
                //, (new HTuple(360)).TupleRad(), (new HTuple(0.4391)).TupleRad(), (new HTuple("point_reduction_high")).TupleConcat(
                //"no_pregeneration"), "use_polarity", ((new HTuple(12)).TupleConcat(20)).TupleConcat(
                //6), 4, out hv_ModelID);




                Window.SetColor("green");
                Window.SetDraw("margin");
                ModelRegion.DispObj(Window);
                if (!(bool)ImageCheckBox.IsChecked)
                {
                    image.WriteImage("tiff", 0, System.Environment.CurrentDirectory + "\\ModelImage.tiff");
                }

                ShapeModel.WriteShapeModel(System.Environment.CurrentDirectory + "\\ShapeModel.shm");
            }
            else
            {
                MsgTextBox.Text = AddMessage("少于2个Region，无法创建");
            }


        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            if (!ShutdownFlag)
            {
                try
                {
                    hdev_export.CloseCamera();

                }
                catch
                {


                }
                try
                {
                    FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Create);
                    BinaryFormatter b = new BinaryFormatter();
                    b.Serialize(fileStream, CoorPar);
                    fileStream.Close();
                    dispatcherTimer1.Stop();
                }
                catch 
                {

                    
                }
   

            }



        }

        private void Calib1Button_Click(object sender, RoutedEventArgs e)
        {

            LoadinCount = 0;

            grapAction();
            Action();
            if (RowCheck.Length == 1)
            {
                CoorPar.Row1 = RowCheck.D;
                CoorPar.DRow1 = CX;
                MsgTextBox.Text = AddMessage("CX1: " + CoorPar.Row1.ToString() + "; " + CoorPar.DRow1.ToString());
            }
        }

        #region Bing

        public void gen_arrow_contour_xld(out HObject ho_Arrow, HTuple hv_Row1, HTuple hv_Column1,
   HTuple hv_Row2, HTuple hv_Column2, HTuple hv_HeadLength, HTuple hv_HeadWidth)
        {
            // Stack for temporary objects
            HObject[] OTemp = new HObject[20];

            // Local iconic variables

            HObject ho_TempArrow = null;

            // Local control variables

            HTuple hv_Length = null, hv_ZeroLengthIndices = null;
            HTuple hv_DR = null, hv_DC = null, hv_HalfHeadWidth = null;
            HTuple hv_RowP1 = null, hv_ColP1 = null, hv_RowP2 = null;
            HTuple hv_ColP2 = null, hv_Index = null;
            // Initialize local and output iconic variables
            HOperatorSet.GenEmptyObj(out ho_Arrow);
            HOperatorSet.GenEmptyObj(out ho_TempArrow);
            try
            {
                //This procedure generates arrow shaped XLD contours,
                //pointing from (Row1, Column1) to (Row2, Column2).
                //If starting and end point are identical, a contour consisting
                //of a single point is returned.
                //
                //input parameteres:
                //Row1, Column1: Coordinates of the arrows' starting points
                //Row2, Column2: Coordinates of the arrows' end points
                //HeadLength, HeadWidth: Size of the arrow heads in pixels
                //
                //output parameter:
                //Arrow: The resulting XLD contour
                //
                //The input tuples Row1, Column1, Row2, and Column2 have to be of
                //the same length.
                //HeadLength and HeadWidth either have to be of the same length as
                //Row1, Column1, Row2, and Column2 or have to be a single element.
                //If one of the above restrictions is violated, an error will occur.
                //
                //
                //Init
                ho_Arrow.Dispose();
                HOperatorSet.GenEmptyObj(out ho_Arrow);
                //
                //Calculate the arrow length
                HOperatorSet.DistancePp(hv_Row1, hv_Column1, hv_Row2, hv_Column2, out hv_Length);
                //
                //Mark arrows with identical start and end point
                //(set Length to -1 to avoid division-by-zero exception)
                hv_ZeroLengthIndices = hv_Length.TupleFind(0);
                if ((int)(new HTuple(hv_ZeroLengthIndices.TupleNotEqual(-1))) != 0)
                {
                    if (hv_Length == null)
                        hv_Length = new HTuple();
                    hv_Length[hv_ZeroLengthIndices] = -1;
                }
                //
                //Calculate auxiliary variables.
                hv_DR = (1.0 * (hv_Row2 - hv_Row1)) / hv_Length;
                hv_DC = (1.0 * (hv_Column2 - hv_Column1)) / hv_Length;
                hv_HalfHeadWidth = hv_HeadWidth / 2.0;
                //
                //Calculate end points of the arrow head.
                hv_RowP1 = (hv_Row1 + ((hv_Length - hv_HeadLength) * hv_DR)) + (hv_HalfHeadWidth * hv_DC);
                hv_ColP1 = (hv_Column1 + ((hv_Length - hv_HeadLength) * hv_DC)) - (hv_HalfHeadWidth * hv_DR);
                hv_RowP2 = (hv_Row1 + ((hv_Length - hv_HeadLength) * hv_DR)) - (hv_HalfHeadWidth * hv_DC);
                hv_ColP2 = (hv_Column1 + ((hv_Length - hv_HeadLength) * hv_DC)) + (hv_HalfHeadWidth * hv_DR);
                //
                //Finally create output XLD contour for each input point pair
                for (hv_Index = 0; (int)hv_Index <= (int)((new HTuple(hv_Length.TupleLength())) - 1); hv_Index = (int)hv_Index + 1)
                {
                    if ((int)(new HTuple(((hv_Length.TupleSelect(hv_Index))).TupleEqual(-1))) != 0)
                    {
                        //Create_ single points for arrows with identical start and end point
                        ho_TempArrow.Dispose();
                        HOperatorSet.GenContourPolygonXld(out ho_TempArrow, hv_Row1.TupleSelect(
                            hv_Index), hv_Column1.TupleSelect(hv_Index));
                    }
                    else
                    {
                        //Create arrow contour
                        ho_TempArrow.Dispose();
                        HOperatorSet.GenContourPolygonXld(out ho_TempArrow, ((((((((((hv_Row1.TupleSelect(
                            hv_Index))).TupleConcat(hv_Row2.TupleSelect(hv_Index)))).TupleConcat(
                            hv_RowP1.TupleSelect(hv_Index)))).TupleConcat(hv_Row2.TupleSelect(hv_Index)))).TupleConcat(
                            hv_RowP2.TupleSelect(hv_Index)))).TupleConcat(hv_Row2.TupleSelect(hv_Index)),
                            ((((((((((hv_Column1.TupleSelect(hv_Index))).TupleConcat(hv_Column2.TupleSelect(
                            hv_Index)))).TupleConcat(hv_ColP1.TupleSelect(hv_Index)))).TupleConcat(
                            hv_Column2.TupleSelect(hv_Index)))).TupleConcat(hv_ColP2.TupleSelect(
                            hv_Index)))).TupleConcat(hv_Column2.TupleSelect(hv_Index)));
                    }
                    {
                        HObject ExpTmpOutVar_0;
                        HOperatorSet.ConcatObj(ho_Arrow, ho_TempArrow, out ExpTmpOutVar_0);
                        ho_Arrow.Dispose();
                        ho_Arrow = ExpTmpOutVar_0;
                    }
                }
                ho_TempArrow.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_TempArrow.Dispose();

                throw HDevExpDefaultException;
            }
        }

        public void pts_to_best_line(out HObject ho_Line, HTuple hv_Rows, HTuple hv_Cols,
           HTuple hv_ActiveNum, out HTuple hv_Row1, out HTuple hv_Col1, out HTuple hv_Row2,
           out HTuple hv_Col2)
        {
            // Local iconic variables

            HObject ho_Contour = null;

            // Local control variables

            HTuple hv_Length = null, hv_Nr = new HTuple();
            HTuple hv_Nc = new HTuple(), hv_Dist = new HTuple(), hv_Length1 = new HTuple();
            // Initialize local and output iconic variables
            HOperatorSet.GenEmptyObj(out ho_Line);
            HOperatorSet.GenEmptyObj(out ho_Contour);
            try
            {
                hv_Row1 = 0;
                hv_Col1 = 0;
                hv_Row2 = 0;
                hv_Col2 = 0;
                ho_Line.Dispose();
                HOperatorSet.GenEmptyObj(out ho_Line);
                HOperatorSet.TupleLength(hv_Cols, out hv_Length);

                if ((int)((new HTuple(hv_Length.TupleGreaterEqual(hv_ActiveNum))).TupleAnd(
                    new HTuple(hv_ActiveNum.TupleGreater(1)))) != 0)
                {
                    ho_Contour.Dispose();
                    HOperatorSet.GenContourPolygonXld(out ho_Contour, hv_Rows, hv_Cols);
                    HOperatorSet.FitLineContourXld(ho_Contour, "tukey", hv_ActiveNum, 0, 5, 2,
                        out hv_Row1, out hv_Col1, out hv_Row2, out hv_Col2, out hv_Nr, out hv_Nc,
                        out hv_Dist);
                    HOperatorSet.TupleLength(hv_Dist, out hv_Length1);
                    if ((int)(new HTuple(hv_Length1.TupleLess(1))) != 0)
                    {
                        ho_Contour.Dispose();

                        return;
                    }
                    ho_Line.Dispose();
                    HOperatorSet.GenContourPolygonXld(out ho_Line, hv_Row1.TupleConcat(hv_Row2),
                        hv_Col1.TupleConcat(hv_Col2));
                }

                ho_Contour.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_Contour.Dispose();

                throw HDevExpDefaultException;
            }
        }

        public void rake(HObject ho_Image, out HObject ho_Regions, HTuple hv_Elements,
            HTuple hv_DetectHeight, HTuple hv_DetectWidth, HTuple hv_Sigma, HTuple hv_Threshold,
            HTuple hv_Transition, HTuple hv_Select, HTuple hv_Row1, HTuple hv_Column1, HTuple hv_Row2,
            HTuple hv_Column2, out HTuple hv_ResultRow, out HTuple hv_ResultColumn)
        {
            // Stack for temporary objects
            HObject[] OTemp = new HObject[20];

            // Local iconic variables

            HObject ho_Rectangle = null, ho_Arrow1 = null;

            // Local control variables

            HTuple hv_Width = null, hv_Height = null, hv_ATan = null;
            HTuple hv_Deg1 = null, hv_Deg = null, hv_i = null, hv_RowC = new HTuple();
            HTuple hv_ColC = new HTuple(), hv_Distance = new HTuple();
            HTuple hv_RowL2 = new HTuple(), hv_RowL1 = new HTuple();
            HTuple hv_ColL2 = new HTuple(), hv_ColL1 = new HTuple();
            HTuple hv_MsrHandle_Measure = new HTuple(), hv_RowEdge = new HTuple();
            HTuple hv_ColEdge = new HTuple(), hv_Amplitude = new HTuple();
            HTuple hv_tRow = new HTuple(), hv_tCol = new HTuple();
            HTuple hv_t = new HTuple(), hv_Number = new HTuple(), hv_j = new HTuple();
            HTuple hv_Select_COPY_INP_TMP = hv_Select.Clone();
            HTuple hv_Transition_COPY_INP_TMP = hv_Transition.Clone();

            // Initialize local and output iconic variables
            HOperatorSet.GenEmptyObj(out ho_Regions);
            HOperatorSet.GenEmptyObj(out ho_Rectangle);
            HOperatorSet.GenEmptyObj(out ho_Arrow1);
            try
            {
                HOperatorSet.GetImageSize(ho_Image, out hv_Width, out hv_Height);

                ho_Regions.Dispose();
                HOperatorSet.GenEmptyObj(out ho_Regions);
                hv_ResultRow = new HTuple();
                hv_ResultColumn = new HTuple();
                HOperatorSet.TupleAtan2((-hv_Row2) + hv_Row1, hv_Column2 - hv_Column1, out hv_ATan);
                HOperatorSet.TupleDeg(hv_ATan, out hv_Deg1);

                hv_ATan = hv_ATan + ((new HTuple(90)).TupleRad());

                HOperatorSet.TupleDeg(hv_ATan, out hv_Deg);

                HTuple end_val13 = hv_Elements;
                HTuple step_val13 = 1;
                for (hv_i = 1; hv_i.Continue(end_val13, step_val13); hv_i = hv_i.TupleAdd(step_val13))
                {
                    hv_RowC = hv_Row1 + (((hv_Row2 - hv_Row1) * hv_i) / (hv_Elements + 1));
                    hv_ColC = hv_Column1 + (((hv_Column2 - hv_Column1) * hv_i) / (hv_Elements + 1));
                    if ((int)((new HTuple((new HTuple((new HTuple(hv_RowC.TupleGreater(hv_Height - 1))).TupleOr(
                        new HTuple(hv_RowC.TupleLess(0))))).TupleOr(new HTuple(hv_ColC.TupleGreater(
                        hv_Width - 1))))).TupleOr(new HTuple(hv_ColC.TupleLess(0)))) != 0)
                    {
                        continue;
                    }
                    if ((int)(new HTuple(hv_Elements.TupleEqual(1))) != 0)
                    {
                        HOperatorSet.DistancePp(hv_Row1, hv_Column1, hv_Row2, hv_Column2, out hv_Distance);
                        ho_Rectangle.Dispose();
                        HOperatorSet.GenRectangle2ContourXld(out ho_Rectangle, hv_RowC, hv_ColC,
                            hv_Deg.TupleRad(), hv_DetectHeight / 2, hv_Distance / 2);
                    }
                    else
                    {
                        ho_Rectangle.Dispose();
                        HOperatorSet.GenRectangle2ContourXld(out ho_Rectangle, hv_RowC, hv_ColC,
                            hv_Deg.TupleRad(), hv_DetectHeight / 2, hv_DetectWidth / 2);
                    }

                    {
                        HObject ExpTmpOutVar_0;
                        HOperatorSet.ConcatObj(ho_Regions, ho_Rectangle, out ExpTmpOutVar_0);
                        ho_Regions.Dispose();
                        ho_Regions = ExpTmpOutVar_0;
                    }
                    if ((int)(new HTuple(hv_i.TupleEqual(1))) != 0)
                    {
                        hv_RowL2 = hv_RowC + ((hv_DetectHeight / 2) * (((-hv_ATan)).TupleSin()));
                        hv_RowL1 = hv_RowC - ((hv_DetectHeight / 2) * (((-hv_ATan)).TupleSin()));
                        hv_ColL2 = hv_ColC + ((hv_DetectHeight / 2) * (((-hv_ATan)).TupleCos()));
                        hv_ColL1 = hv_ColC - ((hv_DetectHeight / 2) * (((-hv_ATan)).TupleCos()));
                        ho_Arrow1.Dispose();
                        gen_arrow_contour_xld(out ho_Arrow1, hv_RowL1, hv_ColL1, hv_RowL2, hv_ColL2,
                            25, 25);
                        {
                            HObject ExpTmpOutVar_0;
                            HOperatorSet.ConcatObj(ho_Regions, ho_Arrow1, out ExpTmpOutVar_0);
                            ho_Regions.Dispose();
                            ho_Regions = ExpTmpOutVar_0;
                        }
                    }
                    HOperatorSet.GenMeasureRectangle2(hv_RowC, hv_ColC, hv_Deg.TupleRad(), hv_DetectHeight / 2,
                        hv_DetectWidth / 2, hv_Width, hv_Height, "nearest_neighbor", out hv_MsrHandle_Measure);

                    if ((int)(new HTuple(hv_Transition_COPY_INP_TMP.TupleEqual("negative"))) != 0)
                    {
                        hv_Transition_COPY_INP_TMP = "negative";
                    }
                    else
                    {
                        if ((int)(new HTuple(hv_Transition_COPY_INP_TMP.TupleEqual("positive"))) != 0)
                        {
                            hv_Transition_COPY_INP_TMP = "positive";
                        }
                        else
                        {
                            hv_Transition_COPY_INP_TMP = "all";
                        }
                    }

                    if ((int)(new HTuple(hv_Select_COPY_INP_TMP.TupleEqual("first"))) != 0)
                    {
                        hv_Select_COPY_INP_TMP = "first";
                    }
                    else
                    {
                        if ((int)(new HTuple(hv_Select_COPY_INP_TMP.TupleEqual("last"))) != 0)
                        {
                            hv_Select_COPY_INP_TMP = "last";
                        }
                        else
                        {
                            hv_Select_COPY_INP_TMP = "all";
                        }
                    }

                    HOperatorSet.MeasurePos(ho_Image, hv_MsrHandle_Measure, hv_Sigma, hv_Threshold,
                        hv_Transition_COPY_INP_TMP, hv_Select_COPY_INP_TMP, out hv_RowEdge, out hv_ColEdge,
                        out hv_Amplitude, out hv_Distance);
                    HOperatorSet.CloseMeasure(hv_MsrHandle_Measure);
                    hv_tRow = 0;
                    hv_tCol = 0;
                    hv_t = 0;
                    HOperatorSet.TupleLength(hv_RowEdge, out hv_Number);
                    if ((int)(new HTuple(hv_Number.TupleLess(1))) != 0)
                    {
                        continue;
                    }
                    HTuple end_val69 = hv_Number - 1;
                    HTuple step_val69 = 1;
                    for (hv_j = 0; hv_j.Continue(end_val69, step_val69); hv_j = hv_j.TupleAdd(step_val69))
                    {
                        if ((int)(new HTuple(((((hv_Amplitude.TupleSelect(hv_j))).TupleAbs())).TupleGreater(
                            hv_t))) != 0)
                        {
                            hv_tRow = hv_RowEdge.TupleSelect(hv_j);
                            hv_tCol = hv_ColEdge.TupleSelect(hv_j);
                            hv_t = ((hv_Amplitude.TupleSelect(hv_j))).TupleAbs();
                        }
                    }
                    if ((int)(new HTuple(hv_t.TupleGreater(0))) != 0)
                    {
                        hv_ResultRow = hv_ResultRow.TupleConcat(hv_tRow);
                        hv_ResultColumn = hv_ResultColumn.TupleConcat(hv_tCol);
                    }
                }
                HOperatorSet.TupleLength(hv_ResultRow, out hv_Number);

                ho_Rectangle.Dispose();
                ho_Arrow1.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_Rectangle.Dispose();
                ho_Arrow1.Dispose();

                throw HDevExpDefaultException;
            }
        }

        private double[] findLine(double x1, double y1, double x2, double y2, HTuple thred, HTuple height,HTuple elem,HTuple actnum)
        {
            double[] results = new double[4];

            try
            {
                HObject ho_Regions1, ho_Line;
                HTuple hv_ResultRow, hv_ResultColumn;//negative  positive
                rake(image, out ho_Regions1, elem, height, 3, 1, thred, "positive", "max", x1,
             y1, x2, y2, out hv_ResultRow, out hv_ResultColumn);
                HTuple hv_Rowf1, hv_Colf1, hv_Rowf2, hv_Colf2;
                pts_to_best_line(out ho_Line, hv_ResultRow, hv_ResultColumn, actnum, out hv_Rowf1,
                    out hv_Colf1, out hv_Rowf2, out hv_Colf2);


                results[0] = hv_Rowf1.D;
                results[1] = hv_Colf1.D;
                results[2] = hv_Rowf2.D;
                results[3] = hv_Colf2.D;
                return results;
            }
            catch { return results; }
        }
        #endregion


        private bool savetocsv(string strFilePath, string[] value)
        {
            try
            {
                var filewriter = new StreamWriter(strFilePath, true, Encoding.Default);
                filewriter.WriteLine(string.Join(",", value));
                filewriter.Flush();
                filewriter.Close();
                return true;
            }
            catch (Exception ex)
            {
                MsgTextBox.Text = AddMessage(ex.Message);
                return false;
            }
        }

        private string GetRecordFileName()
        {
            string rs = "";
            switch (EE6Mode)
            {
                case 0:
                    rs += "ee6";
                    break;
                case 1:
                    rs += "dp8";
                    break;
                case 2:
                    rs += "lc6";
                    break;
                case 3:
                    rs += "kz4";
                    break;
                case 4:
                    rs += "kz3";
                    break;
                case 5:
                    rs += "lm8";
                    break;
                case 6:
                    rs += "lm9";
                    break;
                default:
                    break;
            }
            if (DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 20)
            {
                rs += DateTime.Now.ToString("yyyyMMdd") + "Day";
            }
            else
            {
                if (DateTime.Now.Hour >= 0 && DateTime.Now.Hour < 8)
                {
                    rs += DateTime.Now.AddDays(-1).ToString("yyyyMMdd") + "Night";
                }
                else
                {
                    rs += DateTime.Now.ToString("yyyyMMdd") + "Night";
                }
            }
            return rs;
        }
        private bool FindLines()
        {
            double line1r1, line1c1, line1r2, line1c2;
            double line2r1, line2c1, line2r2, line2c2;

            double shuxian_x, shuxian_y;
            //RolConvert(CoorPar.ShuXiam.row, CoorPar.ShuXiam.column, CoorPar.MoBan.row, CoorPar.MoBan.column, AngleCheck.D, out shuxian_x, out shuxian_y);
            //shuxian_x += RowCheck.D - CoorPar.MoBan.row;
            //shuxian_y += ColumnCheck.D - CoorPar.MoBan.column;
            shuxian_x = CoorPar.ShuXiam.row;
            shuxian_y = CoorPar.ShuXiam.column;
            //HObject Rec1;
            //HOperatorSet.GenRectangle2(out Rec1, shuxian_x, shuxian_y, CoorPar.ShuXiam.phi + AngleCheck.D, CoorPar.ShuXiam.length1, CoorPar.ShuXiam.length2);
            //HRegion Rec1Region = new HRegion(Rec1);
            RolConvert(shuxian_x + 90, shuxian_y, CoorPar.MoBan.row, CoorPar.MoBan.column, AngleCheck.D, out line1r1, out line1c1);
            RolConvert(shuxian_x - 90, shuxian_y, CoorPar.MoBan.row, CoorPar.MoBan.column, AngleCheck.D, out line1r2, out line1c2);
            line1r1 += RowCheck.D - CoorPar.MoBan.row;
            line1c1 += ColumnCheck.D - CoorPar.MoBan.column;
            line1r2 += RowCheck.D - CoorPar.MoBan.row;
            line1c2 += ColumnCheck.D - CoorPar.MoBan.column;
            //line1r1 = shuxian_x + 90;
            //line1c1 = shuxian_y;
            //line1r2 = shuxian_x - 90;
            //line1c2 = shuxian_y;


            double hengxian_x, hengxian_y;
            //RolConvert(CoorPar.HengXiam.row, CoorPar.HengXiam.column, CoorPar.MoBan.row, CoorPar.MoBan.column, AngleCheck.D, out hengxian_x, out hengxian_y);
            //hengxian_x += RowCheck.D - CoorPar.MoBan.row;
            //hengxian_y += ColumnCheck.D - CoorPar.MoBan.column;
            hengxian_x = CoorPar.HengXiam.row;
            hengxian_y = CoorPar.HengXiam.column;
            //HObject Rec2;
            //HOperatorSet.GenRectangle2(out Rec2, hengxian_x, hengxian_y, CoorPar.HengXiam.phi + AngleCheck.D, CoorPar.HengXiam.length1, CoorPar.HengXiam.length2);
            //HRegion Rec2Region = new HRegion(Rec2);
            RolConvert(hengxian_x, hengxian_y - 200, CoorPar.MoBan.row, CoorPar.MoBan.column, AngleCheck.D, out line2r1, out line2c1);
            RolConvert(hengxian_x, hengxian_y + 200, CoorPar.MoBan.row, CoorPar.MoBan.column, AngleCheck.D, out line2r2, out line2c2);
            //line2r1 = hengxian_x;
            //line2c1 = hengxian_y - 200;
            //line2r2 = hengxian_x;
            //line2c2 = hengxian_y + 200;

            line2r1 += RowCheck.D - CoorPar.MoBan.row;
            line2c1 += ColumnCheck.D - CoorPar.MoBan.column;
            line2r2 += RowCheck.D - CoorPar.MoBan.row;
            line2c2 += ColumnCheck.D - CoorPar.MoBan.column;

            if (AngleCheck.DArr[0] > Math.PI)
            {
                TextAngle1.Text = ((AngleCheck.DArr[0] - 2 * Math.PI) / Math.PI * 180).ToString("F2");
            }
            else
            {
                TextAngle1.Text = (AngleCheck.DArr[0] / Math.PI * 180).ToString("F2");
            }

            Window.SetColor("blue");
            Window.SetDraw("fill");
            Window.DispCross(line1r1, line1c1, 40, 0);
            Window.DispCross(line1r2, line1c2, 40, 0);
            Window.SetColor("yellow");
            Window.SetDraw("fill");
            Window.DispCross(line2r1, line2c1, 40, 0);
            Window.DispCross(line2r2, line2c2, 40, 0);

            var line1 = findLine(line1r1, line1c1, line1r2, line1c2, CoorPar.ShuXianThreshold, CoorPar.ShuXianPixNum, 100, 24);
            var line2 = findLine(line2r1, line2c1, line2r2, line2c2, CoorPar.HengXianThreshold, CoorPar.HengXianPixNum, 150, 50);
            HOperatorSet.AngleLx(line2[0], line2[1], line2[2], line2[3], out AngleCheck);



            HTuple fpx, fpy, fpn;
            try
            {
                HOperatorSet.IntersectionLines(line1[0], line1[1], line1[2], line1[3], line2[0], line2[1], line2[2], line2[3], out fpx, out fpy, out fpn);
                CrossPoint = new double[2];
                CrossPoint[1] = fpx.D;
                CrossPoint[0] = fpy.D;
            }
            catch { return false; }


            //HImage ImgReduced = image.ReduceDomain(Rec1Region);
            //HObject EdgeAmplitude, EdgeDirection;
            //HOperatorSet.SobelDir(ImgReduced, out EdgeAmplitude, out EdgeDirection, "sum_abs", 3);
            //HObject region1;
            //HOperatorSet.Threshold(EdgeAmplitude, out region1, CoorPar.ShuXianThreshold, 255);
            //region1.DispObj(Window);
            //HTuple angle, dist;
            //HOperatorSet.HoughLines(region1, 8, CoorPar.ShuXianPixNum, 30, 30, out angle, out dist);
            //HObject LinesHNF;
            //if (dist.Length > 0)
            //{
            //    HOperatorSet.GenRegionHline(out LinesHNF, angle, dist);
            //    LinesHNF.DispObj(Window);
            //    Line1Angle = angle.DArr[0];
            //    Line1Dist = dist.DArr[0];
            //}
            //else
            //{
            //    return false;
            //}

            //ImgReduced = image.ReduceDomain(Rec2Region);
            //HOperatorSet.SobelDir(ImgReduced, out EdgeAmplitude, out EdgeDirection, "sum_abs", 3);
            //HOperatorSet.Threshold(EdgeAmplitude, out region1, CoorPar.HengXianThreshold, 255);
            //region1.DispObj(Window);
            //HOperatorSet.HoughLines(region1, 8, CoorPar.HengXianPixNum, 30, 30, out angle, out dist);
            //HObject LinesHNF1;

            //if (dist.Length > 0)
            //{
            //    HOperatorSet.GenRegionHline(out LinesHNF1, angle, dist);

            //    LinesHNF1.DispObj(Window);
            //    Line2Angle = angle.DArr[0];
            //    Line2Dist = dist.DArr[0];
            //}
            //else
            //{
            //    return false;
            //}
            // CrossPoint = GetCrostPoint(Line1Angle, Line1Dist, Line2Angle, Line2Dist);
            //MsgTextBox.Text = AddMessage(CrossPoint[0].ToString() + ","+ CrossPoint[1].ToString());
            Window.SetColor("green");
            Window.SetDraw("fill");
            Window.DispCross(CrossPoint[1], CrossPoint[0], 40, 0);//  xy反的
            Window.DispLine(line1[0], line1[1], line1[2], line1[3]);
            Window.DispLine(line2[0], line2[1], line2[2], line2[3]);
            return true;
        }
        private void CalcButton_Click(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            if (CoorPar.DRow1 != CoorPar.DRow2)
            {

                CoorPar.Calc();
                MsgTextBox.Text = AddMessage("像素计算完成: " + CoorPar.DisT.ToString());
            }
            else
            {
                MsgTextBox.Text = AddMessage("点1、点2重合，无法计算");
            }
        }
        struct DWORDStruct
        {
            public String RigisterName;
            public int Value;
        }
        //private async void WriteCoorData()
        //{
        //    FileStream stream;
        //    WriteCoor.IsEnabled = false;
        //    System.Windows.Forms.OpenFileDialog ofdialog = new System.Windows.Forms.OpenFileDialog();
        //    ofdialog.InitialDirectory = "D:\\";
        //    ofdialog.Filter = "Microsoft Excel 2013|*.xlsx";
        //    ofdialog.RestoreDirectory = true;
        //    if (ofdialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //    {

        //        try
        //        {
        //            stream = new FileStream(ofdialog.FileName, FileMode.Open);

        //        }
        //        catch (IOException ex)
        //        {
        //            MsgTextBox.Text = AddMessage(ex.Message);
        //            WriteCoor.IsEnabled = true;
        //            return;
        //        }
        //        using (stream)
        //        {
        //            ExcelPackage package = new ExcelPackage(stream);
        //            ExcelWorksheet sheet = package.Workbook.Worksheets[1];
        //            if (sheet == null)
        //            {
        //                MsgTextBox.Text = AddMessage("Excel format error!");
        //                WriteCoor.IsEnabled = true;
        //                return;
        //            }
        //            if (!sheet.Cells[1, 1].Value.Equals("NAME"))
        //            {
        //                MsgTextBox.Text = AddMessage("Excel format error!");
        //                WriteCoor.IsEnabled = true;
        //                return;
        //            }
        //            int lastRow = sheet.Dimension.End.Row;
        //            for (int i = 2; i < lastRow; i++)
        //            {
        //                if (sheet.Cells[i, 1].Value != null && sheet.Cells[i, 2].Value != null)
        //                {
        //                    await Task.Delay(10);
        //                    lock (modbustcp)
        //                    {
        //                        aS300ModbusTCP.WriteDWORD(sheet.Cells[i, 1].Value.ToString(), int.Parse(sheet.Cells[i, 2].Value.ToString()));
        //                    }
        //                }
        //            }
        //            MsgTextBox.Text = AddMessage("写入坐标数据完成");
        //            WriteCoor.IsEnabled = true;
        //        }
        //    }
        //    else
        //    {
        //        WriteCoor.IsEnabled = true;
        //        return;
        //    }

        //}
        //private async void ReadCoorData()
        //{
        //    ReadCoor.IsEnabled = false;
        //    List<DWORDStruct> DD20000 = new List<DWORDStruct>();


        //    System.Windows.Forms.SaveFileDialog sfdialog = new System.Windows.Forms.SaveFileDialog();
        //    sfdialog.Filter = "Microsoft Excel 2013|*.xlsx";
        //    sfdialog.DefaultExt = "xlsx";
        //    sfdialog.AddExtension = true;
        //    sfdialog.Title = "Save Excel";
        //    sfdialog.InitialDirectory = "D:\\";
        //    sfdialog.FileName = DateTime.Now.ToString("yyyyMMdd") + DateTime.Now.ToString("HHmmss");
        //    DialogResult? result = sfdialog.ShowDialog();
        //    if (result == null || result.Value != System.Windows.Forms.DialogResult.OK)
        //    {
        //        ReadCoor.IsEnabled = true;
        //        return;

        //    }
        //    else
        //    {
        //        for (int i = 0; i < 200; i++)
        //        {
        //            await Task.Delay(10);
        //            DWORDStruct dw = new DWORDStruct();
        //            dw.RigisterName = "D" + (20000 + 2 * i).ToString();
        //            lock (modbustcp)
        //            {
        //                dw.Value = aS300ModbusTCP.ReadDWORD(dw.RigisterName);
        //            }
        //            DD20000.Add(dw);
        //        }
        //        FileStream stream;
        //        try
        //        {
        //            stream = new FileStream(sfdialog.FileName, FileMode.Create);
        //        }
        //        catch (IOException ex)
        //        {

        //            ReadCoor.IsEnabled = true;
        //            MsgTextBox.Text = AddMessage(ex.Message);
        //            return;
        //        }
        //        using (stream)
        //        {
        //            ExcelPackage package = new ExcelPackage(stream);
        //            package.Workbook.Worksheets.Add("PLC坐标数据");
        //            ExcelWorksheet sheet = package.Workbook.Worksheets[1];
        //            sheet.Cells[1, 1].Value = "NAME";
        //            sheet.Cells[1, 2].Value = "VALUE";
        //            using (ExcelRange range = sheet.Cells[1, 1, 1, 2])
        //            {
        //                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Gray);
        //                range.AutoFitColumns(4);
        //            }
        //            int pos = 2;
        //            foreach (DWORDStruct item in DD20000)
        //            {
        //                sheet.Cells[pos, 1].Value = item.RigisterName;
        //                sheet.Cells[pos, 2].Value = item.Value;
        //                pos++;
        //            }
        //            package.Save();

        //            ReadCoor.IsEnabled = true;
        //            MsgTextBox.Text = AddMessage("导出坐标数据完成");
        //        }

        //    }


        //}
        private async void PLCScanAction(string str)
        {

            string[] strs = str.Split('\r');
            MsgTextBox.Text = AddMessage(strs[0]);
            BarcodeString.Text = strs[0];
            string NewStr = strs[0];
            string FindStr = "";
            if (NewStr != "Error")
            {
                Func<Task> startTask = () =>
                {
                    return Task.Run(
                        () =>
                        {
                            FindStr = mySQLClass.FindResult(NewStr);
                        }
                        );
                };
                Task taskDelay = Task.Delay(5000);
                var completeTask = await Task.WhenAny(startTask(), taskDelay);
                if (completeTask == taskDelay)
                {
                    MsgTextBox.Text = AddMessage("查询数据超时,请检查网线,并重起电脑.");
                }
                else
                {
                    MsgTextBox.Text = AddMessage(FindStr);
                    switch (EE6Mode)
                    {
                        case 0:
                            if (FindStr.Length == 36)
                            {
                                bool[] Rb = new bool[36];
                                for (int i = 0; i < 6; i++)
                                {
                                    if (FindStr[0 + 6 * i] == '1')
                                    {
                                        Rb[0 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[0 + i] = false;
                                    }
                                    if (FindStr[1 + 6 * i] == '1')
                                    {
                                        Rb[6 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[6 + i] = false;
                                    }
                                    if (FindStr[2 + 6 * i] == '1')
                                    {
                                        Rb[12 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[12 + i] = false;
                                    }
                                    if (FindStr[3 + 6 * i] == '1')
                                    {
                                        Rb[35 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[35 - i] = false;
                                    }
                                    if (FindStr[4 + 6 * i] == '1')
                                    {
                                        Rb[29 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[29 - i] = false;
                                    }
                                    if (FindStr[5 + 6 * i] == '1')
                                    {
                                        Rb[23 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[23 - i] = false;
                                    }
                                }
                                bool[] _AllowBarRecord;
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteMultCoils("M5103", Rb);
                                    _AllowBarRecord = aS300ModbusTCP.ReadCoils("M6000", 5);
                                    //aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                }
                                if (UpdateRecode(NewStr, _AllowBarRecord[4]))
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                    }


                                }
                                else
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                    }
                                }
                            }
                            else
                            {
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                }
                            }
                            break;
                        case 1:
                            if (FindStr.Length == 24)
                            {
                                bool[] Rb = new bool[24];
                                for (int i = 0; i < 6; i++)
                                {
                                    if (FindStr[0 + 4 * i] == '1')
                                    {
                                        Rb[0 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[0 + i] = false;
                                    }
                                    if (FindStr[1 + 4 * i] == '1')
                                    {
                                        Rb[6 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[6 + i] = false;
                                    }

                                    if (FindStr[2 + 4 * i] == '1')
                                    {
                                        Rb[23 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[23 - i] = false;
                                    }
                                    if (FindStr[3 + 4 * i] == '1')
                                    {
                                        Rb[17 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[17 - i] = false;
                                    }

                                }
                                bool[] _AllowBarRecord;
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteMultCoils("M5103", Rb);
                                    _AllowBarRecord = aS300ModbusTCP.ReadCoils("M6000", 5);
                                    //aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                }
                                if (UpdateRecode(NewStr, _AllowBarRecord[4]))
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                    }

                                }
                                else
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                    }
                                }
                            }
                            else
                            {
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                }
                            }
                            break;
                        case 2:
                            if (FindStr.Length == 30)
                            {
                                bool[] Rb = new bool[30];
                                for (int i = 0; i < 5; i++)
                                {
                                    if (FindStr[0 + 6 * i] == '1')
                                    {
                                        Rb[0 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[0 + i] = false;
                                    }
                                    if (FindStr[1 + 6 * i] == '1')
                                    {
                                        Rb[5 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[5 + i] = false;
                                    }

                                    if (FindStr[2 + 6 * i] == '1')
                                    {
                                        Rb[10 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[10 + i] = false;
                                    }
                                    if (FindStr[3 + 6 * i] == '1')
                                    {
                                        Rb[29 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[29 - i] = false;
                                    }
                                    if (FindStr[4 + 6 * i] == '1')
                                    {
                                        Rb[24 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[24 - i] = false;
                                    }
                                    if (FindStr[5 + 6 * i] == '1')
                                    {
                                        Rb[19 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[19 - i] = false;
                                    }

                                }
                                bool[] _AllowBarRecord;
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteMultCoils("M5103", Rb);
                                    _AllowBarRecord = aS300ModbusTCP.ReadCoils("M6000", 5);
                                    //aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                }
                                if (UpdateRecode(NewStr, _AllowBarRecord[4]))
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                    }

                                }
                                else
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                    }
                                }
                            }
                            else
                            {
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                }
                            }
                            break;
                        case 3:
                            if (FindStr.Length == 24)
                            {
                                bool[] Rb = new bool[24];
                                for (int i = 0; i < 3; i++)
                                {
                                    if (FindStr[0 + 8 * i] == '1')
                                    {
                                        Rb[0 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[0 + i] = false;
                                    }
                                    if (FindStr[1 + 8 * i] == '1')
                                    {
                                        Rb[3 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[3 + i] = false;
                                    }

                                    if (FindStr[2 + 8 * i] == '1')
                                    {
                                        Rb[6 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[6 + i] = false;
                                    }
                                    if (FindStr[3 + 8 * i] == '1')
                                    {
                                        Rb[9 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[9 + i] = false;
                                    }
                                    if (FindStr[4 + 8 * i] == '1')
                                    {
                                        Rb[23 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[23 - i] = false;
                                    }
                                    if (FindStr[5 + 8 * i] == '1')
                                    {
                                        Rb[20 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[20 - i] = false;
                                    }
                                    if (FindStr[6 + 8 * i] == '1')
                                    {
                                        Rb[17 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[17 - i] = false;
                                    }
                                    if (FindStr[7 + 8 * i] == '1')
                                    {
                                        Rb[14 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[14 - i] = false;
                                    }

                                }
                                bool[] _AllowBarRecord;
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteMultCoils("M5103", Rb);
                                    _AllowBarRecord = aS300ModbusTCP.ReadCoils("M6000", 5);
                                    //aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                }
                                if (UpdateRecode(NewStr, _AllowBarRecord[4]))
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                    }

                                }
                                else
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                    }
                                }
                            }
                            else
                            {
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                }
                            }
                            break;
                        case 4:
                            if (FindStr.Length == 20)
                            {
                                bool[] Rb = new bool[20];
                                for (int i = 0; i < 5; i++)//列数
                                {
                                    if (FindStr[0 + 4 * i] == '1')//圈内个数
                                    {
                                        Rb[0 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[0 + i] = false;
                                    }
                                    if (FindStr[1 + 4 * i] == '1')
                                    {
                                        Rb[5 + i] = true;//列数
                                    }
                                    else
                                    {
                                        Rb[5 + i] = false;
                                    }

                                    if (FindStr[2 + 4 * i] == '1')
                                    {
                                        Rb[19 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[19 - i] = false;
                                    }
                                    if (FindStr[3 + 4 * i] == '1')
                                    {
                                        Rb[14 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[14 - i] = false;
                                    }

                                }
                                bool[] _AllowBarRecord;
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteMultCoils("M5103", Rb);
                                    _AllowBarRecord = aS300ModbusTCP.ReadCoils("M6000", 5);
                                    //aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                }
                                if (UpdateRecode(NewStr, _AllowBarRecord[4]))
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                    }

                                }
                                else
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                    }
                                }
                            }
                            else
                            {
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                }
                            }
                            break;
                        case 5:
                            if (FindStr.Length == 20)
                            {
                                bool[] Rb = new bool[20];
                                for (int i = 0; i < 5; i++)//列数
                                {
                                    if (FindStr[0 + 4 * i] == '1')//圈内个数
                                    {
                                        Rb[0 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[0 + i] = false;
                                    }
                                    if (FindStr[1 + 4 * i] == '1')
                                    {
                                        Rb[5 + i] = true;//列数
                                    }
                                    else
                                    {
                                        Rb[5 + i] = false;
                                    }

                                    if (FindStr[2 + 4 * i] == '1')
                                    {
                                        Rb[19 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[19 - i] = false;
                                    }
                                    if (FindStr[3 + 4 * i] == '1')
                                    {
                                        Rb[14 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[14 - i] = false;
                                    }

                                }
                                bool[] _AllowBarRecord;
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteMultCoils("M5103", Rb);
                                    _AllowBarRecord = aS300ModbusTCP.ReadCoils("M6000", 5);
                                    //aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                }
                                if (UpdateRecode(NewStr, _AllowBarRecord[4]))
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                    }

                                }
                                else
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                    }
                                }
                            }
                            else
                            {
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                }
                            }
                            break;
                        case 6:
                            if (FindStr.Length == 36)
                            {
                                bool[] Rb = new bool[36];
                                for (int i = 0; i < 6; i++)
                                {
                                    if (FindStr[0 + 6 * i] == '1')
                                    {
                                        Rb[0 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[0 + i] = false;
                                    }
                                    if (FindStr[1 + 6 * i] == '1')
                                    {
                                        Rb[6 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[6 + i] = false;
                                    }
                                    if (FindStr[2 + 6 * i] == '1')
                                    {
                                        Rb[12 + i] = true;
                                    }
                                    else
                                    {
                                        Rb[12 + i] = false;
                                    }
                                    if (FindStr[3 + 6 * i] == '1')
                                    {
                                        Rb[35 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[35 - i] = false;
                                    }
                                    if (FindStr[4 + 6 * i] == '1')
                                    {
                                        Rb[29 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[29 - i] = false;
                                    }
                                    if (FindStr[5 + 6 * i] == '1')
                                    {
                                        Rb[23 - i] = true;
                                    }
                                    else
                                    {
                                        Rb[23 - i] = false;
                                    }
                                }
                                bool[] _AllowBarRecord;
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteMultCoils("M5103", Rb);
                                    _AllowBarRecord = aS300ModbusTCP.ReadCoils("M6000", 5);
                                    //aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                }
                                if (UpdateRecode(NewStr, _AllowBarRecord[4]))
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5100", true);
                                    }


                                }
                                else
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                    }
                                }
                            }
                            else
                            {
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteSigleCoil("M5101", true);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }


            }
            else
            {
                scanFailTimes++;
                Inifile.INIWriteValue(maintainIniFilename, "maintain", "scanFailTimes", scanFailTimes.ToString());
                TextScanFailTimes.Text = scanFailTimes.ToString();
                lock (modbustcp)
                {
                    aS300ModbusTCP.WriteSigleCoil("M5101", true);
                }
            }
        }
        private bool UpdateLoaclTime()
        {
            bool r;
            try
            {
                OraDB oraDB = new OraDB("fpcsfcdb", "sfcdar", "sfcdardata");
                if (oraDB.isConnect())
                {
                    string dbtime = oraDB.sfc_getServerDateTime();
                    MsgTextBox.Text = AddMessage("数据库时间:" + dbtime);
                    DateTimeUtility.SYSTEMTIME st = new DateTimeUtility.SYSTEMTIME();
                    DateTime dt = Convert.ToDateTime(dbtime);
                    st.FromDateTime(dt);
                    DateTimeUtility.SetLocalTime(ref st);
                    r = true;
                }
                else
                {
                    MsgTextBox.Text = AddMessage("数据库连接失败");
                    r = false;
                }
                oraDB.disconnect();
            }
            catch (Exception ex)
            {
                MsgTextBox.Text = AddMessage(ex.Message);
                r = false;
            }
            return r;
        }
        private bool CheckGUMBAR(string str)
        {
            try
            {
                OraDB oraDB = new OraDB("fpcsfcdb", "sfcdar", "sfcdardata");
                string tablename1 = "sfcdata.zx_pnl_data";
                string[] arrField = new string[1];
                string[] arrValue = new string[1];
                if (oraDB.isConnect())
                {
                    arrField[0] = "GUMBAR";
                    arrValue[0] = str;
                    DataSet s1 = oraDB.selectSQL(tablename1.ToUpper(), arrField, arrValue);
                    DataTable PanelDt = s1.Tables[0];
                    if (PanelDt.Rows.Count == 0)
                    {
                        return true;
                    }
                    else
                    {
                        MsgTextBox.Text = AddMessage("背胶码重复");
                        return false;
                    }
                }
                else
                {
                    MsgTextBox.Text = AddMessage("数据库未连接");
                    return false;
                }
            }
            catch (Exception ex)
            {
                MsgTextBox.Text = AddMessage(ex.Message);
                return false;
            }
        }
        private bool UpdateRecode(string barcode, bool mode)
        {
            string filename = "D:\\record\\" + GetRecordFileName() + ".csv";
            string[] arrField = new string[1];
            string[] arrValue = new string[1];
            try
            {
                OraDB oraDB = new OraDB("fpcsfcdb", "sfcdar", "sfcdardata");
                //string tablename = "sfcdata.barautbind";
                string tablename1 = "sfcdata.zx_pnl_data";
                if (oraDB.isConnect())
                {
                    arrField[0] = "SCBARCODE";
                    arrValue[0] = barcode;
                    DataSet s1 = oraDB.selectSQL(tablename1.ToUpper(), arrField, arrValue);
                    DataTable PanelDt = s1.Tables[0];
                    if (PanelDt.Rows.Count == 0)
                    {
                        MsgTextBox.Text = AddMessage(barcode + "插入新数据");
                        string[] arrField1 = { "BLDATE", "BLID", "BLNAME", "BLUID", "BLMID", "SCBARCODE", "GUMBAR" };
                        string[] arrValue1 = { "to_date('" + DateTime.Now.ToString() + "', 'yyyy/mm/dd hh24:mi:ss')", CoorPar.ZhiJuBianHao, CoorPar.ZhiJuMingChen, CoorPar.ZheXianRenYuan, CoorPar.JiTaiBianHao, barcode, CoorPar.JuanLiaoXinXi };
                        oraDB.insertSQL2(tablename1.ToUpper(), arrField1, arrValue1);
                        oraDB.disconnect();
                        if (!File.Exists(filename))
                        {
                            string[] heads = { "BLDATE", "BLID", "BLNAME", "BLUID", "BLMID", "SCBARCODE", "GUMBAR" };
                            savetocsv(filename, heads);
                        }
                        string[] counts = { DateTime.Now.ToString(), CoorPar.ZhiJuBianHao, CoorPar.ZhiJuMingChen, CoorPar.ZheXianRenYuan, CoorPar.JiTaiBianHao, barcode, CoorPar.JuanLiaoXinXi };
                        savetocsv(filename, counts);
                        return true;
                    }
                    else
                    {
                        if (mode)
                        {
                            MsgTextBox.Text = AddMessage(barcode + "插入新数据");
                            string[] arrField1 = { "BLDATE", "BLID", "BLNAME", "BLUID", "BLMID", "SCBARCODE", "GUMBAR" };
                            string[] arrValue1 = { "to_date('" + DateTime.Now.ToString() + "', 'yyyy/mm/dd hh24:mi:ss')", CoorPar.ZhiJuBianHao, CoorPar.ZhiJuMingChen, CoorPar.ZheXianRenYuan, CoorPar.JiTaiBianHao, barcode, CoorPar.JuanLiaoXinXi };
                            oraDB.insertSQL2(tablename1.ToUpper(), arrField1, arrValue1);
                            oraDB.disconnect();
                            if (!File.Exists(filename))
                            {
                                string[] heads = { "BLDATE", "BLID", "BLNAME", "BLUID", "BLMID", "SCBARCODE", "GUMBAR" };
                                savetocsv(filename, heads);
                            }
                            string[] counts = { DateTime.Now.ToString(), CoorPar.ZhiJuBianHao, CoorPar.ZhiJuMingChen, CoorPar.ZheXianRenYuan, CoorPar.JiTaiBianHao, barcode, CoorPar.JuanLiaoXinXi };
                            savetocsv(filename, counts);
                            return true;
                        }
                        else
                        {
                            MsgTextBox.Text = AddMessage(barcode + " 条码重复");
                            oraDB.disconnect();
                            return false;
                        }
                        //if (PanelDt.Rows[0]["BLID"].ToString() == "" || mode)
                        //{
                        //    string[,] arrFieldAndNewValue = { { "BLDATE", "to_date('" + DateTime.Now.ToString() + "', 'yyyy/mm/dd hh24:mi:ss')" }, { "BLID", CoorPar.ZhiJuBianHao }, { "BLNAME", CoorPar.ZhiJuMingChen }, { "BLUID", CoorPar.ZheXianRenYuan }, { "BLMID", CoorPar.JiTaiBianHao } };

                        //    string[,] arrFieldAndOldValue = { { "SCBARCODE", barcode } };
                        //    oraDB.updateSQL2(tablename1.ToUpper(), arrFieldAndNewValue, arrFieldAndOldValue);

                        //    if (!File.Exists(filename))
                        //    {
                        //        string[] heads = { "BLDATE", "BLID", "BLNAME", "BLUID", "BLMID", "SCBARCODE" };
                        //        savetocsv(filename, heads);
                        //    }
                        //    string[] counts = { DateTime.Now.ToString(), CoorPar.ZhiJuBianHao, CoorPar.ZhiJuMingChen, CoorPar.ZheXianRenYuan, CoorPar.JiTaiBianHao, barcode };
                        //    savetocsv(filename, counts);
                            
                        //    MsgTextBox.Text = AddMessage(barcode + "数据更新完成");
                        //    oraDB.disconnect();
                        //    return true;

                        //}
                        //else
                        //{
                        //    MsgTextBox.Text = AddMessage(barcode + " 条码重复");
                        //    oraDB.disconnect();
                        //    return false;
                        //}
                    }



                }
                else
                {
                    MsgTextBox.Text = AddMessage("数据库连接失败");
                    oraDB.disconnect();
                    return false;

                }
            }
            catch (Exception ex)
            {
                MsgTextBox.Text = AddMessage(ex.Message);
                return false;
            }


        }
       
        private void WriteAlarm(string str)
        {
            string filename = "D:\\alarm\\" + "alarm" + GetRecordFileName() + ".csv";
            if (!File.Exists(filename))
            {
                string[] heads = { "BLDATE", "BLID", "BLNAME", "BLUID", "BLMID", "ALARM" };
                savetocsv(filename, heads);
            }
            string[] counts = { DateTime.Now.ToString(), CoorPar.ZhiJuBianHao, CoorPar.ZhiJuMingChen, CoorPar.ZheXianRenYuan, CoorPar.JiTaiBianHao, str };
            savetocsv(filename, counts);
        }
        private async void PLCRun()
        {
            bool ScanCMD = false, GigECMD = false;
            bool[] PlcAlarm;
            bool m4005 = false, m4006 = false, m4007 = false, m4008 = false, m4009 = false, m4010 = false, m4013 = false, m4014 = false, m4022 = false, m4023 = false, m4030 = false, m4031 = false, m4032 = false, m4033 = false, m4034 = false, m4035 = false, m4044 = false, m4050 = false, m4051 = false, m4052 = false, m4053 = false;
            bool m5010 = false, m5011 = false, m5012 = false;

            while (true)
            {
                await Task.Delay(100);
                try
                {
                    lock (modbustcp)
                    {
                        TextJuanLiaoCount.Text = aS300ModbusTCP.ReadRegisters("D20042", 1)[0].ToString();
                        PLC_In = aS300ModbusTCP.ReadCoils("M5000", 96);
                        CX = aS300ModbusTCP.ReadDWORD("D6");
                        TextX1.Text = ((double)CX / 100).ToString();
                        CY = aS300ModbusTCP.ReadDWORD("D10");
                        TextY1.Text = ((double)CY / 100).ToString();
                        PlcAlarm = aS300ModbusTCP.ReadCoils("M4000", 64);
                    }
                    if (ScanCMD != PLC_In[0])
                    {
                        ScanCMD = PLC_In[0];
                        if (ScanCMD)
                        {
                            ScanCMD = false;
                            lock (modbustcp)
                            {
                                aS300ModbusTCP.WriteSigleCoil("M5000", false);
                            }
                            Scan.GetBarCode(PLCScanAction);

                        }
                    }

                    if (GigECMD != PLC_In[2])
                    {
                        GigECMD = PLC_In[2];
                        if (GigECMD)
                        {
                            GigECMD = false;

                            //dispatcherTimer.Stop();
                            if (grapAction())
                            {
                                //hdev_export.SaveImage();
                                Action();
                                if (RowCheck.Length == 1)
                                {
                                    if (FindLines())
                                    {
                                        TextMaxX1.Text = CrossPoint[0].ToString("F2");
                                        TextMinX1.Text = CrossPoint[1].ToString("F2");
                                        int DD12, DD14, DD16;
                                        DD12 = (int)((CrossPoint[1] - CoorPar.Y0) * -1 / CoorPar.DisT);
                                        DD14 = (int)(((CrossPoint[0] - CoorPar.X0)) * -1 / CoorPar.DisT);
                                        //DD16 = Convert.ToInt32(((CoorPar.Line1Angle + CoorPar.Line2Angle - Line1Angle - Line2Angle) / 2 / Math.PI * 180) * 100);
                                        if (AngleCheck.D > Math.PI)
                                        {
                                            DD16 = Convert.ToInt32(((AngleCheck.D - 2 * Math.PI) / Math.PI * 180) * 100);

                                            //MsgTextBox.Text = AddMessage(( - Line1Angle + CoorPar.Line1Angle).ToString() + "," + (Line2Angle - CoorPar.Line2Angle).ToString() +"," + (AngleCheck.D - 2 * Math.PI).ToString());
                                        }
                                        else
                                        {
                                            DD16 = Convert.ToInt32((AngleCheck.D / Math.PI * 180) * 100);
                                            //MsgTextBox.Text = AddMessage(( - Line1Angle + CoorPar.Line1Angle).ToString() + "," + (Line2Angle - CoorPar.Line2Angle).ToString() + "," + AngleCheck.D .ToString());
                                        }
                                        if (DD12 > 1000 || DD12 < -1000 || DD14 > 1000 || DD14 < -1000 || DD16 > 2000 || DD16 < -2000)
                                        {
                                            lock (modbustcp)
                                            {
                                                aS300ModbusTCP.WriteDWORD("D12", 0);
                                                aS300ModbusTCP.WriteDWORD("D14", 0);
                                                aS300ModbusTCP.WriteDWORD("D16", 0);
                                                MsgTextBox.Text = AddMessage("数值异常");
                                                hdev_export.SaveImage();
                                                aS300ModbusTCP.WriteSigleCoil("M5143", true);
                                            }

                                        }
                                        else
                                        {
                                            lock (modbustcp)
                                            {
                                                aS300ModbusTCP.WriteDWORD("D12", DD12);
                                                aS300ModbusTCP.WriteDWORD("D14", DD14);
                                                aS300ModbusTCP.WriteDWORD("D16", DD16);


                                                MsgTextBox.Text = AddMessage("X:" + DD12.ToString() + ",Y:" + DD14.ToString() + ",U:" + DD16.ToString());
                                                aS300ModbusTCP.WriteSigleCoil("M5140", true);
                                            }

                                        }


                                    }
                                    else
                                    {

                                        lock (modbustcp)
                                        {
                                            aS300ModbusTCP.WriteDWORD("D12", 0);
                                            aS300ModbusTCP.WriteDWORD("D14", 0);
                                            aS300ModbusTCP.WriteDWORD("D16", 0);
                                            MsgTextBox.Text = AddMessage("直线未找到");
                                            hdev_export.SaveImage();
                                            aS300ModbusTCP.WriteSigleCoil("M5143", true);
                                        }
                                    }

                                }
                                else
                                {
                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteDWORD("D12", 0);
                                        aS300ModbusTCP.WriteDWORD("D14", 0);
                                        aS300ModbusTCP.WriteDWORD("D16", 0);
                                        MsgTextBox.Text = AddMessage("未找到模板");
                                        hdev_export.SaveImage();
                                        aS300ModbusTCP.WriteSigleCoil("M5143", true);
                                    }

                                }
                            }
                            else
                            {
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteDWORD("D12", 0);
                                    aS300ModbusTCP.WriteDWORD("D14", 0);
                                    aS300ModbusTCP.WriteDWORD("D16", 0);
                                    MsgTextBox.Text = AddMessage("拍照失败");
                                    hdev_export.SaveImage();
                                    aS300ModbusTCP.WriteSigleCoil("M5143", true);
                                }
                            }




                            //await Task.Delay(100);
                        }
                    }
                    if (m5010 != PLC_In[10])
                    {
                        string maintainCsvFilename = "D:\\maintain\\" + GetRecordFileName() + "maintain.csv";
                        m5010 = PLC_In[10];

                        if (m5010)
                        {
                            opendoorflag = true;


                            if (File.Exists(maintainCsvFilename))
                            {
                                string[] counts = { DateTime.Now.ToString(), CoorPar.ZhiJuBianHao, CoorPar.ZhiJuMingChen, CoorPar.ZheXianRenYuan, CoorPar.JiTaiBianHao, "OpenDoor" };
                                savetocsv(maintainCsvFilename, counts);
                            }

                        }
                        else
                        {
                            if (File.Exists(maintainCsvFilename))
                            {
                                string[] counts = { DateTime.Now.ToString(), CoorPar.ZhiJuBianHao, CoorPar.ZhiJuMingChen, CoorPar.ZheXianRenYuan, CoorPar.JiTaiBianHao, "CloseDoor" };
                                savetocsv(maintainCsvFilename, counts);
                            }

                        }
                    }
                    if (!m5010)
                    {
                        opendoorflag = false;
                    }
                    if (m5011 != PLC_In[11])
                    {
                        string maintainCsvFilename = "D:\\maintain\\" + GetRecordFileName() + "maintain.csv";
                        m5011 = PLC_In[11];

                        if (m5011)
                        {
                            leisureflag = true;
                            if (File.Exists(maintainCsvFilename))
                            {
                                string[] counts = { DateTime.Now.ToString(), CoorPar.ZhiJuBianHao, CoorPar.ZhiJuMingChen, CoorPar.ZheXianRenYuan, CoorPar.JiTaiBianHao, "WaitPcs" };
                                savetocsv(maintainCsvFilename, counts);
                            }

                        }
                        else
                        {
                            if (File.Exists(maintainCsvFilename))
                            {
                                string[] counts = { DateTime.Now.ToString(), CoorPar.ZhiJuBianHao, CoorPar.ZhiJuMingChen, CoorPar.ZheXianRenYuan, CoorPar.JiTaiBianHao, "StarWork" };
                                savetocsv(maintainCsvFilename, counts);
                            }

                        }
                    }
                    if (!m5011)
                    {
                        leisureflag = false;
                    }
                    if (m5012 != PLC_In[12])
                    {
                        m5012 = PLC_In[12];
                        if (m5012)
                        {
                            tumotimes++;
                            Inifile.INIWriteValue(maintainIniFilename, "maintain", "tumotimes", tumotimes.ToString());
                            TextTumoTimes.Text = tumotimes.ToString();
                        }
                    }
                    if (m4005 != PlcAlarm[5])
                    {
                        m4005 = PlcAlarm[5];
                        if (m4005)
                        {
                            WriteAlarm("急停");
                        }
                    }
                    if (m4006 != PlcAlarm[6])
                    {
                        m4006 = PlcAlarm[6];
                        if (m4006)
                        {
                            WriteAlarm("搬产品吸取失败");
                        }
                    }
                    if (m4007 != PlcAlarm[7])
                    {
                        m4007 = PlcAlarm[7];
                        if (m4007)
                        {
                            fanzhuanFailTimes++;
                            Inifile.INIWriteValue(maintainIniFilename, "maintain", "fanzhuanFailTimes", fanzhuanFailTimes.ToString());
                            TextFanZhuanFailTimes.Text = fanzhuanFailTimes.ToString();
                            WriteAlarm("翻转真空报警");
                        }
                    }
                    if (m4008 != PlcAlarm[8])
                    {
                        m4008 = PlcAlarm[8];
                        if (m4008)
                        {
                            WriteAlarm("背胶吸取失败");
                        }
                    }
                    if (m4009 != PlcAlarm[9])
                    {
                        m4009 = PlcAlarm[9];
                        if (m4009)
                        {
                            WriteAlarm("成品吸取失败");
                        }
                    }
                    if (m4010 != PlcAlarm[10])
                    {
                        m4010 = PlcAlarm[10];
                        if (m4010)
                        {
                            WriteAlarm("气压报警");
                        }
                    }
                    if (m4013 != PlcAlarm[13])
                    {
                        m4013 = PlcAlarm[13];
                        if (m4013)
                        {
                            WriteAlarm("拍照失败1");
                        }
                    }
                    if (m4014 != PlcAlarm[14])
                    {
                        m4014 = PlcAlarm[14];
                        if (m4014)
                        {
                            WriteAlarm("拍照失败2");
                        }
                    }
                    if (m4022 != PlcAlarm[22])
                    {
                        m4022 = PlcAlarm[22];
                        if (m4022)
                        {
                            fanzhuanFailTimes++;
                            Inifile.INIWriteValue(maintainIniFilename, "maintain", "fanzhuanFailTimes", fanzhuanFailTimes.ToString());
                            TextFanZhuanFailTimes.Text = fanzhuanFailTimes.ToString();
                            WriteAlarm("翻转真空报警2");
                        }
                    }
                    if (m4023 != PlcAlarm[23])
                    {
                        m4023 = PlcAlarm[23];
                        if (m4023)
                        {
                            WriteAlarm("未放料盘报警");
                        }
                    }
                    if (m4030 != PlcAlarm[30])
                    {
                        m4030 = PlcAlarm[30];
                        if (m4030)
                        {
                            WriteAlarm("剥胶气缸报警");
                        }
                    }
                    if (m4031 != PlcAlarm[31])
                    {
                        m4031 = PlcAlarm[31];
                        if (m4031)
                        {
                            WriteAlarm("搬运轴磁感异常");
                        }
                    }
                    if (m4032 != PlcAlarm[32])
                    {
                        m4032 = PlcAlarm[32];
                        if (m4032)
                        {
                            WriteAlarm("贴胶轴磁感异常");
                        }
                    }
                    if (m4033 != PlcAlarm[33])
                    {
                        m4033 = PlcAlarm[33];
                        if (m4033)
                        {
                            WriteAlarm("翻转气缸置位异常");
                        }
                    }
                    if (m4034 != PlcAlarm[34])
                    {
                        m4034 = PlcAlarm[34];
                        if (m4034)
                        {
                            WriteAlarm("翻转气缸复位异常");
                        }
                    }
                    if (m4035 != PlcAlarm[35])
                    {
                        m4035 = PlcAlarm[35];
                        if (m4035)
                        {
                            WriteAlarm("折线工位有产品");
                        }
                    }
                    if (m4044 != PlcAlarm[44])
                    {
                        m4044 = PlcAlarm[44];
                        if (m4044)
                        {
                            WriteAlarm("背胶超时报警");
                        }
                    }
                    if (m4050 != PlcAlarm[50])
                    {
                        m4050 = PlcAlarm[50];
                        if (m4050)
                        {
                            WriteAlarm("翻转-下压气缸-SET超时");
                        }
                    }
                    if (m4051 != PlcAlarm[51])
                    {
                        m4051 = PlcAlarm[51];
                        if (m4051)
                        {
                            WriteAlarm("翻转-下压气缸-RST超时");
                        }
                    }
                    if (m4052 != PlcAlarm[52])
                    {
                        m4052 = PlcAlarm[52];
                        if (m4052)
                        {
                            WriteAlarm("翻转-前推气缸-SET超时");
                        }
                    }
                    if (m4053 != PlcAlarm[53])
                    {
                        m4053 = PlcAlarm[53];
                        if (m4053)
                        {
                            WriteAlarm("翻转-前推气缸-RST超时");
                        }
                    }
                    //throw new Exception(PLC_In[0].ToString());
                }
                catch (Exception ex)
                {

                    MsgTextBox.Text = AddMessage(ex.Message);
                    if (ex.Message == "Request Failed")
                    {
                        PlcStatus = false;
                        //System.Threading.Thread.Sleep(1000);
                        await Task.Delay(1000);
                        try
                        {
                            aS300ModbusTCP.Init();
                            MsgTextBox.Text = AddMessage("PLC连接");

                            lock (modbustcp)
                            {
                                EE6Mode = aS300ModbusTCP.ReadRegisters("D20040", 1)[0];
                            }
                            switch (EE6Mode)
                            {
                                case 0:
                                    EE6Checkbox.IsChecked = true;
                                    //MsgTextBox.Text = AddMessage("EE6 程序");
                                    break;
                                case 1:
                                    DP8Checkbox.IsChecked = true;
                                    //MsgTextBox.Text = AddMessage("DP8 程序");
                                    break;
                                case 2:
                                    LC6Checkbox.IsChecked = true;

                                    break;
                                case 3:
                                    KZ4Checkbox.IsChecked = true;

                                    break;
                                case 4:
                                    KZ3Checkbox.IsChecked = true;

                                    break;
                                case 5:
                                    LM8Checkbox.IsChecked = true;

                                    break;
                                case 6:
                                    LM9Checkbox.IsChecked = true;

                                    break;
                                default:
                                    break;
                            }
                            PlcStatus = true;
                        }
                        catch (Exception ex1)
                        {

                            MsgTextBox.Text = AddMessage(ex1.Message);
                        }

                    }
                }
            }

        }

        //private void AdjustButton_Click(object sender, RoutedEventArgs e)
        //{
        //    dispatcherTimer.Start();
        //}
        private double[] GetCrostPoint(double Angle1, double Dist1, double Angle2, double Dist2)
        {
            double[] xy0 = new double[2];
            double a1, b1, a2, b2;
            a2 = Math.Tan(Angle2 - Math.PI / 2);
            b2 = Dist2 / Math.Sin(Angle2);
            if (Angle1 == 0)
            {
                xy0[0] = Dist1;
            }
            else
            {
                a1 = Math.Tan(Angle1 - Math.PI / 2);
                b1 = Dist1 / Math.Sin(Angle1);
                xy0[0] = (b2 - b1) / (a1 - a2);
            }
            xy0[1] = a2 * xy0[0] + b2;
            return xy0;
        }
        public void PrintBarcode(string str)
        {
            string[] strs = str.Split('\r');
            MsgTextBox.Text = AddMessage(strs[0]);
            BarcodeString.Text = strs[0];
        }
        void Init()
        {


            hdev_export.OpenCamera();
            MsgTextBox.Text = AddMessage("相机开启");
            FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryFormatter b = new BinaryFormatter();
            CoorPar = b.Deserialize(fileStream) as DataAxisCoor;
            fileStream.Close();

            //GethomMat2D();

            //CalcRolCenter();
            //GetNewhomMat2D();
            //  HImage img1 = new HImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
            //  Rectangle = new HRegion(CoorPar.RectangleRow1, CoorPar.RectangleColumn1, CoorPar.RectangleRow2, CoorPar.RectangleColumn2);

            //  Rectangle.AreaCenter(out Row, out Column);
            //  ImgReduced = img1.ReduceDomain(Rectangle);
            //  ImgReduced.InspectShapeModel(out ModelRegion, 1, 20);//Constract(20)可设置，类似于阀值，值月底黑色像素越明显
            //  ShapeModel = new HShapeModel(ImgReduced, 4, 0, new HTuple(360.0).TupleRad().D,
            //new HTuple(1.0).TupleRad().D, "none", "use_polarity", 20, 10);

            //  img1.Dispose();
            //  ImgReduced.Dispose();
            ShapeModel = new HShapeModel(System.Environment.CurrentDirectory + "\\ShapeModel.shm");
        }
        private void Action()
        {

            //ShapeModel.FindScaledShapeModel(image, 0,
            //        new HTuple(360).TupleRad().D, 0.5, 2,
            //        0.4, 1, 0.5, "least_squares",
            //        4, 0.9, out RowCheck, out ColumnCheck,
            //        out AngleCheck, out ScaleCheck, out Score);
            HObject Rec1;
            HOperatorSet.GenRectangle1(out Rec1, 500, 100, 1700, 1200);
            HRegion Region1 = new HRegion(Rec1);
            HImage imgreduced = image.ReduceDomain(Region1);
            ShapeModel.FindShapeModel(imgreduced, -0.5,
                    1.04, 0.5, 1,
                    0.5, new HTuple("least_squares"),
                    new HTuple(4).TupleConcat(1), 0.9, out RowCheck, out ColumnCheck,
                    out AngleCheck, out Score);
            Window.SetColor("green");
            Window.SetDraw("margin");
            HObject rec;
            HOperatorSet.GenRectangle1(out rec, 500, 100, 1700, 1200);
            rec.DispObj(Window);

            if (RowCheck.Length == 1)
            {
                Window.SetColor("green");
                Window.SetDraw("fill");
                Window.DispCross(RowCheck, ColumnCheck, 60, 0);
                TextRow1.Text = RowCheck.DArr[0].ToString("F2");
                TextColumn1.Text = ColumnCheck.DArr[0].ToString("F2");
                if (AngleCheck.DArr[0] > Math.PI)
                {
                    TextAngle1.Text = ((AngleCheck.DArr[0] - 2 * Math.PI) / Math.PI * 180).ToString("F2");
                }
                else
                {
                    TextAngle1.Text = (AngleCheck.DArr[0] / Math.PI * 180).ToString("F2");
                }

                TextScore1.Text = Score.DArr[0].ToString("F2");

            }
        }
        private void Action2()
        {

        }
        private void Calib2Button_Click(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            grapAction();
            Action();
            if (RowCheck.Length == 1)
            {
                CoorPar.Row2 = RowCheck.D;
                CoorPar.DRow2 = CX;
                MsgTextBox.Text = AddMessage("CX2: " + CoorPar.Row2.ToString() + "; " + CoorPar.DRow2.ToString());
            }
        }

        //private void ImgLive_Click(object sender, RoutedEventArgs e)
        //{

        //    if (iCImagingControl.DeviceValid)
        //    {
        //        ImgLive.IsEnabled = false;
        //        ImgStop.IsEnabled = true;
        //        iCImagingControl.LiveStart();
        //    }
        //}

        //private void ImgStop_Click(object sender, RoutedEventArgs e)
        //{
        //    if (iCImagingControl.DeviceValid)
        //    {
        //        iCImagingControl.LiveStop();
        //        ImgLive.IsEnabled = true;
        //        ImgStop.IsEnabled = false;
        //    }
        //}
        //private void ImgSnap()
        //{
        //    if (iCImagingControl.DeviceValid)
        //    {
        //        if (iCImagingControl.LiveVideoRunning)
        //        {
        //            iCImagingControl.LiveStop();
        //            ImgLive.IsEnabled = true;
        //            ImgStop.IsEnabled = false;
        //        }
        //        iCImagingControl.MemorySnapImage();
        //        if (ImgBitmap != null)
        //        {
        //            ImgBitmap.Dispose();
        //        }
        //        ImgBitmap = new Bitmap(iCImagingControl.ImageActiveBuffer.Bitmap);
        //    }
        //}

        //private void HWindowControlWPF2_HInitWindow(object sender, EventArgs e)
        //{
        //    Window2 = HWindowControlWPF2.HalconWindow;
        //    HWindowControlWPF2.HalconWindow.SetPart(0.0, 0.0, new HTuple(ImgBitmap.Height - 1), new HTuple(ImgBitmap.Width - 1));
        //    HWindowControlWPF2.HalconWindow.AttachBackgroundToWindow(new HImage(BitmaptoHImage(ImgBitmap)));
        //    Window2Init = true;

        //}
        private HObject BitmaptoHImage(Bitmap bmp)
        {
            HObject ho_Image;
            HOperatorSet.GenEmptyObj(out ho_Image);
            // Lock the bitmap's bits.    
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmp.PixelFormat);
            HOperatorSet.GenImageInterleaved(out ho_Image, bmpData.Scan0, "bgrx", bmp.Width, bmp.Height, -1, "byte", bmp.Width, bmp.Height, 0, 0, -1, 0);
            return ho_Image;
        }

        //private void USBCameraAction_Click(object sender, RoutedEventArgs e)
        //{
        //    ImgSnap();
        //    HWindowControlWPF2.HalconWindow.DispObj(new HImage(BitmaptoHImage(ImgBitmap)));
        //}
        /// <summary>
        /// 找竖线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawButton_Click1(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            HTuple draw_id;

            //hdev_export.GrapCamera();
            hdev_export.ReadImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
            background_image = hdev_export.ho_Image;
            hSmartWindowControlWPF1.HalconWindow.AttachBackgroundToWindow(new HImage(background_image));
            hdev_export.add_new_drawing_object("rectangle2", hSmartWindowControlWPF1.HalconID, out draw_id);
            SetCallbacks(draw_id, 1);

        }
        /// <summary>
        /// 创建直线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateButton_Click1(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            //if (ShuXian != null && HengXian != null)
            //{

            //}
            HTuple hv_ParamValues;
            if (ShuXian != null)
            {
                HOperatorSet.GetDrawingObjectParams(ShuXian, (new HTuple("row")).TupleConcat(new HTuple("column")
).TupleConcat(new HTuple("phi")).TupleConcat(new HTuple("length1")).TupleConcat(new HTuple("length2")), out hv_ParamValues);
                DataAxisCoor.MRectangle2 rec2 = new DataAxisCoor.MRectangle2();
                rec2.row = hv_ParamValues.DArr[0];
                rec2.column = hv_ParamValues.DArr[1];
                rec2.phi = hv_ParamValues.DArr[2];
                rec2.length1 = hv_ParamValues.DArr[3];
                rec2.length2 = hv_ParamValues.DArr[4];
                CoorPar.ShuXiam = rec2;
                FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Create);
                BinaryFormatter b = new BinaryFormatter();
                b.Serialize(fileStream, CoorPar);
                fileStream.Close();
            }
            else
            {
                MsgTextBox.Text = AddMessage("竖线区域不存在");
            }
            if (HengXian != null)
            {
                HOperatorSet.GetDrawingObjectParams(HengXian, (new HTuple("row")).TupleConcat(new HTuple("column")
).TupleConcat(new HTuple("phi")).TupleConcat(new HTuple("length1")).TupleConcat(new HTuple("length2")), out hv_ParamValues);
                DataAxisCoor.MRectangle2 rec2 = new DataAxisCoor.MRectangle2();
                rec2.row = hv_ParamValues.DArr[0];
                rec2.column = hv_ParamValues.DArr[1];
                rec2.phi = hv_ParamValues.DArr[2];
                rec2.length1 = hv_ParamValues.DArr[3];
                rec2.length2 = hv_ParamValues.DArr[4];
                CoorPar.HengXiam = rec2;
                FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Create);
                BinaryFormatter b = new BinaryFormatter();
                b.Serialize(fileStream, CoorPar);
                fileStream.Close();
            }
            else
            {
                MsgTextBox.Text = AddMessage("横线区域不存在");
            }
            grapAction1();
            Action();
            if (RowCheck.Length == 1)
            {
                DataAxisCoor.MRectangle2 rec2 = new DataAxisCoor.MRectangle2();
                rec2.row = RowCheck.DArr[0];
                rec2.column = ColumnCheck.DArr[0];
                rec2.phi = AngleCheck.DArr[0];
                rec2.length1 = 0;
                rec2.length2 = 0;
                CoorPar.MoBan = rec2;

                FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Create);
                BinaryFormatter b = new BinaryFormatter();
                b.Serialize(fileStream, CoorPar);
                fileStream.Close();
            }
            else
            {
                MsgTextBox.Text = AddMessage("模板匹配失败");
            }


        }
        public void RolConvert(double x, double y, double rx0, double ry0, double a, out double x0, out double y0)
        {
            //http://jingyan.baidu.com/article/2c8c281dfbf3dd0009252a7b.html
            x0 = (x - rx0) * Math.Cos(a) - (y - ry0) * Math.Sin(a) + rx0;
            y0 = (x - rx0) * Math.Sin(a) + (y - ry0) * Math.Cos(a) + ry0;
        }

        private void ReadCoor_Click(object sender, RoutedEventArgs e)
        {
            //ReadCoorData();
        }

        private void FunctionTest_Click(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            grapAction1();
            Action();
            if (RowCheck.Length > 0)
            {
                if (FindLines())
                {
                    CoorPar.Line1Angle = Line1Angle;
                    CoorPar.Line1Dist = Line1Dist;
                    CoorPar.Line2Angle = Line2Angle;
                    CoorPar.Line2Dist = Line2Dist;
                    CoorPar.X0 = CrossPoint[0];
                    CoorPar.Y0 = CrossPoint[1];
                    MsgTextBox.Text = AddMessage("直线保存成功");
                }
                else
                {
                    MsgTextBox.Text = AddMessage("找直线失败");
                }
            }
            else
            {
                MsgTextBox.Text = AddMessage("未找到模板");
            }

        }

        private void Com_DropDownClosed(object sender, EventArgs e)
        {
            LoadinCount = 0;
            CoorPar.ScanCom = Com.Text;
        }

        private void FunctionButton_Click(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            Scan.GetBarCode(PrintBarcode);
        }

        private void ImageCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            ImageFiles = Directory.GetFiles(@"D:\image");
            ImageIndex = 0;
            if (ImageFiles.Length == 0)
            {
                ImageCheckBox.IsChecked = false;
                MsgTextBox.Text = AddMessage("无本地图片");
            }
        }

        private void MuBanContrast_TextChanged(object sender, TextChangedEventArgs e)
        {
            
            try
            {
                CoorPar.MuBanContrast = new HTuple(double.Parse(MuBanContrast.Text));
            }
            catch 
            {

                
            }
            
        }

        private void ShuXianThreshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                CoorPar.ShuXianThreshold = new HTuple(double.Parse(ShuXianThreshold.Text));
            }
            catch 
            {

              
            }
            
        }

        private void ShuXianPixNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                CoorPar.ShuXianPixNum = new HTuple(double.Parse(ShuXianPixNum.Text));
            }
            catch 
            {

                
            }
            
        }

        private void HengXianThreshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                CoorPar.HengXianThreshold = new HTuple(double.Parse(HengXianThreshold.Text));
            }
            catch
            {

               
            }
            
        }

        private void HengXianPixNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                CoorPar.HengXianPixNum = new HTuple(double.Parse(HengXianPixNum.Text));
            }
            catch
            {

                
            }
            
        }

        private void ImageIndexNumAction(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            if ((bool)ImageCheckBox.IsChecked)
            {
                ImageIndex--;
                if (ImageIndex < 0)
                {
                    ImageIndex = ImageFiles.Length - 1;
                }
                ImageIndexNum.Text = ImageIndex.ToString();
            }
        }

        private async void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            //ActionMessages.ExecuteAction("winclose");
            mydialog.changeaccent("Red");
            var r = await mydialog.showconfirm("确定要关闭程序吗？");
            if (r)
            {
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                mydialog.changeaccent("Cobalt");
            }
        }

        private async void LoadinButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> r;
            if (!Loadin)
            {
                r = await mydialog.showlogin();
                if (r[1] == "543337")
                {
                    Loadin = true;
                    LoadinButton.Content = "登出";
                    ChuangJianTabItem.IsEnabled = true;
                    BiaoDingTabItem.IsEnabled = true;
                    SaoMaTabItem.IsEnabled = true;
                    QiTaTabItem.IsEnabled = true;
                    ZhiJuBianHao.IsReadOnly = false;
                    ZhiJuMingChen.IsReadOnly = false;
                    ZheXianRenYuan.IsReadOnly = false;
                    JiTaiBianHao.IsReadOnly = false;
                    MsgTextBox.Text = AddMessage("管理员登录");
                    LoadinCount = 0;
                    dispatcherTimer.Start();
                }
                //if (r[1] == "123456")
                //{
                //    Loadin = true;
                //    LoadinButton.Content = "登出";
                //    //ZhiJuBianHao.IsReadOnly = false;
                //    //ZhiJuMingChen.IsReadOnly = false;
                //    ZheXianRenYuan.IsReadOnly = false;
                //    //JiTaiBianHao.IsReadOnly = false;
                //    MsgTextBox.Text = AddMessage("作业员登录");
                //}

            }
            else
            {
                LoadoutAction();
                dispatcherTimer.Stop();
            }
        }
        private void LoadoutAction()
        {

            Loadin = false;
            LoadinButton.Content = "登录";
            ChuangJianTabItem.IsEnabled = false;
            BiaoDingTabItem.IsEnabled = false;
            SaoMaTabItem.IsEnabled = false;
            QiTaTabItem.IsEnabled = false;
            ZhiJuBianHao.IsReadOnly = true;
            ZhiJuMingChen.IsReadOnly = true;
            //ZheXianRenYuan.IsReadOnly = true;
            JiTaiBianHao.IsReadOnly = true;
            FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Create);
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(fileStream, CoorPar);
            fileStream.Close();

        }
        private void ZhiJuBianHao_LostFocus(object sender, RoutedEventArgs e)
        {
            CoorPar.ZhiJuBianHao = ZhiJuBianHao.Text;
        }

        private void ZhiJuMingChen_LostFocus(object sender, RoutedEventArgs e)
        {
            CoorPar.ZhiJuMingChen = ZhiJuMingChen.Text;
        }

        private void JiTaiBianHao_LostFocus(object sender, RoutedEventArgs e)
        {
            CoorPar.JiTaiBianHao = JiTaiBianHao.Text;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //UpdateRecode("EE6-WLB-HB0015-0105", false);
            //string r = webServiceQHD.ws.wsFun("test1","test1","FQAPDP8D2A002", "field2|field3|field4|field5|field10",JiTaiBianHao.Text +"|" + ZheXianRenYuan.Text + "|" + ZhiJuMingChen.Text + "|"+ZhiJuBianHao.Text + "|" + "DP8-WLB-HC0121-0187", System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
            //MsgTextBox.Text = AddMessage(r);
            UpdateRecode("DP8-WLB-HC0121-0107", true);
        }

        private void EE6Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            if (PlcStatus)
            {

                lock (modbustcp)
                {
                    aS300ModbusTCP.WriteSigleRegister("D20040", 0);
                }
                DP8Checkbox.IsChecked = false;
                LC6Checkbox.IsChecked = false;
                KZ4Checkbox.IsChecked = false;
                KZ3Checkbox.IsChecked = false;
                LM8Checkbox.IsChecked = false;
                LM9Checkbox.IsChecked = false;
                EE6Mode = 0;
                MsgTextBox.Text = AddMessage("EE6 程序");
            }
            else
            {
                DP8Checkbox.IsChecked = false;
                EE6Checkbox.IsChecked = false;
                LC6Checkbox.IsChecked = false;
                KZ4Checkbox.IsChecked = false;
                KZ3Checkbox.IsChecked = false;
                LM8Checkbox.IsChecked = false;
                LM9Checkbox.IsChecked = false;
            }


        }

        private void DP8Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            if (PlcStatus)
            {

                lock (modbustcp)
                {
                    aS300ModbusTCP.WriteSigleRegister("D20040", 1);
                }
                EE6Checkbox.IsChecked = false;
                LC6Checkbox.IsChecked = false;
                KZ4Checkbox.IsChecked = false;
                KZ3Checkbox.IsChecked = false;
                LM8Checkbox.IsChecked = false;
                LM9Checkbox.IsChecked = false;
                EE6Mode = 1;
                MsgTextBox.Text = AddMessage("DP8 程序");
            }
            else
            {
                DP8Checkbox.IsChecked = false;
                EE6Checkbox.IsChecked = false;
                LC6Checkbox.IsChecked = false;
                KZ4Checkbox.IsChecked = false;
                KZ3Checkbox.IsChecked = false;
                LM8Checkbox.IsChecked = false;
                LM9Checkbox.IsChecked = false;
            }

        }
        private void LC6Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            if (PlcStatus)
            {

                lock (modbustcp)
                {
                    aS300ModbusTCP.WriteSigleRegister("D20040", 2);
                }
                DP8Checkbox.IsChecked = false;
                EE6Checkbox.IsChecked = false;
                KZ4Checkbox.IsChecked = false;
                KZ3Checkbox.IsChecked = false;
                LM8Checkbox.IsChecked = false;
                LM9Checkbox.IsChecked = false;
                EE6Mode = 2;
                MsgTextBox.Text = AddMessage("LC6 程序");
            }
            else
            {
                DP8Checkbox.IsChecked = false;
                EE6Checkbox.IsChecked = false;
                LC6Checkbox.IsChecked = false;
                KZ4Checkbox.IsChecked = false;
                KZ3Checkbox.IsChecked = false;
                LM8Checkbox.IsChecked = false;
                LM9Checkbox.IsChecked = false;
            }

        }
        private void KZ4Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            if (PlcStatus)
            {

                lock (modbustcp)
                {
                    aS300ModbusTCP.WriteSigleRegister("D20040", 3);
                }
                DP8Checkbox.IsChecked = false;
                EE6Checkbox.IsChecked = false;
                LC6Checkbox.IsChecked = false;
                KZ3Checkbox.IsChecked = false;
                LM8Checkbox.IsChecked = false;
                LM9Checkbox.IsChecked = false;
                EE6Mode = 3;
                MsgTextBox.Text = AddMessage("KZ4 程序");
            }
            else
            {
                DP8Checkbox.IsChecked = false;
                EE6Checkbox.IsChecked = false;
                LC6Checkbox.IsChecked = false;
                KZ4Checkbox.IsChecked = false;
                KZ3Checkbox.IsChecked = false;
                LM8Checkbox.IsChecked = false;
                LM9Checkbox.IsChecked = false;
            }

        }
        private void KZ3Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            if (PlcStatus)
            {

                lock (modbustcp)
                {
                    aS300ModbusTCP.WriteSigleRegister("D20040", 4);
                }
                DP8Checkbox.IsChecked = false;
                EE6Checkbox.IsChecked = false;
                LC6Checkbox.IsChecked = false;
                KZ4Checkbox.IsChecked = false;
                LM8Checkbox.IsChecked = false;
                LM9Checkbox.IsChecked = false;
                EE6Mode = 4;
                MsgTextBox.Text = AddMessage("KZ3 程序");
            }
            else
            {
                DP8Checkbox.IsChecked = false;
                EE6Checkbox.IsChecked = false;
                LC6Checkbox.IsChecked = false;
                KZ4Checkbox.IsChecked = false;
                KZ3Checkbox.IsChecked = false;
                LM8Checkbox.IsChecked = false;
                LM9Checkbox.IsChecked = false;
            }

        }
        private void LM8Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            if (PlcStatus)
            {

                lock (modbustcp)
                {
                    aS300ModbusTCP.WriteSigleRegister("D20040", 5);
                }
                DP8Checkbox.IsChecked = false;
                EE6Checkbox.IsChecked = false;
                LC6Checkbox.IsChecked = false;
                KZ4Checkbox.IsChecked = false;
                LM9Checkbox.IsChecked = false;
                KZ3Checkbox.IsChecked = false;
                EE6Mode = 5;
                MsgTextBox.Text = AddMessage("LM8 程序");
            }
            else
            {
                DP8Checkbox.IsChecked = false;
                EE6Checkbox.IsChecked = false;
                LC6Checkbox.IsChecked = false;
                KZ4Checkbox.IsChecked = false;
                KZ3Checkbox.IsChecked = false;
                LM8Checkbox.IsChecked = false;
                LM9Checkbox.IsChecked = false;
            }

        }

        private void 卷料信息_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                MsgTextBox.Text = AddMessage(卷料信息.Text);
                if (CheckGUMBAR(卷料信息.Text))
                {
                    CoorPar.JuanLiaoXinXi = 卷料信息.Text;
                    string[] ss = 卷料信息.Text.Split(new char[] { '/' });
                    try
                    {
                        
                        lock (modbustcp)
                        {
                            aS300ModbusTCP.WriteSigleRegister("D20042", (short)(int.Parse(ss[3]) + 200));
                        }

                        MsgTextBox.Text = AddMessage("更换卷料：" + ss[3]);

                        
                    }
                    catch(Exception ex)
                    {
                        MsgTextBox.Text = AddMessage(ex.Message);
                    }                    
                } 
            }
        }

        private void ZheXianRenYuan_TextChanged(object sender, TextChangedEventArgs e)
        {
            CoorPar.ZheXianRenYuan = ZheXianRenYuan.Text;
        }

        private void LM9Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            if (PlcStatus)
            {

                lock (modbustcp)
                {
                    aS300ModbusTCP.WriteSigleRegister("D20040", 6);
                }
                DP8Checkbox.IsChecked = false;
                EE6Checkbox.IsChecked = false;
                LC6Checkbox.IsChecked = false;
                KZ4Checkbox.IsChecked = false;
                LM8Checkbox.IsChecked = false;
                KZ3Checkbox.IsChecked = false;
                EE6Mode = 6;
                MsgTextBox.Text = AddMessage("LM9 程序");
            }
            else
            {
                DP8Checkbox.IsChecked = false;
                EE6Checkbox.IsChecked = false;
                LC6Checkbox.IsChecked = false;
                KZ4Checkbox.IsChecked = false;
                KZ3Checkbox.IsChecked = false;
                LM8Checkbox.IsChecked = false;
                LM9Checkbox.IsChecked = false;
            }

        }
        private void FunctionButton1_Click(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            if (BarcodeString.Text != "Error" && BarcodeString.Text.Length > 5)
            {
                MsgTextBox.Text = AddMessage(mySQLClass.FindResult(BarcodeString.Text));
            }
        }

        private void WriteCoor_Click(object sender, RoutedEventArgs e)
        {
            //WriteCoorData();
        }

        private void FindModelButton_Click(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            if ((bool)ImageCheckBox.IsChecked)
            {
                grapAction2();
            }
            else
            {
                grapAction();
            }

            Action();
        }

        private void FindLineButton_Click(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            if ((bool)ImageCheckBox.IsChecked)
            {
                grapAction2();
            }
            else
            {
                grapAction();
            }
            Action();
            if (RowCheck.Length == 1)
            {
                if (FindLines())
                {
                    TextMaxX1.Text = CrossPoint[0].ToString("F2");
                    TextMinX1.Text = CrossPoint[1].ToString("F2");
                }

                //if (Score.D >= 0.7)
                //{


                //    FindLines();
                //    MsgTextBox.Text = AddMessage("查找模板完成");
                //}
                //else
                //{
                //    MsgTextBox.Text = AddMessage("模板质量低");
                //}

            }
            else
            {
                MsgTextBox.Text = AddMessage("未找到模板");
            }










        }

        /// <summary>
        /// 找横线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawButton_Click2(object sender, RoutedEventArgs e)
        {
            LoadinCount = 0;
            HTuple draw_id;

            //hdev_export.GrapCamera();
            hdev_export.ReadImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
            background_image = hdev_export.ho_Image;
            hSmartWindowControlWPF1.HalconWindow.AttachBackgroundToWindow(new HImage(background_image));
            hdev_export.add_new_drawing_object("rectangle2", hSmartWindowControlWPF1.HalconID, out draw_id);
            SetCallbacks(draw_id, 2);
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {

            Init();
            Com.Text = CoorPar.ScanCom;
            LoadinButton.Content = "登录";
            ChuangJianTabItem.IsEnabled = false;
            BiaoDingTabItem.IsEnabled = false;
            SaoMaTabItem.IsEnabled = false;
            QiTaTabItem.IsEnabled = false;
            ZhiJuBianHao.IsReadOnly = true;
            ZhiJuMingChen.IsReadOnly = true;
            ZheXianRenYuan.IsReadOnly = false;
            JiTaiBianHao.IsReadOnly = true;
            ZhiJuBianHao.Text = CoorPar.ZhiJuBianHao;
            ZhiJuMingChen.Text = CoorPar.ZhiJuMingChen;
            ZheXianRenYuan.Text = CoorPar.ZheXianRenYuan;
            JiTaiBianHao.Text = CoorPar.JiTaiBianHao;
            卷料信息.Text = CoorPar.JuanLiaoXinXi;
            if (CoorPar.MuBanContrast != null)
            {
                MuBanContrast.Text = CoorPar.MuBanContrast.D.ToString();
            }
            if (CoorPar.ShuXianPixNum != null)
            {
                ShuXianPixNum.Text = CoorPar.ShuXianPixNum.D.ToString();
            }
            if (CoorPar.ShuXianThreshold != null)
            {
                ShuXianThreshold.Text = CoorPar.ShuXianThreshold.D.ToString();
            }
            if (CoorPar.HengXianPixNum != null)
            {
                HengXianPixNum.Text = CoorPar.HengXianPixNum.D.ToString();
            }
            if (CoorPar.HengXianThreshold != null)
            {
                HengXianThreshold.Text = CoorPar.HengXianThreshold.D.ToString();
            }
            Scan.ini(CoorPar.ScanCom);
            Scan.Connect();

            MsgTextBox.Text = AddMessage("扫码枪连接");
            //hdev_export.GrapCamera();
            hdev_export.ReadImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
            image = new HImage(hdev_export.ho_Image);
            hSmartWindowControlWPF1.HalconWindow.DispObj(image);
            BaoYangHour = Inifile.INIGetStringValue(Paramaterini, "BaoYang", "BaoYangHour", "0");
            dispatcherTimer.Tick += new EventHandler(DispatcherTimerAction);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer1.Tick += new EventHandler(DispatcherTimer1Action);
            dispatcherTimer1.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer1.Start();
            if (!Directory.Exists(@"D:\image"))
            {
                Directory.CreateDirectory(@"D:\image");

            }
            if (!Directory.Exists(@"D:\record"))
            {
                Directory.CreateDirectory(@"D:\record");

            }
            if (!Directory.Exists(@"D:\alarm"))
            {
                Directory.CreateDirectory(@"D:\alarm");

            }
            if (!Directory.Exists(@"D:\maintain"))
            {
                Directory.CreateDirectory(@"D:\maintain");

            }
            if (!File.Exists("D:\\maintain.csv"))
            {
                string[] heads = { "BLDATE", "BLID", "BLNAME", "BLUID", "BLMID", "OpenDoor", "Leisure", "TumoTimes", "FanZhuanFailTimes", "ScanFailTimes", "PNLCount", "PCSCount" };
                savetocsv("D:\\maintain.csv", heads);
            }
            aS300ModbusTCP = new AS300ModbusTCP();
            try
            {
                aS300ModbusTCP.Init();
                MsgTextBox.Text = AddMessage("PLC连接成功");
                PlcStatus = true;
                lock (modbustcp)
                {
                    EE6Mode = aS300ModbusTCP.ReadRegisters("D20040", 1)[0];
                }
                switch (EE6Mode)
                {
                    case 0:
                        EE6Checkbox.IsChecked = true;
                        //MsgTextBox.Text = AddMessage("EE6 程序");
                        break;
                    case 1:
                        DP8Checkbox.IsChecked = true;
                        //MsgTextBox.Text = AddMessage("DP8 程序");
                        break;
                    case 2:
                        LC6Checkbox.IsChecked = true;
                        
                        break;
                    case 3:
                        KZ4Checkbox.IsChecked = true;
                        
                        break;
                    case 4:
                        KZ3Checkbox.IsChecked = true;
                        
                        break;
                    case 5:
                        LM8Checkbox.IsChecked = true;

                        break;
                    case 6:
                        LM9Checkbox.IsChecked = true;

                        break;
                    default:
                        break;
                }
          
            }
            catch (Exception ex)
            {
                PlcStatus = false;
                MsgTextBox.Text = AddMessage(ex.Message);
            }
            PLCRun();
            ImageFiles = Directory.GetFiles(@"D:\image");
            if (ImageFiles.Length >= 1000)
            {
                foreach (string item in ImageFiles)
                {
                    File.Delete(item);
                    MsgTextBox.Text = AddMessage("清理本地图片");
                }
            }
            if (UpdateLoaclTime())
            {
                MsgTextBox.Text = AddMessage("更新时间成功");
            }
            else
            {
                MsgTextBox.Text = AddMessage("更新时间失败");
            }
            opendoorsec = long.Parse(Inifile.INIGetStringValue(maintainIniFilename, "maintain", "opendoorsec", "0"));
            TextOpenDoor.Text = ((double)opendoorsec / 3600).ToString("F2");
            leisuresec = long.Parse(Inifile.INIGetStringValue(maintainIniFilename, "maintain", "leisuresec", "0"));
            TextLeisure.Text = ((double)leisuresec / 3600).ToString("F2");
            tumotimes = int.Parse(Inifile.INIGetStringValue(maintainIniFilename, "maintain", "tumotimes", "0"));            
            TextTumoTimes.Text = tumotimes.ToString();

            fanzhuanFailTimes = int.Parse(Inifile.INIGetStringValue(maintainIniFilename, "maintain", "fanzhuanFailTimes", "0"));
            TextFanZhuanFailTimes.Text = fanzhuanFailTimes.ToString();
            scanFailTimes = int.Parse(Inifile.INIGetStringValue(maintainIniFilename, "maintain", "scanFailTimes", "0"));
            TextScanFailTimes.Text = scanFailTimes.ToString();
            //datagrid.ItemsSource = (System.Collections.IEnumerable)mySQLClass.test().Tables[0];
            //var aaaa = mySQLClass.test().Tables[0];
            //try
            //{
            //    iCImagingControl.LoadDeviceStateFromFile("device.xml", true);
            //}
            //catch (Exception ex)
            //{

            //    MsgTextBox.Text = AddMessage(ex.Message);
            //}

            //if (!iCImagingControl.DeviceValid)
            //{
            //    iCImagingControl.ShowDeviceSettingsDialog();
            //}
            ////imageViewer.viewController.repaint();

            //if (iCImagingControl.DeviceValid)
            //{
            //    iCImagingControl.SaveDeviceStateToFile("device.xml");
            //    iCImagingControl.Size = new System.Drawing.Size(600, 400);
            //    iCImagingControl.LiveDisplayDefault = false;
            //    iCImagingControl.LiveDisplayHeight = iCImagingControl.Height;
            //    iCImagingControl.LiveDisplayWidth = iCImagingControl.Width;
            //    ImgSnap();
            //    //SmartWindowControlWPF2Init();
            //}
            //ImgStop.IsEnabled = false;
        }
        private void DispatcherTimerAction(Object sender, EventArgs e)
        {
            LoadinCount++;
            if (LoadinCount > 30)
            {
                TabControl1.SelectedIndex = 0;
                LoadoutAction();
                dispatcherTimer.Stop();
                MsgTextBox.Text = AddMessage("自动登出");
            }
        }
        private void DispatcherTimer1Action(Object sender, EventArgs e)
        {
            

            if (BaoYangHour != DateTime.Now.Hour.ToString())
            {
                if (DateTime.Now.Hour.ToString() == "7" || DateTime.Now.Hour.ToString() == "19")
                {
                    BaoYangHour = DateTime.Now.Hour.ToString();
                    Inifile.INIWriteValue(Paramaterini, "BaoYang", "BaoYangHour", BaoYangHour.ToString());
                    aS300ModbusTCP.WriteSigleCoil("M6008", true);
                    MsgTextBox.Text = AddMessage("发出保养命令");
                }
            }
            string maintainCsvFilename = "D:\\maintain\\" + GetRecordFileName() + "maintain.csv";

            if (!File.Exists(maintainCsvFilename))
            {
                string[] heads = { "BLDATE", "BLID", "BLNAME", "BLUID", "BLMID", "Action" };
                savetocsv(maintainCsvFilename, heads);
                int pnl_count, pcs_count;
                lock (modbustcp)
                {
                    pnl_count = aS300ModbusTCP.ReadDWORD("D20030");
                    pcs_count = aS300ModbusTCP.ReadDWORD("D20046");
                    aS300ModbusTCP.WriteDWORD("D20030", 0);
                    aS300ModbusTCP.WriteDWORD("D20046", 0);
                }
                string[] counts = { DateTime.Now.ToString(), CoorPar.ZhiJuBianHao, CoorPar.ZhiJuMingChen, CoorPar.ZheXianRenYuan, CoorPar.JiTaiBianHao, ((double)opendoorsec / 3600).ToString("F2"), ((double)leisuresec / 3600).ToString("F2"), tumotimes.ToString(), fanzhuanFailTimes.ToString(), scanFailTimes.ToString(), pnl_count.ToString(), pcs_count.ToString() };
                savetocsv("D:\\maintain.csv", counts);
                opendoorsec = 0;
                leisuresec = 0;
                TextOpenDoor.Text = ((double)opendoorsec / 3600).ToString("F2");
                Inifile.INIWriteValue(maintainIniFilename, "maintain", "opendoorsec", opendoorsec.ToString());
                TextLeisure.Text = ((double)leisuresec / 3600).ToString("F2");
                Inifile.INIWriteValue(maintainIniFilename, "maintain", "leisuresec", leisuresec.ToString());
                tumotimes = 0;
                Inifile.INIWriteValue(maintainIniFilename, "maintain", "tumotimes", tumotimes.ToString());
                TextTumoTimes.Text = tumotimes.ToString();
                fanzhuanFailTimes = 0;
                Inifile.INIWriteValue(maintainIniFilename, "maintain", "fanzhuanFailTimes", fanzhuanFailTimes.ToString());
                TextFanZhuanFailTimes.Text = fanzhuanFailTimes.ToString();
                scanFailTimes = 0;
                Inifile.INIWriteValue(maintainIniFilename, "maintain", "scanFailTimes", scanFailTimes.ToString());
                TextScanFailTimes.Text = scanFailTimes.ToString();
            }
            if (opendoorflag)
            {
                opendoorsec++;
                TextOpenDoor.Text = ((double)opendoorsec / 3600).ToString("F2");
                Inifile.INIWriteValue(maintainIniFilename, "maintain", "opendoorsec", opendoorsec.ToString());
            }
            if (leisureflag)
            {
                leisuresec++;
                TextLeisure.Text = ((double)leisuresec / 3600).ToString("F2");
                Inifile.INIWriteValue(maintainIniFilename, "maintain", "leisuresec", leisuresec.ToString());
            }

        }
        //private void GrapContinue(Object sender, EventArgs e)
        //{
        //    grapAction();
        //    Action();
        //}
        private void MsgTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            MsgTextBox.ScrollToEnd();
        }
        /// <summary>
        /// 打印窗口字符处理函数
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string AddMessage(string str)
        {
            string[] s = MessageStr.Split('\n');
            if (s.Length > 1000)
            {
                MessageStr = "";
            }
            //MessageStr += "\n" + System.DateTime.Now.ToString() + " " + str;
            MessageStr += "\n" + " " + str;
            return MessageStr;
        }
        protected void DisplayCallback(IntPtr draw_id, IntPtr window_handle, string type)
        {
            // On callback, process and display image
            lock (image_lock)
            {
                hdev_export.process_image(background_image, out ho_EdgeAmplitude, hSmartWindowControlWPF1.HalconID, draw_id);
            }
            // You need to switch to the UI thread to display the results
            Dispatcher.BeginInvoke(display_results_delegate);
        }
        private void hSmartWindowControlWPF1_HInitWindow(object sender, EventArgs e)
        {
            HTuple width, height;
            Window = hSmartWindowControlWPF1.HalconWindow;
            hdev_export.hv_ExpDefaultWinHandle = hSmartWindowControlWPF1.HalconID;
            //hdev_export.GrapCamera();
            hdev_export.ReadImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
            background_image = hdev_export.ho_Image;
            HOperatorSet.GetImageSize(background_image, out width, out height);
            hSmartWindowControlWPF1.HalconWindow.SetPart(0.0, 0.0, height - 1, width - 1);
            hSmartWindowControlWPF1.HalconWindow.AttachBackgroundToWindow(new HImage(background_image));
            display_results_delegate = new DisplayResultsDelegate(() =>
            {
                lock (image_lock)
                {
                    if (ho_EdgeAmplitude != null)
                        hdev_export.display_results(ho_EdgeAmplitude);
                }
            });
            cb = new HDrawingObject.HDrawingObjectCallback(DisplayCallback);
        }
    }
    [Serializable]
    public class DataAxisCoor
    {
        [Serializable]
        public class MRectangle2
        {
            public double row;
            public double column;
            public double phi;
            public double length1;
            public double length2;
        }
        public MRectangle2 ShuXiam;
        public MRectangle2 HengXiam;
        public MRectangle2 MoBan;
        public double Row1;
        public double Row2;
        public int DRow1;
        public int DRow2;
        public double DisT;
        public double Line1Angle;
        public double Line1Dist;
        public double Line2Angle;
        public double Line2Dist;
        public double X0;
        public double Y0;
        public string ScanCom;
        public HTuple ShuXianThreshold;
        public HTuple ShuXianPixNum;
        public HTuple HengXianThreshold;
        public HTuple HengXianPixNum;
        public HTuple MuBanContrast;
        public string ZhiJuBianHao;
        public string ZhiJuMingChen;
        public string ZheXianRenYuan;
        public string JiTaiBianHao;
        public string JuanLiaoXinXi;

        public void Calc()
        {
            DisT = (Row1 - Row2) / (DRow1 - DRow2);
        }
    }

}
