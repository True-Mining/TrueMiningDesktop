using MaterialDesignThemes.Wpf;
using System;
using TrueMiningDesktop.Janelas;

namespace TrueMiningDesktop.Core
{
    public class Device
    {
        public static DeviceInfo Cpu = new DeviceInfo("cpu", "x64 CPU(s)", User.Settings.Device.cpu.Algorithm, (bool)User.Settings.Device.cpu.MiningSelected, -1, PackIconKind.Cpu64Bit);
        public static DeviceInfo Opencl = new DeviceInfo("opencl", "AMD GPU(s)", User.Settings.Device.opencl.Algorithm, (bool)User.Settings.Device.opencl.MiningSelected, -1, PackIconKind.Gpu);
        public static DeviceInfo Cuda = new DeviceInfo("cuda", "NVIDIA GPU(s)", User.Settings.Device.cuda.Algorithm, (bool)User.Settings.Device.cuda.MiningSelected, -1, PackIconKind.Gpu);

        public Device()
        {
            Cpu.PropertieChanged += new EventHandler(cpuChanged);
            Cuda.PropertieChanged += new EventHandler(cudaChanged);
            Opencl.PropertieChanged += new EventHandler(openclChanged);
        }

        public static System.Collections.Generic.List<DeviceInfo> DevicesList = new System.Collections.Generic.List<DeviceInfo>() { Cpu, Opencl, Cuda };

        private void openclChanged(object sender, EventArgs e)
        {
            User.Settings.Device.opencl.MiningSelected = Opencl.IsSelected;
        }

        private void cudaChanged(object sender, EventArgs e)
        {
            User.Settings.Device.cuda.MiningSelected = Cuda.IsSelected;
        }

        private void cpuChanged(object sender, EventArgs e)
        {
            User.Settings.Device.cpu.MiningSelected = Cpu.IsSelected;
        }
    }
}