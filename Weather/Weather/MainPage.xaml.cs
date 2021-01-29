using FFImageLoading.Forms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Weather.Helper;
using Weather.Models;
using Xamarin.Forms;

namespace Weather
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            Rotate_wind_mill_deg.Rotation = 0;
            GetWeather();
            Device.StartTimer(TimeSpan.FromSeconds(1.0/(20*Wind_mill_speed)), () =>
            {
                Rotate_wind_mill_deg.Rotation = (Rotate_wind_mill_deg.Rotation + 0.3) % 360;
                return true;
            });
        } 
        public string city{ get; set; } = "Astana";
        public string Wind_degree { get; set; } = "";
        public double Wind_mill_speed { get; set; } = 1;
        private async Task<bool> GetWeather()
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&APPID=23d6fed02b2ea14fcdcdab93be3632fa&lang=ru&units=metric";
            var result = await ApiCaller.Get(url);

            if (result.Successful)
            {
                try
                {
                    var weatherInfo = JsonConvert.DeserializeObject<WeatherInfo>(result.Response);  // парсим json
                    Description_Weather.Text = weatherInfo.weather[0].description.ToUpper();  // описание погоды
                    City_Name.Text = weatherInfo.name.ToUpper();                        // город
                    Current_Temperature.Text = weatherInfo.main.temp.ToString("0");    // температура
                    Current_Humidity.Text = $"{weatherInfo.main.humidity}%";   // влажность
                    Temperature_Min.Text = weatherInfo.main.temp_min.ToString("0");         // минимальная температура
                    Temperature_Max.Text = weatherInfo.main.temp_min.ToString("0");          // максимальная температура
                    Wind_speed.Text = $"{weatherInfo.wind.speed} m/s";        // скорость ветра
                    Wind_degree = $"{weatherInfo.wind.deg}";
                    //GetForecast();
                    Wind_Direction.Text = GetDirection(Wind_degree);
                    Wind_degree_dir.Rotation = Get_degree(Wind_degree);
                    Wind_mill_speed = weatherInfo.wind.speed;
                    return true;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Weather Info", ex.Message, "OK");
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
        private void ToolbarItem_kek(object sender, EventArgs e)
        {

        }
    }
}  
