using System;
using System.Collections.Generic;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using Google.Apis.Auth.OAuth2;

namespace HttpTester
{
    public class HttpResponseDetails
    {
        public string ResponseData { get; set; }
        public long DurationMS { get; set; }
        public HttpStatusCode Status { get; set; }
    }

    public class CampaignInstance
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }

    public class Campaigns
    {
        [JsonProperty(PropertyName = "total")]
        public int Total { get; set; }

        [JsonProperty(PropertyName = "data")]
        public List<CampaignInstance> AllCampaigns { get; set; }

    }

    public class BatchInstance
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
       
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "totalCodes")]
        public string TotalCodes { get; set; }
    }

    public class Batches
    {
        [JsonProperty(PropertyName = "total")]
        public int Total { get; set; }

        [JsonProperty(PropertyName = "data")]
        public List<BatchInstance> AllBatches { get; set; }

    }

    public class VoucherInstance
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "code")]
        public string Code { get; set; }
    }

    public class Vouchers
    {
        [JsonProperty(PropertyName = "total")]
        public int Total { get; set; }

        [JsonProperty(PropertyName = "data")]
        public List<BatchInstance> AllVouchers { get; set; }

    }

    class Program
    {
        static void Main()
        {
            
            if (true)
            {
                //just reading vouchers
                Task t = new Task(HttpVoucherReaderTesterAsync);
                t.Start();
            }

            if (false)
            {
                //reading & writing vouchers
                Task t1 = new Task(HttpVoucherReadWriterTesterAsync);
                t1.Start();
            }
            
            Console.WriteLine("doing requests...");
            Console.ReadLine();
        }

        static async void HttpVoucherReadWriterTesterAsync()
        {
            var token = await GetGoogleOAuthTokenAsync();
            // ... Target page.
            string rootlUrl = "https://2-dot-admin-dot-mq-vouchers-dev.appspot.com/api/";
            string url = string.Empty;


            // #1... create campiagn
            string postCampaignsUrl = "communities/mtv1/campaigns";
            url = rootlUrl + postCampaignsUrl;

            const string CampaignName = "Neil Test Campaign";


            const string postCampaignBody = @"{
                                              ""name"":""" + CampaignName + @""",
                                              ""product"": {
                                                ""id"": 1,
                                                ""name"": ""Full Access 90 days""
                                              },
                                              ""community"": {
                                                ""id"": ""mtv1"",
                                                ""name"": ""mtv1 commuity"" 
                                              },
                                              ""redemptionUrl"": ""http://traxm.tv/a""
                                            }";

            const string postBatchesBody = "{{\"name\": \"{0}\",\"generatedCodes\": {1}, \"startDate\": \"2015-08-01\", \"endDate\": \"2019-12-31\",\"creator\": {{ \"userName\": \"neilm\" }} }}";
            
            var postCampaignResponse = await DoPostRequestAsync(url, postCampaignBody, token);
            Console.WriteLine("Create Campaign - status={0}, duration mS={1}, name = {2}", postCampaignResponse.Status, postCampaignResponse.DurationMS, CampaignName);

            //get campaign id
            string getCampaignsGetUrl = "communities/mtv1/campaigns?page=0&size=1000&sorting=campaignName,ASC";
            url = rootlUrl + getCampaignsGetUrl;
            var res = await DoGetRequestAsync(url, token);
            //find one where name matches our campaign that we have just created
            var campaignsDeser = default(Campaigns);
            var campaignId = string.Empty;
            if (res.Status == HttpStatusCode.OK)
            {
                campaignsDeser = JsonConvert.DeserializeObject<Campaigns>(res.ResponseData);
                var match = campaignsDeser.AllCampaigns.Find(s => s.Name == CampaignName);
                if(match != null)
                {
                    campaignId = match.Id;
                }
            }

            
            
            //#2... create batches of vouchers
            if (campaignId != string.Empty)
            {
                //found match

                var noBatchesToCreate = 1;
                var noVouchersPerBatch = 50000;

               


                if (postCampaignResponse.Status == HttpStatusCode.Created || postCampaignResponse.Status == HttpStatusCode.Conflict)
                {
                    string postBatchesUrl = "communities/mtv1/campaigns/{0}/batches";
                    url = rootlUrl + string.Format(postBatchesUrl, campaignId);

                    //campaign exists, now create a batch
                    for (int i = 0; i < noBatchesToCreate; i++)
                    {
                        var bid = "batch" + DateTime.Now.ToUniversalTime().Ticks.ToString();
                        var body = string.Format(postBatchesBody, bid, noVouchersPerBatch);
                        var postBatchResponse = await DoPostRequestAsync(url, body, token);
                        Console.WriteLine("  Post batch - status={0}, duration mS={1}, # codes created = {2}", postBatchResponse.Status, postBatchResponse.DurationMS, noVouchersPerBatch);
                    }
                }
            }



            Console.WriteLine("end processing...");

        }

        static async void HttpVoucherReaderTesterAsync()
        {
            var token = await GetGoogleOAuthTokenAsync();
            // ... Target page.
            string rootlUrl = "https://1-dot-admin-dot-mq-vouchers-dev.appspot.com/api/";
            string url = string.Empty;

            // #1... get campiagns
            string getCampaignsUrl = "communities/mtv1/campaigns?page=0&size=1000&sorting=campaignName,ASC";
            url = rootlUrl + getCampaignsUrl;
            var res = await DoGetRequestAsync(url, token);

            var campaignsDeser = default(Campaigns);
            if(res.Status == HttpStatusCode.OK)
                campaignsDeser = JsonConvert.DeserializeObject<Campaigns>(res.ResponseData);
            var totalCampaigns = campaignsDeser != null ? campaignsDeser.Total : 0;
            Console.WriteLine("Get All Campaigns - status={0}, duration mS={1}, count = {2}", res.Status, res.DurationMS, totalCampaigns);

            

            // #2... get batches for each campaign
            string getBatchesUrl = "communities/mtv1/campaigns/{0}/batches?page=0&size=1000";

            foreach (var i in campaignsDeser.AllCampaigns)
            {
                url = rootlUrl + string.Format(getBatchesUrl, i.Id);

                Console.WriteLine("Campaign = {0}", i.Name);
                res = await DoGetRequestAsync(url, token);
                
                var batchesDeser = default(Batches);
                if (res.Status == HttpStatusCode.OK)
                    batchesDeser = JsonConvert.DeserializeObject<Batches>(res.ResponseData);
                var totalBatches = batchesDeser != null ? batchesDeser.Total : 0;
                Console.WriteLine("  Get All Batches - status={0}, duration mS={1}, count = {2}", res.Status, res.DurationMS, totalBatches);

                //#3... get vouchers under each batch - ONLY getting ones with voucher count > threshold

                int VoucherCountSize = 1001;

                if(batchesDeser != null)
                {
                    string getVouchersUrl = "communities/mtv1/campaigns/{0}/batches/{1}/vouchers?page=0&size=100000";
                    foreach(var batch in batchesDeser.AllBatches)
                    {
                        url = rootlUrl + string.Format(getVouchersUrl, i.Id, batch.Id);

                        if (int.Parse(batch.TotalCodes) > VoucherCountSize)
                        {
                            Console.WriteLine("    Batch = {0}, codes in batch = {1}", i.Name, batch.TotalCodes);
                            res = await DoGetRequestAsync(url, token);
                            var vouchersDeser = default(Vouchers);
                            if (res.Status == HttpStatusCode.OK)
                                vouchersDeser = JsonConvert.DeserializeObject<Vouchers>(res.ResponseData);
                            var totalVouchers = vouchersDeser != null ? vouchersDeser.Total : 0;
                            Console.WriteLine("      Get All Vouchers - status={0}, duration mS={1}, count = {2}", res.Status, res.DurationMS, totalVouchers);
                        }
                    }
                }
               
                
            }



            Console.WriteLine("end processing...");
           
        }

        static async Task<string> GetGoogleOAuthTokenAsync()
        {
            //setup ouath
            string serviceAccountEmail = "419775333754-ge99gv2escv9mqq2gq6jhamjr59nsupl@developer.gserviceaccount.com"; //refers to my MQ Vouchers DEV
            string O_AUTH_EMAIL_SCOPE = "https://www.googleapis.com/auth/userinfo.email";

            var certificate = new X509Certificate2(@"key.p12", "notasecret", X509KeyStorageFlags.Exportable);

            ServiceAccountCredential credential = new ServiceAccountCredential(
               new ServiceAccountCredential.Initializer(serviceAccountEmail)
               {
                   Scopes = new[] { O_AUTH_EMAIL_SCOPE }
               }.FromCertificate(certificate));



            //var done = await credential.RequestAccessTokenAsync(new System.Threading.CancellationToken());
            return await credential.GetAccessTokenForRequestAsync();


        }

        static async Task<HttpResponseDetails> DoGetRequestAsync(string url, string oauthToken)
        {
            using (HttpClient client = new HttpClient())
            {
                //Console.WriteLine("GET {0}", url);
                //add the token as HTTP Authorization header
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + oauthToken);

                var start = DateTime.Now;
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    var end = DateTime.Now;
                    using (HttpContent content = response.Content)
                    {
                        // ... Read the string.
                        string result = await content.ReadAsStringAsync();
                        return new HttpResponseDetails() { ResponseData = result, DurationMS = (long)((end - start).TotalMilliseconds), Status = response.StatusCode};
                    }
                }
            }
        }

        static void DisplayResults(string res)
        {
            if (res != null &&
            res.Length >= 50)
            {
                Console.WriteLine(res.Substring(0, 50) + "...");
            }
        }

        static async Task<HttpResponseDetails> DoPostRequestAsync(string url, string postBody, string oauthToken)
        {
            using (HttpClient client = new HttpClient())
            {
                var postContent = new StringContent(postBody, Encoding.UTF8, "application/json");

                //add the token as HTTP Authorization header
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + oauthToken);

                var start = DateTime.Now;
                using (HttpResponseMessage response = await client.PostAsync(url, postContent))
                {
                    var end = DateTime.Now;
                    using (HttpContent content = response.Content)
                    {
                        // ... Read the string.
                        string result = await content.ReadAsStringAsync();
                        return new HttpResponseDetails() { ResponseData = result, DurationMS = (long)((end - start).TotalMilliseconds), Status = response.StatusCode };
                    }
                }
            }
        }
    }

   

}
