using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using ProcessSharePoint.Entities;
using IdentityModel.Client;
using System.Linq;
using System;
using System.Net;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Threading.Tasks;
using Microsoft.Graph.Communications.CallRecords.MicrosoftGraphCallRecordsGetDirectRoutingCallsWithFromDateTimeWithToDateTime;
using System.Runtime.InteropServices.WindowsRuntime;
using Azure;
using System.IO;
using Microsoft.Graph.Drives.Item.Items.Item.Invite;
using static Microsoft.Graph.Constants;
using System.Net.NetworkInformation;
using System.Net.Http.Headers;

namespace ProcessSharePoint
{
    public class Program
    {
        private static readonly IConfiguration _configuration;

        static Program()
        {
            var builder = new ConfigurationBuilder().AddNewtonsoftJsonFile($"appsettings.json", true, true);
            _configuration = builder.Build();
        }
        static async Task Main(string[] args)
        {
            var TokenEndpoint = _configuration["SharePointOnline:TokenEndpoint"];
            var ClientID = _configuration["SharePointOnline:client_id"];
            var ClientSecret = _configuration["SharePointOnline:client_secret"];
            var resource = _configuration["SharePointOnline:resource"];
            var GrantType = _configuration["SharePointOnline:grant_type"];
            var Tenant = _configuration["SharePointOnline:tenant"];
            var scope = _configuration["SharePointOnline:scope"];
            TokenEndpoint = string.Format(TokenEndpoint, Tenant);



            var keyValues = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", GrantType),
                new KeyValuePair<string, string>("client_id", ClientID),
                new KeyValuePair<string, string>("client_secret", ClientSecret),
                new KeyValuePair<string, string>("resource", resource),
                new KeyValuePair<string, string>("scope", scope),
                new KeyValuePair<string, string>("tenant", Tenant),
            };


            HttpContent content = new FormUrlEncodedContent(keyValues);

            var httpClient = new HttpClient();
            var response = httpClient.PostAsync(TokenEndpoint, content).Result;
            var token = response.Content.ReadAsStringAsync().Result;
            var accessToken = (JsonConvert.DeserializeObject<AccessToken>(token)).access_token;


            var SiteDataEndPoint = _configuration["SharePointOnline:SiteDataEndPoint"];

            httpClient.SetBearerToken(accessToken);
            response = httpClient.GetAsync(SiteDataEndPoint).Result;
            var siteData = response.Content.ReadAsStringAsync().Result;
            var sharepointSite = JsonConvert.DeserializeObject<SharePointSite>(siteData);


            var ListsEndPoint = _configuration["SharePointOnline:ListsEndPoint"];
            ListsEndPoint = string.Format(ListsEndPoint, sharepointSite.id);


            httpClient.SetBearerToken(accessToken);
            response = httpClient.GetAsync(ListsEndPoint).Result;
            var listData = response.Content.ReadAsStringAsync().Result;
            var sharePointList = JsonConvert.DeserializeObject<SharePointList>(listData);
            var listid = sharePointList.value.FirstOrDefault(obj => obj.displayName == "Documents").id;


            //var ListDataEndPoint = _configuration["SharePointOnline:ListDataByFilter"];
            //ListDataEndPoint = string.Format(ListDataEndPoint, sharepointSite.id, listid);
            //httpClient.SetBearerToken(accessToken);
            //response = httpClient.GetAsync(ListDataEndPoint).Result;

            var ListDataEndPoint = _configuration["SharePointOnline:ListDataByFilterLib"];
            ListDataEndPoint = string.Format(ListDataEndPoint, sharepointSite.id, listid);
            httpClient.SetBearerToken(accessToken);
            response = httpClient.GetAsync(ListDataEndPoint).Result;


            //Below logic is to handle TooManyRequests Error. We wait for seconds mentioned in Header with name "Retry-After" and try to call the endpoint again.
            int maxRetryCount = 3;
            int retriesCount = 0;

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                do
                {
                    // Determine the retry after value - use the "Retry-After" header
                    var retryAfterInterval = Int32.Parse(response.Headers.GetValues("Retry-After").FirstOrDefault());

                    //we get retryAfterInterval in seconds. We need to pass milliseconds to Thread.Sleep method, hence we multiply retryAfterInterval with 1000
                    System.Threading.Thread.Sleep(retryAfterInterval * 1000);
                    response = httpClient.GetAsync(ListDataEndPoint).Result;
                    retriesCount += 1;
                } while (response.StatusCode == HttpStatusCode.TooManyRequests && retriesCount <= maxRetryCount);
            }

            var ListData = response.Content.ReadAsStringAsync().Result;

            //https://graph.microsoft.com/v1.0/drives/{{DriveID}}/root:/ERP-CRM Enhancement (ERP Dev)/BC.png:/content
            //https://graph.microsoft.com/v1.0/sites/{{SiteID}} /Drives.
            //https://graph.microsoft.com/v1.0/sites/{site-id}/drives

            var GetDrives = _configuration["SharePointOnline:GetDrives"];
            GetDrives = string.Format(GetDrives, sharepointSite.id);


            httpClient.SetBearerToken(accessToken);
            response = httpClient.GetAsync(GetDrives).Result;
            var getDrives = response.Content.ReadAsStringAsync().Result;


            //https://graph.microsoft.com/v1.0/Drives/{{DriveID}}/root:/ERP-CRM Enhancement (ERP Dev):/Children.


            var path = "https://graph.microsoft.com/v1.0/Drives/b!TTTR5HPR00qKMbhd4Ua5kqnQZpNBJrJAg4NkBP-KwGwT1AmTV73TRqdNngV2bl_H/root:/123:/Children";

            httpClient.SetBearerToken(accessToken);
            response = httpClient.GetAsync(path).Result;
            var getChildFiles = response.Content.ReadAsStringAsync().Result;
            var finalJSON = JsonConvert.DeserializeObject<Downlaod>(getChildFiles);

            var FINALLINK = finalJSON.value.FirstOrDefault(obj => obj.name == "Passport_123.pdf").microsoftgraphdownloadUrl;

            //using (var client = new WebClient())
            //{
            //    client.DownloadFile(FINALLINK, "Passport_123.pdf");
            //}


            WebClient webClient = new WebClient();
            webClient.DownloadFile(FINALLINK, @"C:\\Download\\Passport_123.pdf");

            //var newFinalpath = "https://graph.microsoft.com/v1.0/drives/b!TTTR5HPR00qKMbhd4Ua5kqnQZpNBJrJAg4NkBP-KwGwT1AmTV73TRqdNngV2bl_H/root:/123/Passport_123.pdf:/content";
            //httpClient.SetBearerToken(accessToken);
            //response = httpClient.GetAsync(newFinalpath).Result;


            //await testAsync(newFinalpath, accessToken);

            //var pathfiledonwlaod = "https://graph.microsoft.com/v1.0//drives/{0}/items/01R56NZ4MSDHGJPUOYFFEJ2GGYB6FUNIVL/content";
            //pathfiledonwlaod = string.Format(pathfiledonwlaod, sharepointSite.id);

            ////Updating List fields
            //var ListFieldsUpdateEndPoint = _configuration["SharePointOnline:ListFieldsUpdateEndPoint"];
            //ListFieldsUpdateEndPoint = string.Format(ListFieldsUpdateEndPoint, sharepointSite.id, listid, "ItemId");

            //httpClient.SetBearerToken(accessToken);
            //var sharePointObject = new
            //{
            //    field1 = "value1",
            //    field2 = "value2"
            //};

            //string strSharePointObject = JsonConvert.SerializeObject(sharePointObject, Newtonsoft.Json.Formatting.Indented);
            //var httpContent = new StringContent(strSharePointObject);
            //httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            //var updateResponse = httpClient.PatchAsync(ListFieldsUpdateEndPoint, httpContent);

        }

        public static async Task testAsync(string newFinalpath, string accessToken)
        {
            //string url = "https://your-api-url-here"; // Replace with your API URL
            //string accessToken = accessToken; // Replace with your access token

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.GetAsync(newFinalpath);

                if (response.IsSuccessStatusCode)
                {
                    //string content = await response.Content.ReadAsStringAsync();


                    //var httpResult = await client.GetAsync(newFinalpath);
                    //using var resultStream = await httpResult.Content.ReadAsStreamAsync();
                    //using var fileStream = File.Create("E:\\Download");
                    //resultStream.CopyTo(fileStream);

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    using (FileStream fileStream = File.Create("C:\\Download"))
                    {
                        contentStream.CopyTo(fileStream);
                        Console.WriteLine($"File downloaded and saved to {"E:\\Download"}");
                    }
                  

                    //Console.WriteLine(content);
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
        }

    }


}
