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
using Swell.Resources.Fragments;
using Android.Support.V7.App;

namespace Swell.Main
{
    [Activity(Label = "Create Droplet - Step 3")]
    public class Step3Activity : AppCompatActivity
    {
        private Loading_Fragment _Loading_Fragment;
        private Step3_Fragment _Step3_Fragment;
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
            _Loading_Fragment = new Loading_Fragment();
            _Step3_Fragment = new Step3_Fragment();



            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var api_key = prefs.GetString("api_key", null);

            var trans = SupportFragmentManager.BeginTransaction();
            trans.Add(Resource.Id.Step3_Frame, _Step3_Fragment, "Fragment");
            trans.Add(Resource.Id.Step3_Frame, _Loading_Fragment, "Fragment");
            trans.Hide(_Step3_Fragment);
            trans.Show(_Loading_Fragment);
            trans.Commit();

            DigitalOceanClient client = new DigitalOceanClient(api_key);
            var keys = await client.Keys.GetAll();

            trans = SupportFragmentManager.BeginTransaction();
            trans.Hide(_Loading_Fragment);
            trans.Show(_Step3_Fragment);
            trans.Commit();

            List<sshkey> sshkeys = new List<sshkey>();

            RadioGroup lls = FindViewById<RadioGroup>(Resource.Id.sshkeygroup);

            for(int i = 0; i < keys.Count; i++)
            {
                CheckBox cb = new CheckBox(this);
                cb.Text = keys[i].Name;
                cb.Id = View.GenerateViewId();
                sshkey x = new sshkey();
                x.Id = keys[i].Id;
                x.Name = keys[i].Name;
                x.radioid = cb.Id;
                sshkeys.Add(x);
                cb.Click += delegate (object sender, EventArgs e) { CheckBoxOnClick(cb, sshkeys);  };
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
            Button back = FindViewById<Button>(Resource.Id.BackS3);

            EditText tagsedit = FindViewById<EditText>(Resource.Id.Tags);
                    

            back.Click += (o, e) =>
            {
                var intent = new Intent(this, typeof(Step2Activity));
                intent.PutExtra("DropletName", createdrop.Name);
                intent.PutExtra("DropletDistro", Intent.Extras.GetString("DropletDistro"));
                StartActivity(intent);
            };

            next.Click += async (o, e) =>
            {
                for(int i = 0; i < sshkeys.Count; i++)
                {
                    if(sshkeys[i].selected)
                    {
                        createdrop.SshIdsOrFingerprints.Add(sshkeys[i].Id);
                    }
                }
                createdrop.UserData = null;
                string[] tags = tagsedit.Text.Split(",");
                if (tags.Length > 0)
                {
                    for (int i = 0; i < tags.Length; i++)
                    {
                        if (tags[i] != "" && tags[i] != null)
                        {
                            createdrop.Tags.Add(tags[i]);
                        }
                    }
                }

                trans = SupportFragmentManager.BeginTransaction();
                trans.Show(_Loading_Fragment);
                trans.Hide(_Step3_Fragment);
                trans.Commit();

                var creationAction = await client.Droplets.Create(createdrop);

                trans = SupportFragmentManager.BeginTransaction();
                trans.Hide(_Loading_Fragment);
                trans.Show(_Step3_Fragment);
                trans.Commit();

                var intent = new Intent(this, typeof(MainActivity));
                StartActivity(intent);
            };
        }

        private void CheckBoxOnClick(CheckBox cb, List<sshkey> sshkeys)
        {
            for (int i = 0; i < sshkeys.Count; i++)
            {
                if (cb.Id == sshkeys[i].radioid)
                {
                    if (cb.Checked)
                    {
                        cb.Selected = true;
                    }
                    else
                    {
                        cb.Selected = false;
                    }
                }
            }
            return;
        }

    }
}