using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;

namespace MatchServer
{
    enum MessageTypev1
    {
        PLAYER,//
        MATCHSERVER,//
    }
    struct FMessagePackagev1
    {
        public MessageTypev1 MT;

        public string PayLoad;
        public FMessagePackagev1(string s)
        {
            MT = MessageTypev1.PLAYER;
            PayLoad = "";
        }
    }
    struct FRoom
    {
        public string map;

        public string mapip;
        public FRoom(string s)
        {
            map = s;
            mapip = s;
        }
    }
    struct FRoomlist
    {
        public string space;
        public List<FRoom> roomlist;
        public FRoomlist(string s)
        {
            space = s;
            roomlist = new List<FRoom>();
        }
    }
    class Reportroomlist
    {
        static Reportroomlist()
        {
            int roomlistsize = Program.roomlist.Count;
            Thread httpthread = new Thread(new ThreadStart(httpthreadwork));
            httpthread.Start();
        }
        static void httpthreadwork()
        {
            while (true)
            {
                try
                {
                    //var payload = new Dictionary<string, string>
                    //{
                    //  {"result","failure"},
                    //  {"reason","访问的RAR文件不存在"},
                    // // {"mid",Program.currentwid},
                    //};
                    var payload = new FRoomlist("");
                    payload.space = "space";
                    var room = new FRoom("");
                    foreach (var v in Program.roomlist)
                    {
                        room.map = v.tcpclienttype.map;
                        room.mapip = v.getnumberofpeopleinroom().ToString();
                        payload.roomlist.Add(room);
                    }
                    string strPayload = JsonConvert.SerializeObject(payload);
                    HttpContent httpContent = new StringContent(strPayload, Encoding.UTF8, "application/json");
                    HttpclientHelper.httppost("http://172.16.5.188:7001/", httpContent);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                Thread.Sleep(1000 * 5);
            }
        }
    }
}
