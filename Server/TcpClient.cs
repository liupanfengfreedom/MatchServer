#define UTF16
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
namespace MatchServer
{
   // public delegate void OnReceivedCompleted(List<byte>mcontent);
    public delegate void OnReceivedCompleted(byte[] buffer);
    public delegate void OnJoinroom(Room room);
 public class TCPClient
    {
        public String map { private set; get; }
        public String mapID { private set; get; }
        public String vip { private set; get; }
        public String rank { private set; get; }
        public String nvn { private set; get; }
        private String guid;
        public OnJoinroom onjoinroom;
        /// <summary>
        /// //////////////////////////////////////////////
        /// </summary>
        public  Room room;
        public bool isinmatchpool=false;
        public bool mclosed = false;
        Socket clientsocket;
        public OnReceivedCompleted OnReceivedCompletePointer=null;
        bool entrymapok;
        const int BUFFER_SIZE = 65536;
        const int SENDBUFFER_SIZE = 4096;
        byte[] sendbuffer = new byte[SENDBUFFER_SIZE];
        public byte[] receivebuffer = new byte[BUFFER_SIZE];
        string filestringpayload;
        bool isfile = false;
        Thread ReceiveThread;
        Thread KeepAliveThread;
        public bool getentrymapisok() {
            return entrymapok;
        }
        public TCPClient(Socket msocket)
        {
            Console.WriteLine("TCPClient "+ msocket.RemoteEndPoint);
            entrymapok = false;
            clientsocket = msocket;
            clientsocket.NoDelay = true;//false send immediately,this seem is opposite to msdn document
            OnReceivedCompletePointer += messagehandler;

            ReceiveThread = new Thread(new ThreadStart(ReceiveLoop));
            ReceiveThread.IsBackground = true;
            ReceiveThread.Start();

            KeepAliveThread = new Thread(new ThreadStart(keeptcpalive));
            KeepAliveThread.IsBackground = true;
            KeepAliveThread.Start();
            onjoinroom += AssignNumber;
            //AssignGUID();
        }
        ~TCPClient()
        {
            Console.WriteLine("TCPClient In destructor.");
        }
        private void AssignGUID()
        {
            Guid g;
            // Create and display the value of two GUIDs.
            g = Guid.NewGuid();
            string guidstring = g.ToString();
            FMessagePackage mp = new FMessagePackage();
            mp.MT = MessageType.GUID;
            mp.PayLoad = guidstring;
            String str = JsonConvert.SerializeObject(mp);
            Send(str);
        }
        private void AssignNumber(Room room)
        {
            FMessagePackage mp = new FMessagePackage();
            mp.MT = MessageType.NUMBER;
            mp.PayLoad = room.entrycounter.ToString();
            String str = JsonConvert.SerializeObject(mp);
            Send(str);
        }
        public void Send(byte[] buffer)
        {
            if (clientsocket != null)
            {
                clientsocket.Send(buffer);
                //buffer.CopyTo(sendbuffer, 0);
                //clientsocket.Send(sendbuffer);
                //Array.Clear(sendbuffer, 0, SENDBUFFER_SIZE);
                Thread.Sleep(300);
            }
        }
        public void Send(String message)
        {
#if UTF16
            UnicodeEncoding asen = new UnicodeEncoding();
#else
            ASCIIEncoding asen = new ASCIIEncoding();
#endif
            if (clientsocket != null)
            {
                this.Send(asen.GetBytes(message));
            }
        }
        void ReceiveLoop()
        {
            while (true)
            {
                try {
                    Array.Clear(receivebuffer, 0, receivebuffer.Length);
                    clientsocket.Receive(receivebuffer);
                    OnReceivedCompletePointer?.Invoke(receivebuffer);
                    Thread.Sleep(30);
                }
                catch (SocketException)
                {
                    mclosed = true;
                    CloseSocket();
                    room?.Remove(this);
                    ReceiveThread.Abort();
                }
            }

        }
        public void CloseSocket()
        {
            clientsocket.Close();
        }
        void messagehandler(byte[] buffer)
        {
            FMessagePackage mp;
            try
            {
#if UTF16
                var str = System.Text.Encoding.Unicode.GetString(buffer);
#else
            var str = System.Text.Encoding.UTF8.GetString(buffer);
#endif
                int len = str.Length;
                string filestr = "{\r\n\t\"mT\": \"FILE";
                string fileendstr = "{\r\n\t\"mT\": \"FILEEND";
                if (isfile)
                {
                    if (str.StartsWith(fileendstr))
                    {
                        int size = filestringpayload.Length;
                        isfile = false;
                        return;
                    }

                    filestringpayload += str;
                    int size1 = filestringpayload.Length;
                    FMessagePackage filesend = new FMessagePackage();
                    filesend.MT = MessageType.FILE;//go on             
                    String strsend = JsonConvert.SerializeObject(filesend);
                    Send(strsend);
                    return;
                }
                if (str.StartsWith(filestr))
                {
                    isfile = true;
                    return;
                }
                bool bisjson = Utility.IsValidJson(str);
                if (!bisjson)
                {
                    killthegameclient();
                    return;
                }
                mp = JsonConvert.DeserializeObject<FMessagePackage>(str);
                switch (mp.MT)
                {
                    case MessageType.MATCH:
/////////////////////////////////////////////////////
                        ///
                        lock (Program.singinLock)
                        {
                            Program.singinpool.Add(this);
                        }
                        int singinpoollen = Program.singinpool.Count;
                        Console.WriteLine("singinpool " + singinpoollen.ToString());
 //////////////////////
                        String[] strarray = mp.PayLoad.Split('?');
                        map = strarray[0];//map
                        mapID = strarray[1];//mapID
                        nvn = strarray[2];//nvn
                        Console.WriteLine(map);
                        break;
                    case MessageType.EntryMAPOK:
                        entrymapok = true;
                        break;
                    case MessageType.EXITGAME:
                        mclosed = true;
                        CloseSocket();
                        room?.Remove(this);
                        ReceiveThread.Abort();
                        break;
                }

            }
            catch(Newtonsoft.Json.JsonSerializationException){//buffer all zero//occur when mobile client force kill the game client
                killthegameclient();
            }
        }
        void keeptcpalive()
        {
            while (true)
            {
                Thread.Sleep(1000*60);
                bool bconnected = this.clientsocket.Connected;
                if (bconnected)
                {
                    this.Send("keepalive");
                }
                else {
                    killthegameclient();
                }
            }
        }
        void killthegameclient()
        {
            mclosed = true;
            CloseSocket();
            room?.Remove(this);
            ReceiveThread.Abort();
            KeepAliveThread.Abort();
        }

    }
}
