using MaterialDesignThemes.Wpf;
using System;
using TrueMiningDesktop.Janelas;

namespace TrueMiningDesktop.Core
{
    public class Device
    {
        public static DeviceInfo cpu = new DeviceInfo("cpu", "x64 CPU(s)", User.Settings.Device.cpu.Algorithm, (bool)User.Settings.Device.cpu.MiningSelected, -1, PackIconKind.Cpu64Bit);
        public static DeviceInfo opencl = new DeviceInfo("opencl", "AMD GPU(s)", User.Settings.Device.opencl.Algorithm, (bool)User.Settings.Device.opencl.MiningSelected, -1, PackIconKind.Gpu);
        public static DeviceInfo cuda = new DeviceInfo("cuda", "NVIDIA GPU(s)", User.Settings.Device.cuda.Algorithm, (bool)User.Settings.Device.cuda.MiningSelected, -1, PackIconKind.Gpu);

        public Device()
        {
            cpu.PropertieChanged += new EventHandler(cpuChanged);
            cuda.PropertieChanged += new EventHandler(cudaChanged);
            opencl.PropertieChanged += new EventHandler(openclChanged);
        }

        public static System.Collections.Generic.List<DeviceInfo> DevicesList = new System.Collections.Generic.List<DeviceInfo>() { cpu, opencl, cuda };

        private void openclChanged(object sender, EventArgs e)
        {
            User.Settings.Device.opencl.MiningSelected = opencl.IsSelected;
        }

        private void cudaChanged(object sender, EventArgs e)
        {
            User.Settings.Device.cuda.MiningSelected = cuda.IsSelected;
        }

        private void cpuChanged(object sender, EventArgs e)
        {
            User.Settings.Device.cpu.MiningSelected = cpu.IsSelected;
        }
    }
}