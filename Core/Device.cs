using MaterialDesignThemes.Wpf;
using System;
using True_Mining_v4.Janelas;

namespace True_Mining_v4.Core
{
    public class Device
    {
        public static DeviceInfo cpu = new DeviceInfo("cpu", "x64 CPU(s)", "RandomX", (bool)User.Settings.Device.cpu.MiningSelected, -1, PackIconKind.Cpu64Bit);
        public static DeviceInfo opencl = new DeviceInfo("opencl", "AMD GPU(s)", "RandomX", (bool)User.Settings.Device.opencl.MiningSelected, -1, PackIconKind.Gpu);
        public static DeviceInfo cuda = new DeviceInfo("cuda", "NVIDIA GPU(s)", "RandomX", (bool)User.Settings.Device.cuda.MiningSelected, -1, PackIconKind.Gpu);

        public Device()
        {
            cpu.PropertieChanged += new EventHandler(cpuChanged);
            cuda.PropertieChanged += new EventHandler(cudaChanged);
            opencl.PropertieChanged += new EventHandler(openclChanged);
        }

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