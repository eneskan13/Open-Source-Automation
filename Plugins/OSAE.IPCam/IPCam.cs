﻿using System;
using System.ComponentModel;
using System.Net;
using OSAE;

namespace OSAE.IPCam
{
    public class IPCam : OSAEPluginBase
    {
        OSAE.General.OSAELog Log;
        string pName = "";
        string camName;
        string sMethod;
        string sProperty;
        string newData;
        OSAEObject camobj;

        public override void RunInterface(string pluginName)
        {
            pName = pluginName;
            Log = new OSAE.General.OSAELog(pName);
            Log.Info(pluginName + " is starting...");
            Log.Info("===================================================");
            OwnTypes();
        }

        public void OwnTypes()
        {
            //Added the follow to automatically own Speech Base types that have no owner.
            OSAEObjectType oType = OSAEObjectTypeManager.ObjectTypeLoad("IPCAM");

            if (oType.OwnedBy == "")
            {
                OSAEObjectTypeManager.ObjectTypeUpdate(oType.Name, oType.Name, oType.Description, pName, oType.BaseType, oType.Owner, oType.SysType, oType.Container, oType.HideRedundant);
                Log.Info("IPCam Plugin took ownership of the IPCAM Object Type.");
            }
            else
                Log.Info("IPCam Plugin correctly owns the IPCAM Object Type.");

            oType = OSAEObjectTypeManager.ObjectTypeLoad("IP CAMERA");

            if (oType.OwnedBy == "")
            {
                OSAEObjectTypeManager.ObjectTypeUpdate(oType.Name, oType.Name, oType.Description, pName, oType.BaseType, oType.Owner, oType.SysType, oType.Container, oType.HideRedundant);
                Log.Info("IPCam Plugin took ownership of the IP CAMERA Object Type.");
            }
            else
                Log.Info("IPCam Plugin correctly owns the IP CAMERA Object Type.");
        }

        public override void ProcessCommand(OSAEMethod method)
        {
            Log.Info("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Log.Info("RECEIVED: " + method.ObjectName + " - " + method.MethodName);
            sMethod = method.MethodName;
            camName = method.ObjectName;
            camobj = OSAEObjectManager.GetObjectByName(camName);
            if (sMethod == "SNAPSHOT")
            {
                string i = DateTime.Now.ToLongTimeString();
                string j = DateTime.Now.ToShortDateString();
                i = i.Replace(":", "_");
                j = j.Replace("/", "_");
                i = j + "_" + i;
                i = i.Replace(" ", "");
                try
                {
                    string camSnapShot = camobj.Property("camSnapShot").Value;
                    string camSloc = camobj.Property("Save Location").Value;
                    camSloc = camSloc + @"\";
                    string filename = camSloc + camName + "_" + i + ".jpg";
                    var URI = new Uri(renameingSys(camSnapShot, "", ""));
                    WebClient wc = new WebClient();
                    wc.Headers["Content-Type"] = "application/x-www-form-urlencoded";
                    wc.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadFileCallback2);
                    wc.DownloadFile(URI, filename);
                    Log.Info(filename + " was created");
                }
                catch (Exception ex)
                { Log.Error("An error occurred durning the snapshot!!!", ex); }
            }
            else
            {
                try
                {
                    WebClient wc = new WebClient();
                    sProperty = camobj.Property(sMethod).Value.ToString();
                    wc.Headers["Content-Type"] = "application/x-www-form-urlencoded";
                    wc.UploadStringCompleted += new UploadStringCompletedEventHandler(UploadStringCallback2);
                    sProperty = renameingSys(sProperty, method.Parameter1, method.Parameter2);
                    wc.UploadStringAsync(new Uri(sProperty), "POST", "");
                    this.Log.Info("SENT TO: " + method.ObjectName + ": " + sProperty);
                }
                catch (Exception ex)
                { Log.Error("An error occurred!!!", ex); }
            }
            Log.Info("===================================================");
        }

        public override void Shutdown()
        {
            Log.Info("IPCam Plugin has STOPPED");
        }

        public string renameingSys(string fieldData, string camParam1, string camParam2)
        {
            string renameProperty;
            string getProperty;
            string changeProperty="";
            newData = fieldData.Replace("http://", "");
            try
            {
                while (newData.IndexOf("[") != -1)
            {
                int ss = newData.IndexOf("[");
                int es = newData.IndexOf("]");
                renameProperty = newData.Substring(ss + 1, (es - ss) - 1);          
                if (camParam1 != "")
                {
                    if (camParam1 == renameProperty)
                    {
                        if (camParam2 == "")
                            Log.Debug("Property change Error: NO Parameter 2 for property " + camParam1);
                        else
                        {
                            int ssp = camParam2.IndexOf("[");
                            int esp = camParam2.IndexOf("]");
                            if (ssp > -1 && esp > 0)
                            {
                                camParam2 = camParam2.Substring(ssp + 1, esp - 1);
                                camParam2 = camobj.Property(camParam2).Value;
                            }
                            changeProperty = camParam2;
                            Log.Debug("Property changed by Method Parameter 1: [" + renameProperty + "] to " + camParam2);
                        }
                    }
                    else
                        changeProperty = camobj.Property(renameProperty).Value;
                }
                else
                   changeProperty = camobj.Property(renameProperty).Value;
                
                getProperty = changeProperty;

                    // log any errors
                if (getProperty.Length > 0)
                    newData = newData.Replace("[" + renameProperty + "]", getProperty);
                else
                    Log.Error("Property " + getProperty + " has NO data");

                if (getProperty == null)
                    Log.Error("Property " + getProperty + " NOT FOUND");
            }
            newData = @"http://" + newData;
            return newData;
            }
            catch (Exception ex)
            {
                Log.Error("A renaming error occurred!!!", ex);
                return null;
            }
        }

        private void UploadStringCallback2(Object sender, UploadStringCompletedEventArgs e)
        {
            string reply = e.Result;
            Log.Debug("Device CGI returned info: " + reply);
        }

        private void DownloadFileCallback2(Object sender, AsyncCompletedEventArgs e)
        {
            string reply = e.ToString();
            Log.Debug("Device CGI returned info: " + reply);
        }
    }
}
