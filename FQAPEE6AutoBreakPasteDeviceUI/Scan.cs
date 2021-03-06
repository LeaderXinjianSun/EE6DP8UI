﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Diagnostics;

namespace FQAPEE6AutoBreakPasteDeviceUI
{
    public class Scan
    {
        public static event EventHandler StateChanged;
        public static bool mState;
        public static bool State
        {
            get { return mState; }
            set
            {
                if (mState != value)
                {
                    mState = value;
                    if (StateChanged != null)
                        StateChanged(null, null);
                }
            }
        }
        public static void ini(string Com)
        {
            mSerialPort = new SerialPort(Com, 115200, System.IO.Ports.Parity.Even, 8, System.IO.Ports.StopBits.One);
            mSerialPort.ReadTimeout = 1000;
            mSerialPort.WriteTimeout = 1000;
        }
        public static void Connect()
        {
            try
            {
                mSerialPort.Open();
                State = true;
            }
            catch (Exception ex) { Trace.WriteLine(ex.Message, "扫码连接"); }
        }

        public static bool DoBarcode = true;

        public static SerialPort mSerialPort;
        //public static bool DoBarcode=true;
        public static string BarCode;
        //static byte[] START_DECODE = new byte[] { 0x16, 0x54, 0x0D };//{0x03,0x53,0x80,0xFF,0x2A };
        static byte[] START_DECODE = new byte[] { 0x4C, 0x4F, 0x4E, 0x0D, 0x0A };//{0x03,0x53,0x80,0xFF,0x2A };
        //static byte[] STOP_DECODE = new byte[] { 0x16, 0x55, 0x0D };
        static byte[] STOP_DECODE = new byte[] { 0x4C, 0x4F, 0x46, 0x46, 0x0D, 0x0A};
        static byte[] MODE_DECODE = new byte[] { 0x16, 0x4D, 0x0D, 0x30, 0x34, 0x30, 0x31, 0x44, 0x30, 0x35, 0x2E };
        public delegate void ProcessDelegate(string barcode);
        public static async void GetBarCode(ProcessDelegate CallBack)
        {
            BarCode = "Error";
            Func<System.Threading.Tasks.Task> taskFunc = () =>
            {
                return System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        if (DoBarcode)
                        {
                            if (!mSerialPort.IsOpen)
                                Connect();
                            mSerialPort.ReadExisting();
                            mSerialPort.Write(START_DECODE, 0, START_DECODE.Length);
                            BarCode = mSerialPort.ReadLine();
                        }
                        State = true;
                    }
                    catch (Exception ex)
                    {
                        State = false;
                        Trace.WriteLine(ex.Message, "GetBarCode");
                    }
                    try
                    {
                        if (DoBarcode)
                        {
                            if (!mSerialPort.IsOpen)
                                Connect();
                            mSerialPort.ReadExisting();
                            mSerialPort.Write(STOP_DECODE, 0, STOP_DECODE.Length);
                        }
                        State = true;
                    }
                    catch (Exception ex)
                    {
                        State = false;
                        Trace.WriteLine(ex.Message, "StopBarCode");
                    }
                });
            };
            await taskFunc();
            CallBack(BarCode);
        }
        public static void SetMode()
        {
            try
            {
                if (DoBarcode)
                {
                    if (!mSerialPort.IsOpen)
                        Connect();
                    mSerialPort.ReadExisting();
                    mSerialPort.Write(MODE_DECODE, 0, MODE_DECODE.Length);
                }
                State = true;
            }
            catch (Exception ex)
            {
                State = false;
                Trace.WriteLine(ex.Message, "SetMode");
            }
        }
    }
}
