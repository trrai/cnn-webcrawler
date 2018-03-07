using ClassLibrary1;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class Admin : System.Web.Services.WebService
    {
        //Trie to be used in the service
        private static Trie trie;
        private static Dictionary<string, List<Tuple<int, string, string, string, string, string>>> Cache = 
            new Dictionary<string, List<Tuple<int, string, string, string, string, string>>>();
        private string fullPath = "";
        public static string suggestionStats = "No stats available";

        //Performance counter to keep track of memory usage
        private PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");

        //Method to download the wiki inputs file from the Azure blob
        [WebMethod]
        public string BlobAccess()
        {

            CloudBlobContainer container = DBManager.getWikiStorage();

            //Make sure to add blob storage to this storage account, it does not have the wiki inputs yet!
            if (container.Exists())
            {

                foreach (IListBlobItem item in container.ListBlobs(null, false))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;

                        //override the path
                        fullPath = System.IO.Path.GetTempFileName();

                        //download the file
                        blob.DownloadToFile(fullPath, FileMode.Create);

                        return fullPath;

                    }
                }
            }
            else
            {
                return "Not found!";
            }

            return fullPath;
        }

        //Returns the statistics for the trie such as the last added and number added 
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetSuggestionStats()
        {
            return suggestionStats;
        }

        //Builds the trie that is used to return suggestions
        [WebMethod]
        public string BuildTrie()
        {
            //initate the trie
            trie = new Trie();

            //get the most updated version of the file
            string path = BlobAccess();

            //read the file
            using (StreamReader sr = new StreamReader(path))
            {

                var keepRunning = true;
                var currentInsertionNum = 0;
                var lastInserted = "";

                //while memory is above 15mb
                while (keepRunning)
                {

                    //check every 10k inserts
                    if (currentInsertionNum % 10000 == 0)
                    {
                        if (currentInsertionNum > 1000000)
                        {
                            keepRunning = false;
                        }
                        //RESTORE THIS FOR CLOUD
                        //keepRunning = memProcess.NextValue() >= 15;
                    }

                    //current line being read
                    string line = sr.ReadLine();

                    //if the line is not an empty string
                    if (line != "" && line.Length > 0 && line != null)
                    {
                        trie.InsertString(line);
                        lastInserted = line;
                        currentInsertionNum++;
                    }

                }
                //return the stats
                suggestionStats = "Last Inserted String: " + lastInserted +
                    " " + "| Inserted: " + currentInsertionNum +
                    " | Memory Remaining: " + memProcess.NextValue();

                return suggestionStats;
            }


        }

        //Method to search the trie with the user input by calling the Search function in Trie.cs
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> Search(string input)
        {
            try
            {
                var list = trie.Search(input);
                return list;
            }
            catch
            {
                return new List<string>();
            }


        }

        // Method to begin the crawling process, or resume if stopped
        [WebMethod]
        public string StartCrawl()
        {

            CloudQueueMessage msg = DBManager.getStatusQueue().PeekMessage();

            if (msg == null)
            {

                CloudQueueMessage cnnMsg = new CloudQueueMessage("http://cnn.com/robots.txt");
                DBManager.getUrlQueue().AddMessage(cnnMsg);

                CloudQueueMessage brMsg = new CloudQueueMessage("http://bleacherreport.com/robots.txt");
                DBManager.getUrlQueue().AddMessage(brMsg);

            }

            UpdateStatus("Started");
            return "Started";
        }

        // Method to stop the crawling process if it is underway
        [WebMethod]
        public string StopCrawl()
        {
            UpdateStatus("Stopped");
            return "Stopped";
        }

        // Returns the current status of the crawler
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetStatus()
        {
            CloudQueueMessage msg = DBManager.getStatusQueue().PeekMessage();
            if (msg != null)
            {
                return new JavaScriptSerializer().Serialize(msg.AsString);
            }
            else
            {
                return new JavaScriptSerializer().Serialize("Idle");
            }
        }

        // Updates the status by changing the message in the queue
        private void UpdateStatus(string newStatus)
        {
            CloudQueueMessage msg = DBManager.getStatusQueue().PeekMessage();

            if (msg != null)
            {
                //System.Diagnostics.Debug.WriteLine("Found message in ASMX: " + msg.AsString);
                DBManager.getStatusQueue().Clear();
            }


            //System.Diagnostics.Debug.WriteLine("Msg found to be null");
            System.Diagnostics.Debug.WriteLine(newStatus);
            CloudQueueMessage nStatus = new CloudQueueMessage(newStatus);
            DBManager.getStatusQueue().AddMessage(nStatus);
            System.Diagnostics.Debug.WriteLine("Updated");
        }

        // Clears the url queue's content
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string ClearAll()
        {
            StopCrawl();
            DBManager.getUrlQueue().Clear();
            DBManager.getDataQueue().Clear();
            DBManager.getStatusQueue().Clear();
            DBManager.getPerformanceTable().DeleteAsync();
            DBManager.getResultsTable().DeleteAsync();
            DBManager.getErrorsTable().DeleteAsync();
            return "Cleared";
        }

        // Returns the current count of the queue and table separated by a pipe symbol
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetCount()
        {
            CloudQueue q = DBManager.getDataQueue();
            q.FetchAttributes();
            var qCnt = q.ApproximateMessageCount;

            TableOperation retrieve = TableOperation.Retrieve<Website>("COUNT", "COUNT");

            TableResult retrievedResult = DBManager.getResultsTable().Execute(retrieve);

            int tableCount = 0;
            // Print the phone number of the result.
            if (retrievedResult.Result != null)
            {
                int currentCount = ((Website)retrievedResult.Result).Count;
                tableCount = (int)currentCount;
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine("Failed to retrieve");
            }

            return new JavaScriptSerializer().Serialize(qCnt.ToString() + "|" + tableCount.ToString());

        }

        // Returns a JSON formatted list of the last 10 links added to the results table
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> GetLast10Added()
        {

            TableQuery<Website> rangeQuery = new TableQuery<Website>()
                .Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, " ")
                    );

            List<string> returnList = new List<string>();

            var q = DBManager.getResultsTable().ExecuteQuery(rangeQuery);

            try
            {
                //System.Diagnostics.Debug.WriteLine("===== LIST =====");
                foreach (var item in q.Take(10))
                {
                    //System.Diagnostics.Debug.WriteLine(item.Address);
                    returnList.Add(item.Address);
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Results Table Error");
            }
            return returnList;

        }

        // Method to get the last 10 performance entries being recorded by the worker role
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> GetPerformance()
        {
            TableQuery<PerformanceStat> rangeQuery = new TableQuery<PerformanceStat>()
                .Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, " ")
                    );

            List<string> returnList = new List<string>();

            try
            {
                var q = DBManager.getPerformanceTable().ExecuteQuery(rangeQuery);


                //System.Diagnostics.Debug.WriteLine("===== PERFORMANCE LIST =====");
                foreach (var item in q.Take(10))
                {
                    //System.Diagnostics.Debug.WriteLine("CPU: " + item.CPU + " --- Memory: " + item.Memory);
                    returnList.Add("CPU: " + item.CPU.ToString() + " --- Memory: " + item.Memory.ToString());
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Table Error");
            }
            return returnList;
        }

        // Method to get the performance data that is specially formatted to create the chart
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> GetPerformanceChartData()
        {
            TableQuery<PerformanceStat> rangeQuery = new TableQuery<PerformanceStat>()
                .Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, " ")
                    );

            List<string> returnList = new List<string>();

            var q = DBManager.getPerformanceTable().ExecuteQuery(rangeQuery);


            //System.Diagnostics.Debug.WriteLine("===== PERFORMANCE LIST =====");
            foreach (var item in q.Take(10))
            {
                //System.Diagnostics.Debug.WriteLine("CPU: " + item.CPU + " --- Memory: " + item.Memory);
                returnList.Add(item.CPU.ToString() + "|" + item.Memory.ToString());
            }
            return returnList;
        }

        // Returns the last 10 errors recorded by the worker role when processing links
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> GetErrors()
        {
            TableQuery<Error> rangeQuery = new TableQuery<Error>()
                .Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, " ")
                    );

            List<string> returnList = new List<string>();

            var q = DBManager.getErrorsTable().ExecuteQuery(rangeQuery);

            try
            {
                //System.Diagnostics.Debug.WriteLine("===== ERROR LIST =====");
                foreach (var item in q.Take(10))
                {
                    //System.Diagnostics.Debug.WriteLine("CPU: " + item.CPU + " --- Memory: " + item.Memory);
                    returnList.Add(item.Link.ToString() + " | " + item.ErrorMsg.ToString());
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Errors Table Error");
            }

            return returnList;
        }

        //Method to return the relevant results in the table using user input
        [WebMethod]
        [ScriptMethod(UseHttpGet = true, ResponseFormat = ResponseFormat.Json)]
        public string SearchResults(string input)
        {
            try
            {
                input = input.ToLower();
                if (!Cache.ContainsKey(input))
                {

                    System.Diagnostics.Debug.WriteLine("Case 1, Not Using Cache");
                    Regex rgx = new Regex("[^a-zA-Z0-9 ]");
                    input = rgx.Replace(input, "");

                    //stop words we dont want being sent to the table
                    string[] stopwords = new string[] {"a", "about", "above", "above", "across",
                    "after", "afterwards", "again", "against", "all", "almost", "alone",
                    "along", "already", "also", "although", "always", "am", "among", "amongst",
                    "amoungst", "amount", "an", "and", "another", "any", "anyhow", "anyone", "anything",
                    "anyway", "anywhere", "are", "around", "as", "at", "back", "be", "became", "because", "become",
                    "becomes", "becoming", "been", "before", "beforehand", "behind", "being", "below", "beside", "besides",
                    "between", "beyond", "bill", "both", "bottom", "but", "by", "call", "cnn", "cannot", "cant", "co", "con",
                    "could", "couldnt", "cry", "de", "describe", "detail", "do", "done", "down", "due", "during", "each", "eg",
                    "eight", "either", "eleven", "else", "elsewhere", "empty", "enough", "etc", "even", "ever", "every", "everyone",
                    "everything", "everywhere", "except", "few", "fifteen", "fify", "fill", "find", "fire", "first", "five", "for",
                    "former", "formerly", "forty", "found", "four", "from", "front", "full", "further", "get", "give", "go", "had",
                    "has", "hasnt", "have", "he", "hence", "her", "here", "hereafter", "hereby", "herein", "hereupon", "hers", "herself",
                    "him", "himself", "his", "how", "however", "hundred", "ie", "if", "in", "inc", "indeed", "interest", "into", "is",
                    "it", "its", "itself", "keep", "last", "latter", "latterly", "least", "less", "ltd", "made", "many", "may", "me",
                    "meanwhile", "might", "mill", "mine", "more", "moreover", "most", "mostly", "move", "much", "must", "my", "myself",
                    "name", "namely", "neither", "never", "nevertheless", "next", "nine", "no", "nobody", "none", "noone", "nor", "not",
                    "nothing", "now", "nowhere", "of", "off", "often", "on", "once", "one", "only", "onto", "or", "other", "others",
                    "otherwise", "our", "ours", "ourselves", "out", "over", "own", "part", "per", "perhaps", "please", "put", "rather",
                    "re", "same", "see", "seem", "seemed", "seeming", "seems", "serious", "several", "she", "should", "show", "side",
                    "since", "sincere", "six", "sixty", "so", "some", "somehow", "someone", "something", "sometime", "sometimes",
                    "somewhere", "still", "such", "system", "take", "ten", "than", "that", "the", "their", "them", "themselves", "then",
                    "thence", "there", "thereafter", "thereby", "therefore", "therein", "thereupon", "these", "they", "thickv", "thin",
                    "third", "this", "those", "though", "three", "through", "throughout", "thru", "thus", "to", "together", "too", "top",
                    "toward", "towards", "twelve", "twenty", "two", "un", "under", "until", "up", "upon", "us", "very", "via", "was", "we",
                    "well", "were", "what", "whatever", "when", "whence", "whenever", "where", "whereafter", "whereas", "whereby", "wherein",
                    "whereupon", "wherever", "whether", "which", "while", "whither", "who", "whoever", "whole", "whom", "whose", "why", "will",
                    "with", "wont", "within", "without", "would", "yet", "you", "your", "yours", "yourself", "yourselves", "the" };

                    //keywords from input
                    string[] keywords = input.Split(null);

                    //filtered keywords
                    var filteredKeywords = keywords.Except(stopwords);

                    List<string> returnList = new List<string>();
                    List<Website> webList = new List<Website>();

                    Dictionary<string, int> occurences = new Dictionary<string, int>();

                    foreach (var word in filteredKeywords)
                    {
                        TableQuery<Website> rangeQuery = new TableQuery<Website>()
                            .Where(
                                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, word)
                                );
                        var q = DBManager.getResultsTable().ExecuteQuery(rangeQuery);

                        foreach (var item in q)
                        {
                            /*
                            //System.Diagnostics.Debug.WriteLine(item.Address);
                            string key = item.Name + " | " + item.Address + " | " + item.ImageLink + " | " + item.BodyText;

                            try
                            {
                                //System.Diagnostics.Debug.WriteLine("New Link Found: " + key);
                                occurences.Add(key, 1);
                            }
                            catch
                            {
                                int oldCount = occurences[key] + 1;
                                //System.Diagnostics.Debug.WriteLine("Old Link Found: " + key + " ... count: " + oldCount);
                                occurences[key] = oldCount + 1;

                            }
                            */
                            //returnList.Add(key);
                            webList.Add(item);
                        }


                    }

                    //var sorted = occurences.OrderByDescending(pair => pair.Value)
                    //    .Select(pair => pair.Key);
                    //var sortedList = sorted.ToList();

                    //Order first by relevance, then order by the date it was published
                    List<Tuple<int, string, string, string, string, string>> sortedResults = webList
                        .GroupBy(x => x.Address)
                        .Select(x => new Tuple<int, string, string, string, string, string>(x.ToList().Count(), x.First().Name,
                        x.First().BodyText, x.First().Address,  x.First().Date, x.First().ImageLink))
                        .OrderByDescending(x => x.Item1)
                        .ThenByDescending(x => x.Item5)
                        .Take(25).ToList();

                    //Empty cache if it's too big 
                    if (Cache.Count > 99)
                    {
                        Cache.Clear();
                    }

                    Cache[input] = sortedResults;

                    return JsonConvert.SerializeObject(sortedResults);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Case 2, Using Cache!");
                    return JsonConvert.SerializeObject(Cache[input]); 
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Server Side Search Error: " + e.Message);
                return "No Results";
            }

        }

        //Takes in a link element and returns the title associated to that article
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string SearchLink(string input)
        {
            TableQuery<Website> rangeQuery = new TableQuery<Website>()
                            .Where(
                                TableQuery.GenerateFilterCondition("Address", QueryComparisons.Equal, input)
                                );
            var q = DBManager.getResultsTable().ExecuteQuery(rangeQuery);

            foreach (var item in q)
            {
                return item.Name;
            }

            return "Not Found";
        }

    }
}
