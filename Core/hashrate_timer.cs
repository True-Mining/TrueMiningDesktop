using System;
using TrueMiningDesktop.Janelas;

namespace TrueMiningDesktop.Core
{
    internal class Hashrate_timer
    {
        private readonly DeviceInfo DeviceInfo;
        private readonly System.Timers.Timer Timer = new(5000);

        public event EventHandler HashrateUpdated;

        private decimal hashrate;

        public decimal Hashrate
        { get { return hashrate; } set { hashrate = value; OnNewHashrate(); } }

        public Hashrate_timer(DeviceInfo _parent)
        {
            DeviceInfo = _parent;
            Timer.Stop();
            Timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Hashrate = Miner.GetHashrate(DeviceInfo.BackendName, DeviceInfo.MiningAlgo);
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