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
using System.Net.Http.Headers;
using System.IO;

namespace HttpTester
{
    public class HttpResponseDetails
    {
        public string ResponseData { get; set; }
        public long DurationMS { get; set; }
        public HttpStatusCode Status { get; set; }
        public string LocationHeader { get; set; }
    }

    public class CampaignInstance
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "active")]
        public string IsActive { get; set; }
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

    public class ExportToken
    {
        [JsonProperty(PropertyName = "token")]
        public string Token { get; set; }
    }

    class Program
    {
        static void Main()
        {
            if(true)
            {
                //just reading vouchers
                Task t = new Task(CreateEECampaign);
                t.Start();
            }
            if (false)
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


        static async void CreateEECampaign()
        {
            var token = await GetGoogleOAuthTokenAsync();

            var __PROD__ = true;

            string rootlUrl ="";
            string campaignName = "";
            string productId = "";
            string productName = "";
            string redemptionUrl = "";
            string exportBatchName = "";
            string removeRemainingFreeTrial = "";
            int noVouchersPerBatch = 0;
            DateTime batchStartDate = DateTime.Now;
            DateTime batchEndDate = DateTime.Now;
            string batchPrefix = "";




            if (__PROD__)
            {
                rootlUrl = "https://2-dot-admin-dot-mq-vouchers.appspot.com/api/"; //PROD
                campaignName = "EE Mtv Trax Sim Play Bundle";
                productId = "416";
                productName = "32 days voucher duration";
                redemptionUrl = "http://traxm.tv/ee";
                //if want to just export an existing batch of vouchers then specify batch name here
                exportBatchName = "prod_batch2016-02-23T013054_3500";
                removeRemainingFreeTrial = "true";
                noVouchersPerBatch = 3500;
                batchStartDate = new DateTime(2016, 2, 28);
                batchEndDate = batchStartDate.AddMonths(4);
                batchPrefix = "prod_";
            }
            else
            {
                rootlUrl = "https://1-dot-admin-dot-mq-vouchers-qa.appspot.com/api/"; //QA
                campaignName = "Neil Test Campaign 2";
                productId = "166";
                productName = "30 days voucher duration";
                redemptionUrl = "http://traxm.tv/a";
                //if want to just export an existing batch of vouchers then specify batch name here
                exportBatchName = "batch2016-02-23T111033_20";
                removeRemainingFreeTrial = "true";
                noVouchersPerBatch = 20;
                batchStartDate = new DateTime(2016, 2, 23);
                batchEndDate = batchStartDate.AddDays(1);
                batchPrefix = "qa_";
            }
            

            string url = string.Empty;

            //#1 see if campaign is already created
            string getCampaignsUrl = "communities/mtv1/campaigns?page=0&size=1000&sorting=campaignName,ASC";
            url = rootlUrl + getCampaignsUrl;
            var res = await DoGetRequestAsync(url, token);

            var campaignsDeser = default(Campaigns);
            if(res.Status == HttpStatusCode.OK)
                campaignsDeser = JsonConvert.DeserializeObject<Campaigns>(res.ResponseData);
            var totalCampaigns = campaignsDeser != null ? campaignsDeser.Total : 0;
            Console.WriteLine("Get All Campaigns - status={0}, duration mS={1}, count = {2}", res.Status, res.DurationMS, totalCampaigns);
            var match = campaignsDeser.AllCampaigns.Find(s => s.Name == campaignName);

            var campaignId = "";
            var batchId = "";
            if(match!=null)
            {
                //campaign already exists
                Console.WriteLine("Campaign {0} already exists. Id = {1}", match.Name, match.Id);
                campaignId = match.Id;
            }
            else
            {
                //create a campaign
                // #1... create campiagn
                string postCampaignsUrl = "communities/mtv1/campaigns";
                url = rootlUrl + postCampaignsUrl;

                string postCampaignBody = @"{
                                                  ""name"":""" + campaignName + @""",
                                                  ""product"": {
                                                    ""id"":""" + productId + @""",
                                                    ""name"":""" + productName + @"""
                                                  },
                                                  ""community"": {
                                                    ""id"": ""mtv1"",
                                                    ""name"": ""mtv1 community"" 
                                                  },
                                                  ""redemptionUrl"":""" + redemptionUrl + @""",
                                                  ""removeRemainingFreeTrial"" :" + removeRemainingFreeTrial +
                                                "}";

           

                var postCampaignResponse = await DoPostRequestAsync(url, postCampaignBody, token);

                if (string.IsNullOrEmpty(postCampaignResponse.LocationHeader) == false)
                {
                    campaignId = postCampaignResponse.LocationHeader.Replace("communities/mtv1/campaigns/", "");
                }
                Console.WriteLine("Create Campaign - status={0}, duration mS={1}, name = {2}, Location={3}, campaignId={4}", postCampaignResponse.Status, postCampaignResponse.DurationMS, campaignName, postCampaignResponse.LocationHeader, campaignId);
            }
            
            //#2... create batch of vouchers
            if (campaignId != string.Empty && exportBatchName == "") //only create a new batch if the batch name is not already set
            {
                const string postBatchesBody = "{{\"name\": \"{0}\",\"generatedCodes\": {1}, \"startDate\": \"{2}\", \"endDate\": \"{3}\",\"creator\": {{ \"userName\": \"neil.mcalpine@musicqubed.com\" }} }}";

                string postBatchesUrl = "communities/mtv1/campaigns/{0}/batches";
                url = rootlUrl + string.Format(postBatchesUrl, campaignId);

                var start = batchStartDate.ToString("yyy-MM-dd");
                var end = batchEndDate.ToString("yyy-MM-dd");

                //campaign exists, now create a batch                
                var batchName = batchPrefix + "batch" + DateTime.UtcNow.ToString("yyyy-MM-ddThhmmss") + "_" + noVouchersPerBatch.ToString();
                var body = string.Format(postBatchesBody, batchName, noVouchersPerBatch, start, end);
                var postBatchResponse = await DoPostRequestAsync(url, body, token);
                
                var remove = string.Format("communities/mtv1/campaigns/{0}/batches/", campaignId);
                batchId = postBatchResponse.LocationHeader.Replace(remove, "");
                Console.WriteLine("  Post batch - status={0}, duration mS={1}, # codes created = {2}, batchId={3}, location={4}, batch name={5}", postBatchResponse.Status, postBatchResponse.DurationMS, noVouchersPerBatch, batchId, postBatchResponse.LocationHeader, batchName);
                batchId = string.Empty; //don't export batch immediately after creation as will fail because batch still being generated
            }
            else if (exportBatchName != "")
            {
                //#3...get the batch given a name

                string getBatchesUrl = "communities/mtv1/campaigns/{0}/batches?page=0&size=1000";


                url = rootlUrl + string.Format(getBatchesUrl, campaignId);

                res = await DoGetRequestAsync(url, token);

                var batchesDeser = default(Batches);
                if (res.Status == HttpStatusCode.OK)
                    batchesDeser = JsonConvert.DeserializeObject<Batches>(res.ResponseData);
                var totalBatches = batchesDeser != null ? batchesDeser.Total : 0;
                Console.WriteLine("  Got All Batches - status={0}, duration mS={1}, count = {2}", res.Status, res.DurationMS, totalBatches);

                var batchmatch = batchesDeser.AllBatches.Find(s => s.Name == exportBatchName);

                if (batchmatch != null)
                {
                    //batch found
                    Console.WriteLine("Found batch name = {0}. Id = {1}", batchmatch.Name, batchmatch.Id);
                    batchId = batchmatch.Id;
                }
            }

            //#3...export the batch
            if (string.IsNullOrEmpty(batchId) != true)
            {
                //get token
                var getExportTokenUrl = "communities/mtv1/campaigns/{0}/batches/{1}/vouchers/token";
                var exportTokenUrl = rootlUrl + string.Format(getExportTokenUrl, campaignId, batchId);
                
                res = await DoGetRequestAsync(exportTokenUrl, token);
                var exportTokenDeser = default(ExportToken);
                if (res.Status == HttpStatusCode.OK)
                {
                    exportTokenDeser = JsonConvert.DeserializeObject<ExportToken>(res.ResponseData);

                    //get voucher export list 
                    var getExportVouchersUrl = "communities/mtv1/campaigns/{0}/batches/{1}/vouchers?token={2}";
                    var exportVoucherUrl = rootlUrl + string.Format(getExportVouchersUrl, campaignId, batchId, exportTokenDeser.Token);

                    //cannot use HttpClient for the export request as it won't let me set the Content-Type in the Header
                    //have to use HttpWebRequest that seems to be OK with it.

                    HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(exportVoucherUrl);
                    //This time, our method is GET.
                    WebReq.Method = "GET";
                    WebReq.ContentType = "text/csv";

                    //From here on, it's all the same as above.
                    HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();
                    //Let's show some information about the response
                    Console.WriteLine(WebResp.StatusCode);

                    //Now, we read the response (the string), and output it.
                    Stream answer = WebResp.GetResponseStream();
                    StreamReader answerSR = new StreamReader(answer);
                    var resp = answerSR.ReadToEnd();
                    var csv = resp.Split('\n');

                    //save to file
                    var fpath = string.Format(@"C:\Users\Neil\Downloads\{0}.csv",exportBatchName); 
                    System.IO.File.WriteAllLines(fpath, csv);
                    Console.WriteLine("written CSV export file to " + fpath);


                }

            }



            

            

       }


        static async void HttpVoucherReadWriterTesterAsync()
        {
            var token = await GetGoogleOAuthTokenAsync();
            // ... Target page.
            //string rootlUrl = "https://2-dot-admin-dot-mq-vouchers-dev.appspot.com/api/"; //DEV
            string rootlUrl = "https://1-dot-admin-dot-mq-vouchers-qa.appspot.com/api/"; //QA
            
            string url = string.Empty;


            // #1... create campiagn
            string postCampaignsUrl = "communities/mtv1/campaigns";
            url = rootlUrl + postCampaignsUrl;

            const string CampaignName = "EE Sim Play Test Campaign";


            const string postCampaignBody = @"{
                                              ""name"":""" + CampaignName + @""",
                                              ""product"": {
                                                ""id"": 166,
                                                ""name"": ""30 days voucher duration""
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
                var noVouchersPerBatch = 10;

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
            //string rootlUrl = "https://1-dot-admin-dot-mq-vouchers-dev.appspot.com/api/"; //DEV
            string rootlUrl = "https://1-dot-admin-dot-mq-vouchers-qa.appspot.com/api/"; //QA
            //string rootlUrl = "https://1-dot-admin-dot-mq-vouchers.appspot.com/api/"; //PROD
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

                Console.WriteLine("Campaign = {0}, isActive = {1}", i.Name, i.IsActive);
                res = await DoGetRequestAsync(url, token);
                
                var batchesDeser = default(Batches);
                if (res.Status == HttpStatusCode.OK)
                    batchesDeser = JsonConvert.DeserializeObject<Batches>(res.ResponseData);
                var totalBatches = batchesDeser != null ? batchesDeser.Total : 0;
                Console.WriteLine("  Get All Batches - status={0}, duration mS={1}, count = {2}", res.Status, res.DurationMS, totalBatches);

                //#3... get vouchers under each batch - ONLY getting ones with voucher count > threshold

                int VoucherCountSize = 1;

                if(batchesDeser != null)
                {
                    string getVouchersUrl = "communities/mtv1/campaigns/{0}/batches/{1}/vouchers?page=0&size=100000";
                    foreach(var batch in batchesDeser.AllBatches)
                    {
                        url = rootlUrl + string.Format(getVouchersUrl, i.Id, batch.Id);

                        if (int.Parse(batch.TotalCodes) >= VoucherCountSize)
                        {
                            Console.WriteLine("    Batch = {0}, codes in batch = {1}", i.Name, batch.TotalCodes);
                            res = await DoGetRequestAsync(url, token);
                            var vouchersDeser = default(Vouchers);
                            if (res.Status == HttpStatusCode.OK)
                                vouchersDeser = JsonConvert.DeserializeObject<Vouchers>(res.ResponseData);
                            var totalVouchers = vouchersDeser != null ? vouchersDeser.Total : 0;
                            Console.WriteLine("      Get All Vouchers - status={0}, duration mS={1}, count = {2}", res.Status, res.DurationMS, totalVouchers);

                            //export voucher list

                            if (totalVouchers > 0 && totalVouchers < 100) //only get small export
                            {
                                //get token
                                var getExportTokenUrl = "communities/mtv1/campaigns/{0}/batches/{1}/vouchers/token";
                                var exportTokenUrl = rootlUrl + string.Format(getExportTokenUrl, i.Id, batch.Id);
                                res = await DoGetRequestAsync(exportTokenUrl, token);
                                var exportTokenDeser = default(ExportToken);
                                if (res.Status == HttpStatusCode.OK)
                                {
                                    exportTokenDeser = JsonConvert.DeserializeObject<ExportToken>(res.ResponseData);

                                    //get voucher export list 
                                    var getExportVouchersUrl = "communities/mtv1/campaigns/{0}/batches/{1}/vouchers?token={2}";
                                    var exportVoucherUrl = rootlUrl + string.Format(getExportVouchersUrl, i.Id, batch.Id, exportTokenDeser.Token);
                                    res = await DoGetRequestAsync(exportVoucherUrl, token, includeAcceptCsv:true);
                                }
                            }
                        }
                    }
                }
               
                
            }



            Console.WriteLine("end processing...");
           
        }

        static async Task<string> GetGoogleOAuthTokenAsync()
        {
            //setup ouath
            //string serviceAccountEmail = "419775333754-ge99gv2escv9mqq2gq6jhamjr59nsupl@developer.gserviceaccount.com"; //refers to my MQ Vouchers DEV
            //string serviceAccountEmail = "218371433014-pg0qcg9ut4ik2vepq6lp4vnveuf1oeus@developer.gserviceaccount.com"; //refers to my MQ Vouchers QA
            string serviceAccountEmail = "422570771885-3baroql6b5epifmrejhpauvvkqhrb2ki@developer.gserviceaccount.com"; //refers to my MQ Vouchers PROD
            
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

        static async Task<HttpResponseDetails> DoGetRequestAsync(string url, string oauthToken, bool includeAcceptCsv = false)
        {
            using (HttpClient client = new HttpClient())
            {
                //Console.WriteLine("GET {0}", url);
                //add the token as HTTP Authorization header
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + oauthToken);
                //client.DefaultRequestHeaders.Add("Accept", "application/json");
                //client.DefaultRequestHeaders.Add("Accept", "text/html");
                if(includeAcceptCsv)
                {
                    //client.DefaultRequestHeaders.Add("Accept", "text/html");
                    var d = client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "text/csv");
                }
                    

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
                        
                        //get location headerif it exists
                        HttpHeaders headers = response.Headers;
                        IEnumerable<string> values;
                        var location = "";
                        if (headers.TryGetValues("Location", out values))
                        {
                            foreach (var val in values)
	                        {
                                location = val;
	                        }
                        }
                        return new HttpResponseDetails() { ResponseData = result, DurationMS = (long)((end - start).TotalMilliseconds), Status = response.StatusCode, LocationHeader = location };
                    }
                }
            }
        }
    }

   

}
