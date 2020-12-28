using System;
using System.Net;
using System.Threading;
using System.Linq;
using System.Text;

namespace ShellHelper
{
    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;

        public WebServer(System.Collections.Generic.List<string> prefixes, Func<HttpListenerRequest, string> method)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "Needs Windows XP SP2, Server 2003 or later.");

            // URI prefixes are required, for example                          
            if (prefixes == null || prefixes.Count == 0)
                throw new ArgumentException("prefixes");

            // A responder method is required
            if (method == null)
                throw new ArgumentException("method");

            foreach (string s in prefixes)
                _listener.Prefixes.Add(s);

            _responderMethod = method;
            _listener.Start();
        }

        public WebServer(Func<HttpListenerRequest, string> method, System.Collections.Generic.List<string> prefixes)
            : this(prefixes, method) { }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Console.WriteLine("Webserver running...");
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                string rstr = _responderMethod(ctx.Request);
                                
                                
                                string _Command = ctx.Request.RawUrl.Regx("/[\\w|_|\\.]+\\??$");
                                
                                _Command = _Command.Replace("/", "").Replace("?", "");
                                _Command = _Command.ToLower();
                                bool bFoundResource = false;
                                if (ctx.Request.RawUrl == "/")
                                    _Command = "index.html";
                                if(!string.IsNullOrEmpty(_Command))
                                {
                                    var bufResult = Utils.GetFileResource($"ShellHelper.WebStore.{_Command}");
                                    if(bufResult != null)
                                    {
                                        ctx.Response.ContentLength64 = bufResult.Length;
                                        ctx.Response.OutputStream.Write(bufResult, 0, bufResult.Length);
                                        bFoundResource = true;
                                    }
                                }
                                //if (ctx.Request.RawUrl.Contains("favicon.ico"))
                                //{
                                //    var bufIco = Utils.GetFileResource("ShellHelper.WebStore.favicon.ico");
                                //    ctx.Response.ContentLength64 = bufIco.Length;
                                //    ctx.Response.OutputStream.Write(bufIco, 0, bufIco.Length);
                                //}
                                //else if (ctx.Request.RawUrl == "/" || ctx.Request.RawUrl.Contains("index.html"))
                                //{

                                //    var bufIco = Utils.GetFileResource("ShellHelper.WebStore.index.html");
                                //    ctx.Response.ContentLength64 = bufIco.Length;
                                //    ctx.Response.OutputStream.Write(bufIco, 0, bufIco.Length);

                                //}
                                //else if (ctx.Request.RawUrl.EndsWith("script.js"))
                                //{
                                //    //var bufIco = Net.Utils.GetFileResource("SMSRecv.Web.script.js");
                                //    //ctx.Response.ContentLength64 = bufIco.Length;
                                //    //ctx.Response.OutputStream.Write(bufIco, 0, bufIco.Length);
                                //}
                                //else 
                                if(!bFoundResource)
                                {
                                    ctx.Response.Headers.Add("Accept", "*");
                                    ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                                    //
                                    byte[] buf = Encoding.UTF8.GetBytes(rstr);
                                    ctx.Response.ContentLength64 = buf.Length;
                                    ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                                }
                            }
                            catch { } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
}