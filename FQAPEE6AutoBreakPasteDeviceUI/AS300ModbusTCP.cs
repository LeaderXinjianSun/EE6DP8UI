using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net; // for the ip address
using System.Runtime.InteropServices; // for the P/Invoke

namespace Leader.DeltaAS300ModbusTCP
{
    enum FunctionCode
    {
        ReadCoils,
        ReadDiscreteInputs,
        ReadHoldingRegisters,
        ReadHoldingRegisters32bit,
        ReadInputRegisters,
        WriteSingleCoil,
        WriteSingleRegister,
        WriteSingleRegister32bit,
        WriteMultipleCoils,
        WriteMultipleRegisters,
        WriteMultipleRegisters32bit
    }
    public class AS300ModbusTCP
    {
        System.IntPtr hDMTDll; // handle of a loaded DLL

        delegate void DelegateClose(int conn_num); // function pointer for disconnection

        // About .Net P/Invoke:

        // [DllImport("XXX.dll", CharSet = CharSet.Auto)] 
        // static extern int ABC(int a , int b);

        // indicates that "ABC" function is imported from XXX.dll
        // XXX.dll exports a function of the same name with "ABC"
        // the return type and the parameter's data type of "ABC" 
        // must be identical with the function exported from XXX.dll
        // and the CharSet = CharSet.Auto causes the CLR 
        // to use the appropriate character set based on the host OS   
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr LoadLibrary(string dllPath);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern bool FreeLibrary(IntPtr hDll);

        // Data Access
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int RequestData(int comm_type, int conn_num, int slave_addr, int func_code, byte[] sendbuf, int sendlen);
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int ResponseData(int comm_type, int conn_num, ref int slave_addr, ref int func_code, byte[] recvbuf);

        // Serial Communication
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int OpenModbusSerial(int conn_num, int baud_rate, int data_len, char parity, int stop_bits, int modbus_mode);
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern void CloseSerial(int conn_num);
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int GetLastSerialErr();
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern void ResetSerialErr();

        // Socket Communication
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int OpenModbusTCPSocket(int conn_num, int ipaddr);
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern void CloseSocket(int conn_num);
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int GetLastSocketErr();
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern void ResetSocketErr();
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int ReadSelect(int conn_num, int millisecs);

        // MODBUS Address Calculation
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int DevToAddrW(string series, string device, int qty);

        // Wrapped MODBUS Funcion : 0x01
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int ReadCoilsW(int comm_type, int conn_num, int slave_addr, int dev_addr, int qty, UInt32[] data_r, StringBuilder req, StringBuilder res);

        // Wrapped MODBUS Funcion : 0x02
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int ReadInputsW(int comm_type, int conn_num, int slave_addr, int dev_addr, int qty, UInt32[] data_r, StringBuilder req, StringBuilder res);

        // Wrapped MODBUS Funcion : 0x03
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int ReadHoldRegsW(int comm_type, int conn_num, int slave_addr, int dev_addr, int qty, UInt32[] data_r, StringBuilder req, StringBuilder res);
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int ReadHoldRegs32W(int comm_type, int conn_num, int slave_addr, int dev_addr, int qty, UInt32[] data_r, StringBuilder req, StringBuilder res);

        // Wrapped MODBUS Funcion : 0x04
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int ReadInputRegsW(int comm_type, int conn_num, int slave_addr, int dev_addr, int qty, UInt32[] data_r, StringBuilder req, StringBuilder res);

        // Wrapped MODBUS Funcion : 0x05		   
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int WriteSingleCoilW(int comm_type, int conn_num, int slave_addr, int dev_addr, UInt32 data_w, StringBuilder req, StringBuilder res);

        // Wrapped MODBUS Funcion : 0x06
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int WriteSingleRegW(int comm_type, int conn_num, int slave_addr, int dev_addr, UInt32 data_w, StringBuilder req, StringBuilder res);
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int WriteSingleReg32W(int comm_type, int conn_num, int slave_addr, int dev_addr, UInt32 data_w, StringBuilder req, StringBuilder res);

        // Wrapped MODBUS Funcion : 0x0F
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int WriteMultiCoilsW(int comm_type, int conn_num, int slave_addr, int dev_addr, int qty, UInt32[] data_w, StringBuilder req, StringBuilder res);

        // Wrapped MODBUS Funcion : 0x10
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int WriteMultiRegsW(int comm_type, int conn_num, int slave_addr, int dev_addr, int qty, UInt32[] data_w, StringBuilder req, StringBuilder res);
        [DllImport("DMT.dll", CharSet = CharSet.Auto)]
        static extern int WriteMultiRegs32W(int comm_type, int conn_num, int slave_addr, int dev_addr, int qty, UInt32[] data_w, StringBuilder req, StringBuilder res);


        public string IP { set; get; }
        private int ip;

        DelegateClose CloseModbus;

        int status = 0;
        int comm_type = 1; // 0:RS-232 , 1:Ethernet
        int conn_num = 0;

        UInt32[] data_from_dev = new UInt32[100];
        UInt32[] data_to_dev = new UInt32[100];

        //string strDev = "D0";
        //string strProduct = "AS";

        //int slave_addr = Convert.ToInt32("0");
        //int dev_qty = Convert.ToInt32("10");

        string strDev;
        string strProduct = "AS";

        int slave_addr = 0;
        int dev_qty = 1;

        public AS300ModbusTCP()
        {
            IP = "192.168.1.5";
            //Init();

        }
        public AS300ModbusTCP(string _ip)
        {
            IP = _ip;
            //Init();
        }
        public void Init()
        {
            string path = System.Environment.CurrentDirectory;

            path = path.Replace("\\", "\\\\");
            path = path.Insert(path.Length, "DMT.dll"); // obtain the relative path where the DMT.dll resides

            hDMTDll = LoadLibrary(path); // explicitly link to DMT.dll 
            CloseModbus = CloseSocket;
            IPAddress ipaddress = IPAddress.Parse(IP);
            ip = BitConverter.ToInt32(ipaddress.GetAddressBytes(), 0);
            status = OpenModbusTCPSocket(conn_num, ip);
            if (status == -1)
            {
                throw new Exception("Socket Connection Failed");
            }
        }
        public bool[] ReadCoils(string CoilName,int count)
        {
            strDev = CoilName;
            dev_qty = count;
            bool[] rb = new bool[count];
            ModbusPrecess(FunctionCode.ReadCoils);
            for (int i = 0; i < count; i++)
            {
                if (data_from_dev[i] != 0)
                {
                    rb[i] = true;
                }
                else
                {
                    rb[i] = false;
                }
            }
            return rb;
        }
        public int[] ReadRegisters(string RegisterName, int count)
        {
            strDev = RegisterName;
            dev_qty = count;
            int[] rd = new int[count];
            ModbusPrecess(FunctionCode.ReadHoldingRegisters);
            for (int i = 0; i < count; i++)
            {
                string hex = data_from_dev[i].ToString("X4");
                rd[i] = Convert.ToInt16(hex, 16);
            }
            return rd;
        }
        public int ReadDWORD(string RegisterName)
        {
            strDev = RegisterName;
            dev_qty = 2;
            ModbusPrecess(FunctionCode.ReadHoldingRegisters);
            string hex = data_from_dev[1].ToString("X4") + data_from_dev[0].ToString("X4");
            return Convert.ToInt32(hex, 16);
        }
        //public int[] ReadRegisters32(string RegisterName, int count)
        //{
        //    strDev = RegisterName;
        //    dev_qty = count;
        //    int[] rd = new int[count];
        //    ModbusPrecess(FunctionCode.ReadHoldingRegisters32bit);
        //    for (int i = 0; i < count; i++)
        //    {
        //        rd[i] = (int)data_from_dev[i];
        //    }
        //    return rd;
        //}
        public void WriteSigleCoil(string CoilName,bool v)
        {
            strDev = CoilName;
            data_to_dev[0] = (UInt32)(v ? 1 : 0);
            ModbusPrecess(FunctionCode.WriteSingleCoil);
        }
        public void WriteMultCoils(string CoilName, bool[] vs)
        {
            strDev = CoilName;
            dev_qty = vs.Length;
            for (int i = 0; i < vs.Length; i++)
            {
                data_to_dev[i] = (UInt32)(vs[i] ? 1 : 0);
            }
            ModbusPrecess(FunctionCode.WriteMultipleCoils);
        }
        public void WriteSigleRegister(string RegisterName, Int16 v)
        {
            strDev = RegisterName;
            string hex = v.ToString("X4");
            data_to_dev[0] = Convert.ToUInt16(hex, 16);
            ModbusPrecess(FunctionCode.WriteSingleRegister);
        }
        public void WriteDWORD(string RegisterName, int v)
        {
            string hex = v.ToString("X8");
            string hd1 = hex.Substring(0, 4);
            string hd2 = hex.Substring(4, 4);
            Int16[] vs = new Int16[2];
            vs[0] = Convert.ToInt16(hd2, 16);
            vs[1] = Convert.ToInt16(hd1, 16);
            WriteMultRegisters(RegisterName, vs);
        }
        //public void WriteSigleRegister32(string RegisterName, UInt32 v)
        //{
        //    strDev = RegisterName;
        //    data_to_dev[0] = v;
        //    ModbusPrecess(FunctionCode.WriteSingleRegister32bit);
        //}
        public void WriteMultRegisters(string RegisterName, Int16[] vs)
        {
            strDev = RegisterName;
            dev_qty = vs.Length;
            for (int i = 0; i < vs.Length; i++)
            {
                string hex = vs[i].ToString("X4");
                data_to_dev[i] = Convert.ToUInt16(hex, 16);
            }
            ModbusPrecess(FunctionCode.WriteMultipleRegisters);
        }
        //public void WriteMultRegisters32(string RegisterName, UInt32[] vs)
        //{
        //    strDev = RegisterName;
        //    dev_qty = vs.Length;
        //    for (int i = 0; i < vs.Length; i++)
        //    {
        //        data_to_dev[i] = vs[i];
        //    }
        //    ModbusPrecess(FunctionCode.WriteMultipleRegisters32bit);
        //}
        private void ModbusPrecess(FunctionCode funcode)
        {
            StringBuilder req = new StringBuilder(1024);
            StringBuilder res = new StringBuilder(1024);


            int addr = DevToAddrW(strProduct, strDev, dev_qty);
            if (addr == -1)
                throw new Exception("Invalid Product of Series");
            else if (addr == -2)
                throw new Exception("Invalid Device");
            else if (addr == -3)
                throw new Exception("Device With Such Quantity Is Out Of Valid Range");
            else
            {
                int ret = 0;
                switch (funcode)
                {
                    case FunctionCode.ReadCoils:
                        ret = ReadCoilsW(comm_type, conn_num, slave_addr, addr, dev_qty, data_from_dev, req, res);
                        break;
                    case FunctionCode.ReadDiscreteInputs:
                        ret = ReadInputsW(comm_type, conn_num, slave_addr, addr, dev_qty, data_from_dev, req, res);
                        break;
                    case FunctionCode.ReadHoldingRegisters:
                        ret = ReadHoldRegsW(comm_type, conn_num, slave_addr, addr, dev_qty, data_from_dev, req, res);
                        break;
                    case FunctionCode.ReadHoldingRegisters32bit:
                        ret = ReadHoldRegs32W(comm_type, conn_num, slave_addr, addr, dev_qty, data_from_dev, req, res);
                        break;
                    case FunctionCode.ReadInputRegisters:
                        ret = ReadInputRegsW(comm_type, conn_num, slave_addr, addr, dev_qty, data_from_dev, req, res);
                        break;
                    case FunctionCode.WriteSingleCoil:
                        ret = WriteSingleCoilW(comm_type, conn_num, slave_addr, addr, data_to_dev[0], req, res);
                        break;
                    case FunctionCode.WriteSingleRegister:
                        ret = WriteSingleRegW(comm_type, conn_num, slave_addr, addr, data_to_dev[0], req, res);
                        break;
                    case FunctionCode.WriteSingleRegister32bit:
                        ret = WriteSingleReg32W(comm_type, conn_num, slave_addr, addr, data_to_dev[0], req, res);
                        break;
                    case FunctionCode.WriteMultipleCoils:
                        ret = WriteMultiCoilsW(comm_type, conn_num, slave_addr, addr, dev_qty, data_to_dev, req, res);
                        break;
                    case FunctionCode.WriteMultipleRegisters:
                        ret = WriteMultiRegsW(comm_type, conn_num, slave_addr, addr, dev_qty, data_to_dev, req, res);
                        break;
                    case FunctionCode.WriteMultipleRegisters32bit:
                        ret = WriteMultiRegs32W(comm_type, conn_num, slave_addr, addr, dev_qty, data_to_dev, req, res);
                        break;
                    default:
                        break;
                }
                if (ret == -1)
                {
                    ResetSocketErr();
                    throw new Exception("Request Failed");
                }
            }
                
        }
        public void CloseClass()
        {
            CloseSocket(conn_num);
            FreeLibrary(hDMTDll);
        }
    }
}
