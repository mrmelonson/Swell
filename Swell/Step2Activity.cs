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
    [Activity(Label = "Step2Activity")]
    public class Step2Activity : Activity
    {
        DigitalOcean.API.Models.Requests.Droplet createdrop = new DigitalOcean.API.Models.Requests.Droplet();

        public class DisplayedDrops
        {
            public int dropindex { get; set; }
            public int radioid { get; set; }
            public string slug { get; set; }
            public string name { get; set; }
        }
        private CancellationTokenSource cts;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Step2);

            createdrop.Name = Intent.Extras.GetString("dropletName");
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
            string[,] sizesSlugsArr = { { "s-1vcpu-1gb", "1 CPU, 1gb Memory, 25gb Disk, 1TB Transfer -- 5$/month (0.007$/hour)" },
                                     { "s-1vcpu-2gb", "1 CPU, 2gb Memory, 50gb Disk, 2TB Transfer -- 10$/month (0.015$/hour)"},
                                     { "s-1vcpu-3gb", "1 CPU, 3gb Memory, 60gb Disk, 3TB Transfer -- 15$/month (0.022$/hour)"},
                                     { "s-2vcpu-2gb", "2 CPUs, 2gb Memory, 60gb Disk, 3TB Transfer -- 15$/month (0.022$/hour)"},
                                     { "s-3vcpu-1gb", "3 CPUs, 1gb Memory, 60gb Disk, 3TB Transfer -- 15$/month (0.022$/hour)"},
                                     { "s-2vcpu-4gb", "2 CPUs, 4gb Memory, 80gb Disk, 4TB Transfer -- 20$/month (0.022$/hour)"},
                                   };

            

            var currentdrops = new List<DisplayedDrops>();
            var regionsslug = new List<DisplayedDrops>();
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var api_key = prefs.GetString("api_key", null);

            DigitalOceanClient client = new DigitalOceanClient(api_key);

            RadioGroup rgp = FindViewById<RadioGroup>(Resource.Id.radiogroupplan);
            RadioGroup rgr = FindViewById<RadioGroup>(Resource.Id.radiogroupregion);

            var dropletsizes = await client.Sizes.GetAll();
            var regions = await client.Regions.GetAll();


            for (int i = 0; i < dropletsizes.Count; i++)
            {
                for (int x = 0; x < sizesSlugsArr.GetLength(0); x++)
                {
                    if (dropletsizes[i].Slug == sizesSlugsArr[x, 0] /*&& dropletsizes[i].Available*/)
                    {
                        RadioButton rdbtn = new RadioButton(this);
                        rdbtn.Text = sizesSlugsArr[x, 1];
                        rdbtn.Id = View.GenerateViewId();
                        var toadd = new DisplayedDrops();
                        toadd.dropindex = i;
                        toadd.radioid = rdbtn.Id;
                        toadd.slug = sizesSlugsArr[x, 0];
                        currentdrops.Add(toadd);
                        rgp.AddView(rdbtn);
                    }
                }
                Console.WriteLine(dropletsizes[i].Slug);
            }

            for (int i = 0; i < regions.Count; i++)
            {
                if (regions[i].Available == true)
                {
                    var x = new DisplayedDrops();
                    x.name = regions[i].Name;
                    x.slug = regions[i].Slug;
                    regionsslug.Add(x);
                }
            }

            regionsslug.Sort(new Comparison<DisplayedDrops>((x, y) => string.Compare(x.name, y.name)));

            for (int i = 0; i < regionsslug.Count; i++)
            {
                RadioButton rdbtn = new RadioButton(this);
                rdbtn.Text = regionsslug[i].name;
                rdbtn.Id = View.GenerateViewId();
                regionsslug[i].radioid = rdbtn.Id;
                rgr.AddView(rdbtn);
            }



            RadioButton checkedrgp = FindViewById<RadioButton>(rgp.CheckedRadioButtonId);
            RadioButton checkedrgr = FindViewById<RadioButton>(rgr.CheckedRadioButtonId);

            rgp.CheckedChange += (o, e) =>
            {
                checkedrgp = FindViewById<RadioButton>(rgp.CheckedRadioButtonId);
                for (int i = 0; i < currentdrops.Count; i++)
                {
                    if(currentdrops[i].radioid == checkedrgp.Id)
                    {
                        createdrop.SizeSlug = currentdrops[i].slug;
                        break;
                    }
                }
            };

            rgr.CheckedChange += (o, e) =>
            {
                checkedrgr = FindViewById<RadioButton>(rgr.CheckedRadioButtonId);
                for (int i = 0; i < regionsslug.Count; i++)
                {
                    if (regionsslug[i].radioid == checkedrgp.Id)
                    {
                        createdrop.SizeSlug = regionsslug[i].slug;
                        break;
                    }
                }
            };

            Button next = FindViewById<Button>(Resource.Id.NextS2);
            Button back = FindViewById<Button>(Resource.Id.BackS2);

            back.Click += (o, e) => 
            {
                var intent = new Intent(this, typeof(Step1Activity));
                intent.PutExtra("DropletName", createdrop.Name);
                intent.PutExtra("DropletDistro", createdrop.ImageIdOrSlug.ToString());
                StartActivity(intent);
            };

            next.Click += (o, e) =>
            {
                if (checkedrgp == null)
                {
                    Toast.MakeText(this, "Please plan", ToastLength.Short).Show();
                    return;
                }
                if (checkedrgr == null)
                {
                    Toast.MakeText(this, "Please select region", ToastLength.Short).Show();
                    return;
                }

                var intent = new Intent(this, typeof(Step3Activity));
                intent.PutExtra("DropletName", createdrop.Name);
                intent.PutExtra("DropletDistro", createdrop.ImageIdOrSlug.ToString());
                intent.PutExtra("DropletRegion", createdrop.RegionSlug);
                intent.PutExtra("DropletSize", createdrop.SizeSlug);
                StartActivity(intent);

            };

        }

    }
}