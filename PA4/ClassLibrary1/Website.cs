using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class Website : TableEntity
    {
        public Website(string name, string address, string date, string keyword, string imgLink, string bodyTxt)
        {
            //this.PartitionKey = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString();
            this.PartitionKey = keyword;
            this.Name = name;
            this.Date = date;
            this.Address = address;
            this.ImageLink = imgLink;
            this.BodyText = bodyTxt;
            this.RowKey = CreateMD5(address);
        }

        public Website(int count)
        {
            this.PartitionKey = "COUNT";
            this.Count = count;
            this.RowKey = "COUNT";
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public Website() { }
        public string Name { get; set; }
        public string Date { get; set; }
        public string Address { get; set; }
        public string Keyword { get; set; }
        public string ImageLink { get; set; }

        public string BodyText { get; set; }
        public int Count { get; set; }
    }
}


