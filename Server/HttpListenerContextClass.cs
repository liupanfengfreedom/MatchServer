using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net.Http;

namespace MatchServer
{
    class HttpListenerContextClass
    {
        HttpListenerContext mhttplistenercontext;
        Thread handleThread;
        public HttpListenerContextClass(HttpListenerContext p)
        {
            mhttplistenercontext = p;
            handleThread = new Thread(new ThreadStart(handlethreadfunc));
            handleThread.IsBackground = true;
            handleThread.Start();
        }
        ~HttpListenerContextClass()
        {
            Console.WriteLine("HttpListenerContextClass deconstruct");
        }
        void handlethreadfunc()
        {
            try
            {
                HttpListenerRequest request = mhttplistenercontext.Request;
                Stream stream = request.InputStream;
                
                System.Collections.Specialized.NameValueCollection header = request.Headers;
                string[] headerallkeys = header.AllKeys;
                string mid = "";
                string rarpath = "";
                foreach (var a in headerallkeys)
                {
                    if (a.Equals("mid"))
                    {
                        string[] values = header.GetValues(a);
                        mid = values[0];
                        window_file_log.Log(" 0mmessage.mid :" + mid);
                    }
                    else if (a.Equals("rarPath"))//
                    {
                        string[] values = header.GetValues(a);
                        rarpath = values[0];
                        window_file_log.Log(" 0mmessage.rarpath :" + rarpath);
                    }
                    else if (a.Equals("pakCallbackUrl"))//
                    {
                        string[] values = header.GetValues(a);
                       // Program.remotehttpserver = values[0];              
                    }
                }
                //rarmessage mmessage;
                //mmessage.rarpath = rarpath;
                //mmessage.mid = mid;
                //window_file_log.Log(" mmessage.rarpath :" + mmessage.rarpath);
                //window_file_log.Log(" mmessage.mid :" + mmessage.mid);
                //Program.rarqueue.Enqueue(mmessage);
                //foreach (var a in headerallkeys)
                //{
                //    if (a.Equals("rarPath"))//
                //    {
                //        string[] values = header.GetValues(a);
                //        rarmessage mmessage;
                //        mmessage.rarpath = values[0];
                //        mmessage.mid = mid;
                //        window_file_log.Log(" mmessage.rarpath :" + mmessage.rarpath);
                //        window_file_log.Log(" mmessage.mid :" + mmessage.mid);
                //        Program.rarqueue.Enqueue(mmessage);      
                //    }
                //}
                System.IO.Stream input = request.InputStream;
                byte[] array = new byte[request.ContentLength64];
                input.Read(array, 0, (int)request.ContentLength64);//larg file may encounter error
                string utfString = Encoding.UTF8.GetString(array, 0, array.Length);

                HttpListenerResponse response = mhttplistenercontext.Response;
                // Construct a response.
                string responseString = "success";
                //responseString += utfString;
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                Thread.Sleep(30);
                input.Close();
                output.Close();
            }
            catch(Exception e)
            {
                //throw (e);
                Console.WriteLine(e);

            }

        }
    }
}
