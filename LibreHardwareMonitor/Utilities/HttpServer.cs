// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using LibreHardwareMonitor.UI;
using System.Web;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace LibreHardwareMonitor.Utilities
{
    public class HttpServer
    {
        private readonly HttpListener _listener;
        private readonly Node _root;
        private readonly UnitManager _unitManager;

        private Thread _listenerThread;

        public int ListenerPort { get; set; }

        public HttpServer(Node node, int port, UnitManager unitManager)
        {
            _root = node;
            ListenerPort = port;
            _unitManager = unitManager;

            try
            {
                _listener = new HttpListener { IgnoreWriteExceptions = true };
            }
            catch (PlatformNotSupportedException)
            {
                _listener = null;
            }
        }

        public bool PlatformNotSupported
        {
            get
            {
                return _listener == null;
            }
        }

        public bool StartHttpListener()
        {
            if (PlatformNotSupported)
                return false;

            try
            {
                if (_listener.IsListening)
                    return true;

                string prefix = "http://+:" + ListenerPort + "/";
                _listener.Prefixes.Clear();
                _listener.Prefixes.Add(prefix);
                _listener.Start();

                if (_listenerThread == null)
                {
                    _listenerThread = new Thread(HandleRequests);
                    _listenerThread.Start();
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public bool StopHttpListener()
        {
            if (PlatformNotSupported)
                return false;

            try
            {
                _listenerThread?.Abort();
                _listener.Stop();
                _listenerThread = null;
            }
            catch (HttpListenerException) 
            { }
            catch (ThreadAbortException)
            { }
            catch (NullReferenceException)
            { }
            catch (Exception)
            { }

            return true;
        }

        private void HandleRequests()
        {
            while (_listener.IsListening)
            {
                IAsyncResult context = _listener.BeginGetContext(ListenerCallback, _listener);
                context.AsyncWaitHandle.WaitOne();
            }
        }

        public static IDictionary<string, string> ToDictionary(NameValueCollection col)
        {
            IDictionary<string, string> dict = new Dictionary<string, string>();
            foreach (string k in col.AllKeys)
            {
                dict.Add(k, col[k]);
            }
            return dict;
        }

        public SensorNode FindSensor(Node node, string id)
        {
            if (node is SensorNode sNode)
            {
                if (sNode.Sensor.Identifier.ToString() == id)
                    return sNode;
            }

            foreach (Node child in node.Nodes)
            {
                SensorNode s = FindSensor(child, id);
                if (s != null)
                {
                    return s;
                }
            }
            return null;
        }

        public void SetSensorControlValue(SensorNode node, string value)
        {
            if (node.Sensor.Control == null)
            {
                throw new ArgumentException("Specified sensor '" + node.Sensor.Identifier + "' can not be set");
            }

            if (value == "null")
            {
                node.Sensor.Control.SetDefault();
            }
            else
            {
                node.Sensor.Control.SetSoftware(float.Parse(value, CultureInfo.InvariantCulture));
            }
        }

        //Handles "/sensor" requests.
        //Parameters are taken from the query part of the URL.
        //Get:
        //http://localhost:8085/sensor?action=get&id=/some/node/path/0
        //The output is either:
        //{"result":"fail","message":"Some error message"}
        //or:
        //{"result":"ok","value":42.0, "format":"{0:F2} RPM"}
        //
        //Set:
        //http://localhost:8085/sensor?action=set&id=/some/node/path/0&value=42.0
        //http://localhost:8085/sensor?action=set&id=/some/node/path/0&value=null
        //The output is either:
        //{"result":"fail","message":"Some error message"}
        //or:
        //{"result":"ok"}
        private void HandleSensorRequest(HttpListenerRequest request, JObject result)
        {
            IDictionary<string, string> dict = ToDictionary(HttpUtility.ParseQueryString(request.Url.Query));

            if (!dict.ContainsKey("action"))
            {
                throw new ArgumentException("No action provided");
            }

            if (!dict.ContainsKey("id"))
            {
                throw new ArgumentException("No id provided");
            }

            SensorNode sensorNode = FindSensor(_root, dict["id"]);

            if (sensorNode == null)
            {
                throw new ArgumentException("Unknown id " + dict["id"] + " specified");
            }

            switch (dict["action"].ToLower())
            {
                case "set" when dict.ContainsKey("value"):
                    SetSensorControlValue(sensorNode, dict["value"]);
                    break;
                case "set":
                    throw new ArgumentException("No value provided");
                case "get":
                    result["value"] = sensorNode.Sensor.Value;
                    result["format"] = _unitManager.GetFormat(sensorNode.Sensor.SensorType);
                    break;
                default:
                    throw new ArgumentException("Unknown action type " + dict["action"]);
            }
        }

        // Handles http POST requests in a REST like manner.
        // Currently the only supported base URL is http://localhost:8085/sensor.
        private string HandlePostRequest(HttpListenerRequest request)
        {
            JObject result = new JObject { ["result"] = "ok" };
            
            try
            {
                if (request.Url.Segments.Length == 2)
                {
                    if (string.Equals(request.Url.Segments[1], "sensor", StringComparison.OrdinalIgnoreCase))
                    {
                        HandleSensorRequest(request, result);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid URL ('" + request.Url.Segments[1] + "'), possible values: ['sensor']");
                    }
                }
                else
                    throw new ArgumentException("Empty URL, possible values: ['sensor']");
            }
            catch (Exception e)
            {
                result["result"] = "fail";
                result["message"] = e.ToString();
            }
#if DEBUG
            return result.ToString(Newtonsoft.Json.Formatting.Indented);
#else
            return result.ToString(Newtonsoft.Json.Formatting.None);
#endif
        }

        private void ListenerCallback(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;

            if (listener == null || !listener.IsListening)
                return;

            // Call EndGetContext to complete the asynchronous operation.
            HttpListenerContext context;
            try
            {
                context = listener.EndGetContext(result);
            }
            catch (Exception)
            {
                return;
            }

            HttpListenerRequest request = context.Request;

            if (request.HttpMethod == "POST")
            {
                string postResult = HandlePostRequest(request);

                Stream output = context.Response.OutputStream;
                byte[] utfBytes = Encoding.UTF8.GetBytes(postResult);

                context.Response.AddHeader("Cache-Control", "no-cache");

                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = utfBytes.Length;

                output.Write(utfBytes, 0, utfBytes.Length);
                output.Close();

                return;
            }
            else if (request.HttpMethod == "GET")
            {
                string requestedFile = request.RawUrl.Substring(1);

                if (string.Equals(requestedFile, "data.json", StringComparison.OrdinalIgnoreCase))
                {
                    SendJson(context.Response);
                    return;
                }
            }

            context.Response.StatusCode = 404;
            context.Response.Close();
        }

        private void SendJson(HttpListenerResponse response)
        {
            JObject json = new JObject();

            int nodeIndex = 0;

            json["id"] = nodeIndex++;
            json["text"] = "Sensor";
            json["value"] = "Value";
            json["min"] = "Min";
            json["max"] = "Max";

            JArray children = new JArray
            {
                GenerateJsonForNode(_root, ref nodeIndex)
            };

            json["children"] = children;
#if DEBUG
            string responseContent = json.ToString(Newtonsoft.Json.Formatting.Indented);
#else
            string responseContent = json.ToString(Newtonsoft.Json.Formatting.None);
#endif
            byte[] buffer = Encoding.UTF8.GetBytes(responseContent);

            response.AddHeader("Cache-Control", "no-cache");
            response.AddHeader("Access-Control-Allow-Origin", "*");

            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;

            try
            {
                Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
            catch (HttpListenerException)
            {
            }

            response.Close();
        }

        private static JObject GenerateJsonForNode(Node node, ref int nodeIndex)
        {
            JObject jsonNode = new JObject
            {
                ["id"] = nodeIndex++,
                ["text"] = node.Text,
                ["value"] = string.Empty,
                ["min"] = string.Empty,
                ["max"] = string.Empty
            };

            if (node is SensorNode sensorNode)
            {
                jsonNode["sensorId"] = sensorNode.Sensor.Identifier.ToString();
                jsonNode["type"] = sensorNode.Sensor.SensorType.ToString();
                jsonNode["value"] = sensorNode.Value;
                jsonNode["min"] = sensorNode.Min;
                jsonNode["max"] = sensorNode.Max;
            }

            JArray children = new JArray();
            foreach (Node child in node.Nodes)
            {
                children.Add(GenerateJsonForNode(child, ref nodeIndex));
            }
            jsonNode["children"] = children;

            return jsonNode;
        }

        ~HttpServer()
        {
            if (PlatformNotSupported)
                return;

            StopHttpListener();

            _listener.Abort();
        }

        public void Quit()
        {
            if (PlatformNotSupported)
                return;

            StopHttpListener();

            _listener.Abort();
        }
    }
}
