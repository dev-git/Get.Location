using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Locations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;

namespace GetLocation
{
    [Activity(Label = "GetLocation", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, ILocationListener
    {
        // http://developer.xamarin.com/recipes/android/os_device_resources/gps/get_current_device_location/

        Location _currentLocation;
        LocationManager _locationManager;
        TextView _locationText;
        TextView _addressText;
        TextView _MACAddressText;
        TextView _batteryLevelText;
        String _locationProvider;
        int _batteryLevel = 0;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            _addressText = FindViewById<TextView>(Resource.Id.address_text);
            _locationText = FindViewById<TextView>(Resource.Id.location_text);
            _MACAddressText = FindViewById<TextView>(Resource.Id.MACAddress_text);
            _batteryLevelText = FindViewById<TextView>(Resource.Id.batterylevel_text);
            FindViewById<TextView>(Resource.Id.get_address_button).Click += AddressButton_OnClick;

            InitializeLocationManager();
        }

        private void InitializeLocationManager()
        {
            _locationManager = (LocationManager)GetSystemService(LocationService);
            Criteria criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };

            IList<string> acceptableLocationProviders = _locationManager.GetProviders(criteriaForLocationService, true);

            if (acceptableLocationProviders.Any())
            {
                _locationProvider = acceptableLocationProviders.First();
            }
            else
            {
                _locationProvider = String.Empty;
            }
        }

        async void AddressButton_OnClick(object sender, EventArgs eventArgs)
        {
            String macAddress = GetMACAddress();
            _MACAddressText.Text = macAddress;

            int batteryLevel = GetBatteryLevel();
            _batteryLevelText.Text = batteryLevel.ToString();


            //PostData(macAddress, -3.1234, 0.98756, batteryLevel);

            if (_currentLocation == null)
            {
                _addressText.Text = "Can't determine the current address. Try again in a few minutes.";
                return;
            }

            PostData(macAddress.Replace(':', '-'), _currentLocation.Latitude, _currentLocation.Longitude, batteryLevel);

            Address address = await ReverseGeocodeCurrentLocation();
            DisplayAddress(address);
            
        }

        private async void PostData(string macAddress, double latitude, double longitude, int batteryLevel)
        {
            // url: 'http://ibrium.webhop.me/plog/api/pLog/D2-A0-F1-00/-3.1254/0.34534/25'
            // http://maps.google.co.nz/maps?q=-36.85538833,174.77183783

            using (var client = new HttpClient())
            {
                //var content = new FormUrlEncodedContent(values);
                var content = new StringContent(macAddress);
                var url = String.Format("http://ibrium.webhop.me/plog/api/pLog/{0}/{1}/{2}/{3}", macAddress, latitude, longitude, batteryLevel);

                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();
            }
        }

        async System.Threading.Tasks.Task<Address> ReverseGeocodeCurrentLocation()
        {
            Geocoder geocoder = new Geocoder(this); 
            IList<Address> addressList = await geocoder.GetFromLocationAsync(_currentLocation.Latitude, _currentLocation.Longitude, 10);

            Address address = addressList.FirstOrDefault();
            return address;
        }

        void DisplayAddress(Address address)
        {
            if (address != null)
            {
                StringBuilder deviceAddress = new StringBuilder();
                for (int i = 0; i < address.MaxAddressLineIndex; i++)
                {
                    deviceAddress.AppendLine(address.GetAddressLine(i));
                }
                // Remove the last comma from the end of the address.
                _addressText.Text = deviceAddress.ToString();
            }
            else
            {
                _addressText.Text = "Unable to determine the address. Try again in a few minutes.";
            }
        }

        private int GetBatteryLevel()
        {
            // Get battery level
            var filter = new IntentFilter(Intent.ActionBatteryChanged);
            var battery = RegisterReceiver(null, filter);
            int level = battery.GetIntExtra(BatteryManager.ExtraLevel, -1);
            int scale = battery.GetIntExtra(BatteryManager.ExtraScale, -1);

            _batteryLevel = (int)Math.Floor(level * 100D / scale);

            return _batteryLevel;

        }

        private String GetMACAddress()
        {
            // http://www.technetexperts.com/mobile/getting-unique-device-id-of-an-android-smartphone/
            String macAddress = "00-00-00-00-00-01";

            try
            {
                //WLAN MAC Address              
                Android.Net.Wifi.WifiManager wifiManager = (Android.Net.Wifi.WifiManager)GetSystemService(Android.Content.Context.WifiService);

                macAddress = wifiManager.ConnectionInfo.MacAddress;
                if (String.IsNullOrEmpty(macAddress))
                {
                    macAddress = "00-00-00-00-01-A1";
                }

            }
            catch (Exception ex)
            {
                macAddress = "00-00-00-00-00-A1";
            }

            return macAddress;
        }

        public void OnLocationChanged(Location location)
        {
            _currentLocation = location;
            if (_currentLocation == null)
            {
                _locationText.Text = "Unable to determine your location.";
            }
            else
            {
                _locationText.Text = String.Format("{0},{1}", _currentLocation.Latitude, _currentLocation.Longitude);
            }
        }

        public void OnProviderDisabled(string provider)
        {
            throw new NotImplementedException();
        }

        public void OnProviderEnabled(string provider)
        {
            throw new NotImplementedException();
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            throw new NotImplementedException();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _locationManager.RemoveUpdates(this);
        }
    }
}

