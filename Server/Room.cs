#define MODE1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace MatchServer
{
    enum MessageType
    {
        FILE,//changless
        FILEEND,//changless
        CLIENT_FILE,
        CLIENT_FILEEND,
        CLIENT_FILERECEIVEOK,
        SINGUP,
        LOGIN,
        MATCH,
        SAVEMAPACTORINFOR,
        GETMAPACTORINFOR,
        MAPACTORINFORSENDOK,
        EntryMAP,
        EntryMAPOK,
        MAPISFULL,//the number in the map has reach the ceilling
        EXITGAME,
        FILERECEIVEOK,//server side receive ok
    }
    struct FMessagePackage
    {
        public MessageType MT;

        public string PayLoad;
        public FMessagePackage(string s)
        {
            MT = MessageType.MATCH;
            PayLoad = "";
        }
    }
    class processkiller
    {
        public Thread threadkiller;
        public bool runing;
    }
    class Room
    {
        public TCPClienttype tcpclienttype;
        public List<Room> listroom;
        public string map;
        List<TCPClient> mPeopleinroom;
        int maxpeople = 0;
        string roomipaddress;
        public Process mprocess { get; private set; }
        processkiller pker = new processkiller();
        public Room(int maxpeople, Roomipprocess rp)
        {
            this.roomipaddress = rp.mip;
            this.maxpeople = maxpeople;
            this.mprocess = rp.mprocess;
            mPeopleinroom = new List<TCPClient>();
            pker.threadkiller = new Thread(new ThreadStart(shouldclosthisroom));
        }
        ~Room()
        {
            if (mprocess != null && !mprocess.HasExited)
            {
                mprocess.Kill();
            }
            Console.WriteLine("Room In destructor.");
        }
        public bool Add(TCPClient mtc)
        {
            mtc.room = this;
            mPeopleinroom.Add(mtc);
#if MODE1
            ///////////////////
            ///
            FMessagePackage mp = new FMessagePackage();
            mp.MT = MessageType.EntryMAP;
            mp.PayLoad = roomipaddress;
            String str = JsonConvert.SerializeObject(mp);
            mtc.Send(str);
#endif
            if (pker.runing == false)
            {
                pker.runing = true;
                pker.threadkiller.Start();
            }
            ////////////////////////////
            int len = mPeopleinroom.Count;
            Console.WriteLine("Room :" + len.ToString());
            if (len < maxpeople)
            {
                return true;
            }
            else
            {
#if MODE1
#else
                Thread mt = new Thread(new ThreadStart(allpeoplepresentjustgo));
                mt.Start();
                Console.WriteLine("allpeoplepresentjustgo :");

#endif
                return false;
            }


        }
        public bool Remove(TCPClient mtc)
        {
            mPeopleinroom.Remove(mtc);
            int len = mPeopleinroom.Count;
            Console.WriteLine("Room :" + len.ToString());
            return true;
        }
        void destroythisroom()
        {
            if (mprocess != null && !mprocess.HasExited)
            {
                mprocess.Kill();
            }

            listroom.Remove(this);
            Console.WriteLine("listroom :" + listroom.Count.ToString());

        }

        public List<TCPClient> GetAllMember()
        {
            return mPeopleinroom;
        }
        public int getnumberofpeopleinroom()
        {
            return mPeopleinroom.Count;
        }
        public int getroommaxpeoplenumber()
        {
            return maxpeople;
        }
        void allpeoplepresentjustgo()
        {
            int len = mPeopleinroom.Count;
            for (int i = 0; i < len; i++)
            {
                FMessagePackage mp = new FMessagePackage();
                mp.MT = MessageType.EntryMAP;
                mp.PayLoad = roomipaddress;
                String str = JsonConvert.SerializeObject(mp);
                mPeopleinroom[i].Send(str);
                Console.WriteLine("mp.MT = MessageType.EntryMAP");
                while (!mPeopleinroom[i].getentrymapisok())
                {
                    Thread.Sleep(100);
                    Console.WriteLine("wait for entry room :");
                }
            }
        }
        void shouldclosthisroom()
        {

            while (true)
            {
                if (mPeopleinroom.Count == 0)
                {
                    destroythisroom();
                    break;
                }
                Thread.Sleep(1000);
            }

        }
    }
}
