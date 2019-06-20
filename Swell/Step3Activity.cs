using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DigitalOcean.API;

namespace Swell.Main
{
    [Activity(Label = "Step3Activity")]
    public class Step3Activity : Activity
    {
        public class sshkey
        {
            public string Name { get; set; }
            public int radioid { get; set; }
            public int Id { get; set; }
            public bool selected {get; set; }
        }
        private CancellationTokenSource cts;
        DigitalOcean.API.Models.Requests.Droplet createdrop = new DigitalOcean.API.Models.Requests.Droplet();
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Step3);

            createdrop.Name = Intent.Extras.GetString("DropletName");
            createdrop.RegionSlug = Intent.Extras.GetString("DropletRegion");
            createdrop.SizeSlug = Intent.Extras.GetString("DropletSize");
            createdrop.ImageIdOrSlug = Intent.Extras.GetString("DropletDistro");
            StartUpdate();

        }

        public void StartUpdate()
        {
            if (cts != null) cts.Cancel();
            cts = new CancellationTokenSource();
            var ignore = UpdaterAsync(cts.Token);
        }

        public void StopUpdate() // To stop Updating
        {
            if (cts != null) cts.Cancel();
            cts = null;
        }


        public async Task UpdaterAsync(CancellationToken ct)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var api_key = prefs.GetString("api_key", null);

            List<sshkey> sshkeys = new List<sshkey>();

            DigitalOceanClient client = new DigitalOceanClient(api_key);

            RadioGroup lls = FindViewById<RadioGroup>(Resource.Id.sshkeygroup);

            var keys = await client.Keys.GetAll();

            for(int i = 0; i < keys.Count; i++)
            {
                CheckBox cb = new CheckBox(this);
                cb.Text = keys[i].Name;
                cb.Id = View.GenerateViewId();
                sshkey x = new sshkey();
                x.Id = keys[i].Id;
                x.Name = keys[i].Name;
                x.radioid = cb.Id;
                cb.SetOnClickListener(new );
                lls.AddView(cb);
            }

            createdrop.Backups = false;
            createdrop.Ipv6 = false;
            createdrop.Monitoring = false;
            createdrop.PrivateNetworking = false;

            CheckBox bcb = FindViewById<CheckBox>(Resource.Id.checkBoxbackup);
            CheckBox ipbcb = FindViewById<CheckBox>(Resource.Id.checkBoxipv6net);
            CheckBox moncb = FindViewById<CheckBox>(Resource.Id.checkBoxmonitor);
            CheckBox pvcb = FindViewById<CheckBox>(Resource.Id.checkBoxprivnet);

            

            bcb.CheckedChange += (o, e) =>
            {
                if (FindViewById<CheckBox>(Resource.Id.checkBoxbackup).Checked)
                {
                    createdrop.Backups = true;
                }
                else
                {
                    createdrop.Backups = false;
                }
            };

            ipbcb.CheckedChange += (o, e) => 
            {
                if (FindViewById<CheckBox>(Resource.Id.checkBoxipv6net).Checked)
                {
                    createdrop.Ipv6 = true;
                }
                else
                {
                    createdrop.Ipv6 = false;
                }
            };

            moncb.CheckedChange += (o, e) => 
            {
                if (FindViewById<CheckBox>(Resource.Id.checkBoxmonitor).Checked)
                {
                    createdrop.Monitoring = true;
                }
                else
                {
                    createdrop.Monitoring = false;
                }
            };

            pvcb.CheckedChange += (o, e) => 
            {
                if (FindViewById<CheckBox>(Resource.Id.checkBoxprivnet).Checked)
                {
                    createdrop.PrivateNetworking = true;
                }
                else
                {
                    createdrop.PrivateNetworking = false;
                }
            };

            Button next = FindViewById<Button>(Resource.Id.NextS3);

            next.Click += (o, e) =>
            {
                
                
            };
        }

        private void CheckBoxOnClick(View v)
        {

        }

    }
}