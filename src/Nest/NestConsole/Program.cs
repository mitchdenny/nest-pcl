﻿using Nest;
using NestConsole.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NestConsole
{
    public class Program
    {
        private static async Task<string> GetAccessTokenAsync(string authorizationCode) {
            var clientID = Settings.Default.ClientID;
            var clientSecret = Settings.Default.ClientSecret;

            var requestUrl = string.Format(
                "https://api.home.nest.com/oauth2/access_token?code={0}&client_id={1}&client_secret={2}&grant_type=authorization_code",
                authorizationCode,
                clientID,
                clientSecret
                );

            var client = new HttpClient();
            var response = await client.PostAsync(requestUrl, null);
            var payloadString = await response.Content.ReadAsStringAsync();
            var payload = JsonConvert.DeserializeAnonymousType(
                payloadString,
                new { access_token = string.Empty }
                );
  
            return payload.access_token;
        }

        [STAThread()]
        public static void Main(string[] args)
        {
            var accessToken = Program.GetAccessToken();
            var client = new NestClient(accessToken);

            var thermostats = Task.Run(() => client.GetThermostatsAsync()).Result;
            var smokeAlarms = Task.Run(() => client.GetSmokeAlarmsAsync()).Result;
            var structures = Task.Run(() => client.GetStructuresAsync()).Result;

            foreach (var item in thermostats)
            {
                var thermostat = Task.Run(() => client.GetThermostatAsync(item.Key).Result).Result;
                Console.WriteLine("Thermostat: {0}", thermostat.DeviceID);
            }

            foreach (var item in smokeAlarms)
            {
                var smokeAlarm = Task.Run(() => client.GetSmokeAlarmAsync(item.Key).Result).Result;
                Console.WriteLine("Smoke Alarm: {0}", smokeAlarm.DeviceID);
            }

            foreach (var item in structures)
            {
                var structure = Task.Run(() => client.GetStructureAsync(item.Key).Result).Result;
                Console.WriteLine("Structure: {0}", structure.StructureID);
            }
        }

        private static string GetAccessToken()
        {
            var cachedAccessToken = Settings.Default.CachedAccessToken;

            if (string.IsNullOrEmpty(cachedAccessToken))
            {
                var clientID = Settings.Default.ClientID;
                var clientSecret = Settings.Default.ClientSecret;


                var prompter = new PrompterForm(clientID, clientSecret);
                var result = prompter.ShowDialog();

                if (result == DialogResult.OK)
                {
                    var authorizationCode = prompter.AuthorizationCode;
                    var accessToken = Task.Run(() => Program.GetAccessTokenAsync(authorizationCode)).Result;
                    Settings.Default.CachedAccessToken = accessToken;
                    Settings.Default.Save();
                    return accessToken;
                }
                else
                {
                    throw new InvalidOperationException("Failed to authenticate.");
                }
            }
            else
            {
                return cachedAccessToken;
            }
        }
    }
}
