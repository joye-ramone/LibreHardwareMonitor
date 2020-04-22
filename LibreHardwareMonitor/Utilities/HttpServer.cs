﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using LibreHardwareMonitor.UI;
using LibreHardwareMonitor.Hardware;
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

        private Thread _listenerThread;

        public int ListenerPort { get; set; }

        public HttpServer(Node node, int port)
        {
            _root = node;
            ListenerPort = port;

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

            if (dict.ContainsKey("action"))
            {
                if (dict.ContainsKey("id"))
                {
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
                            throw new ArgumentNullException("action", "No value provided");
                        case "get":
                            result["value"] = sensorNode.Sensor.Value;
                            result["format"] = sensorNode.Format;
                            break;
                        default:
                            throw new ArgumentException("Unknown action type " + dict["action"]);
                    }
                }
                else
                {
                    throw new ArgumentNullException("No id provided");
                }
            }
            else
            {
                throw new ArgumentNullException("No action provided");
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
            }
            else if (request.HttpMethod == "GET")
            {
                string requestedFile = request.RawUrl.Substring(1);

                if (string.Equals(requestedFile, "data.json", StringComparison.OrdinalIgnoreCase))
                {
                    SendJson(context.Response);
                    return;
                }

                if (requestedFile.Contains("images_icon"))
                {
                    ServeResourceImage(context.Response, requestedFile.Replace("images_icon/", string.Empty));
                    return;
                }

                // default file to be served
                if (string.IsNullOrEmpty(requestedFile))
                {
                    requestedFile = "index.html";
                }

                string ext = requestedFile.Substring(requestedFile.LastIndexOf(".", StringComparison.Ordinal) + 1);

                ServeResourceFile(context.Response, "Web." + requestedFile.Replace('/', '.'), ext);
            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
            }
        }

        private static void ServeResourceFile(HttpListenerResponse response, string name, string ext)
        {
            // resource names do not support the hyphen
            name = "LibreHardwareMonitor.Resources." + name.Replace("custom-theme", "custom_theme");

            string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (string t in names)
            {
                if (t.Replace('\\', '.') == name)
                {
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(t))
                    {
                        response.ContentType = GetContentType("." + ext);
                        response.ContentLength64 = stream.Length;

                        byte[] buffer = new byte[512 * 1024];
                        try
                        {
                            Stream output = response.OutputStream;
                            int len;
                            while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                output.Write(buffer, 0, len);
                            }

                            output.Flush();
                            output.Close();

                            response.Close();
                        }
                        catch (HttpListenerException)
                        {
                        }
                        catch (InvalidOperationException)
                        {
                        }
                        return;
                    }
                }
            }

            response.StatusCode = 404;
            response.Close();
        }

        private static void ServeResourceImage(HttpListenerResponse response, string name)
        {
            name = "LibreHardwareMonitor.Resources." + name;

            string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            foreach (string t in names)
            {
                if (t.Replace('\\', '.') == name)
                {
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(t))
                    {
                        using (Image image = Image.FromStream(stream ?? throw new InvalidOperationException()))
                        {
                            response.ContentType = "image/png";
                            try
                            {
                                Stream output = response.OutputStream;
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    image.Save(ms, ImageFormat.Png);
                                    ms.WriteTo(output);
                                }

                                output.Flush();
                                output.Close();

                                response.Close();
                            }
                            catch (HttpListenerException)
                            {
                            }

                            return;
                        }
                    }
                }
            }

            response.StatusCode = 404;
            response.Close();
        }

        private void SendJson(HttpListenerResponse response)
        {
            JObject json = new JObject();

            int nodeIndex = 0;

            json["id"] = nodeIndex++;
            json["Text"] = "Sensor";
            json["Min"] = "Min";
            json["Value"] = "Value";
            json["Max"] = "Max";
            json["ImageURL"] = string.Empty;

            JArray children = new JArray
            {
                GenerateJsonForNode(_root, ref nodeIndex)
            };

            json["Children"] = children;
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
                ["Text"] = node.Text,
                ["Min"] = string.Empty,
                ["Value"] = string.Empty,
                ["Max"] = string.Empty
            };

            if (node is SensorNode sensorNode)
            {
                jsonNode["SensorId"] = sensorNode.Sensor.Identifier.ToString();
                jsonNode["Type"] = sensorNode.Sensor.SensorType.ToString();
                jsonNode["Min"] = sensorNode.Min;
                jsonNode["Value"] = sensorNode.Value;
                jsonNode["Max"] = sensorNode.Max;
                jsonNode["ImageURL"] = "images/transparent.png";
            }
            else if (node is HardwareNode hardwareNode)
            {
                jsonNode["ImageURL"] = "images_icon/" + GetHardwareImageFile(hardwareNode);
            }
            else if (node is TypeNode typeNode)
            {
                jsonNode["ImageURL"] = "images_icon/" + GetTypeImageFile(typeNode);
            }
            else
            {
                jsonNode["ImageURL"] = "images_icon/computer.png";
            }

            JArray children = new JArray();
            foreach (Node child in node.Nodes)
            {
                children.Add(GenerateJsonForNode(child, ref nodeIndex));
            }
            jsonNode["Children"] = children;

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

        private static string GetContentType(string extension)
        {
            switch (extension)
            {
                case ".avi":
                    return "video/x-msvideo";
                case ".css":
                    return "text/css";
                case ".doc":
                    return "application/msword";
                case ".gif":
                    return "image/gif";
                case ".htm":
                case ".html":
                    return "text/html";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".js":
                    return "application/x-javascript";
                case ".mp3":
                    return "audio/mpeg";
                case ".png":
                    return "image/png";
                case ".pdf":
                    return "application/pdf";
                case ".ppt":
                    return "application/vnd.ms-powerpoint";
                case ".zip":
                    return "application/zip";
                case ".txt":
                    return "text/plain";
                default:
                    return "application/octet-stream";
            }
        }

        private static string GetHardwareImageFile(HardwareNode hn)
        {
            switch (hn.Hardware.HardwareType)
            {
                case HardwareType.Cpu:
                    return "cpu.png";
                case HardwareType.GpuNvidia:
                    return "nvidia.png";
                case HardwareType.GpuAmd:
                    return "ati.png";
                case HardwareType.Storage:
                    return "hdd.png";
                case HardwareType.Heatmaster:
                    return "bigng.png";
                case HardwareType.Motherboard:
                    return "mainboard.png";
                case HardwareType.SuperIO:
                    return "chip.png";
                case HardwareType.TBalancer:
                    return "bigng.png";
                case HardwareType.Memory:
                    return "ram.png";
                case HardwareType.AquaComputer:
                    return "acicon.png";
                case HardwareType.Network:
                    return "nic.png";
                default:
                    return "cpu.png";
            }
        }

        private static string GetTypeImageFile(TypeNode tn)
        {
            switch (tn.SensorType)
            {
                case SensorType.Voltage:
                    return "voltage.png";
                case SensorType.Clock:
                    return "clock.png";
                case SensorType.Load:
                    return "load.png";
                case SensorType.Temperature:
                    return "temperature.png";
                case SensorType.Fan:
                    return "fan.png";
                case SensorType.Flow:
                    return "flow.png";
                case SensorType.Control:
                    return "control.png";
                case SensorType.Level:
                    return "level.png";
                case SensorType.Power:
                    return "power.png";
                case SensorType.Throughput:
                    return "throughput.png";
                default:
                    return "power.png";
            }
        }
    }
}
