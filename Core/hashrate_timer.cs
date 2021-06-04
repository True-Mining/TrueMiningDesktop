using System;
using TrueMiningDesktop.Janelas;

namespace TrueMiningDesktop.Core
{
    internal class Hashrate_timer
    {
        private DeviceInfo DeviceInfo;
        private System.Timers.Timer Timer = new System.Timers.Timer(5000);

        public event EventHandler HashrateUpdated;

        private decimal hashrate;
        public decimal Hashrate { get { return hashrate; } set { hashrate = value; OnNewHashrate(); } }

        public Hashrate_timer(DeviceInfo _parent)
        {
            this.DeviceInfo = _parent;
            Timer.Stop();
            Timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.Hashrate = Miner.GetHashrate(DeviceInfo.Alias, DeviceInfo.MiningAlgo);
        }

        protected virtual void OnNewHashrate()
        {
            HashrateUpdated?.Invoke(this, null);
        }

        public void Start()
        {
            Timer.Start();
        }

        public void Stop()
        {
            Timer.Stop(); Hashrate = -1;
        }
    }
}