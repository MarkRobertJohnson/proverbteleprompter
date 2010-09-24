using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace ProverbTeleprompter
{
    public static class WebHelpers
    {
        public static string GetUrlContent(Uri url)
        {
            // Set up the request to the server
            var request = WebRequest.Create(url);
            request.Method = "GET";

            string responseContent ="";
            try
            {
                // Read the response from the server
                using (var myResponse = request.GetResponse())
                {
                    using (var read = new StreamReader(myResponse.GetResponseStream()))
                    {
                        responseContent = read.ReadToEnd();
                    }


                }
            }
            catch (WebException ex)
            {
                MessageBox.Show("An internet connection is required to load random books of theBible","No internet connection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                
            }


            return responseContent;
        }
    }
}
