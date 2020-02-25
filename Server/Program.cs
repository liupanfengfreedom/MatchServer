///////////////match server ok
///copyright***
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;

namespace MatchServer
{
    class TCPClienttype
    {
        public  string map;
        public string mapID;
        public string vip;
        public string rank;
        public string nvn;
        public  HashSet<TCPClient> MatchHashsetPool;
        public Room currentroom=null;
        public TCPClienttype()
        {
            map = null;
            vip = null;
            rank = null;
            MatchHashsetPool = new HashSet<TCPClient>();
        }
    }
    class Program
    {
        private static readonly object matchLock = new object();
        public static readonly object singinLock = new object();
        static List<TCPClienttype> AllWaitforMatchpools = new List<TCPClienttype>();
        static public List<TCPClient> singinpool = new List<TCPClient>();
        static List<Room> roomlist = new List<Room>();
        static TcpListener myList;
        public static Config config = new Config();
        static void Main(string[] args)
        {
             
            IPAddress ipAd = IPAddress.Parse(config.configinfor.ipaddress);//local ip address  "172.16.5.188"
            myList = new TcpListener(ipAd, config.configinfor.ipport);//8001
            /* Start Listeneting at the specified port */
            myList.Start();
            Thread matchalgorithm = new Thread(new ThreadStart(Matchalgorithm));
            Thread matchalhelp = new Thread(new ThreadStart(singinpooltomatchpool));
            Thread tcplistenthread = new Thread(new ThreadStart(tcplistenthreadwork));
            matchalhelp.IsBackground = true;
            matchalhelp.Start();
            matchalgorithm.IsBackground = true;
            matchalgorithm.Start();
            tcplistenthread.IsBackground = true;
            tcplistenthread.Start();
            int id = Thread.CurrentThread.ManagedThreadId;

            while (true)
            {
                bool b = tcplistenthread.IsAlive;
                if (!b)
                {
                    tcplistenthread = new Thread(new ThreadStart(tcplistenthreadwork));
                    tcplistenthread.IsBackground = true;
                    tcplistenthread.Start();
                    Console.WriteLine("tcplistenthread restart");
                    window_file_log.Log("tcplistenthread restart");
                }
                Thread.Sleep(1000);
            }
        }

       static async void Accept()
        {
            while (true)
            {
                int id = Thread.CurrentThread.ManagedThreadId;
                Socket st = await myList.AcceptSocketAsync();
                TCPClient tcpClient = new TCPClient(st);
            }
        }
        static void tcplistenthreadwork()
        {
            int id = Thread.CurrentThread.ManagedThreadId;
            Accept();
            while (true)
            {
                Thread.Sleep(10000);
                //Console.WriteLine("tcplistenthread is alive");
                //window_file_log.Log("tcplistenthread is alive");
            }
        }
        static void Matchalgorithm()
        {
            while (true)
            {
                lock (matchLock)
                {
                    for (int j = 0; j < AllWaitforMatchpools.Count; j++)
                    {
                        int len = AllWaitforMatchpools[j].MatchHashsetPool.Count;
                        // Console.WriteLine("for ahead AllWaitforMatchpools len" + len.ToString());
                        for (int i = 0; i < len; i++)
                        {
                            //  Console.WriteLine("AllWaitforMatchpools len"+ len.ToString());

                            if (AllWaitforMatchpools[j].currentroom == null || AllWaitforMatchpools[j].currentroom.mprocess.HasExited)
                            {
                                int nvn=0;
                                if (!Int32.TryParse(AllWaitforMatchpools[j].nvn, out nvn))
                                {
                                    nvn = 16;
                                }
                                AllWaitforMatchpools[j].currentroom = new Room(nvn, LanchServer.CreateOneRoom());//the client who create room determine the nvn 
                                AllWaitforMatchpools[j].currentroom.listroom = roomlist;
                                AllWaitforMatchpools[j].currentroom.tcpclienttype = AllWaitforMatchpools[j];
                               //Thread.Sleep(100);//wait IP port take effect
                            }
                            TCPClient temp = AllWaitforMatchpools[j].MatchHashsetPool.ElementAt(i); ;
                            int maxnumber = AllWaitforMatchpools[j].currentroom.getroommaxpeoplenumber();
                            int currentnumber = AllWaitforMatchpools[j].currentroom.getnumberofpeopleinroom();
                            if (currentnumber < maxnumber)
                            {
                                AllWaitforMatchpools[j].currentroom.Add(temp);
                            }
                            else//full
                            {
                                FMessagePackage mp = new FMessagePackage();
                                mp.MT = MessageType.MAPISFULL;
                                String str = JsonConvert.SerializeObject(mp);
                                temp.Send(str);// tell client the number of people in current map has reach the ceilling
                            }

                            temp.isinmatchpool = true;
                            AllWaitforMatchpools[j].MatchHashsetPool.Remove(temp);
                            len = AllWaitforMatchpools[j].MatchHashsetPool.Count;
                            i = 0;
                            //if (!notfull)
                            //{
                            //    roomlist.Add(AllWaitforMatchpools[j].currentroom);
                            //    AllWaitforMatchpools[j].currentroom = null;
                            //}
                        }

                    }
 
                }
                Thread.Sleep(200);
            }
        }
        static void singinpooltomatchpool()
        {
            while (true)
            {
                lock (matchLock)
                {
                    int len = 0;
                    lock (singinLock)
                    {
                         len = singinpool.Count;
                        for (int i = 0; i < len; i++)
                        {
                            if (singinpool[i].mclosed)//clear offline client
                            {
                                singinpool.RemoveAt(i);

                                len = singinpool.Count;
                                i = 0;
                            }
                        }
                    }

                    len = singinpool.Count;
                    for (int i = 0; i < len; i++)
                    {
                        if (singinpool[i].isinmatchpool)
                        {
                            continue;
                        }
                        //singinpool[i].isinmatchpool = true;
                        bool b_hasmap = false;
                        for (int j=0 ; j < AllWaitforMatchpools.Count; j++)
                        {
                            bool b = AllWaitforMatchpools[j].map == singinpool[i].map && !String.IsNullOrEmpty(singinpool[i].map);
                            bool b1 = AllWaitforMatchpools[j].rank == singinpool[i].rank;
                            b1 = true;
                            bool b2 = AllWaitforMatchpools[j].vip  == singinpool[i].vip;
                            b2 = true;
                            bool b3 = AllWaitforMatchpools[j].nvn == singinpool[i].nvn;
                            bool b4 = AllWaitforMatchpools[j].mapID == singinpool[i].mapID;
                            if (b&&b1&&b2 && b3 && b4)
                            {
                                b_hasmap = true;
                                AllWaitforMatchpools[j].MatchHashsetPool.Add(singinpool[i]);
                                Console.WriteLine(" if (b&&b1&&b2)");

                            }
                        }
                        if (!b_hasmap)
                        {
                            Console.WriteLine("if (!b_hasmap)");
                            TCPClienttype temptype = new TCPClienttype();
                            temptype.map = singinpool[i].map;
                            temptype.rank = singinpool[i].rank;
                            temptype.vip = singinpool[i].vip;
                            temptype.nvn = singinpool[i].nvn;
                            temptype.mapID = singinpool[i].mapID;
                            AllWaitforMatchpools.Add(temptype);//
                            ///remove some TCPClienttype from AllWaitforMatchpools maybe not necessary;
                        }
                    }
                }
                Thread.Sleep(200);
            }
        }
    }
}
