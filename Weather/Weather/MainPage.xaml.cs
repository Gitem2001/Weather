using FFImageLoading.Forms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Weather.Helper;
using Weather.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Weather
{
    public partial class MainPage : MasterDetailPage
    {
        public MainPage()
        {
            InitializeComponent();
            HDisplay = new ObservableCollection<MyDisplayInfo>();
            DDisplay = new ObservableCollection<MyDisplayInfo>();
            cities = new ObservableCollection<string>();
            DoOneCall();
            list.ItemsSource = cities;
            GetCitiesList();
        }
        private OneCall WeatherInfo;
        private ObservableCollection<MyDisplayInfo> HDisplay { get; set; }
        private ObservableCollection<MyDisplayInfo> DDisplay { get; set; }
        private ObservableCollection<string> cities { get; set; }

        private async Task<bool> GetCity(double lat, double lon)
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid=23d6fed02b2ea14fcdcdab93be3632fa";
            var result = await ApiCaller.Get(url);

            if (result.Successful)
            {
                try
                {
                    var City = JsonConvert.DeserializeObject<CityInfo>(result.Response);
                    City_Name.Text = City.name;
                    return true;
                }
                catch(Exception ex)
                {
                    return false;
                }
            }
            return false;
        }

        private async Task<bool> GetCity(string city)
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid=23d6fed02b2ea14fcdcdab93be3632fa";
            var result = await ApiCaller.Get(url);

            if (result.Successful)
            {
                try
                {
                    var City = JsonConvert.DeserializeObject<CityInfo>(result.Response);
                    City_Name.Text = City.name;
                    if (!cities.Contains(city))
                    {
                        cities.Add(city);
                        App.Current.Properties.Remove("cities");
                        App.Current.Properties.Add("cities", JsonConvert.SerializeObject(cities));
                        await App.Current.SavePropertiesAsync();
                    }
                    await DoOneCall(City.coord.lat, City.coord.lon);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            return false;
        }

        private async Task<bool> DoOneCall(double lat = 43.10562, double lon = 131.87353)
        {
            var url = $"https://api.openweathermap.org/data/2.5/onecall?lat={lat}&lon={lon}&appid=23d6fed02b2ea14fcdcdab93be3632fa&exclude=minutely,alerts&lang=ru&units=metric";
            var result = await ApiCaller.Get(url);

            if (result.Successful)
            {
                try
                {
                    // convertin' JSON
                    WeatherInfo = JsonConvert.DeserializeObject<OneCall>(result.Response);

                    // 1st block
                    await GetCity(lat, lon);
                    Current_Temperature.Text = WeatherInfo.current.temp.ToString().Replace(',', '.') + '°';    // температура
                    Temperature_Min.Text = WeatherInfo.daily[0].temp.min.ToString().Replace(',', '.') + '°';        // минимальная температура
                    Temperature_Max.Text = WeatherInfo.daily[0].temp.max.ToString().Replace(',', '.') + '°';       // максимальная температура
                    Description_Weather.Text = WeatherInfo.current.weather[0].description.ToUpper();  // описание погоды

                    // 2nd block
                    HDisplay.Clear();
                    var offset = WeatherInfo.timezone_offset;
                    for(int i = 0; i < WeatherInfo.hourly.Count; i++)
                    {
                        var hour_ = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToUniversalTime().AddSeconds(WeatherInfo.hourly[i].dt);
                        if (DateTime.UtcNow.AddSeconds(offset) < hour_&& hour_ < DateTime.UtcNow.AddDays(1).AddSeconds(offset))
                        {
                            HDisplay.Add(new MyDisplayInfo
                            {
                                data = hour_.ToString("HH:mm"),
                                path = $"https://openweathermap.org/img/wn/{WeatherInfo.hourly[i].weather[0].icon}@2x.png",
                                temp = WeatherInfo.hourly[i].temp.ToString().Replace(',', '.') + '°'
                            });
                        }
                    }
                    BindableLayout.SetItemsSource(hourly, HDisplay);

                    // 3rd block
                    DDisplay.Clear();
                    for (int i=0;i< WeatherInfo.daily.Count; i++)
                    {
                        var date_ = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToUniversalTime().AddSeconds(WeatherInfo.daily[i].dt);
                        DDisplay.Add(new MyDisplayInfo
                        {
                            data = date_.ToLongDateString(),
                            path = $"https://openweathermap.org/img/wn/{WeatherInfo.daily[i].weather[0].icon}@2x.png",
                            temp = (WeatherInfo.daily[i].temp.min.ToString() +  " / " + WeatherInfo.daily[i].temp.max.ToString() + '°').Replace(",",".")
                        });
                    }
                    BindableLayout.SetItemsSource(daily, DDisplay);
                    //4th block
                    Current_Humidity.Text = WeatherInfo.current.humidity.ToString();   // влажность

                    // 5th block
                    Wind_speed.Text = $"{WeatherInfo.current.wind_speed} m/s";        // скорость ветра
                    Wind_Direction.Text = GetDirection(WeatherInfo.current.wind_deg.ToString());
                    Wind_degree_dir.Rotation = Get_degree(WeatherInfo.current.wind_deg.ToString());
                    Device.StartTimer(TimeSpan.FromSeconds(1.0 / (20 * WeatherInfo.current.wind_speed)), () =>
                    {
                        Rotate_wind_mill_deg.Rotation = (Rotate_wind_mill_deg.Rotation + 0.3) % 360;
                        return true;
                    });

                    // return
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            else
            {
                await DisplayAlert("Weather Info", "No weather information found", "OK");
                return false;
            }

        }

        private string GetDirection(string buf)
        {
            var degree = float.Parse(buf);
            if ((degree >= 337.5 && degree < 360) || (degree >= 0 && degree < 22.5)) { return "С"; }
            else if (degree >= 22.5 && degree < 67.5) { return "С-В"; }
            else if (degree >= 67.5 && degree < 112.5) { return "В"; }
            else if (degree >= 122.5 && degree < 157.5) { return "Ю-В"; }
            else if (degree >= 157.5 && degree < 202.5) { return "Ю"; }
            else if (degree >= 202.5 && degree < 247.5) { return "Ю-З"; }
            else if (degree >= 247.5 && degree < 292.5) { return "З"; }
            else if (degree >= 292.5 && degree < 337.5) { return "С-З"; }
            return "Невозможно определить";
        }

        private double Get_degree(string buf)
        {
            return double.Parse(buf)-80;
        }

        private async void src_Clicked(object sender, EventArgs e)
        {
            await GetCity(editor.Text);
        }

        private void geo_Clicked(object sender, EventArgs e)
        {
            GetByGeo();
        }

        private async void GetByGeo()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best);
                var Location = await Geolocation.GetLocationAsync(request);

                if (Location != null)
                {
                    await DoOneCall(Location.Latitude, Location.Longitude);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void list_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            editor.Text = list.SelectedItem.ToString();
        }

        private async void GetCitiesList()
        {
            if (!App.Current.Properties.ContainsKey("cities"))
            {
                //cities.Add("Владивосток");
                App.Current.Properties.Add("cities", JsonConvert.SerializeObject(cities));
                await App.Current.SavePropertiesAsync();
            }
            else
            {
                cities.Clear();
                foreach(var city in JsonConvert.DeserializeObject<ObservableCollection<string>>(App.Current.Properties["cities"].ToString()))
                {
                    cities.Add(city);
                }
            }
        }
    }
}  
