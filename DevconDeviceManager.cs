using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using WindowsMicrophoneMuteLibrary;

class DevConDeviceManager
{
    private Process devcon;
    // Status variables
    private Dictionary<string, string> deviceDictionary;
    private Dictionary<string, string> disabledDeviceDictionary;
    private Dictionary<string, string> currentlyDisabledDeviceDictionary;

    // Public settings
    public bool disableWebcams = true;
    public bool disableMicrophones = true;
    public bool disableNetworkDevices = false;
    public bool disableBluetoothDevices = false;
    public DevConDeviceManager(string devconPath) {
        if (!File.Exists(devconPath)) {
            Debug.WriteLine("The path specified for Devcon was not found.");
            return;
        }
        this.disabledDeviceDictionary = new Dictionary<string, string>();
        this.currentlyDisabledDeviceDictionary = new Dictionary<string, string>();
        // Configure devcon for global usage.
        this.devcon = new System.Diagnostics.Process();
        this.devcon.StartInfo.FileName = devconPath;
        // Pull in a list of all current devices.
        this.devcon.StartInfo.Arguments = "find *";
        this.devcon.StartInfo.RedirectStandardError = true;
        this.devcon.StartInfo.RedirectStandardOutput = true;
        this.devcon.StartInfo.UseShellExecute = false;
        this.devcon.StartInfo.CreateNoWindow = true;
        this.devcon.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        this.devcon.Start();
        // Setup key kvp, key = device mahcine instance id, value = device name
        this.deviceDictionary = new Dictionary<string, string>();
        string[] tmp;
        while (!this.devcon.StandardOutput.EndOfStream){
            string line = this.devcon.StandardOutput.ReadLine();
            //   Debug.WriteLine("Found device :");
            //    Debug.WriteLine(line);
            // Devcon prints device line by line, only lines with ':' are devices
            if (line.Contains(":"))
            {
                tmp = line.Split(':');
                // left side of line = hardware id, right side = device name
                this.deviceDictionary.Add(tmp[0].Trim(), tmp[1].Trim());
            }
        }
    }

    private string GetUniqueID(string deviceName)
    {
        string id = this.FindDeviceIDByString(deviceName);
        // string id = deviceName; // DEBUG ONLY
        bool found = false;
        while (found == false)
        {
            try
            {
                this.devcon.StartInfo.Arguments = "find " + id;
                this.devcon.Start();
                Debug.WriteLine("Checking for : " + id);
                while (!this.devcon.StandardOutput.EndOfStream)
                {
                    string line = this.devcon.StandardOutput.ReadLine();
                    if (line.Contains("1 matching device(s) found"))
                    {
                        return id;
                    }
                }
                id = id.Remove(id.Length - 1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return "";

            }
        }
        Debug.WriteLine("Couldn't find a unique ID match.");
        return "";
    }

    public bool DisableDeviceByString(string match)
    {
        string uniqueID = this.GetUniqueID(match);
        if (uniqueID.Length == 0)
        {
            return false;
        }
        this.devcon.StartInfo.Arguments = "disable " + uniqueID;
        Debug.WriteLine(this.devcon.StartInfo.Arguments);
        this.devcon.Start();
        while (!this.devcon.StandardOutput.EndOfStream)
        {
            string line = this.devcon.StandardOutput.ReadLine();
            if (line.Contains("1 device(s) disabled"))
            {
                return true;
            }
            else if (line.Contains("1 device(s) are ready to be disabled"))
            {
                return true;
            }
        }
        return false;
    }

    private KeyValuePair<string, string> FindDeviceByString(string match)
    {
        foreach (KeyValuePair<string, string> kvp in this.deviceDictionary)
        {
            if (kvp.Value.ToLower().Contains(match.ToLower()))
            {
                return kvp;
            }
            if (kvp.Key.ToLower().Contains(match.ToLower()))
            {
                return kvp;
            }
        }
        return default(KeyValuePair<string, string>);
    }

    private string FindDeviceIDByString(string match)
    {
        KeyValuePair<string, string> result = FindDeviceByString(match);
        if (result.Equals(default(KeyValuePair<string, string>)))
        {
            return "";
        }
        else
        {
            return result.Key;
        }

    }

    private string FindDeviceNameByString(string match)
    {
        KeyValuePair<string, string> result = FindDeviceByString(match);
        if (result.Equals(default(KeyValuePair<string, string>)))
        {
            return "";
        }
        else
        {
            return result.Value;
        }
    }

    public List<string> DisabledDeviceNamesToList()
    {
        if (this.disabledDeviceDictionary == null)
        {
            return new List<String>();
        }
        List<string> devices = new List<string>();
        foreach (KeyValuePair<string, string> kvp in this.disabledDeviceDictionary)
        {
            devices.Add(kvp.Value);
        }
        return devices;
    }
    public List<string> DisabledDeviceIDsToList()
    {
        if (this.disabledDeviceDictionary == null)
        {
            return new List<String>();
        }
        List<string> devices = new List<string>();
        foreach (KeyValuePair<string, string> kvp in this.disabledDeviceDictionary)
        {
            devices.Add(kvp.Key);
        }
        return devices;
    }

    public List<string> DeviceNamesToList()
    {
        List<string> devices = new List<string>();
        foreach (KeyValuePair<string, string> kvp in this.deviceDictionary)
        {
            devices.Add(kvp.Value);
        }
        return devices;
    }

    public List<string> DeviceIDsToList()
    {
        List<string> devicesIDs = new List<string>();
        foreach (KeyValuePair<string, string> kvp in this.deviceDictionary)
        {
            devicesIDs.Add(kvp.Key);
        }
        return devicesIDs;
    }


    

    private void AddDefaultDevicesToDisable()
    {
        string[] defaultDeviceClasses = { "MEDIA", "Image", "PCMCIA", "WCEVSBS" }; // Remove USB, too dangerous.
        string[] defaultDeviceMatchNames = { "Webcam", "Camera", "VideoCamera", "Microphone", "Android", "Iphone" };
        string[] tmp;
        // Loop through all our default device classes, and use devcon to grab associated devices.
        foreach (string dClass in defaultDeviceClasses)
        {
            this.devcon.StartInfo.Arguments = "find =" + dClass;
            this.devcon.Start();
            while (!this.devcon.StandardOutput.EndOfStream)
            {
                string line = this.devcon.StandardOutput.ReadLine();
                // Devcon prints device line by line, only lines with ':' are devices
                if (line.Contains(":"))
                {
                    Debug.WriteLine("Found defaut device " + line);
                    tmp = line.Split(':');
                    AddDeviceToDisable(new KeyValuePair<string, string>(tmp[0].Trim(), tmp[1].Trim()));
                }
            }

        }

        KeyValuePair<string, string> tmpDevice;
        foreach (string name in defaultDeviceMatchNames)
        {
            tmpDevice = FindDeviceByString(name);
            if (!tmpDevice.Equals(default(KeyValuePair<string, string>)))
            {
                AddDeviceToDisable(tmpDevice);

            }
        }
    }
    //public string GetDeviceStatus(string deviceId){
    //    this.devcon.StartInfo.Arguments = @"status """ + deviceId + @"*""";
    //    Console.WriteLine(this.devcon.StartInfo.Arguments);
    //    this.devcon.Start();
    //    return this.devcon.StandardOutput.ReadToEnd();
    //}

    private void AddDeviceToDisable(KeyValuePair<string, string> device)
    {
        string[] blackList = { "keyboard", "mouse", "display" };
        bool blackListed = false;
        foreach (string s in blackList)
        {
            if (device.Key.ToLower().Contains(s.ToLower()))
            {
                blackListed = true;
            }
            if (device.Value.ToLower().Contains(s.ToLower()))
            {
                blackListed = true;
            }
        }
        if (blackListed)
        {
            return;
        }
        else
        {
            try { this.disabledDeviceDictionary.Add(device.Key, device.Value); }
            catch (Exception ex) { Debug.WriteLine(ex.Message); }
        }
    }

    public void EnableDevices()
    {
        try
        {
            WindowsMicrophoneMuteLibrary.WindowsMicMute micMute = new WindowsMicMute();
            micMute.UnMuteMic();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

        List<string> keysToRemove = new List<string>();
        foreach (KeyValuePair<string, string> kvp in this.currentlyDisabledDeviceDictionary)
        {
            string uniqueID = this.GetUniqueID(kvp.Key);
            if (uniqueID.Length == 0)
            {
                throw new Exception("Unable to enable device, a unique id could not be found.");
            }
            this.devcon.StartInfo.Arguments = @"enable """ + uniqueID + @"""";
            this.devcon.Start();
            while (!this.devcon.StandardOutput.EndOfStream)
            {
                string line = this.devcon.StandardOutput.ReadLine();
                Debug.WriteLine(line);
                if (line.Contains(kvp.Key + ": Enabled"))
                {
                    Debug.WriteLine("The device has been renabled. " + uniqueID);
                    keysToRemove.Add(kvp.Key);
                    
                }
                else
                {
                    Debug.WriteLine("Unable to enable device : " + uniqueID);
                }
            }

        }

        if (keysToRemove.Count > 0)
        {
            foreach (string k in keysToRemove)
            {
                this.currentlyDisabledDeviceDictionary.Remove(k);
            }
        }

    }

    public void EnableDevicesSimple()
    {
        try
        {
            WindowsMicrophoneMuteLibrary.WindowsMicMute micMute = new WindowsMicMute();
            micMute.UnMuteMic();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

        this.devcon.StartInfo.Arguments = "enable =Image";
        this.devcon.Start();
        while (!this.devcon.StandardOutput.EndOfStream)
        {
            string line = this.devcon.StandardOutput.ReadLine();
            Debug.WriteLine(line);
        }
        this.devcon.StartInfo.Arguments = "enable =MEDIA";
        this.devcon.Start();
        while (!this.devcon.StandardOutput.EndOfStream)
        {
            string line = this.devcon.StandardOutput.ReadLine();
            Debug.WriteLine(line);
        }

    }
    public void DisableDevicesSimple()
    {
        // Mute the microphone.
        try
        {
            WindowsMicrophoneMuteLibrary.WindowsMicMute micMute = new WindowsMicMute();
            micMute.MuteMic();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Couldn't mute mic " + ex.Message);
        }
        this.devcon.StartInfo.Arguments = "disable =Image";
        this.devcon.Start();
        while (!this.devcon.StandardOutput.EndOfStream)
        {
            string line = this.devcon.StandardOutput.ReadLine();
            Debug.WriteLine(line);
        }
        this.devcon.StartInfo.Arguments = "disable =MEDIA";
        this.devcon.Start();
        while (!this.devcon.StandardOutput.EndOfStream)
        {
            string line = this.devcon.StandardOutput.ReadLine();
            Debug.WriteLine(line);
        }
        
    }
    public void DisableDevices(List<string> deviceNames = null)
    {
        // Mute the microphone.
        try
        {
            WindowsMicrophoneMuteLibrary.WindowsMicMute micMute = new WindowsMicMute();
            micMute.MuteMic();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Couldn't mute mic " + ex.Message);
        }
        if (deviceNames == null)
        {
            AddDefaultDevicesToDisable();
        }
        else
        {
            KeyValuePair<string, string> deviceTemp;
            foreach (string name in deviceNames)
            {
                deviceTemp = FindDeviceByString(name);
                if (!deviceTemp.Equals(default(KeyValuePair<string, string>)))
                {
                    AddDeviceToDisable(deviceTemp);
                }
            }
        }
        // Loops through the list of devices to disable, and disable with devcon.
        foreach (string deviceID in DisabledDeviceIDsToList())
        {
            Debug.WriteLine("Attempting to disable " + deviceID);
            if (DisableDeviceByString(deviceID))
            {
                Debug.WriteLine("Device disabled : " + deviceID);
                currentlyDisabledDeviceDictionary.Add(deviceID, FindDeviceNameByString(deviceID));
            }
            else
            {
                Debug.WriteLine("Unable to disable device by string : " + deviceID);
            }
        }


    }

}
